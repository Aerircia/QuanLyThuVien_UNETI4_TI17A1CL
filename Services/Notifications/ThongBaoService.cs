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
    private readonly IEmailQueue _emailQueue;
    private readonly ILogger<ThongBaoService> _logger;

    public ThongBaoService(AppDbContext db, IEmailQueue emailQueue, ILogger<ThongBaoService> logger)
    {
        _db = db;
        _emailQueue = emailQueue;
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

    public async Task<ThongBaoPageResult> GetPageAsync(int maTaiKhoan, string filter = "all", int page = 1,
        int pageSize = 12, CancellationToken ct = default)
    {
        var baseQuery = _db.ThongBaos.AsNoTracking()
            .Where(x => x.MaTaiKhoan == maTaiKhoan && x.DangHienThi);

        var total = await baseQuery.CountAsync(ct);
        var unread = await baseQuery.CountAsync(x => !x.DaDoc, ct);

        var query = filter switch
        {
            "unread" => baseQuery.Where(x => !x.DaDoc),
            "loan" => baseQuery.Where(x => x.LoaiThongBao == "loan" || x.LoaiThongBao == "due"),
            "extension" => baseQuery.Where(x => x.LoaiThongBao == "extension"),
            "fine" => baseQuery.Where(x => x.LoaiThongBao == "fine"),
            "system" => baseQuery.Where(x => x.LoaiThongBao != "loan" && x.LoaiThongBao != "due"
                && x.LoaiThongBao != "extension" && x.LoaiThongBao != "fine"),
            _ => baseQuery
        };

        var filteredTotal = await query.CountAsync(ct);
        pageSize = Math.Clamp(pageSize, 5, 50);
        var pageCount = Math.Max(1, (int)Math.Ceiling(filteredTotal / (double)pageSize));
        page = Math.Clamp(page, 1, pageCount);

        var items = await query.OrderByDescending(x => x.NgayTao)
            .ThenByDescending(x => x.MaThongBao)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ThongBaoItem(x.MaThongBao, x.TieuDe, x.NoiDung, x.LoaiThongBao,
                x.MucDo, x.Icon, x.DuongDan, x.NgayTao, x.DaDoc))
            .ToListAsync(ct);

        return new ThongBaoPageResult(items, total, unread, filteredTotal, page, pageCount);
    }

    public async Task<ThongBaoItem?> GetByIdAsync(int maThongBao, int maTaiKhoan, CancellationToken ct = default) =>
        await _db.ThongBaos.AsNoTracking()
            .Where(x => x.MaThongBao == maThongBao && x.MaTaiKhoan == maTaiKhoan && x.DangHienThi)
            .Select(x => new ThongBaoItem(x.MaThongBao, x.TieuDe, x.NoiDung, x.LoaiThongBao,
                x.MucDo, x.Icon, x.DuongDan, x.NgayTao, x.DaDoc))
            .FirstOrDefaultAsync(ct);

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
        var now = DateTime.UtcNow;
        var affected = await _db.ThongBaos
            .Where(x => x.MaThongBao == maThongBao && x.MaTaiKhoan == maTaiKhoan
                     && x.DangHienThi && !x.DaDoc)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.DaDoc, true)
                .SetProperty(x => x.NgayDoc, now), ct);

        if (affected > 0) return true;

        // Trường hợp đã đọc từ trước vẫn trả true, không tìm thấy/không thuộc tài khoản thì false
        return await _db.ThongBaos.AsNoTracking().AnyAsync(x =>
            x.MaThongBao == maThongBao && x.MaTaiKhoan == maTaiKhoan && x.DangHienThi, ct);
    }

    public async Task<int> MarkAllReadAsync(int maTaiKhoan, CancellationToken ct = default)
    {
        // UPDATE trực tiếp trong SQL, không tải toàn bộ thông báo chưa đọc vào bộ nhớ ứng dụng
        var now = DateTime.UtcNow;
        return await _db.ThongBaos
            .Where(x => x.MaTaiKhoan == maTaiKhoan && x.DangHienThi && !x.DaDoc)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.DaDoc, true)
                .SetProperty(x => x.NgayDoc, now), ct);
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
            var html = EmailNotificationTemplate.Build(account.HoTen, tieuDe, noiDung, duongDan);
            var plainText = $"Xin chào {account.HoTen},\n\n{noiDung}\n\nVui lòng đăng nhập Thư viện UNETI để xem chi tiết.";
            await _emailQueue.EnqueueAsync(new EmailQueueItem(account.Email,
                $"[Thư viện UNETI] {tieuDe}", plainText, html), ct);
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

    public async Task NotifyReaderForFineAsync(int maTraSach, string tieuDe, string noiDung,
        string khoaSuKien, string? duongDan = null, CancellationToken ct = default)
    {
        var accountId = await _db.TraSachs.AsNoTracking()
            .Where(x => x.MaTra == maTraSach)
            .Select(x => (int?)x.PhieuMuon!.BanDoc!.MaTaiKhoan)
            .FirstOrDefaultAsync(ct);

        if (accountId.HasValue)
        {
            await CreateAsync(accountId.Value, tieuDe, noiDung, khoaSuKien,
                "fine", "in", duongDan ?? "/PhieuMuon/MyFines", "TraSach", maTraSach, ct);
        }
        else
        {
            _logger.LogWarning("Không tìm thấy tài khoản bạn đọc cho bản ghi trả sách {MaTraSach}.", maTraSach);
        }
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
