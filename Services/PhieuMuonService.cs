// Họ và tên: Nguyễn Hữu Tuấn
// Mã sinh viên: 23103100136
// Nội dung thực hiện: Xây dựng service xử lý nghiệp vụ phiếu mượn, đăng ký mượn sách, hủy phiếu và theo dõi phiếu mượn

using System.Data;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using Microsoft.Extensions.Logging;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services;

public class PhieuMuonService : IPhieuMuonService
{
    private readonly AppDbContext _db;
    private readonly IThongBaoService _thongBao;
    private readonly ILogger<PhieuMuonService> _logger;

    public PhieuMuonService(AppDbContext db, IThongBaoService thongBao, ILogger<PhieuMuonService> logger)
    {
        _db = db;
        _thongBao = thongBao;
        _logger = logger;
    }

    private async Task NotifySafelyAsync(Func<Task> action, string eventName)
    {
        try { await action(); }
        catch (Exception ex) { _logger.LogError(ex, "Không thể tạo thông báo cho sự kiện {EventName}", eventName); }
    }

    private async Task<CauHinhThuVien> GetCauHinhAsync(CancellationToken ct = default)
    {
        return await _db.CauHinhThuViens.FirstOrDefaultAsync(ct) ?? new CauHinhThuVien();
    }

    // 7.3. Đăng ký mượn sách: Kiểm tra 7 điều kiện bằng EF Core / LINQ
    public async Task<LoanEligibilityResult> CheckCanBorrowAsync(int maBanDoc, int maSach, CancellationToken ct = default)
    {
        var result = new LoanEligibilityResult { CanBorrow = true };
        var today = LibraryClock.Today();
        var cauHinh = await GetCauHinhAsync(ct);

        // 1. Bạn đọc tồn tại, đang hoạt động và thẻ thư viện còn hạn
        var banDoc = await _db.BanDocs.AsNoTracking().FirstOrDefaultAsync(b => b.MaBanDoc == maBanDoc, ct);
        if (banDoc is null)
        {
            result.CanBorrow = false;
            result.Errors.Add("Hồ sơ bạn đọc không tồn tại trong hệ thống.");
            return result;
        }

        var readerErrors = await GetReaderBlockersAsync(banDoc, today, ct);
        if (readerErrors.Count > 0)
        {
            result.CanBorrow = false;
            result.Errors.AddRange(readerErrors);
        }

        // 2. Sách tồn tại và ở trạng thái Đang phục vụ
        var sach = await _db.Sachs.AsNoTracking().FirstOrDefaultAsync(s => s.MaSach == maSach, ct);
        if (sach is null)
        {
            result.CanBorrow = false;
            result.Errors.Add("Đầu sách không tồn tại.");
            return result;
        }

        if (sach.TrangThai != TrangThaiSach.DangPhucVu)
        {
            result.CanBorrow = false;
            result.Errors.Add($"Sách đang ở trạng thái '{sach.TrangThai.GetDisplayName()}', hiện không phục vụ mượn.");
        }

        // 3. Số lượng còn lại của sách lớn hơn 0
        var heldCopies = await _db.PhieuMuons
            .CountAsync(p => p.MaSach == maSach &&
                            (p.TrangThai == TrangThaiPhieuMuon.DaDuyet || p.TrangThai == TrangThaiPhieuMuon.DangMuon), ct);

        var lostCopies = await _db.TraSachs
            .CountAsync(t => t.PhieuMuon!.MaSach == maSach && t.TinhTrangSach == TinhTrangSach.Mat, ct);

        var soLuongConLai = sach.SoLuongTong - heldCopies - lostCopies;
        result.SoLuongConLai = Math.Max(0, soLuongConLai);

        if (soLuongConLai <= 0)
        {
            result.CanBorrow = false;
            result.Errors.Add("Đầu sách này hiện đã hết bản sẵn sàng phục vụ (tất cả các bản đang được mượn hoặc đã mất).");
        }

        // 4. Bạn đọc không được có hai phiếu đang xử lý (Chờ duyệt, Đã duyệt hoặc Đang mượn) cho cùng một đầu sách
        var daCoPhieuSachNay = await _db.PhieuMuons
            .AnyAsync(p => p.MaBanDoc == maBanDoc && p.MaSach == maSach &&
                          (p.TrangThai == TrangThaiPhieuMuon.ChoDuyet ||
                           p.TrangThai == TrangThaiPhieuMuon.DaDuyet ||
                           p.TrangThai == TrangThaiPhieuMuon.DangMuon), ct);

        if (daCoPhieuSachNay)
        {
            result.CanBorrow = false;
            result.Errors.Add("Bạn đang có một phiếu (Chờ duyệt, Đã duyệt hoặc Đang mượn) cho đầu sách này.");
        }

        // 5. Tổng số phiếu đang xử lý của bạn đọc chưa đạt mức tối đa quy định (mặc định 5 cuốn)
        var tongDangXuLy = await _db.PhieuMuons
            .CountAsync(p => p.MaBanDoc == maBanDoc &&
                            (p.TrangThai == TrangThaiPhieuMuon.ChoDuyet ||
                             p.TrangThai == TrangThaiPhieuMuon.DaDuyet ||
                             p.TrangThai == TrangThaiPhieuMuon.DangMuon), ct);

        if (tongDangXuLy >= cauHinh.SoPhieuToiDa)
        {
            result.CanBorrow = false;
            result.Errors.Add($"Bạn đã đạt mức tối đa {cauHinh.SoPhieuToiDa} phiếu đang xử lý đồng thời.");
        }

        return result;
    }

