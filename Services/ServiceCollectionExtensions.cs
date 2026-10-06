using Microsoft.AspNetCore.Identity;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register every application service here (one line per service) so Program.cs
    /// stays untouched when a module adds its own services.
    /// </summary>
    public static IServiceCollection AddLibraryServices(this IServiceCollection services)
    {
        // Auth
        services.AddScoped<IPasswordHasher<TaiKhoan>, PasswordHasher<TaiKhoan>>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}
