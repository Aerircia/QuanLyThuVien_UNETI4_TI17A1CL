// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng interface để quản lý cấu hình thư viện

using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

/// <summary>Contract: library rules (loan days, limits, fine rates). Single row MaCauHinh = 1.</summary>
public interface ICauHinhThuVienService
{
    /// <summary>Current settings (cached, read-only snapshot: do not modify the returned object).</summary>
    Task<CauHinhThuVien> GetAsync();

    /// <summary>Loan days for a reader type (used to compute HanTra at hand-over).</summary>
    Task<int> GetSoNgayMuonAsync(LoaiBanDoc loai);

    /// <summary>Saves new settings and clears the cache. Returns a ServiceResult (errors go to ModelState via AddTo).</summary>
    Task<ServiceResult> UpdateAsync(CauHinhThuVienViewModel vm);
}
