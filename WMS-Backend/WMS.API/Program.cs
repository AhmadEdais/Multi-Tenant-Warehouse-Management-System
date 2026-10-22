using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using WMS.API.Services;
using WMS.Infrastructure.Demo;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddSingleton<DemoProvisioningLimiter>();
builder.Services.AddScoped<DemoCleanupRunner>();
builder.Services.AddHostedService<DemoCleanupWorker>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<WMS.API.Middleware.GlobalExceptionHandler>();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));

    options.AddPolicy("demo", context => RateLimitPartition.GetTokenBucketLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 3,
            TokensPerPeriod = 1,
            ReplenishmentPeriod = TimeSpan.FromMinutes(5),
            QueueLimit = 0,
            AutoReplenishment = true
        }));

    options.OnRejected = async (context, _) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            var seconds = Math.Max(0, (int)Math.Ceiling(retryAfter.TotalSeconds));
            context.HttpContext.Response.Headers["Retry-After"] = seconds.ToString(CultureInfo.InvariantCulture);
        }

        await Results.Problem(
            title: "Too Many Requests",
            detail: context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName == "demo"
                ? "Too many demo requests. Please wait before trying again."
                : "Too many login attempts. Please wait before trying again.",
            statusCode: StatusCodes.Status429TooManyRequests)
            .ExecuteAsync(context.HttpContext);
    };
});


builder.Services.AddAuthorizationBuilder()
    .AddPolicy(SecurityPolicies.CanManageCategories, policy =>
    {
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager);
        policy.RequireAssertion(context => !context.User.IsInRole(Roles.SystemAdmin));
    })
    .AddPolicy(SecurityPolicies.CanViewCatalog, policy =>
    {
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager, Roles.WarehouseOperator, Roles.Analyst);
        policy.RequireAssertion(context => !context.User.IsInRole(Roles.SystemAdmin));
    })
    .AddPolicy(SecurityPolicies.CanManageProducts, policy =>
    {
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager);
        policy.RequireAssertion(context => !context.User.IsInRole(Roles.SystemAdmin));
    })
    .AddPolicy(SecurityPolicies.CanViewProducts, policy =>
    {
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager, Roles.WarehouseOperator, Roles.Analyst);
        policy.RequireAssertion(context => !context.User.IsInRole(Roles.SystemAdmin));
    })
    .AddPolicy(SecurityPolicies.CanManageLocations, policy =>
        policy.RequireRole(Roles.TenantAdmin))
    .AddPolicy(SecurityPolicies.CanViewLocationsTree, policy =>
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager, Roles.WarehouseOperator, Roles.Analyst))
    .AddPolicy(SecurityPolicies.CanDeactivateLocations, policy =>
        policy.RequireRole(Roles.TenantAdmin))
    .AddPolicy(SecurityPolicies.CanManageSuppliers, policy =>
    {
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager);
        policy.RequireAssertion(context => !context.User.IsInRole(Roles.SystemAdmin));
    })
    .AddPolicy(SecurityPolicies.CanViewSuppliers, policy =>
    {
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager, Roles.WarehouseOperator, Roles.Analyst);
        policy.RequireAssertion(context => !context.User.IsInRole(Roles.SystemAdmin));
    })
    .AddPolicy(SecurityPolicies.CanManageCustomers, policy =>
    {
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager);
        policy.RequireAssertion(context => !context.User.IsInRole(Roles.SystemAdmin));
    })
    .AddPolicy(SecurityPolicies.CanViewCustomers, policy =>
    {
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager, Roles.WarehouseOperator, Roles.Analyst);
        policy.RequireAssertion(context => !context.User.IsInRole(Roles.SystemAdmin));
    })
    .AddPolicy(SecurityPolicies.CanManageWarehouses, policy =>
    {
        policy.RequireRole(Roles.TenantAdmin);
        policy.RequireAssertion(context => !context.User.IsInRole(Roles.SystemAdmin));
    })
    .AddPolicy(SecurityPolicies.CanViewWarehouses, policy =>
    {
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager, Roles.WarehouseOperator);
        policy.RequireAssertion(context => !context.User.IsInRole(Roles.SystemAdmin));
    })
    .AddPolicy(SecurityPolicies.CanViewInventory, policy =>
    {
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager, Roles.WarehouseOperator, Roles.Analyst);
        policy.RequireAssertion(context => !context.User.IsInRole(Roles.SystemAdmin));
    })
    .AddPolicy(SecurityPolicies.CanViewInventorySummary, policy =>
    {
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager, Roles.Analyst);
        policy.RequireAssertion(context => !context.User.IsInRole(Roles.SystemAdmin));
    })
    .AddPolicy(SecurityPolicies.CanManageInbound, policy =>
    {
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager);
        policy.RequireAssertion(context => !context.User.IsInRole(Roles.SystemAdmin));
    })
    .AddPolicy(SecurityPolicies.CanReceiveInbound, policy =>
    {
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager, Roles.WarehouseOperator);
        policy.RequireAssertion(context => !context.User.IsInRole(Roles.SystemAdmin));
    })
    .AddPolicy(SecurityPolicies.CanPutaway, policy =>
    {
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager, Roles.WarehouseOperator);
        policy.RequireAssertion(context => !context.User.IsInRole(Roles.SystemAdmin));
    })
    .AddPolicy(SecurityPolicies.CanManageOutbound, policy =>
    {
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager);
        policy.RequireAssertion(context => !context.User.IsInRole(Roles.SystemAdmin));
    })
    .AddPolicy(SecurityPolicies.CanViewSalesOrders, policy =>
    {
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager, Roles.WarehouseOperator, Roles.Analyst);
        policy.RequireAssertion(context => !context.User.IsInRole(Roles.SystemAdmin));
    })
    .AddPolicy(SecurityPolicies.CanConfirmOutboundPickup, policy =>
    {
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager, Roles.WarehouseOperator);
        policy.RequireAssertion(context => !context.User.IsInRole(Roles.SystemAdmin));
    })
    .AddPolicy(SecurityPolicies.CanViewPurchaseOrders, policy =>
    {
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager, Roles.WarehouseOperator, Roles.Analyst);
        policy.RequireAssertion(context => !context.User.IsInRole(Roles.SystemAdmin));
    });


builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WMS Capstone API",
        Version = "v1",
        Description = "Enterprise Warehouse Management System API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter your JWT token. No need to add 'Bearer' prefix.",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",             
        BearerFormat = "JWT"           
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
  {
      {
          new OpenApiSecurityScheme
          {
              Reference = new OpenApiReference
              {
                  Type = ReferenceType.SecurityScheme,
                  Id = "Bearer"
              },
              Scheme = "bearer",   
              Name = "Bearer",     
              In = ParameterLocation.Header
          },
          Array.Empty<string>()
      }
  });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(builder.Configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>() ?? [])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings.GetValue<string>("Secret");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
        ValidAudience = jwtSettings.GetValue<string>("Audience"),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
        ClockSkew = TimeSpan.Zero 
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.Principal?.FindFirst("sub")?.Value;
            if (!int.TryParse(userClaim, out var userId))
            {
                context.Fail("Invalid user claim.");
                return;
            }

                var sessionClaim = context.Principal?.FindFirst("sid")?.Value
                ?? context.Principal?.FindFirst(ClaimTypes.Sid)?.Value;
            if (!int.TryParse(sessionClaim, out var sessionId) || sessionId <= 0)
            {
                context.Fail("Invalid session claim.");
                return;
            }

            var tenantClaim = context.Principal?.FindFirst("tenantId")?.Value;
            int? tenantId = null;
            if (tenantClaim is not null && int.TryParse(tenantClaim, out var parsedTenantId))
            {
                tenantId = parsedTenantId;
            }
            else if (tenantClaim is not null)
            {
                context.Fail("Invalid tenant claim.");
                return;
            }

            var dbContext = context.HttpContext.RequestServices.GetRequiredService<WMS.Application.Common.Interfaces.IWmsDbContext>();
            var nowUtc = DateTime.UtcNow;
            var isActiveSession = await dbContext.AuthSessions.AsNoTracking()
                .AnyAsync(session => session.Id == sessionId && session.UserId == userId &&
                    session.RevokedAtUtc == null && session.IdleExpiresAtUtc > nowUtc &&
                    session.AbsoluteExpiresAtUtc > nowUtc,
                    context.HttpContext.RequestAborted);
            if (!isActiveSession)
            {
                context.Fail("Authentication session is invalid.");
                return;
            }

            var isActiveUser = await dbContext.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Id == userId && u.TenantId == tenantId && u.IsActive,
                    context.HttpContext.RequestAborted);
            if (!isActiveUser)
            {
                context.Fail("User is inactive or no longer belongs to this tenant.");
                return;
            }

            if (!tenantId.HasValue)
            {
                return;
            }

            var isActive = await dbContext.Tenants
                .AnyAsync(t => t.Id == tenantId.Value && t.IsActive &&
                    (!t.IsDemo || t.DemoExpiresAtUtc > nowUtc), context.HttpContext.RequestAborted);
            if (!isActive)
            {
                context.Fail("Tenant is inactive.");
            }
        }
    };
});
builder.Logging.ClearProviders();

builder.Logging.AddSimpleConsole(options =>
{
    options.TimestampFormat = "HH:mm:ss ";
    options.IncludeScopes = false;

});
builder.Services.AddAuthorization();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
