// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Đọc xác thực từ cookie và truy vấn dữ liệu bạn đọc đang login.

using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;
    private readonly AppDbContext _db;

    // Scoped service: cache trong phạm vi một request.
    private bool _maBanDocLoaded;
    private int? _maBanDoc;

    private bool _hasActiveBanDocLoaded;
    private bool _hasActiveBanDoc;

    public CurrentUserService(IHttpContextAccessor http, AppDbContext db)
    {
        _http = http;
        _db = db;
    }

    public CurrentUser? User
    {
        get
        {
            var principal = _http.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
                return null;

            var idText = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var roleText = principal.FindFirstValue(ClaimTypes.Role);
            if (!int.TryParse(idText, out var id) ||
                !Enum.TryParse<VaiTro>(roleText, out var role) || !Enum.IsDefined(role))
                return null;

            return new CurrentUser(
                id,
                principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
                role,
                principal.FindFirstValue("lms:username") ?? string.Empty,
                principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty);
        }
    }

    public bool IsAuthenticated => User is not null;

    public bool IsInRole(params VaiTro[] roles)
    {
        var user = User;
        return user is not null && roles.Contains(user.VaiTro);
    }

    public async Task<int?> GetMaBanDocAsync(CancellationToken ct = default)
    {
        if (_maBanDocLoaded) return _maBanDoc;

        var user = User;
        if (user is null) return null;

        _maBanDoc = await _db.Set<BanDoc>()
            .AsNoTracking()
            .Where(b => b.MaTaiKhoan == user.MaTaiKhoan)
            .Select(b => (int?)b.MaBanDoc)
            .FirstOrDefaultAsync(ct);

        _maBanDocLoaded = true;
        return _maBanDoc;
    }

    public async Task<bool> HasActiveBanDocAsync(CancellationToken ct = default)
    {
        if (_hasActiveBanDocLoaded) return _hasActiveBanDoc;

        var user = User;
        if (user is null || user.VaiTro != VaiTro.BanDoc)
        {
            _hasActiveBanDoc = false;
        }
        else
        {
            _hasActiveBanDoc = await _db.Set<BanDoc>()
                .AsNoTracking()
                .AnyAsync(b => b.MaTaiKhoan == user.MaTaiKhoan && b.TrangThai == TrangThaiBanDoc.HoatDong, ct);
        }

        _hasActiveBanDocLoaded = true;
        return _hasActiveBanDoc;
    }
}
