// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: ViewModel cho danh sách quản lý phiếu mượn

using Microsoft.AspNetCore.Mvc.Rendering;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

public sealed class QuanLyPhieuMuonIndexViewModel
{
    public string? Search { get; init; }
    public int? MaTheLoai { get; init; }
    public TrangThaiPhieuMuon? TrangThai { get; init; }
    public DateOnly? TuNgay { get; init; }
    public DateOnly? DenNgay { get; init; }
    public bool QuaHan { get; init; }
    public string SapXep { get; init; } = "ngay_giam";
    public int PageSize { get; init; } = PagingDefaults.DefaultPageSize;

    public PagedResult<PhieuMuonListItem> Result { get; init; } = null!;
    public IReadOnlyList<SelectListItem> TheLoaiOptions { get; init; } = Array.Empty<SelectListItem>();
    public PagerViewModel Pager { get; init; } = null!;
}
