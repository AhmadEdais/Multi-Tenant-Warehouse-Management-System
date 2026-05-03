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
        policy.RequireRole(Roles.TenantAdmin, Roles.WarehouseManager, Roles.WarehouseOperator, Roles.Analyst));


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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();