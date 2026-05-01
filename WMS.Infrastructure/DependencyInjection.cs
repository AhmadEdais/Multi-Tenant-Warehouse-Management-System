namespace WMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("WmsConnection");
        services.AddDbContext<WmsDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IWmsDbContext>(provider => provider.GetRequiredService<WmsDbContext>());
        services.AddScoped<ITenantContext, DevTenantContext>();
        services.AddScoped<ICurrentUserService, DevCurrentUserService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtProvider, JwtProvider>();
        return services;
    }
}