    // Điều kiện về phía bạn đọc, dùng chung cho lúc đăng ký và lúc thủ thư duyệt phiếu.
    private Task<List<string>> GetReaderBlockersAsync(BanDoc banDoc, DateOnly today, CancellationToken ct) =>
        ReaderEligibility.GetBlockersAsync(_db, banDoc, today, ct);

    public async Task<ServiceResult<int>> RegisterLoanAsync(int maBanDoc, int maSach, string? ghiChu, CancellationToken ct = default)
    {
        var check = await CheckCanBorrowAsync(maBanDoc, maSach, ct);
        if (!check.CanBorrow)
        {
            var res = new ServiceResult<int>();
            foreach (var err in check.Errors)
                res.AddError(string.Empty, err);
            return res;
        }

        var banDoc = await _db.BanDocs.FindAsync(new object[] { maBanDoc }, ct);
        if (banDoc is null) return ServiceResult<int>.Error(string.Empty, "Bạn đọc không tồn tại.");

        var today = LibraryClock.Today();
        var phieuMuon = new PhieuMuon
        {
            MaBanDoc = maBanDoc,
            MaSach = maSach,
            NgayDangKy = today,        // Do hệ thống xác định, không cho bạn đọc tự nhập
            NgayMuon = null,          // Chỉ do hệ thống ghi khi giao sách
            HanTra = null,            // Chỉ do hệ thống ghi khi giao sách
            GhiChu = string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu.Trim(),
            TrangThai = TrangThaiPhieuMuon.ChoDuyet, // Mới tạo luôn ở trạng thái Chờ duyệt
            NgayXuLy = null,
            NhanXetThuThu = null
        };

        _db.PhieuMuons.Add(phieuMuon);

        // Ghi nhận nhật ký audit trail
        var lichSu = new PhieuMuonLichSu
        {
            PhieuMuon = phieuMuon,
            TrangThaiCu = null,
            TrangThaiMoi = TrangThaiPhieuMuon.ChoDuyet,
            NgayThucHien = LibraryClock.LocalDateTime(),
            MaNguoiThucHien = banDoc.MaTaiKhoan,
            GhiChu = "Bạn đọc đăng ký mượn sách trực tuyến"
        };
        _db.PhieuMuonLichSus.Add(lichSu);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return ServiceResult<int>.Error(string.Empty, "Không thể tạo phiếu mượn do vi phạm ràng buộc dữ liệu.");
        }

