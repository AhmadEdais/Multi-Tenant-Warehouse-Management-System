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
        // TODO(LOGGING): Restore exception error logging after focused demo performance diagnostics.
        // Do not leave unexpected failures unlogged in production.
        // _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

        ProblemDetails problemDetails;

        switch (exception)
        {
            case ForbiddenException forbidden:
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden, Title = "Forbidden", Detail = forbidden.Message
                };
                break;
            case ServiceUnavailableException unavailable:
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable, Title = "Service Unavailable", Detail = unavailable.Message
                };
                break;
            case DbUpdateConcurrencyException:
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Resource Conflict",
                    Detail = "The record was modified by another operation. Refresh and try again."
                };
                break;

            case DbUpdateException databaseConflict
                when databaseConflict.InnerException is SqlException conflictSqlException
                    && conflictSqlException.Number is 1205 or 1222:
            case SqlException directConflict when directConflict.Number is 1205 or 1222:
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Resource Conflict",
                    Detail = "The record was modified by another operation. Refresh and try again."
                };
                break;

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

            case DbUpdateException stockUpdateException
                when stockUpdateException.InnerException is SqlException stockSqlException
                    && stockSqlException.Number is 2601 or 2627
                    && stockSqlException.Message.Contains("UQ_StockLevels_Tenant_Product_Location", StringComparison.Ordinal):
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Resource Conflict",
                    Detail = "Stock at this location was created by another operation. Refresh and try again."
                };
                break;

            case DbUpdateException purchaseOrderUpdateException
                when purchaseOrderUpdateException.InnerException is SqlException purchaseOrderSqlException
                    && purchaseOrderSqlException.Number is 2601 or 2627
                    && (purchaseOrderSqlException.Message.Contains("UQ_PurchaseOrders_Tenant_OrderNumber", StringComparison.Ordinal)
                        || purchaseOrderSqlException.Message.Contains("PK_PurchaseOrderSequences", StringComparison.Ordinal)):
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Resource Conflict",
                    Detail = "A Purchase Order number could not be reserved. Please try again."
                };
                break;

            case DbUpdateException salesOrderUpdateException
                when salesOrderUpdateException.InnerException is SqlException salesOrderSqlException
                    && salesOrderSqlException.Number is 2601 or 2627
                    && (salesOrderSqlException.Message.Contains("UQ_SalesOrders_Tenant_OrderNumber", StringComparison.Ordinal)
                        || salesOrderSqlException.Message.Contains("PK_SalesOrderSequences", StringComparison.Ordinal)):
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Resource Conflict",
                    Detail = "A Sales Order number could not be reserved. Please try again."
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
