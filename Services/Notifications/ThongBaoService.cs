// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: Service thông báo
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Notifications;

public sealed class ThongBaoService : IThongBaoService
{
    private readonly AppDbContext _db;
    private readonly IEmailSender _email;
    private readonly ILogger<ThongBaoService> _logger;

    public ThongBaoService(AppDbContext db, IEmailSender email, ILogger<ThongBaoService> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    public async Task<ThongBaoBellResult> GetBellAsync(int maTaiKhoan, int take = 8, CancellationToken ct = default)
    {
        var unread = await GetUnreadCountAsync(maTaiKhoan, ct);
        var items = await GetListAsync(maTaiKhoan, take, ct);
        return new ThongBaoBellResult(unread, items);
    }

    public async Task<int> GetUnreadCountAsync(int maTaiKhoan, CancellationToken ct = default) =>
        await _db.ThongBaos.CountAsync(x => x.MaTaiKhoan == maTaiKhoan && x.DangHienThi && !x.DaDoc, ct);

    public async Task<IReadOnlyList<ThongBaoItem>> GetListAsync(int maTaiKhoan, int take = 20, CancellationToken ct = default) =>
        await _db.ThongBaos.AsNoTracking()
            .Where(x => x.MaTaiKhoan == maTaiKhoan && x.DangHienThi)
            .OrderByDescending(x => x.NgayTao).Take(Math.Clamp(take, 1, 100))
            .Select(x => new ThongBaoItem(x.MaThongBao, x.TieuDe, x.NoiDung, x.LoaiThongBao,
                x.MucDo, x.Icon, x.DuongDan, x.NgayTao, x.DaDoc)).ToListAsync(ct);

    public async Task<bool> HideAsync(int maThongBao, int maTaiKhoan, CancellationToken ct = default)
    {
        var item = await _db.ThongBaos.FirstOrDefaultAsync(x => x.MaThongBao == maThongBao
            && x.MaTaiKhoan == maTaiKhoan && x.DangHienThi, ct);
        if (item is null) return false;
        item.DangHienThi = false;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<CaiDatThongBao> GetSettingsAsync(int maTaiKhoan, CancellationToken ct = default)
    {
        var settings = await _db.CaiDatThongBaos.FirstOrDefaultAsync(x => x.MaTaiKhoan == maTaiKhoan, ct);
        if (settings is not null) return settings;
        settings = new CaiDatThongBao { MaTaiKhoan = maTaiKhoan, NgayCapNhat = DateTime.UtcNow };
        _db.CaiDatThongBaos.Add(settings);
        await _db.SaveChangesAsync(ct);
        return settings;
    }

    public async Task UpdateSettingsAsync(int maTaiKhoan, Action<CaiDatThongBao> update, CancellationToken ct = default)
    {
        var settings = await GetSettingsAsync(maTaiKhoan, ct);
        update(settings);
        settings.NgayCapNhat = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> MarkReadAsync(int maThongBao, int maTaiKhoan, CancellationToken ct = default)
    {
        var item = await _db.ThongBaos.FirstOrDefaultAsync(x => x.MaThongBao == maThongBao
            && x.MaTaiKhoan == maTaiKhoan && x.DangHienThi, ct);
        if (item is null) return false;
        if (!item.DaDoc)
        {
            item.DaDoc = true;
            item.NgayDoc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        return true;
    }

    public async Task<int> MarkAllReadAsync(int maTaiKhoan, CancellationToken ct = default)
    {
        var items = await _db.ThongBaos.Where(x => x.MaTaiKhoan == maTaiKhoan && x.DangHienThi && !x.DaDoc).ToListAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var item in items) { item.DaDoc = true; item.NgayDoc = now; }
        await _db.SaveChangesAsync(ct);
        return items.Count;
    }

    public async Task UpdateAsync(int maThongBao, int maTaiKhoan, string tieuDe, string noiDung,
        string? duongDan = null, CancellationToken ct = default)
    {
        var item = await _db.ThongBaos.FirstOrDefaultAsync(x => x.MaThongBao == maThongBao && x.MaTaiKhoan == maTaiKhoan, ct);
        if (item is null) return;
        item.TieuDe = tieuDe; item.NoiDung = noiDung; item.DuongDan = duongDan;
        await _db.SaveChangesAsync(ct);
    }

    private static bool IsCategoryEnabled(CaiDatThongBao settings, string loai) => loai switch
    {
        "loan" or "due" => settings.NhomMuonTra,
        "extension" => settings.NhomGiaHan,
        "fine" => settings.NhomTienPhat,
        _ => settings.NhomHeThong
    };

    public async Task CreateAsync(int maTaiKhoan, string tieuDe, string noiDung, string khoaSuKien,
        string loaiThongBao = "loan", string mucDo = "in", string? duongDan = null,
        string? loaiDoiTuong = null, int? maDoiTuong = null, CancellationToken ct = default)
    {
        var account = await _db.TaiKhoans.AsNoTracking()
            .Where(x => x.MaTaiKhoan == maTaiKhoan)
            .Select(x => new { x.Email, x.HoTen })
            .FirstOrDefaultAsync(ct);
        if (account is null) return;

        var settings = await GetSettingsAsync(maTaiKhoan, ct);
        var old = await _db.ThongBaos.FirstOrDefaultAsync(x => x.MaTaiKhoan == maTaiKhoan && x.KhoaSuKien == khoaSuKien, ct);
        var isNewNotification = old is null;
        if (settings.BatThongBaoWeb && IsCategoryEnabled(settings, loaiThongBao))
        {
            if (old is null)
            {
                _db.ThongBaos.Add(new ThongBao
                {
                    MaTaiKhoan = maTaiKhoan, TieuDe = tieuDe, NoiDung = noiDung, KhoaSuKien = khoaSuKien,
                    LoaiThongBao = loaiThongBao, MucDo = mucDo,
                    Icon = loaiThongBao == "fine" ? "wallet" : "bell", DuongDan = duongDan,
                    LoaiDoiTuong = loaiDoiTuong, MaDoiTuong = maDoiTuong,
                    NgayTao = DateTime.UtcNow, DaDoc = false, DangHienThi = true
                });
            }
            else
            {
                old.TieuDe = tieuDe; old.NoiDung = noiDung; old.DuongDan = duongDan;
                old.LoaiThongBao = loaiThongBao; old.MucDo = mucDo;
                old.LoaiDoiTuong = loaiDoiTuong; old.MaDoiTuong = maDoiTuong;
                old.DangHienThi = true;
            }
            await _db.SaveChangesAsync(ct);
        }

        var emailEnabled = settings.BatThongBaoEmail && IsCategoryEnabled(settings, loaiThongBao);
        if (isNewNotification && emailEnabled && !string.IsNullOrWhiteSpace(account.Email))
        {
            try
            {
                await _email.SendAsync(account.Email, $"[Thư viện UNETI] {tieuDe}",
                    $"Xin chào {account.HoTen},\n\n{noiDung}\n\nVui lòng đăng nhập website Thư viện UNETI để xem chi tiết.", ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Không gửi được email thông báo tới tài khoản {MaTaiKhoan}", maTaiKhoan);
            }
        }
    }

    public async Task NotifyReaderForLoanAsync(int maPhieuMuon, string tieuDe, string noiDung,
        string khoaSuKien, string? duongDan = null, string loaiThongBao = "loan", CancellationToken ct = default)
    {
        var accountId = await _db.PhieuMuons.AsNoTracking()
            .Where(x => x.MaPhieuMuon == maPhieuMuon)
            .Select(x => (int?)x.BanDoc!.MaTaiKhoan).FirstOrDefaultAsync(ct);
        if (accountId.HasValue)
            await CreateAsync(accountId.Value, tieuDe, noiDung, khoaSuKien,
                loaiThongBao, "in", duongDan ?? $"/PhieuMuon/Details/{maPhieuMuon}", "PhieuMuon", maPhieuMuon, ct);
    }

    public async Task NotifyReaderForExtensionAsync(int maGiaHan, string tieuDe, string noiDung,
        string khoaSuKien, string? duongDan = null, string loaiThongBao = "extension", CancellationToken ct = default)
    {
        var accountId = await _db.GiaHanMuons.AsNoTracking()
            .Where(x => x.MaGiaHan == maGiaHan)
            .Select(x => (int?)x.PhieuMuon!.BanDoc!.MaTaiKhoan).FirstOrDefaultAsync(ct);
        if (accountId.HasValue)
            await CreateAsync(accountId.Value, tieuDe, noiDung, khoaSuKien,
                loaiThongBao, "in", duongDan ?? "/PhieuMuon/MyLoans", "GiaHanMuon", maGiaHan, ct);
    }

    public async Task NotifyStaffAsync(string tieuDe, string noiDung, string khoaSuKien,
        string? duongDan = null, string loaiThongBao = "loan", CancellationToken ct = default)
    {
        var staffIds = await _db.TaiKhoans.AsNoTracking()
            .Where(x => x.TrangThai == TrangThaiTaiKhoan.HoatDong &&
                (x.VaiTro == VaiTro.Admin || x.VaiTro == VaiTro.ThuThu))
            .Select(x => x.MaTaiKhoan).ToListAsync(ct);
        foreach (var id in staffIds)
            await CreateAsync(id, tieuDe, noiDung, khoaSuKien, loaiThongBao, "in",
                duongDan ?? "/QuanLyPhieuMuon", "PhieuMuon", null, ct);
    }
}
