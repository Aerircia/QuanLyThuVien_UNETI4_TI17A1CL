// Họ và tên: Nguyễn Hữu Tuấn
// Mã sinh viên: 23103100136
// Nội dung thực hiện: Xây dựng các ViewModels phục vụ Module 3 - Quản lý phiếu mượn và đăng ký mượn sách

using System.ComponentModel.DataAnnotations;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

public sealed class PhieuMuonRegisterViewModel
{
    // Thông tin sách
    public int MaSach { get; set; }
    public string TenSach { get; set; } = string.Empty;
    public string TacGia { get; set; } = string.Empty;
    public string? TheLoai { get; set; }
    public int NamXuatBan { get; set; }
    public decimal GiaBia { get; set; }
    public string? ViTriKe { get; set; }
    public string? NhaXuatBan { get; set; }
    public string? MoTa { get; set; }
    public string? ImgPath { get; set; }
    public int SoLuongTong { get; set; }
    public int SoLuongConLai { get; set; }

    // Thông tin bạn đọc
    public int MaBanDoc { get; set; }
    public string HoTenBanDoc { get; set; } = string.Empty;
    public string? MaSoThe { get; set; }
    public LoaiBanDoc LoaiBanDoc { get; set; }
    public DateOnly NgayHetHanThe { get; set; }
    public int SoPhieuDangXuLy { get; set; }
    public int SoPhieuToiDa { get; set; }
    public bool ThongTinBatBuocHopLe { get; set; }

    // Thông tin nhập
    [StringLength(100, ErrorMessage = "Ghi chú không được vượt quá 100 ký tự")]
    [Display(Name = "Ghi chú cho thủ thư (không bắt buộc)")]
    public string? GhiChu { get; set; }

    // Kết quả kiểm tra điều kiện mượn sách bằng EF Core/LINQ
    public bool DuDieuKienMuon { get; set; }
    public List<string> LyDoKhongDuDieuKien { get; set; } = new();
}

public sealed class PhieuMuonRegisterSuccessViewModel
{
    public int MaPhieuMuon { get; set; }
    public string TenSach { get; set; } = string.Empty;
    public string? ViTriKe { get; set; }
    public string TrangThai { get; set; } = "Chờ duyệt";
}

public sealed class PhieuMuonItemViewModel
{
    public int MaPhieuMuon { get; set; }
    public int MaBanDoc { get; set; }
    public string HoTenBanDoc { get; set; } = string.Empty;
    public string? MaSoThe { get; set; }
    public int MaSach { get; set; }
    public string TenSach { get; set; } = string.Empty;
    public string TacGia { get; set; } = string.Empty;
    public string? TheLoai { get; set; }
    public string? ViTriKe { get; set; }

    public DateOnly NgayDangKy { get; set; }
    public DateOnly? NgayMuon { get; set; }
    public DateOnly? HanTra { get; set; }
    public TrangThaiPhieuMuon TrangThai { get; set; }
    public DateOnly? NgayXuLy { get; set; }
    public string? NhanXetThuThu { get; set; }
    public string? GhiChu { get; set; }

    // Xác định quá hạn bằng LINQ: TrangThai == DangMuon và HanTra < Ngày hiện tại
    public bool IsOverdue { get; set; }
    public int SoNgayQuaHan { get; set; }
    public int SoNgayConLai { get; set; }

    // Cho phép hủy: Chờ duyệt hoặc Đã duyệt nhưng chưa nhận sách (NgayMuon == null)
    public bool CanCancel { get; set; }

    // Thông tin gia hạn & trả sách
    public int SoLanGiaHan { get; set; }
    public decimal? TienPhat { get; set; }
    public TrangThaiPhat? TrangThaiPhat { get; set; }
}

public sealed class PhieuMuonDetailViewModel
{
    public int MaPhieuMuon { get; set; }

    // Sách
    public int MaSach { get; set; }
    public string TenSach { get; set; } = string.Empty;
    public string TacGia { get; set; } = string.Empty;
    public string? TheLoai { get; set; }
    public string? NhaXuatBan { get; set; }
    public int NamXuatBan { get; set; }
    public decimal GiaBia { get; set; }
    public string? ViTriKe { get; set; }

    // Bạn đọc
    public int MaBanDoc { get; set; }
    public string HoTenBanDoc { get; set; } = string.Empty;
    public string? MaSoThe { get; set; }
    public LoaiBanDoc LoaiBanDoc { get; set; }
    public string? SoDienThoai { get; set; }
    public string? Email { get; set; }

    // Phiếu mượn
    public DateOnly NgayDangKy { get; set; }
    public DateOnly? NgayMuon { get; set; }
    public DateOnly? HanTra { get; set; }
    public TrangThaiPhieuMuon TrangThai { get; set; }
    public DateOnly? NgayXuLy { get; set; }
    public string? NhanXetThuThu { get; set; }
    public string? GhiChu { get; set; }

    // Quá hạn
    public bool IsOverdue { get; set; }
    public int SoNgayQuaHan { get; set; }
    public int SoNgayConLai { get; set; }

    // Quyền thao tác
    public bool CanCancel { get; set; }

    // Lịch sử gia hạn
    public List<GiaHanMuonItemViewModel> LichSuGiaHan { get; set; } = new();

    // Thông tin trả sách
    public TraSachItemViewModel? ThongTinTraSach { get; set; }

