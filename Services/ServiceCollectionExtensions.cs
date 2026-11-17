// Họ và tên: Lại Anh Quân; Phùng Ngọc Bảo; Vũ Tiến Thành
// Mã sinh viên: 23103100255
// Nội dung thực hiện: xây dựng cấu hình dịch vụ chung

// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: bổ sung cấu hình antiforgery và dịch vụ email/thông báo

// Họ và tên: Vũ Tiến Thành
// Mã sinh viên: 23103100257
// Nội dung thực hiện: cập nhật giới hạn rate limit.

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.RateLimiting;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Email;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Payment;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Notifications;

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

        // Session + đăng nhập
        services.AddDistributedMemoryCache();
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.Name = ".LMS.Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });

        // Authentication uses a newly issued, protected cookie at sign-in. The
        // anonymous reset-flow state remains in Session and is not an auth token.
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = ".LMS.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = true;
                options.LoginPath = "/TaiKhoan/DangNhap";
                options.AccessDeniedPath = "/TaiKhoan/KhongDuQuyen";
            });

        // Bound password guessing and reset-email probing by source IP.
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
                await context.HttpContext.Response.WriteAsync(
                    "Quá nhiều yêu cầu. Vui lòng thử lại sau.", cancellationToken);
            };

            options.AddPolicy("login", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        // Hạn mức theo IP đủ rộng cho mạng dùng chung (NAT); chống dò mật khẩu
                        // vẫn cần khóa theo tài khoản ở AuthService (xem báo cáo, lỗi 1).
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(5),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

            options.AddPolicy("password-reset", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
        });

        services.AddHttpContextAccessor();
        // Cho phép AJAX gửi antiforgery token qua header RequestVerificationToken
        // Các form MVC truyền thống vẫn tiếp tục dùng hidden field __RequestVerificationToken
        services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

        // module email / otp / password reset
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
        //   - Development: bọc trong LogEmailSender để vừa gửi thật vừa ghi log (OTP) ra terminal.
        //   - Môi trường khác: chỉ SmtpEmailSender, không ghi OTP vào log.
        // Chưa có Email:Host: chỉ Development được ghi OTP vào log.
        var smtpConfigured = !string.IsNullOrWhiteSpace(configuration["Email:Host"]);

        if (smtpConfigured && environment.IsDevelopment())
        {
            services.AddScoped<SmtpEmailSender>();
            services.AddScoped<IEmailSender>(sp => new LogEmailSender(
                sp.GetRequiredService<ILogger<LogEmailSender>>(),
                sp.GetRequiredService<SmtpEmailSender>()));
        }
        else if (smtpConfigured)
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else if (environment.IsDevelopment())
        {
            services.AddScoped<IEmailSender>(sp => new LogEmailSender(
                sp.GetRequiredService<ILogger<LogEmailSender>>()));
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
        services.AddScoped<ISachService, SachService>();
        services.AddScoped<ITheLoaiService, TheLoaiService>();
        services.AddScoped<IAnhBiaService, AnhBiaService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IBanDocService, BanDocService>();
        services.AddScoped<IPhieuMuonService, PhieuMuonService>();
        services.AddScoped<ICauHinhThuVienService, CauHinhThuVienService>();
        services.AddOptions<PayOsSettings>().Bind(configuration.GetSection(PayOsSettings.SectionName)); // không ValidateOnStart
        services.AddSingleton<IPaymentGateway, PayOsGateway>();
        services.AddScoped<IThanhToanService, ThanhToanService>();

        services.AddScoped<IGiaHanMuonService, GiaHanMuonService>();
        services.AddScoped<ITraSachService, TraSachService>();
        services.AddScoped<ITienPhatTreHanService, TienPhatTreHanService>();
        services.AddScoped<ITienPhatTinhTrangSachService, TienPhatTinhTrangSachService>();

        services.AddScoped<IThongKeService, ThongKeService>();
        services.AddScoped<IHomeService, HomeService>();
        services.AddSingleton<IEmailQueue, EmailQueue>();
        services.AddHostedService<EmailNotificationBackgroundService>();
        services.AddScoped<IThongBaoService, ThongBaoService>();
        services.AddScoped<IQuanLyThongBaoService, QuanLyThongBaoService>();
        services.AddScoped<INhacNhoService, NhacNhoService>();
        services.AddHostedService<LoanOverdueBackgroundService>();

        return services;
    }
}
