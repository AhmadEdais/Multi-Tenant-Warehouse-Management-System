namespace WMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AuthSessionPolicy>()
            .Bind(configuration.GetSection("AuthSessionPolicy"))
            .Validate(policy => policy.IsValid(), "Invalid authentication session expiration policy.")
            .ValidateOnStart();
        
        services.AddScoped<TenantStampingInterceptor>();

        services.AddDbContext<WmsDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<TenantStampingInterceptor>();
            options.UseSqlServer(configuration.GetConnectionString("WmsConnection"))
                   .AddInterceptors(interceptor);
            options.EnableSensitiveDataLogging(); // remove later
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
