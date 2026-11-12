// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện:  nhận trả sách: kiểm tra phiếu Đang mượn, tính số ngày trễ và tiền phạt, chuyển phiếu sang Đã trả, đánh dấu đã nộp phạt.

using System.Data;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services;

public class TraSachService : ITraSachService
{
    private const string MsgPhieuNotFound = "Không tìm thấy phiếu mượn.";
    private const string MsgTraNotFound = "Không tìm thấy bản ghi trả sách.";
    private const string MsgSaveFailed = "Không thể lưu thay đổi. Vui lòng thử lại.";

    private readonly AppDbContext _db;
    private readonly TimeProvider _time;
    private readonly ITienPhatTreHanService _phatTre;
    private readonly ITienPhatTinhTrangSachService _phatTinhTrang;
    private readonly IThongBaoService _thongBao;
    private readonly ILogger<TraSachService> _logger;

    public TraSachService(
        AppDbContext db,
        TimeProvider time,
        ITienPhatTreHanService phatTre,
        ITienPhatTinhTrangSachService phatTinhTrang,
        IThongBaoService thongBao,
        ILogger<TraSachService> logger)
    {
        _db = db;
        _time = time;
        _phatTre = phatTre;
        _phatTinhTrang = phatTinhTrang;
        _thongBao = thongBao;
        _logger = logger;
    }

    private async Task NotifySafelyAsync(Func<Task> action, string eventName)
    {
        try { await action(); }
        catch (Exception ex) { _logger.LogError(ex, "Không thể tạo thông báo cho sự kiện {EventName}", eventName); }
    }

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

// Tiền phạt của một lần trả = phạt trả trễ  + phạt tình trạng sách 
    private (int SoNgayTre, decimal PhatTre, decimal PhatTinhTrang, decimal Tong) TinhPhat(
        CauHinhThuVien cfg, DateOnly? hanTra, DateOnly ngayTra, TinhTrangSach tinhTrang, decimal giaBia)
    {
        var tre = _phatTre.Tinh(cfg, hanTra, ngayTra);
        var phatSach = _phatTinhTrang.TinhTienPhat(cfg, tinhTrang, giaBia);
        return (tre.SoNgayTre, tre.TienPhat, phatSach, tre.TienPhat + phatSach);
    }

//Danh sách / chi tiết
    public async Task<PagedResult<TraSachListItem>> GetListAsync(TraSachFilter filter, CancellationToken ct = default)
    {
        var query = _db.TraSachs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            query = query.Where(t => t.PhieuMuon!.BanDoc!.HoTen.Contains(s) ||
                                     t.PhieuMuon!.Sach!.TenSach.Contains(s));
        }

        if (filter.TinhTrang is { } tinhTrang)
            query = query.Where(t => t.TinhTrangSach == tinhTrang);

        if (filter.TrangThaiPhat is { } trangThaiPhat)
            query = query.Where(t => t.TrangThaiPhat == trangThaiPhat);

        // Tab Lịch sử: bỏ các khoản phạt chưa nộp (đã có tab riêng)
        if (filter.LoaiTruChuaNop)
            query = query.Where(t => t.TrangThaiPhat != TrangThaiPhat.ChuaNop);

        var projected = query
            .OrderByDescending(t => t.NgayTra)
            .ThenByDescending(t => t.MaTra)
            .Select(t => new TraSachListItem(
                t.MaTra,
                t.MaPhieuMuon,
                t.PhieuMuon!.BanDoc!.HoTen,
                t.PhieuMuon!.Sach!.TenSach,
                t.NgayTra,
                t.TinhTrangSach,
                t.SoNgayTre,
                t.TienPhat,
                t.TrangThaiPhat));

