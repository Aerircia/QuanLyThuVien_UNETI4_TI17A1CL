// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: ViewModel cho gia hạn, danh sách và xử lý

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

//Form gửi yêu cầu gia hạn
public class GiaHanMuonCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn hạn trả mới.")]
    [DataType(DataType.Date)]
    [Display(Name = "Hạn trả mới")]
    public DateOnly? HanTraMoi { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do gia hạn.")]
    [StringLength(100, ErrorMessage = "Lý do tối đa {1} ký tự.")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Lý do gia hạn")]
    public string? LyDo { get; set; }

    [BindNever, ValidateNever]
    public GiaHanCreateInfo? Info { get; set; }
}

public class GiaHanMuonListViewModel
{
    public string? Search { get; set; }
    public TrangThaiGiaHan? TrangThai { get; set; }
    public int PageSize { get; set; } = PagingDefaults.DefaultPageSize;

    public PagedResult<GiaHanMuonListItem> Result { get; set; } = default!;
    public PagerViewModel Pager { get; set; } = default!;
}

public class GiaHanMuonProcessViewModel
{
    [StringLength(100, ErrorMessage = "Nhận xét tối đa {1} ký tự.")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Nhận xét của Thủ thư")]
    public string? NhanXet { get; set; }

    [BindNever, ValidateNever]
    public GiaHanMuonDetail? Detail { get; set; }
}