using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<WMS.API.Middleware.GlobalExceptionHandler>();


builder.Services.AddAuthorizationBuilder()
    .AddPolicy(SecurityPolicies.CanManageCategories, policy =>
        policy.RequireRole(Roles.TenantAdmin))
    .AddPolicy(SecurityPolicies.CanViewCatalog, policy =>
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager, Roles.WarehouseOperator, Roles.Analyst))
    .AddPolicy(SecurityPolicies.CanManageLocations, policy =>
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager))
    .AddPolicy(SecurityPolicies.CanViewLocationsTree, policy =>
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager, Roles.WarehouseOperator, Roles.Analyst))
    .AddPolicy(SecurityPolicies.CanDeactivateLocations, policy =>
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager))
    .AddPolicy(SecurityPolicies.CanManageSuppliers, policy =>
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager))
    .AddPolicy(SecurityPolicies.CanViewSuppliers, policy =>
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager, Roles.WarehouseOperator, Roles.Analyst))
    .AddPolicy(SecurityPolicies.CanManageCustomers, policy =>
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager))
    .AddPolicy(SecurityPolicies.CanViewCustomers, policy =>
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager, Roles.WarehouseOperator, Roles.Analyst))
    .AddPolicy(SecurityPolicies.CanViewInventory, policy =>
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager, Roles.WarehouseOperator))
    .AddPolicy(SecurityPolicies.CanViewInventorySummary, policy =>
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager))
    .AddPolicy(SecurityPolicies.CanManageInbound, policy =>
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager));


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
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
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
                .AnyAsync(t => t.Id == tenantId.Value && t.IsActive, context.HttpContext.RequestAborted);
            if (!isActive)
            {
                context.Fail("Tenant is inactive.");
            }
        }
    };
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
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