    // Lịch sử trạng thái phiếu
    public List<PhieuMuonLichSuItemViewModel> LichSuTrangThai { get; set; } = new();
}

public sealed class GiaHanMuonItemViewModel
{
    public int MaGiaHan { get; set; }
    public DateOnly HanTraCu { get; set; }
    public DateOnly HanTraMoi { get; set; }
    public string? LyDo { get; set; }
    public DateOnly NgayYeuCau { get; set; }
    public TrangThaiGiaHan TrangThai { get; set; }
    public DateOnly? NgayXuLy { get; set; }
    public string? NhanXet { get; set; }
}

public sealed class TraSachItemViewModel
{
    public int MaTra { get; set; }
    public DateOnly NgayTra { get; set; }
    public TinhTrangSach TinhTrangSach { get; set; }
    public int SoNgayTre { get; set; }
    public decimal TienPhat { get; set; }
    public TrangThaiPhat TrangThaiPhat { get; set; }
    public string? GhiChu { get; set; }
    public string? NguoiNhan { get; set; }
}

public sealed class PhieuMuonLichSuItemViewModel
{
    public int MaLichSu { get; set; }
    public TrangThaiPhieuMuon? TrangThaiCu { get; set; }
    public TrangThaiPhieuMuon TrangThaiMoi { get; set; }
    public DateTime NgayThucHien { get; set; }
    public string? NguoiThucHien { get; set; }
    public string? GhiChu { get; set; }
}

public sealed class MyLoansViewModel
{
    private int? _tongSoPhieu;
    private int? _soPhieuChoDuyet;
    private int? _soPhieuDaDuyet;
    private int? _soPhieuDangMuon;
    private int? _soPhieuQuaHan;
    private int? _soPhieuDaTra;

    public List<PhieuMuonItemViewModel> Loans { get; set; } = new();
    public string Filter { get; set; } = "all";

    public int TongSoPhieu
    {
        get => _tongSoPhieu ?? Loans.Count;
        set => _tongSoPhieu = value;
    }

    public int SoPhieuChoDuyet
    {
        get => _soPhieuChoDuyet ?? Loans.Count(l => l.TrangThai == TrangThaiPhieuMuon.ChoDuyet);
        set => _soPhieuChoDuyet = value;
    }

    public int SoPhieuDaDuyet
    {
        get => _soPhieuDaDuyet ?? Loans.Count(l => l.TrangThai == TrangThaiPhieuMuon.DaDuyet);
        set => _soPhieuDaDuyet = value;
    }

    public int SoPhieuDangMuon
    {
        get => _soPhieuDangMuon ?? Loans.Count(l => l.TrangThai == TrangThaiPhieuMuon.DangMuon);
        set => _soPhieuDangMuon = value;
    }

    public int SoPhieuQuaHan
    {
        get => _soPhieuQuaHan ?? Loans.Count(l => l.IsOverdue);
        set => _soPhieuQuaHan = value;
    }

    public int SoPhieuDaTra
    {
        get => _soPhieuDaTra ?? Loans.Count(l => l.TrangThai == TrangThaiPhieuMuon.DaTra);
        set => _soPhieuDaTra = value;
    }

    public decimal TongTienPhatChuaNop { get; set; }
}

public sealed class FineItemViewModel
{
    public int MaTra { get; set; }
    public int MaPhieuMuon { get; set; }
    public string TenSach { get; set; } = string.Empty;
    public DateOnly NgayTra { get; set; }
    public TinhTrangSach TinhTrangSach { get; set; }
    public int SoNgayTre { get; set; }
    public decimal TienPhat { get; set; }
    public TrangThaiPhat TrangThaiPhat { get; set; }
    public string? GhiChu { get; set; }
}

public sealed class MyFinesViewModel
{
    public List<FineItemViewModel> Fines { get; set; } = new();
    public decimal TongTienPhat => Fines.Sum(f => f.TienPhat);
    public decimal DaNop => Fines.Where(f => f.TrangThaiPhat == TrangThaiPhat.DaNop).Sum(f => f.TienPhat);
    public decimal ChuaNop => Fines.Where(f => f.TrangThaiPhat == TrangThaiPhat.ChuaNop).Sum(f => f.TienPhat);
    public int SoKhoanChuaNop => Fines.Count(f => f.TrangThaiPhat == TrangThaiPhat.ChuaNop);
}

public sealed record PhieuMuonManageFilter(
    string? Search,
    TrangThaiPhieuMuon? TrangThai,
    int? MaTheLoai,
    bool? OverdueOnly,
    int Page = 1,
    int PageSize = PagingDefaults.DefaultPageSize);

public sealed class PhieuMuonManageListViewModel
{
    public string? Search { get; set; }
    public TrangThaiPhieuMuon? TrangThai { get; set; }
    public int? MaTheLoai { get; set; }
    public bool? OverdueOnly { get; set; }
    public int PageSize { get; set; } = PagingDefaults.DefaultPageSize;

    public PagedResult<PhieuMuonItemViewModel> Result { get; set; } = default!;
    public PagerViewModel Pager { get; set; } = default!;

    public int ChoDuyetCount { get; set; }
    public int DaDuyetCount { get; set; }
    public int DangMuonCount { get; set; }
    public int OverdueCount { get; set; }
}

public sealed class LoanEligibilityResult
{
    public bool CanBorrow { get; set; }
    public int SoLuongConLai { get; set; }
    public List<string> Errors { get; set; } = new();
}
