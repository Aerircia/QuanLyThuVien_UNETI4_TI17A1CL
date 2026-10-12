using Microsoft.AspNetCore.Identity;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Email;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register every application service here so Program.cs
    /// stays untouched when a module adds its own services.
    /// </summary>
    public static IServiceCollection AddLibraryServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        
        // MVC + Database
        services.AddControllersWithViews();

        // Session + đăng nhập
        services.AddDistributedMemoryCache();
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.Name = ".LMS.Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        services.AddHttpContextAccessor();

        // Common services
        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher<TaiKhoan>, PasswordHasher<TaiKhoan>>();

        // Cấu hình Email / OTP
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<OtpOptions>()
            .Bind(configuration.GetSection(OtpOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Có Email:Host -> gửi SMTP thật (MailKit).
        // Chưa có: chỉ Development được ghi OTP vào log.
        if (!string.IsNullOrWhiteSpace(configuration["Email:Host"]))
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else if (environment.IsDevelopment())
        {
            services.AddScoped<IEmailSender, LogEmailSender>();
        }
        else
        {
            throw new InvalidOperationException(
                "Email:Host chưa được cấu hình. Đặt Email__Host, Email__Username, " +
                "Email__Password, Email__FromAddress (biến môi trường hoặc user-secrets).");
        }
        
        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITaiKhoanService, TaiKhoanService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IGiaHanMuonService, GiaHanMuonService>();
        services.AddScoped<ITraSachService, TraSachService>();
        services.AddScoped<ITienPhatTreHanService, TienPhatTreHanService>();
        services.AddScoped<ITienPhatTinhTrangSachService, TienPhatTinhTrangSachService>();
        return services;
    }
}