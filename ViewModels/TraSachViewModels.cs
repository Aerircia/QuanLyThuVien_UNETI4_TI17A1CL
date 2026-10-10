// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: ViewModel cho nhận trả sách: danh sách và form nhận trả.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

public class TraSachListViewModel
{
    public string? Search { get; set; }
    public TinhTrangSach? TinhTrang { get; set; }
    public TrangThaiPhat? TrangThaiPhat { get; set; }
    public int PageSize { get; set; } = PagingDefaults.DefaultPageSize;

    public PagedResult<TraSachListItem> Result { get; set; } = default!;
    public PagerViewModel Pager { get; set; } = default!;
}

// chọn tình trạng sách và ghi chú
public class TraSachCreateViewModel
{
    [Display(Name = "Tình trạng sách")]
    public TinhTrangSach TinhTrang { get; set; } = TinhTrangSach.BinhThuong;

    [StringLength(100, ErrorMessage = "Ghi chú tối đa {1} ký tự.")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    [BindNever, ValidateNever]
    public TraSachPreview? Preview { get; set; }
}