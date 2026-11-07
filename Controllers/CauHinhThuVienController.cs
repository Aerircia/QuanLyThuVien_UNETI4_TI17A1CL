// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng controller để quản lý cấu hình thư viện

using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

[AuthorizeRole(VaiTro.Admin)]
public class CauHinhThuVienController : Controller
{
    private readonly ICauHinhThuVienService _service;

    public CauHinhThuVienController(ICauHinhThuVienService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var c = await _service.GetAsync();
        var vm = new CauHinhThuVienViewModel
        {
            SoNgayMuonSinhVien = c.SoNgayMuonSinhVien,
            SoNgayMuonGiangVien = c.SoNgayMuonGiangVien,
            SoNgayMuonKhac = c.SoNgayMuonKhac,
            SoPhieuToiDa = c.SoPhieuToiDa,
            SoLanGiaHanToiDa = c.SoLanGiaHanToiDa,
            SoNgayGiaHanToiDa = c.SoNgayGiaHanToiDa,
            TienPhatMoiNgay = c.TienPhatMoiNgay,
            PhanTramPhatHuHong = c.TyLePhatHuHong * 100m,
            PhanTramPhatMat = c.TyLePhatMat * 100m
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CauHinhThuVienViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var result = await _service.UpdateAsync(vm);
        if (!result.Succeeded)
        {
            result.AddTo(ModelState);
            return View(vm);
        }

        TempData["ThongBao"] = "Đã lưu cấu hình thư viện.";
        return RedirectToAction(nameof(Index));
    }
}
