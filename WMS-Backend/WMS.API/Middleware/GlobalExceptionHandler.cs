using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Exceptions; 

namespace WMS.API.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

        ProblemDetails problemDetails;

        switch (exception)
        {
            case ValidationException validationException:
                var validationErrors = validationException.Errors
                    .Select(err => new { Field = err.PropertyName, Error = err.ErrorMessage })
                    .ToList();

                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation Failed",
                    Detail = "One or more validation errors occurred in your request."
                };
                problemDetails.Extensions.Add("errors", validationErrors);
                break;

            case ConflictException conflictException:
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Resource Conflict",
                    Detail = conflictException.Message 
                };
                break;

            case DbUpdateException dbUpdateException
                when dbUpdateException.InnerException is SqlException sqlException
                    && sqlException.Number is 2601 or 2627
                    && sqlException.Message.Contains("UQ_Products_TenantId_SKU", StringComparison.Ordinal):
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Resource Conflict",
                    Detail = "A product with the same SKU already exists."
                };
                break;

            case DbUpdateException supplierUpdateException
                when supplierUpdateException.InnerException is SqlException supplierSqlException
                    && supplierSqlException.Number is 2601 or 2627
                    && supplierSqlException.Message.Contains("UQ_Suppliers_Tenant_Code", StringComparison.Ordinal):
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Resource Conflict",
                    Detail = "A supplier with the same code already exists."
                };
                break;

            case DbUpdateException customerUpdateException
                when customerUpdateException.InnerException is SqlException customerSqlException
                    && customerSqlException.Number is 2601 or 2627
                    && customerSqlException.Message.Contains("UQ_Customers_Tenant_Code", StringComparison.Ordinal):
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Resource Conflict",
                    Detail = "A customer with the same code already exists."
                };
                break;

            case NotFoundException notFoundException:
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource Not Found",
                    Detail = notFoundException.Message
                };
                break;
            case UnauthorizedAccessException unauthorizedException :
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = unauthorizedException.Message
                };
                break;
            default:
                
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred processing your request."
                };
                break;
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