        await NotifySafelyAsync(() => _thongBao.NotifyStaffAsync(
            "Có yêu cầu mượn sách mới", $"Bạn đọc vừa đăng ký mượn sách (phiếu #{phieuMuon.MaPhieuMuon}).",
            $"loan-register:{phieuMuon.MaPhieuMuon}", "/ThuThu/QuanLyPhieuMuon", "loan", ct), "loan-register");
        return ServiceResult<int>.Ok(phieuMuon.MaPhieuMuon);
    }

    // 7.4. Hủy phiếu mượn
    public async Task<ServiceResult> CancelLoanAsync(int maPhieuMuon, int maBanDoc, CancellationToken ct = default)
    {
        var phieu = await _db.PhieuMuons
            .Include(p => p.BanDoc)
            .FirstOrDefaultAsync(p => p.MaPhieuMuon == maPhieuMuon, ct);

        if (phieu is null)
            return ServiceResult.Fail("Không tìm thấy phiếu mượn.");

        // Bạn đọc chỉ được hủy phiếu của chính mình
        if (phieu.MaBanDoc != maBanDoc)
            return ServiceResult.Fail("Bạn không có quyền thao tác trên phiếu mượn này.");

        // Chỉ được hủy trong các trạng thái cho phép: Chờ duyệt hoặc Đã duyệt nhưng chưa nhận sách
        var duocHuy = phieu.TrangThai == TrangThaiPhieuMuon.ChoDuyet ||
                      (phieu.TrangThai == TrangThaiPhieuMuon.DaDuyet && !phieu.NgayMuon.HasValue);

        if (!duocHuy)
            return ServiceResult.Fail($"Không thể hủy phiếu mượn đang ở trạng thái '{phieu.TrangThai.GetDisplayName()}'. Phiếu đang mượn hoặc đã trả không được hủy.");

        var oldStatus = phieu.TrangThai;
        phieu.TrangThai = TrangThaiPhieuMuon.DaHuy;

        // Ghi nhật ký
        _db.PhieuMuonLichSus.Add(new PhieuMuonLichSu
        {
            MaPhieuMuon = phieu.MaPhieuMuon,
            TrangThaiCu = oldStatus,
            TrangThaiMoi = TrangThaiPhieuMuon.DaHuy,
            NgayThucHien = LibraryClock.LocalDateTime(),
            MaNguoiThucHien = phieu.BanDoc!.MaTaiKhoan,
            GhiChu = "Bạn đọc tự hủy phiếu mượn"
        });

        await _db.SaveChangesAsync(ct);
        await NotifySafelyAsync(() => _thongBao.NotifyStaffAsync(
            "Bạn đọc đã hủy yêu cầu mượn", $"Phiếu #{phieu.MaPhieuMuon} đã được bạn đọc hủy.",
            $"loan-reader-cancel:{phieu.MaPhieuMuon}", "/ThuThu/QuanLyPhieuMuon", "loan", ct), "loan-reader-cancel");
        return ServiceResult.Success();
    }

    // 7.5. Theo dõi phiếu mượn (Dành cho bạn đọc)
    public async Task<List<PhieuMuonItemViewModel>> GetMyLoansAsync(int maBanDoc, string? filter = null, CancellationToken ct = default)
    {
        var today = LibraryClock.Today();
        var query = _db.PhieuMuons
            .Include(p => p.Sach)
                .ThenInclude(s => s!.TheLoai)
            .Include(p => p.BanDoc)
            .Include(p => p.TraSach)
            .Include(p => p.GiaHanMuons)
            .AsNoTracking()
            .Where(p => p.MaBanDoc == maBanDoc);

        // Lọc theo tab
        filter = filter?.ToLowerInvariant();
        if (filter == "pending" || filter == "choduyet")
        {
            query = query.Where(p => p.TrangThai == TrangThaiPhieuMuon.ChoDuyet);
        }
        else if (filter == "active")
        {
            query = query.Where(p => p.TrangThai == TrangThaiPhieuMuon.ChoDuyet ||
                                     p.TrangThai == TrangThaiPhieuMuon.DaDuyet ||
                                     p.TrangThai == TrangThaiPhieuMuon.DangMuon);
        }
        else if (filter == "completed")
        {
            query = query.Where(p => p.TrangThai == TrangThaiPhieuMuon.DaTra ||
                                     p.TrangThai == TrangThaiPhieuMuon.DaHuy ||
                                     p.TrangThai == TrangThaiPhieuMuon.TuChoi);
        }
        else if (filter == "overdue")
        {
            // Trạng thái quá hạn được xác định bằng LINQ
            query = query.Where(p => p.TrangThai == TrangThaiPhieuMuon.DangMuon &&
                                     p.HanTra.HasValue &&
                                     p.HanTra.Value < today);
        }

        var loans = await query
            .OrderByDescending(p => p.NgayDangKy)
            .ThenByDescending(p => p.MaPhieuMuon)
            .Select(p => new PhieuMuonItemViewModel
            {
                MaPhieuMuon = p.MaPhieuMuon,
                MaBanDoc = p.MaBanDoc,
                HoTenBanDoc = p.BanDoc!.HoTen,
                MaSoThe = p.BanDoc.MaSoThe,
                MaSach = p.MaSach,
                TenSach = p.Sach != null ? p.Sach.TenSach : string.Empty,
                TacGia = p.Sach != null ? p.Sach.TacGia : string.Empty,
                TheLoai = p.Sach != null && p.Sach.TheLoai != null ? p.Sach.TheLoai.TenTheLoai : null,
                ViTriKe = p.Sach != null ? p.Sach.ViTriKe : null,
                NgayDangKy = p.NgayDangKy,
                NgayMuon = p.NgayMuon,
                HanTra = p.HanTra,
                TrangThai = p.TrangThai,
                NgayXuLy = p.NgayXuLy,
                NhanXetThuThu = p.NhanXetThuThu,
                GhiChu = p.GhiChu,
                // Trạng thái quá hạn không lưu thủ công mà xác định bằng LINQ
                IsOverdue = p.TrangThai == TrangThaiPhieuMuon.DangMuon && p.HanTra.HasValue && p.HanTra.Value < today,
                SoNgayQuaHan = p.HanTra.HasValue ? Math.Max(0, today.DayNumber - p.HanTra.Value.DayNumber) : 0,
                SoNgayConLai = p.HanTra.HasValue ? Math.Max(0, p.HanTra.Value.DayNumber - today.DayNumber) : 0,
                CanCancel = p.TrangThai == TrangThaiPhieuMuon.ChoDuyet || (p.TrangThai == TrangThaiPhieuMuon.DaDuyet && !p.NgayMuon.HasValue),
                SoLanGiaHan = p.GiaHanMuons.Count(g => g.TrangThai == TrangThaiGiaHan.DaDuyet),
                TienPhat = p.TraSach != null ? (decimal?)p.TraSach.TienPhat : null,
                TrangThaiPhat = p.TraSach != null ? (TrangThaiPhat?)p.TraSach.TrangThaiPhat : null
            })
            .ToListAsync(ct);

        return loans;
    }

    public async Task<PhieuMuonDetailViewModel?> GetLoanDetailAsync(int maPhieuMuon, int? requiredMaBanDoc = null, CancellationToken ct = default)
    {
        var today = LibraryClock.Today();
        var query = _db.PhieuMuons
            .Include(p => p.Sach)
                .ThenInclude(s => s!.TheLoai)
            .Include(p => p.BanDoc)
            .Include(p => p.GiaHanMuons)
            .Include(p => p.TraSach)
                .ThenInclude(t => t!.NguoiNhan)
            .Include(p => p.LichSus)
                .ThenInclude(l => l.NguoiThucHien)
            .AsNoTracking()
            .Where(p => p.MaPhieuMuon == maPhieuMuon);

        // Bảo mật: Bạn đọc chỉ được xem dữ liệu của chính mình, thay đổi mã trên URL không làm lộ dữ liệu
        if (requiredMaBanDoc.HasValue)
            query = query.Where(p => p.MaBanDoc == requiredMaBanDoc.Value);

        var p = await query.FirstOrDefaultAsync(ct);
        if (p is null) return null;

        var isOverdue = p.TrangThai == TrangThaiPhieuMuon.DangMuon && p.HanTra.HasValue && p.HanTra.Value < today;
        var cauHinh = await GetCauHinhAsync(ct);

        return new PhieuMuonDetailViewModel
        {
            MaPhieuMuon = p.MaPhieuMuon,
            MaSach = p.MaSach,
            TenSach = p.Sach?.TenSach ?? string.Empty,
            TacGia = p.Sach?.TacGia ?? string.Empty,
            TheLoai = p.Sach?.TheLoai?.TenTheLoai,
            NhaXuatBan = p.Sach?.NhaXuatBan,
            NamXuatBan = p.Sach?.NamXuatBan ?? 0,
            GiaBia = p.Sach?.GiaBia ?? 0,
            ViTriKe = p.Sach?.ViTriKe,

            MaBanDoc = p.MaBanDoc,
            HoTenBanDoc = p.BanDoc?.HoTen ?? string.Empty,
            MaSoThe = p.BanDoc?.MaSoThe,
            LoaiBanDoc = p.BanDoc?.LoaiBanDoc ?? LoaiBanDoc.SinhVien,
            SoDienThoai = p.BanDoc?.SoDienThoai,
            Email = p.BanDoc?.Email,

            NgayDangKy = p.NgayDangKy,
            NgayMuon = p.NgayMuon,
            HanTra = p.HanTra,
            TrangThai = p.TrangThai,
            NgayXuLy = p.NgayXuLy,
            NhanXetThuThu = p.NhanXetThuThu,
            GhiChu = p.GhiChu,

            SoLanDaGiaHan = p.GiaHanMuons.Count(g => g.TrangThai == TrangThaiGiaHan.DaDuyet),
            SoLanGiaHanToiDa = cauHinh.SoLanGiaHanToiDa,
            SoNgayGiaHanToiDa = cauHinh.SoNgayGiaHanToiDa,

            IsOverdue = isOverdue,
            SoNgayQuaHan = p.HanTra.HasValue ? Math.Max(0, today.DayNumber - p.HanTra.Value.DayNumber) : 0,
            SoNgayConLai = p.HanTra.HasValue ? Math.Max(0, p.HanTra.Value.DayNumber - today.DayNumber) : 0,
            CanCancel = p.TrangThai == TrangThaiPhieuMuon.ChoDuyet || (p.TrangThai == TrangThaiPhieuMuon.DaDuyet && !p.NgayMuon.HasValue),

            LichSuGiaHan = p.GiaHanMuons
                .OrderByDescending(g => g.NgayYeuCau)
                .Select(g => new GiaHanMuonItemViewModel
                {
                    MaGiaHan = g.MaGiaHan,
                    HanTraCu = g.HanTraCu,
                    HanTraMoi = g.HanTraMoi,
                    LyDo = g.LyDo,
                    NgayYeuCau = g.NgayYeuCau,
                    TrangThai = g.TrangThai,
                    NgayXuLy = g.NgayXuLy,
                    NhanXet = g.NhanXet
                }).ToList(),

            ThongTinTraSach = p.TraSach == null ? null : new TraSachItemViewModel
            {
                MaTra = p.TraSach.MaTra,
                NgayTra = p.TraSach.NgayTra,
                TinhTrangSach = p.TraSach.TinhTrangSach,
                SoNgayTre = p.TraSach.SoNgayTre,
                TienPhat = p.TraSach.TienPhat,
                TrangThaiPhat = p.TraSach.TrangThaiPhat,
                GhiChu = p.TraSach.GhiChu,
                NguoiNhan = p.TraSach.NguoiNhan?.HoTen
            },

            LichSuTrangThai = p.LichSus
                .OrderBy(l => l.NgayThucHien)
                .Select(l => new PhieuMuonLichSuItemViewModel
                {
                    MaLichSu = l.MaLichSu,
                    TrangThaiCu = l.TrangThaiCu,
                    TrangThaiMoi = l.TrangThaiMoi,
                    NgayThucHien = l.NgayThucHien,
                    NguoiThucHien = l.NguoiThucHien?.HoTen,
                    GhiChu = l.GhiChu
                }).ToList()
        };
    }

    public async Task<MyFinesViewModel> GetMyFinesAsync(int maBanDoc, CancellationToken ct = default)
    {
        var fines = await _db.PhieuMuons
            .Include(p => p.Sach)
            .Include(p => p.TraSach)
            .AsNoTracking()
            .Where(p => p.MaBanDoc == maBanDoc && p.TraSach != null && p.TraSach.TienPhat > 0)
            .OrderByDescending(p => p.TraSach!.NgayTra)
            .Select(p => new FineItemViewModel
            {
                MaTra = p.TraSach!.MaTra,
                MaPhieuMuon = p.MaPhieuMuon,
                TenSach = p.Sach != null ? p.Sach.TenSach : string.Empty,
                NgayTra = p.TraSach.NgayTra,
                TinhTrangSach = p.TraSach.TinhTrangSach,
                SoNgayTre = p.TraSach.SoNgayTre,
                TienPhat = p.TraSach.TienPhat,
                TrangThaiPhat = p.TraSach.TrangThaiPhat,
                GhiChu = p.TraSach.GhiChu
            })
            .ToListAsync(ct);

        return new MyFinesViewModel { Fines = fines };
    }

    // Quản lý phiếu mượn (Thủ thư / Admin)
    public async Task<PagedResult<PhieuMuonItemViewModel>> GetManageListAsync(PhieuMuonManageFilter filter, CancellationToken ct = default)
    {
        var today = LibraryClock.Today();
        var query = _db.PhieuMuons
            .Include(p => p.Sach)
                .ThenInclude(s => s!.TheLoai)
            .Include(p => p.BanDoc)
            .Include(p => p.TraSach)
            .Include(p => p.GiaHanMuons)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            query = query.Where(p => (p.Sach != null && p.Sach.TenSach.Contains(s))
                                  || (p.BanDoc != null && (p.BanDoc.HoTen.Contains(s) || (p.BanDoc.MaSoThe != null && p.BanDoc.MaSoThe.Contains(s)))));
        }

        if (!string.IsNullOrWhiteSpace(filter.GroupFilter))
        {
            var gf = filter.GroupFilter.Trim().ToLowerInvariant();
            if (gf == "cho_duyet_giao" || gf == "pending_delivery")
            {
                query = query.Where(p => p.TrangThai == TrangThaiPhieuMuon.ChoDuyet || p.TrangThai == TrangThaiPhieuMuon.DaDuyet);
            }
            else if (gf == "dang_muon" || gf == "borrowing")
            {
                query = query.Where(p => p.TrangThai == TrangThaiPhieuMuon.DangMuon);
            }
            else if (gf == "hoan_thanh" || gf == "completed")
            {
                query = query.Where(p => p.TrangThai == TrangThaiPhieuMuon.DaTra ||
                                         p.TrangThai == TrangThaiPhieuMuon.TuChoi ||
                                         p.TrangThai == TrangThaiPhieuMuon.DaHuy);
            }
        }
        else if (filter.TrangThai.HasValue)
        {
            query = query.Where(p => p.TrangThai == filter.TrangThai.Value);
        }

        if (filter.MaTheLoai.HasValue)
            query = query.Where(p => p.Sach != null && p.Sach.MaTheLoai == filter.MaTheLoai.Value);

        if (filter.OverdueOnly == true)
            query = query.Where(p => p.TrangThai == TrangThaiPhieuMuon.DangMuon && p.HanTra.HasValue && p.HanTra.Value < today);

        IOrderedQueryable<PhieuMuon> ordered = filter.Sort switch
        {
            "oldest" => query.OrderBy(p => p.NgayDangKy).ThenBy(p => p.MaPhieuMuon),
            "due_asc" => query.OrderBy(p => p.HanTra == null).ThenBy(p => p.HanTra).ThenByDescending(p => p.MaPhieuMuon),
            "due_desc" => query.OrderByDescending(p => p.HanTra).ThenByDescending(p => p.MaPhieuMuon),
            "reader_asc" => query.OrderBy(p => p.BanDoc != null ? p.BanDoc.HoTen : string.Empty).ThenByDescending(p => p.MaPhieuMuon),
            "book_asc" => query.OrderBy(p => p.Sach != null ? p.Sach.TenSach : string.Empty).ThenByDescending(p => p.MaPhieuMuon),
            _ => query.OrderByDescending(p => p.NgayDangKy).ThenByDescending(p => p.MaPhieuMuon)
        };

        var projected = ordered
            .Select(p => new PhieuMuonItemViewModel
            {
                MaPhieuMuon = p.MaPhieuMuon,
                MaBanDoc = p.MaBanDoc,
                HoTenBanDoc = p.BanDoc != null ? p.BanDoc.HoTen : string.Empty,
                MaSoThe = p.BanDoc != null ? p.BanDoc.MaSoThe : null,
                MaSach = p.MaSach,
                TenSach = p.Sach != null ? p.Sach.TenSach : string.Empty,
                TacGia = p.Sach != null ? p.Sach.TacGia : string.Empty,
                TheLoai = p.Sach != null && p.Sach.TheLoai != null ? p.Sach.TheLoai.TenTheLoai : null,
                ViTriKe = p.Sach != null ? p.Sach.ViTriKe : null,
                NgayDangKy = p.NgayDangKy,
                NgayMuon = p.NgayMuon,
                HanTra = p.HanTra,
                TrangThai = p.TrangThai,
                NgayXuLy = p.NgayXuLy,
                NhanXetThuThu = p.NhanXetThuThu,
                GhiChu = p.GhiChu,
                IsOverdue = p.TrangThai == TrangThaiPhieuMuon.DangMuon && p.HanTra.HasValue && p.HanTra.Value < today,
                SoNgayQuaHan = p.HanTra.HasValue ? Math.Max(0, today.DayNumber - p.HanTra.Value.DayNumber) : 0,
                SoNgayConLai = p.HanTra.HasValue ? Math.Max(0, p.HanTra.Value.DayNumber - today.DayNumber) : 0,
                CanCancel = p.TrangThai == TrangThaiPhieuMuon.ChoDuyet || (p.TrangThai == TrangThaiPhieuMuon.DaDuyet && !p.NgayMuon.HasValue),
                SoLanGiaHan = p.GiaHanMuons.Count(g => g.TrangThai == TrangThaiGiaHan.DaDuyet),
                TienPhat = p.TraSach != null ? (decimal?)p.TraSach.TienPhat : null,
                TrangThaiPhat = p.TraSach != null ? (TrangThaiPhat?)p.TraSach.TrangThaiPhat : null
            });

        return await PagedResult<PhieuMuonItemViewModel>.CreateAsync(projected, filter.Page, filter.PageSize, ct);
    }

    // Tổng hợp số liệu cho trang quản lý phiếu mượn (thủ thư / Admin)
    public async Task<PhieuMuonManageSummary> GetManageSummaryAsync(CancellationToken ct = default)
    {
        var today = LibraryClock.Today();
        var map = (await _db.PhieuMuons.AsNoTracking()
                .GroupBy(p => p.TrangThai)
                .Select(g => new { g.Key, N = g.Count() })
                .ToListAsync(ct))
            .ToDictionary(x => x.Key, x => x.N);

        var quaHan = await _db.PhieuMuons.CountAsync(
            p => p.TrangThai == TrangThaiPhieuMuon.DangMuon && p.HanTra.HasValue && p.HanTra.Value < today, ct);

        var cutoff = today.AddDays(-LoanRules.UncollectedDays);
        var chuaNhan = await _db.PhieuMuons.CountAsync(
            p => p.TrangThai == TrangThaiPhieuMuon.DaDuyet && (p.NgayXuLy ?? p.NgayDangKy) <= cutoff, ct);

        var hoanThanh = map.GetValueOrDefault(TrangThaiPhieuMuon.DaTra)
                      + map.GetValueOrDefault(TrangThaiPhieuMuon.TuChoi)
                      + map.GetValueOrDefault(TrangThaiPhieuMuon.DaHuy);

        return new PhieuMuonManageSummary(
            map.GetValueOrDefault(TrangThaiPhieuMuon.ChoDuyet),
            map.GetValueOrDefault(TrangThaiPhieuMuon.DaDuyet),
            map.GetValueOrDefault(TrangThaiPhieuMuon.DangMuon),
            quaHan,
            chuaNhan,
            hoanThanh);
    }

    // Phiếu Đã duyệt quá LoanRules.UncollectedDays ngày mà bạn đọc chưa đến nhận (vẫn đang giữ một bản sách).
    public async Task<List<PhieuMuonItemViewModel>> GetUncollectedAsync(CancellationToken ct = default)
    {
        var today = LibraryClock.Today();
        var cutoff = today.AddDays(-LoanRules.UncollectedDays);

        return await _db.PhieuMuons.AsNoTracking()
            .Where(p => p.TrangThai == TrangThaiPhieuMuon.DaDuyet && (p.NgayXuLy ?? p.NgayDangKy) <= cutoff)
            .OrderBy(p => p.NgayXuLy ?? p.NgayDangKy)
            .ThenBy(p => p.MaPhieuMuon)
            .Select(p => new PhieuMuonItemViewModel
            {
                MaPhieuMuon = p.MaPhieuMuon,
                MaBanDoc = p.MaBanDoc,
                HoTenBanDoc = p.BanDoc != null ? p.BanDoc.HoTen : string.Empty,
                MaSoThe = p.BanDoc != null ? p.BanDoc.MaSoThe : null,
                MaSach = p.MaSach,
                TenSach = p.Sach != null ? p.Sach.TenSach : string.Empty,
                TacGia = p.Sach != null ? p.Sach.TacGia : string.Empty,
                ViTriKe = p.Sach != null ? p.Sach.ViTriKe : null,
                NgayDangKy = p.NgayDangKy,
                NgayXuLy = p.NgayXuLy,
                TrangThai = p.TrangThai,
                NhanXetThuThu = p.NhanXetThuThu
            })
            .ToListAsync(ct);
    }

    public async Task<ServiceResult> ApproveLoanAsync(int maPhieuMuon, int actingMaTaiKhoan, string? nhanXet = null, CancellationToken ct = default)
    {
        nhanXet = string.IsNullOrWhiteSpace(nhanXet) ? null : nhanXet.Trim();
        if (nhanXet is { Length: > 100 })
            return ServiceResult.Fail("Nhận xét tối đa 100 ký tự.");

        // Serializable: hai thủ thư duyệt cùng lúc không thể cùng lấy bản sách cuối.
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var phieu = await _db.PhieuMuons
            .Include(p => p.Sach)
            .Include(p => p.BanDoc)
            .FirstOrDefaultAsync(p => p.MaPhieuMuon == maPhieuMuon, ct);

        if (phieu is null)
            return ServiceResult.Fail("Không tìm thấy phiếu mượn.");

        if (phieu.TrangThai != TrangThaiPhieuMuon.ChoDuyet)
            return ServiceResult.Fail($"Chỉ có thể duyệt phiếu ở trạng thái 'Chờ duyệt' (hiện tại: {phieu.TrangThai.GetDisplayName()}).");

        // Kiểm tra lại mọi điều kiện: bất kỳ điều kiện nào không đạt thì chặn duyệt.
        var errors = new List<string>();
        var today = LibraryClock.Today();

        if (phieu.BanDoc is null)
            errors.Add("Hồ sơ bạn đọc không tồn tại.");
        else
            errors.AddRange(await GetReaderBlockersAsync(phieu.BanDoc, today, ct));

        var sach = phieu.Sach;
        if (sach is null || sach.TrangThai != TrangThaiSach.DangPhucVu)
        {
            errors.Add("Sách không còn phục vụ.");
        }
        else
        {
            var heldCopies = await _db.PhieuMuons
                .CountAsync(p => p.MaSach == phieu.MaSach &&
                                (p.TrangThai == TrangThaiPhieuMuon.DaDuyet || p.TrangThai == TrangThaiPhieuMuon.DangMuon), ct);

            var lostCopies = await _db.TraSachs
                .CountAsync(t => t.PhieuMuon!.MaSach == phieu.MaSach && t.TinhTrangSach == TinhTrangSach.Mat, ct);

            if (sach.SoLuongTong - heldCopies - lostCopies <= 0)
                errors.Add("Sách này hiện không còn bản trống để duyệt.");
        }

        if (errors.Count > 0)
        {
            var fail = new ServiceResult();
            foreach (var e in errors) fail.AddError(string.Empty, e);
            return fail;
        }

        var oldStatus = phieu.TrangThai;
        phieu.TrangThai = TrangThaiPhieuMuon.DaDuyet;
        phieu.NgayXuLy = today;
        phieu.NhanXetThuThu = nhanXet ?? "Đã duyệt, mời bạn đọc đến nhận sách.";

        _db.PhieuMuonLichSus.Add(new PhieuMuonLichSu
        {
            MaPhieuMuon = phieu.MaPhieuMuon,
            TrangThaiCu = oldStatus,
            TrangThaiMoi = TrangThaiPhieuMuon.DaDuyet,
            NgayThucHien = LibraryClock.LocalDateTime(),
            MaNguoiThucHien = actingMaTaiKhoan,
            GhiChu = phieu.NhanXetThuThu
        });

        try
        {
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Fail("Không thể lưu thay đổi. Vui lòng thử lại.");
        }

        await NotifySafelyAsync(() => _thongBao.NotifyReaderForLoanAsync(
            phieu.MaPhieuMuon, "Yêu cầu mượn sách đã được duyệt",
            "Yêu cầu mượn sách của bạn đã được duyệt. Vui lòng đến thư viện nhận sách trong thời hạn quy định.",
            $"loan-approved:{phieu.MaPhieuMuon}", $"/BanDoc/PhieuMuonCuaToi/Details/{phieu.MaPhieuMuon}", "loan", ct), "loan-approved");
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RejectLoanAsync(int maPhieuMuon, int actingMaTaiKhoan, string? nhanXet = null, CancellationToken ct = default)
    {
        nhanXet = string.IsNullOrWhiteSpace(nhanXet) ? null : nhanXet.Trim();
        if (nhanXet is null)
            return ServiceResult.Fail("Vui lòng nhập lý do từ chối.");
        if (nhanXet.Length > 100)
            return ServiceResult.Fail("Lý do từ chối tối đa 100 ký tự.");

        var phieu = await _db.PhieuMuons.FirstOrDefaultAsync(p => p.MaPhieuMuon == maPhieuMuon, ct);
        if (phieu is null)
            return ServiceResult.Fail("Không tìm thấy phiếu mượn.");

        if (phieu.TrangThai != TrangThaiPhieuMuon.ChoDuyet)
            return ServiceResult.Fail($"Chỉ có thể từ chối phiếu ở trạng thái 'Chờ duyệt' (hiện tại: {phieu.TrangThai.GetDisplayName()}).");

        var oldStatus = phieu.TrangThai;
        phieu.TrangThai = TrangThaiPhieuMuon.TuChoi;
        phieu.NgayXuLy = LibraryClock.Today();
        phieu.NhanXetThuThu = nhanXet;

        _db.PhieuMuonLichSus.Add(new PhieuMuonLichSu
        {
            MaPhieuMuon = phieu.MaPhieuMuon,
            TrangThaiCu = oldStatus,
            TrangThaiMoi = TrangThaiPhieuMuon.TuChoi,
            NgayThucHien = LibraryClock.LocalDateTime(),
            MaNguoiThucHien = actingMaTaiKhoan,
            GhiChu = phieu.NhanXetThuThu
        });

        await _db.SaveChangesAsync(ct);
        await NotifySafelyAsync(() => _thongBao.NotifyReaderForLoanAsync(
            phieu.MaPhieuMuon, "Yêu cầu mượn sách bị từ chối",
            $"Yêu cầu mượn sách của bạn đã bị từ chối. Lý do: {nhanXet}",
            $"loan-rejected:{phieu.MaPhieuMuon}", $"/BanDoc/PhieuMuonCuaToi/Details/{phieu.MaPhieuMuon}", "loan", ct), "loan-rejected");
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeliverBookAsync(int maPhieuMuon, int actingMaTaiKhoan, CancellationToken ct = default)
    {
        var phieu = await _db.PhieuMuons
            .Include(p => p.BanDoc)
            .FirstOrDefaultAsync(p => p.MaPhieuMuon == maPhieuMuon, ct);

        if (phieu is null)
            return ServiceResult.Fail("Không tìm thấy phiếu mượn.");

        if (phieu.TrangThai != TrangThaiPhieuMuon.DaDuyet)
            return ServiceResult.Fail($"Chỉ có thể giao sách cho phiếu ở trạng thái 'Đã duyệt' (hiện tại: {phieu.TrangThai.GetDisplayName()}).");

        var today = LibraryClock.Today();
        if (phieu.BanDoc is null)
            return ServiceResult.Fail("Không tìm thấy hồ sơ bạn đọc của phiếu mượn.");

        var readerBlockers = await GetReaderBlockersAsync(phieu.BanDoc, today, ct);
        if (readerBlockers.Count > 0)
            return ServiceResult.Fail(string.Join(" ", readerBlockers));

        var cauHinh = await GetCauHinhAsync(ct);

        // Số ngày mượn theo loại bạn đọc từ cấu hình thư viện
        var loaiBanDoc = phieu.BanDoc?.LoaiBanDoc ?? LoaiBanDoc.SinhVien;
        var soNgayMuon = loaiBanDoc switch
        {
            LoaiBanDoc.GiangVien => cauHinh.SoNgayMuonGiangVien,
            LoaiBanDoc.Khac => cauHinh.SoNgayMuonKhac,
            _ => cauHinh.SoNgayMuonSinhVien
        };

        var oldStatus = phieu.TrangThai;
        phieu.TrangThai = TrangThaiPhieuMuon.DangMuon;
        phieu.NgayMuon = today;                        // Do hệ thống ghi khi giao sách
        phieu.HanTra = today.AddDays(soNgayMuon);       // Do hệ thống ghi khi giao sách

        _db.PhieuMuonLichSus.Add(new PhieuMuonLichSu
        {
            MaPhieuMuon = phieu.MaPhieuMuon,
            TrangThaiCu = oldStatus,
            TrangThaiMoi = TrangThaiPhieuMuon.DangMuon,
            NgayThucHien = LibraryClock.LocalDateTime(),
            MaNguoiThucHien = actingMaTaiKhoan,
            GhiChu = $"Giao sách thành công. Hạn trả: {phieu.HanTra:dd/MM/yyyy} ({soNgayMuon} ngày)"
        });

        await _db.SaveChangesAsync(ct);
        await NotifySafelyAsync(() => _thongBao.NotifyReaderForLoanAsync(
            phieu.MaPhieuMuon, "Bạn đã nhận sách thành công",
            $"Thư viện đã bàn giao sách cho bạn. Hạn trả: {phieu.HanTra:dd/MM/yyyy}.",
            $"loan-delivered:{phieu.MaPhieuMuon}", $"/BanDoc/PhieuMuonCuaToi/Details/{phieu.MaPhieuMuon}", "loan", ct), "loan-delivered");
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> CancelLoanByStaffAsync(int maPhieuMuon, int actingMaTaiKhoan, string? lyDo = null, CancellationToken ct = default)
    {
        var phieu = await _db.PhieuMuons
            .Include(p => p.BanDoc)
            .FirstOrDefaultAsync(p => p.MaPhieuMuon == maPhieuMuon, ct);

        if (phieu is null)
            return ServiceResult.Fail("Không tìm thấy phiếu mượn.");

        // Chỉ hủy khi chưa giao sách (Chờ duyệt hoặc Đã duyệt nhưng chưa có NgayMuon)
        if (phieu.TrangThai != TrangThaiPhieuMuon.DaDuyet && phieu.TrangThai != TrangThaiPhieuMuon.ChoDuyet)
        {
            return ServiceResult.Fail($"Không thể hủy phiếu mượn ở trạng thái '{phieu.TrangThai.GetDisplayName()}'. Chỉ có thể hủy phiếu chưa giao sách.");
        }

        if (phieu.NgayMuon.HasValue)
        {
            return ServiceResult.Fail("Phiếu mượn đã được giao sách, không thể hủy.");
        }

        var oldStatus = phieu.TrangThai;
        phieu.TrangThai = TrangThaiPhieuMuon.DaHuy;
        phieu.NhanXetThuThu = lyDo ?? "Thủ thư hủy phiếu mượn (chưa nhận/giao sách).";

        _db.PhieuMuonLichSus.Add(new PhieuMuonLichSu
        {
            MaPhieuMuon = phieu.MaPhieuMuon,
            TrangThaiCu = oldStatus,
            TrangThaiMoi = TrangThaiPhieuMuon.DaHuy,
            NgayThucHien = LibraryClock.LocalDateTime(),
            MaNguoiThucHien = actingMaTaiKhoan,
            GhiChu = phieu.NhanXetThuThu
        });

        await _db.SaveChangesAsync(ct);
        await NotifySafelyAsync(() => _thongBao.NotifyReaderForLoanAsync(
            phieu.MaPhieuMuon, "Phiếu mượn đã bị hủy",
            $"Phiếu mượn của bạn đã bị hủy. Lý do: {phieu.NhanXetThuThu}",
            $"loan-staff-cancel:{phieu.MaPhieuMuon}", $"/BanDoc/PhieuMuonCuaToi/Details/{phieu.MaPhieuMuon}", "loan", ct), "loan-staff-cancel");
        return ServiceResult.Success();
    }

    public async Task<int> AutoCancelOverdueApprovedLoansAsync(CancellationToken ct = default)
    {
        // Ngày hết hạn là ngày duyệt + 7 ngày. Phiếu được hủy ngay từ 00:00 của ngày đó
        var today = LibraryClock.Today();
        var cutoff = today.AddDays(-LoanRules.UncollectedDays);

        // Serializable ngăn việc bàn giao sách xảy ra đồng thời với tác vụ tự hủy
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var overdueLoans = await _db.PhieuMuons
            .Include(p => p.BanDoc)
            .Where(p => p.TrangThai == TrangThaiPhieuMuon.DaDuyet
                     && !p.NgayMuon.HasValue
                     && (p.NgayXuLy ?? p.NgayDangKy) <= cutoff)
            .ToListAsync(ct);

        if (overdueLoans.Count == 0)
        {
            await tx.CommitAsync(ct);
            return 0;
        }

        var now = LibraryClock.LocalDateTime();
        foreach (var phieu in overdueLoans)
        {
            var oldStatus = phieu.TrangThai;
            phieu.TrangThai = TrangThaiPhieuMuon.DaHuy;
            phieu.NhanXetThuThu = "Hệ thống tự động hủy do đã hết 7 ngày kể từ khi duyệt mà bạn đọc chưa đến nhận sách.";

            _db.PhieuMuonLichSus.Add(new PhieuMuonLichSu
            {
                MaPhieuMuon = phieu.MaPhieuMuon,
                TrangThaiCu = oldStatus,
                TrangThaiMoi = TrangThaiPhieuMuon.DaHuy,
                NgayThucHien = now,
                MaNguoiThucHien = phieu.BanDoc != null ? phieu.BanDoc.MaTaiKhoan : 1,
                GhiChu = "Tự động hủy lúc 00:00 ngày hết hạn nhận sách (7 ngày sau khi duyệt)"
            });
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // Chỉ gửi thông báo sau khi giao dịch cập nhật trạng thái đã commit.
        foreach (var phieu in overdueLoans)
            await NotifySafelyAsync(() => _thongBao.NotifyReaderForLoanAsync(
                phieu.MaPhieuMuon, "Phiếu mượn tự động bị hủy",
                "Phiếu mượn đã được hệ thống tự động hủy vì bạn chưa đến nhận sách trong vòng 7 ngày kể từ ngày duyệt. Sách đã được giải phóng để phục vụ bạn đọc khác.",
                $"loan-auto-cancel:{phieu.MaPhieuMuon}", $"/BanDoc/PhieuMuonCuaToi/Details/{phieu.MaPhieuMuon}", "loan", ct), "loan-auto-cancel");
        return overdueLoans.Count;
    }
}
