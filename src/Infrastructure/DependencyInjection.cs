using Microsoft.Extensions.DependencyInjection;
using Domain.Interfaces;
using Infrastructure.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Infrastructure.Services;
using Infrastructure.Settings;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? connectionString)
    {
        services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connectionString ?? "Server=localhost;Database=AuthSample;User Id=sa;Password=Your_password123;"));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(Domain.Interfaces.IRepository<Domain.Entities.User>), typeof(UserRepository));
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.Configure<JwtSettings>(s => { /* bound in WebApi */ });
        return services;
    }
}
