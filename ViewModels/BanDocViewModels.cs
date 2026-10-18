// Họ và tên: Nguyễn Hữu Tuấn
// Mã sinh viên: 23103100136
// Nội dung thực hiện: Xây dựng các ViewModels phục vụ Module 3 - Quản lý bạn đọc

using System.ComponentModel.DataAnnotations;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

public sealed record BanDocFilter(
    string? Search,
    LoaiBanDoc? LoaiBanDoc,
    TrangThaiBanDoc? TrangThai,
    int Page = 1,
    int PageSize = PagingDefaults.DefaultPageSize);

public sealed class BanDocListItem
{
    public int MaBanDoc { get; set; }
    public int MaTaiKhoan { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string? MaSoThe { get; set; }
    public LoaiBanDoc LoaiBanDoc { get; set; }
    public DateOnly NgayCapThe { get; set; }
    public DateOnly NgayHetHanThe { get; set; }
    public TrangThaiBanDoc TrangThai { get; set; }
    public string? SoDienThoai { get; set; }
    public string? Email { get; set; }
    public string? TenDangNhap { get; set; }
    public int SoPhieuDangMuon { get; set; }
    public int SoPhieuQuaHan { get; set; }
    public decimal TongTienPhatChuaNop { get; set; }

    public bool TheHetHan => NgayHetHanThe < DateOnly.FromDateTime(DateTime.Today);
}

public sealed class BanDocListViewModel
{
    public string? Search { get; set; }
    public LoaiBanDoc? LoaiBanDoc { get; set; }
    public TrangThaiBanDoc? TrangThai { get; set; }
    public int PageSize { get; set; } = PagingDefaults.DefaultPageSize;
    public PagedResult<BanDocListItem> Result { get; set; } = default!;
    public PagerViewModel Pager { get; set; } = default!;
}

public sealed class BanDocDetail
{
    public int MaBanDoc { get; set; }
    public int MaTaiKhoan { get; set; }
    public string? TenDangNhap { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public DateOnly NgaySinh { get; set; }
    public GioiTinh GioiTinh { get; set; }
    public string? SoDienThoai { get; set; }
    public string? Email { get; set; }
    public string? DiaChi { get; set; }
    public string? MaSoThe { get; set; }
    public LoaiBanDoc LoaiBanDoc { get; set; }
    public DateOnly NgayCapThe { get; set; }
    public DateOnly NgayHetHanThe { get; set; }
    public TrangThaiBanDoc TrangThai { get; set; }
    public string? ImgPath { get; set; }

    public int SoPhieuDangMuon { get; set; }
    public int SoPhieuQuaHan { get; set; }
    public decimal TongTienPhatChuaNop { get; set; }
    public bool TheHetHan => NgayHetHanThe < DateOnly.FromDateTime(DateTime.Today);

    public List<PhieuMuonItemViewModel> PhieuMuons { get; set; } = new();
    public List<FineItemViewModel> KhoanPhats { get; set; } = new();
}

public sealed class BanDocCreateViewModel : IValidatableObject
{
    [Display(Name = "Tài khoản liên kết")]
    public int? MaTaiKhoan { get; set; }

    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [StringLength(255, ErrorMessage = "Họ tên không được vượt quá 255 ký tự")]
    [Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày sinh")]
    public DateOnly NgaySinh { get; set; } = new(2003, 1, 1);

    [Display(Name = "Giới tính")]
    public GioiTinh GioiTinh { get; set; } = GioiTinh.Nam;

    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ (10-11 số, bắt đầu bằng 0)")]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoai { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    [StringLength(255)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
    [StringLength(255)]
    [Display(Name = "Địa chỉ")]
    public string DiaChi { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Mã số thẻ (để trống nếu tạo tự động)")]
    public string? MaSoThe { get; set; }

    [Display(Name = "Loại bạn đọc")]
    public LoaiBanDoc LoaiBanDoc { get; set; } = LoaiBanDoc.SinhVien;

    [Required(ErrorMessage = "Ngày cấp thẻ là bắt buộc")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày cấp thẻ")]
    public DateOnly NgayCapThe { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "Ngày hết hạn thẻ là bắt buộc")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày hết hạn thẻ")]
    public DateOnly NgayHetHanThe { get; set; } = DateOnly.FromDateTime(DateTime.Today).AddYears(1);

    [Display(Name = "Trạng thái")]
    public TrangThaiBanDoc TrangThai { get; set; } = TrangThaiBanDoc.HoatDong;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var homNay = DateOnly.FromDateTime(DateTime.Today);
        if (NgaySinh < new DateOnly(1900, 1, 1) || NgaySinh >= homNay)
            yield return new ValidationResult("Ngày sinh không hợp lệ.", new[] { nameof(NgaySinh) });

        if (NgayHetHanThe <= NgayCapThe)
            yield return new ValidationResult("Ngày hết hạn thẻ phải sau ngày cấp thẻ.", new[] { nameof(NgayHetHanThe) });
    }
}

public sealed class BanDocEditViewModel : IValidatableObject
{
    public int MaBanDoc { get; set; }
    public int MaTaiKhoan { get; set; }
    public string? TenDangNhap { get; set; }

    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [StringLength(255, ErrorMessage = "Họ tên không được vượt quá 255 ký tự")]
    [Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày sinh")]
    public DateOnly NgaySinh { get; set; }

    [Display(Name = "Giới tính")]
    public GioiTinh GioiTinh { get; set; }

    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ (10-11 số, bắt đầu bằng 0)")]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoai { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    [StringLength(255)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
    [StringLength(255)]
    [Display(Name = "Địa chỉ")]
    public string DiaChi { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Mã số thẻ")]
    public string? MaSoThe { get; set; }

    [Display(Name = "Loại bạn đọc")]
    public LoaiBanDoc LoaiBanDoc { get; set; }

    [Required(ErrorMessage = "Ngày cấp thẻ là bắt buộc")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày cấp thẻ")]
    public DateOnly NgayCapThe { get; set; }

    [Required(ErrorMessage = "Ngày hết hạn thẻ là bắt buộc")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày hết hạn thẻ")]
    public DateOnly NgayHetHanThe { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiBanDoc TrangThai { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var homNay = DateOnly.FromDateTime(DateTime.Today);
        if (NgaySinh < new DateOnly(1900, 1, 1) || NgaySinh >= homNay)
            yield return new ValidationResult("Ngày sinh không hợp lệ.", new[] { nameof(NgaySinh) });

        if (NgayHetHanThe <= NgayCapThe)
            yield return new ValidationResult("Ngày hết hạn thẻ phải sau ngày cấp thẻ.", new[] { nameof(NgayHetHanThe) });
    }
}

public sealed class BanDocProfileViewModel
{
    public int MaBanDoc { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string? TenDangNhap { get; set; }
    public DateOnly NgaySinh { get; set; }
    public GioiTinh GioiTinh { get; set; }
    public string? SoDienThoai { get; set; }
    public string? Email { get; set; }
    public string? DiaChi { get; set; }
    public string? MaSoThe { get; set; }
    public LoaiBanDoc LoaiBanDoc { get; set; }
    public DateOnly NgayCapThe { get; set; }
    public DateOnly NgayHetHanThe { get; set; }
    public TrangThaiBanDoc TrangThai { get; set; }

    public int SoPhieuDangMuon { get; set; }
    public int SoPhieuQuaHan { get; set; }
    public decimal TongTienPhatChuaNop { get; set; }
    public bool TheHetHan => NgayHetHanThe < DateOnly.FromDateTime(DateTime.Today);
}

public sealed class BanDocEditProfileViewModel : IValidatableObject
{
    public int MaBanDoc { get; set; }

    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [StringLength(255, ErrorMessage = "Họ tên không được vượt quá 255 ký tự")]
    [Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày sinh")]
    public DateOnly NgaySinh { get; set; }

    [Display(Name = "Giới tính")]
    public GioiTinh GioiTinh { get; set; }

    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ (10-11 số, bắt đầu bằng 0)")]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoai { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    [StringLength(255)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
    [StringLength(255)]
    [Display(Name = "Địa chỉ")]
    public string DiaChi { get; set; } = string.Empty;

    // Các thông tin chỉ xem, không cho bạn đọc tự sửa
    public string? MaSoThe { get; set; }
    public LoaiBanDoc LoaiBanDoc { get; set; }
    public DateOnly NgayCapThe { get; set; }
    public DateOnly NgayHetHanThe { get; set; }
    public TrangThaiBanDoc TrangThai { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var homNay = DateOnly.FromDateTime(DateTime.Today);
        if (NgaySinh < new DateOnly(1900, 1, 1) || NgaySinh >= homNay)
            yield return new ValidationResult("Ngày sinh không hợp lệ.", new[] { nameof(NgaySinh) });
    }
}

public sealed record TaiKhoanLookupItem(int MaTaiKhoan, string TenDangNhap, string HoTen, string Email);
