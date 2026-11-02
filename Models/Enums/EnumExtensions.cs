// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng các enum và extension method để lấy DisplayName của enum, phục vụ cho việc hiển thị trong Razor.

using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

// ===================== Static helper =====================

public static class EnumExtensions
{
    // Returns the [Display(Name = "...")] text of an enum value, or its name when there is none.
    // Usage in Razor: @Model.TrangThai.GetDisplayName()
    public static string GetDisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        return member?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? value.ToString();
    }
}

// ===================== Enums =====================
// Stored in the database as strings (the enum member name, e.g. "ChoDuyet") - see AppDbContext.
// Member names have no diacritics/spaces, so the Vietnamese label lives in [Display].

public enum VaiTro
{
    [Display(Name = "Admin")] Admin,
    [Display(Name = "Thủ thư")] ThuThu,
    [Display(Name = "Bạn đọc")] BanDoc
}

public enum TrangThaiTaiKhoan
{
    [Display(Name = "Hoạt động")] HoatDong,
    [Display(Name = "Bị khóa")] BiKhoa
}

public enum TrangThaiTheLoai
{
    [Display(Name = "Hoạt động")] HoatDong,
    [Display(Name = "Ngừng hoạt động")] NgungHoatDong
}

public enum TrangThaiSach
{
    [Display(Name = "Đang phục vụ")] DangPhucVu,
    [Display(Name = "Tạm ngưng")] TamNgung,
    [Display(Name = "Ngừng phục vụ")] NgungPhucVu
}

public enum GioiTinh
{
    [Display(Name = "Nam")] Nam,
    [Display(Name = "Nữ")] Nu,
    [Display(Name = "Khác")] Khac
}

public enum LoaiBanDoc
{
    [Display(Name = "Sinh viên")] SinhVien,
    [Display(Name = "Giảng viên")] GiangVien,
    [Display(Name = "Khác")] Khac
}

public enum TrangThaiBanDoc
{
    [Display(Name = "Chờ duyệt")] ChoDuyet,
    [Display(Name = "Hoạt động")] HoatDong,
    [Display(Name = "Ngừng hoạt động")] NgungHoatDong,
    [Display(Name = "Từ chối")] TuChoi
}

public enum TrangThaiPhieuMuon
{
    [Display(Name = "Chờ duyệt")] ChoDuyet,
    [Display(Name = "Đã duyệt")] DaDuyet,
    [Display(Name = "Đang mượn")] DangMuon,
    [Display(Name = "Đã trả")] DaTra,
    [Display(Name = "Từ chối")] TuChoi,
    [Display(Name = "Đã hủy")] DaHuy
}

public enum TrangThaiGiaHan
{
    [Display(Name = "Chờ duyệt")] ChoDuyet,
    [Display(Name = "Đã duyệt")] DaDuyet,
    [Display(Name = "Từ chối")] TuChoi
}

public enum TinhTrangSach
{
    [Display(Name = "Bình thường")] BinhThuong,
    [Display(Name = "Hư hỏng")] HuHong,
    [Display(Name = "Mất")] Mat
}

public enum TrangThaiPhat
{
    [Display(Name = "Không phạt")] KhongPhat,
    [Display(Name = "Chưa nộp")] ChuaNop,
    [Display(Name = "Đã nộp")] DaNop
}
