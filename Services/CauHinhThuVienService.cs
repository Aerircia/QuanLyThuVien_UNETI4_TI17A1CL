// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Thực hiện CauHinhThuVienService

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services;

public class CauHinhThuVienService : ICauHinhThuVienService
{
    private const string CacheKey = "CauHinhThuVien";
    private const int MaCauHinh = 1;

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public CauHinhThuVienService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<CauHinhThuVien> GetAsync()
    {
        var cached = await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            return await _db.Set<CauHinhThuVien>()
                .AsNoTracking()
                .FirstAsync(c => c.MaCauHinh == MaCauHinh);
        });
        return cached!;
    }

    public async Task<int> GetSoNgayMuonAsync(LoaiBanDoc loai)
    {
        var c = await GetAsync();
        return loai switch
        {
            LoaiBanDoc.SinhVien => c.SoNgayMuonSinhVien,
            LoaiBanDoc.GiangVien => c.SoNgayMuonGiangVien,
            _ => c.SoNgayMuonKhac
        };
    }

    public async Task<ServiceResult> UpdateAsync(CauHinhThuVienViewModel vm)
    {
        var entity = await _db.Set<CauHinhThuVien>().FirstOrDefaultAsync(c => c.MaCauHinh == MaCauHinh);
        if (entity == null)
            return ServiceResult.Fail("Không tìm thấy cấu hình thư viện.");

        entity.SoNgayMuonSinhVien = vm.SoNgayMuonSinhVien;
        entity.SoNgayMuonGiangVien = vm.SoNgayMuonGiangVien;
        entity.SoNgayMuonKhac = vm.SoNgayMuonKhac;
        entity.SoPhieuToiDa = vm.SoPhieuToiDa;
        entity.SoLanGiaHanToiDa = vm.SoLanGiaHanToiDa;
        entity.SoNgayGiaHanToiDa = vm.SoNgayGiaHanToiDa;
        entity.TienPhatMoiNgay = vm.TienPhatMoiNgay;
        // UI uses percent (0-100); stored as a fraction (0-1)
        entity.TyLePhatHuHong = vm.PhanTramPhatHuHong / 100m;
        entity.TyLePhatMat = vm.PhanTramPhatMat / 100m;

        await _db.SaveChangesAsync();
        _cache.Remove(CacheKey);
        return ServiceResult.Success();
    }
}
