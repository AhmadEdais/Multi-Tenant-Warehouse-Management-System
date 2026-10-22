using WMS.Application.Features.Demo.Interfaces;
using WMS.Infrastructure.Demo;

namespace WMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AuthSessionPolicy>()
            .Bind(configuration.GetSection("AuthSessionPolicy"))
            .Validate(policy => policy.IsValid(), "Invalid authentication session expiration policy.")
            .ValidateOnStart();
        
        services.AddOptions<DemoOptions>()
            .Bind(configuration.GetSection("Demo"))
            .Validate(options => options.MaxActiveTenants is > 0 and <= 1000, "Demo capacity must be between 1 and 1000.")
            .ValidateOnStart();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        services.AddScoped<IAuthSessionIssuer, AuthSessionIssuer>();
        services.AddScoped<IDemoDataSeeder, DemoDataSeeder>();
        services.AddScoped<IDemoCapacityGuard, DemoCapacityGuard>();
        services.AddScoped<TenantStampingInterceptor>();

        services.AddDbContext<WmsDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<TenantStampingInterceptor>();
            options.UseSqlServer(configuration.GetConnectionString("WmsConnection"))
                   .AddInterceptors(interceptor);
        });

        services.AddHttpContextAccessor();
        services.AddScoped<IWmsDbContext>(provider => provider.GetRequiredService<WmsDbContext>());
        services.AddScoped<IPurchaseOrderNumberGenerator, PurchaseOrderNumberGenerator>();
        services.AddScoped<ISalesOrderNumberGenerator, SalesOrderNumberGenerator>();
        services.AddScoped<ITenantContext, HttpTenantContext>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<ICurrentUserService, HttpCurrentUserService>();

        return services;
    }
}
