namespace WMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("WmsConnection");
        
        services.AddScoped<TenantStampingInterceptor>();

        services.AddDbContext<WmsDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<TenantStampingInterceptor>();
            options.UseSqlServer(configuration.GetConnectionString("WmsConnection"))
                   .AddInterceptors(interceptor);
        });

        services.AddHttpContextAccessor();
        services.AddScoped<IWmsDbContext>(provider => provider.GetRequiredService<WmsDbContext>());
        services.AddScoped<ITenantContext, HttpTenantContext>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddScoped<ICurrentUserService, HttpCurrentUserService>();

        return services;
    }
}