        return await PagedResult<TraSachListItem>.CreateAsync(projected, filter.Page, filter.PageSize, ct);
    }

    public async Task<TraSachDetail?> GetAsync(int maTra, CancellationToken ct = default)
{
    var detail = await _db.TraSachs.AsNoTracking()
        .Where(t => t.MaTra == maTra)
                .Select(t => new TraSachDetail(
                    t.MaTra,
                    t.MaPhieuMuon,
                    t.PhieuMuon!.BanDoc!.HoTen,
                    t.PhieuMuon!.BanDoc!.MaSoThe,
                    t.PhieuMuon!.Sach!.TenSach,
                    t.PhieuMuon!.Sach!.GiaBia,
                    t.PhieuMuon!.NgayMuon,
                    t.PhieuMuon!.HanTra,
                    t.NgayTra,
                    t.TinhTrangSach,
                    t.SoNgayTre,
                    t.TienPhat,
                    t.TrangThaiPhat,
                    t.GhiChu,
                    t.NguoiNhan!.HoTen))
                .FirstOrDefaultAsync(ct);

    if (detail is null)
        return null;

    var cfg = await GetConfigAsync(ct);
    return detail with
    {
        PhatTre = _phatTre.TinhTienPhat(cfg, detail.SoNgayTre),
        PhatTinhTrang = _phatTinhTrang.TinhTienPhat(cfg, detail.TinhTrang, detail.GiaBia)
    };
}
// Danh sách phiếu đang mượn chờ nhận trả
    public async Task<PagedResult<PhieuChoTraItem>> GetPhieuChoTraAsync(PhieuChoTraFilter filter, CancellationToken ct = default)
    {
        var today = Today;

        // Chỉ phiếu Đang mượn và chưa có bản ghi trả (mỗi phiếu chỉ trả một lần)
        var query = _db.PhieuMuons.AsNoTracking()
            .Where(p => p.TrangThai == TrangThaiPhieuMuon.DangMuon && p.TraSach == null);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            query = query.Where(p => p.BanDoc!.HoTen.Contains(s) ||
                                     p.Sach!.TenSach.Contains(s) ||
                                     (p.BanDoc!.MaSoThe != null && p.BanDoc!.MaSoThe.Contains(s)));
        }

        if (filter.ChiQuaHan)
            query = query.Where(p => p.HanTra != null && p.HanTra < today);

        // Hạn trả sớm nhất lên đầu: phiếu quá hạn nằm trên cùng
        var projected = query
            .OrderBy(p => p.HanTra)
            .ThenBy(p => p.MaPhieuMuon)
            .Select(p => new PhieuChoTraItem(
                p.MaPhieuMuon,
                p.BanDoc!.HoTen,
                p.BanDoc!.MaSoThe,
                p.Sach!.TenSach,
                p.NgayMuon,
                p.HanTra));

        var page = await PagedResult<PhieuChoTraItem>.CreateAsync(projected, filter.Page, filter.PageSize, ct);

        // Số ngày trễ tính bằng đúng service tính trễ, giống lúc nhận trả
        foreach (var item in page.Items)
            item.SoNgayTre = _phatTre.TinhSoNgayTre(item.HanTra, today);

        return page;
    }

 //Xem trước khi nhận trả
    public async Task<TraSachPreview?> GetPreviewAsync(int maPhieuMuon, CancellationToken ct = default)
    {
        var row = await _db.PhieuMuons.AsNoTracking()
            .Where(p => p.MaPhieuMuon == maPhieuMuon)
            .Select(p => new
            {
                MaPhieuMuon = p.MaPhieuMuon,
                TenBanDoc = p.BanDoc!.HoTen,
                MaSoThe = p.BanDoc!.MaSoThe,
                TenSach = p.Sach!.TenSach,
                GiaBia = p.Sach!.GiaBia,
                NgayMuon = p.NgayMuon,
                HanTra = p.HanTra,
                TrangThai = p.TrangThai,
                DaCoBanGhiTra = p.TraSach != null
            })
            .FirstOrDefaultAsync(ct);

        if (row is null)
            return null;

        var cfg = await GetConfigAsync(ct);
        var ngayTra = Today;
        var soNgayTre = _phatTre.TinhSoNgayTre(row.HanTra, ngayTra);

        var canhBao = new List<string>();
        if (row.TrangThai != TrangThaiPhieuMuon.DangMuon)
            canhBao.Add("Chỉ phiếu đang mượn mới được ghi nhận trả sách.");
        if (row.DaCoBanGhiTra)
            canhBao.Add("Phiếu này đã có bản ghi trả sách.");

                return new TraSachPreview(
            row.MaPhieuMuon, row.TenBanDoc, row.MaSoThe, row.TenSach, row.GiaBia,
            row.NgayMuon, row.HanTra, ngayTra, soNgayTre,
            _phatTre.TinhTienPhat(cfg, soNgayTre),
            TinhPhat(cfg, row.HanTra, ngayTra, TinhTrangSach.BinhThuong, row.GiaBia).Tong,
            TinhPhat(cfg, row.HanTra, ngayTra, TinhTrangSach.HuHong, row.GiaBia).Tong,
            TinhPhat(cfg, row.HanTra, ngayTra, TinhTrangSach.Mat, row.GiaBia).Tong,
            canhBao)
        {
            PhatHuHong = _phatTinhTrang.TinhTienPhat(cfg, TinhTrangSach.HuHong, row.GiaBia),
            PhatMat = _phatTinhTrang.TinhTienPhat(cfg, TinhTrangSach.Mat, row.GiaBia)
        };
    }
 // Nhận trả sách
    public async Task<ServiceResult<int>> CreateAsync(TraSachInput input, int actingMaTaiKhoan, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(input.TinhTrang))
            return ServiceResult<int>.Error(nameof(input.TinhTrang), "Tình trạng sách không hợp lệ.");

        var ghiChu = string.IsNullOrWhiteSpace(input.GhiChu) ? null : input.GhiChu.Trim();
        if (ghiChu is { Length: > 100 })
            return ServiceResult<int>.Error(nameof(input.GhiChu), "Ghi chú tối đa 100 ký tự.");
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var phieu = await _db.PhieuMuons
            .Include(p => p.Sach)
            .FirstOrDefaultAsync(p => p.MaPhieuMuon == input.MaPhieuMuon, ct);

        if (phieu is null || phieu.Sach is null)
            return ServiceResult<int>.Error(string.Empty, MsgPhieuNotFound);

        if (phieu.TrangThai != TrangThaiPhieuMuon.DangMuon)
            return ServiceResult<int>.Error(string.Empty, "Chỉ phiếu đang mượn mới được ghi nhận trả sách.");

        if (await _db.TraSachs.AnyAsync(t => t.MaPhieuMuon == phieu.MaPhieuMuon, ct))
            return ServiceResult<int>.Error(string.Empty, "Phiếu này đã có bản ghi trả sách.");

        var cfg = await GetConfigAsync(ct);
        var ngayTra = Today;
        var phat = TinhPhat(cfg, phieu.HanTra, ngayTra, input.TinhTrang, phieu.Sach.GiaBia);
        var soNgayTre = phat.SoNgayTre;
        var tienPhat = phat.Tong;

        var traSach = new TraSach
        {
            MaPhieuMuon = phieu.MaPhieuMuon,
            NgayTra = ngayTra,
            TinhTrangSach = input.TinhTrang,
            SoNgayTre = soNgayTre,
            TienPhat = tienPhat,
            TrangThaiPhat = tienPhat > 0 ? TrangThaiPhat.ChuaNop : TrangThaiPhat.KhongPhat,
            GhiChu = ghiChu,
            MaNguoiNhan = actingMaTaiKhoan
        };
        _db.TraSachs.Add(traSach);

// nhật ký đổi trạng thái.
        phieu.TrangThai = TrangThaiPhieuMuon.DaTra;
        _db.PhieuMuonLichSus.Add(new PhieuMuonLichSu
        {
            MaPhieuMuon = phieu.MaPhieuMuon,
            TrangThaiCu = TrangThaiPhieuMuon.DangMuon,
            TrangThaiMoi = TrangThaiPhieuMuon.DaTra,
            NgayThucHien = _time.GetLocalNow().DateTime,
            MaNguoiThucHien = actingMaTaiKhoan,
            GhiChu = $"Nhận trả sách - {input.TinhTrang.GetDisplayName()}"
        });

// Phiếu đã trả thì không còn gia hạn được
        var yeuCauTreo = await _db.GiaHanMuons
            .Where(g => g.MaPhieuMuon == phieu.MaPhieuMuon && g.TrangThai == TrangThaiGiaHan.ChoDuyet)
            .ToListAsync(ct);
        foreach (var g in yeuCauTreo)
        {
            g.TrangThai = TrangThaiGiaHan.TuChoi;
            g.NgayXuLy = ngayTra;
            g.NhanXet = "Phiếu mượn đã trả sách.";
        }

        try
        {
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            return ServiceResult<int>.Error(string.Empty, MsgSaveFailed);
        }

        if (tienPhat > 0)
            await NotifySafelyAsync(() => _thongBao.NotifyReaderForFineAsync(
                traSach.MaTra, "Bạn có khoản tiền phạt mới",
                $"Sách đã được nhận trả. Khoản phạt hiện tại: {tienPhat:N0} đ. Vui lòng xem chi tiết trong mục Tiền phạt.",
                $"fine-created:{traSach.MaTra}", "/BanDoc/TienPhat", ct), "fine-created");
        else
            await NotifySafelyAsync(() => _thongBao.NotifyReaderForLoanAsync(
                phieu.MaPhieuMuon, "Trả sách thành công",
                "Thư viện đã ghi nhận bạn trả sách thành công. Cảm ơn bạn đã sử dụng dịch vụ.",
                $"loan-returned:{phieu.MaPhieuMuon}", "/BanDoc/PhieuMuonCuaToi", "loan", ct), "loan-returned");
        return ServiceResult<int>.Ok(traSach.MaTra);
    }

 //Nộp phạt

    public async Task<ServiceResult> MarkPaidAsync(int maTra, CancellationToken ct = default)
    {
        var traSach = await _db.TraSachs.FirstOrDefaultAsync(t => t.MaTra == maTra, ct);
        if (traSach is null)
            return ServiceResult.Fail(MsgTraNotFound);

        if (traSach.TrangThaiPhat != TrangThaiPhat.ChuaNop)
            return ServiceResult.Fail("Chỉ khoản phạt chưa nộp mới được đánh dấu đã nộp.");

        traSach.TrangThaiPhat = TrangThaiPhat.DaNop;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Fail(MsgSaveFailed);
        }

        await NotifySafelyAsync(() => _thongBao.NotifyReaderForFineAsync(
            traSach.MaTra, "Khoản tiền phạt đã được thanh toán",
            "Thư viện đã xác nhận khoản tiền phạt của bạn được thanh toán.",
            $"fine-paid:{traSach.MaTra}", "/BanDoc/TienPhat", ct), "fine-paid");
        return ServiceResult.Success();
    }

    private async Task<CauHinhThuVien> GetConfigAsync(CancellationToken ct) =>
        await _db.CauHinhThuViens.AsNoTracking().FirstOrDefaultAsync(ct) ?? new CauHinhThuVien();
}