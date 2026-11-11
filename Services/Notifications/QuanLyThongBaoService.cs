// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: Service quản lý thông báo của Admin/Thủ thư: xem mọi thông báo, ẩn/hiện, gửi thông báo chung.

using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Notifications;

public sealed class QuanLyThongBaoService : IQuanLyThongBaoService
{
    private readonly AppDbContext _db;

    public QuanLyThongBaoService(AppDbContext db)
    {
        _db = db;
    }

    private static IQueryable<QuanLyThongBaoItem> ToItem(IQueryable<ThongBao> q) =>
        q.Select(x => new QuanLyThongBaoItem(
            x.MaThongBao, x.LoaiThongBao, x.MucDo, x.TieuDe, x.NoiDung,
            x.DuongDan, x.DaDoc, x.DangHienThi, x.NgayTao, x.TaiKhoan!.HoTen));

    public Task<PagedResult<QuanLyThongBaoItem>> GetQuanLyAsync(QuanLyThongBaoFilter f, CancellationToken ct = default)
    {
        var q = _db.ThongBaos.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(f.Loai)) q = q.Where(x => x.LoaiThongBao == f.Loai);
        if (f.DangHienThi.HasValue) q = q.Where(x => x.DangHienThi == f.DangHienThi.Value);
        if (f.NhomNhan == "BanDoc") q = q.Where(x => x.TaiKhoan!.VaiTro == VaiTro.BanDoc);
        else if (f.NhomNhan == "NhanVien") q = q.Where(x => x.TaiKhoan!.VaiTro != VaiTro.BanDoc);
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim();
            q = q.Where(x => x.TieuDe.Contains(s) || x.NoiDung.Contains(s) || x.TaiKhoan!.HoTen.Contains(s));
        }

        var ordered = ToItem(q.OrderByDescending(x => x.NgayTao).ThenByDescending(x => x.MaThongBao));
        return PagedResult<QuanLyThongBaoItem>.CreateAsync(ordered, f.Page, f.PageSize, ct);
    }

    public async Task<ServiceResult> DoiHienThiAsync(int maThongBao, bool hienThi, CancellationToken ct = default)
    {
        var tb = await _db.ThongBaos.FindAsync(new object[] { maThongBao }, ct);
        if (tb is null) return ServiceResult.Fail("Không tìm thấy thông báo.");
        tb.DangHienThi = hienThi;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<int>> GuiThongBaoChungAsync(GuiThongBaoInput input, CancellationToken ct = default)
    {
        var tieuDe = input.TieuDe?.Trim() ?? "";
        var noiDung = input.NoiDung?.Trim() ?? "";
        if (tieuDe.Length == 0) return ServiceResult<int>.Error(nameof(input.TieuDe), "Vui lòng nhập tiêu đề.");
        if (tieuDe.Length > 120) return ServiceResult<int>.Error(nameof(input.TieuDe), "Tiêu đề tối đa 120 ký tự.");
        if (noiDung.Length == 0) return ServiceResult<int>.Error(nameof(input.NoiDung), "Vui lòng nhập nội dung.");
        if (noiDung.Length > 1000) return ServiceResult<int>.Error(nameof(input.NoiDung), "Nội dung tối đa 1000 ký tự.");
        if (!ThongBaoHienThi.CacMucDo.Contains(input.MucDo)) return ServiceResult<int>.Error(nameof(input.MucDo), "Mức độ không hợp lệ.");

        var tk = _db.TaiKhoans.AsNoTracking().Where(x => x.TrangThai == TrangThaiTaiKhoan.HoatDong);
        tk = input.NhomNhan switch
        {
            "BanDoc" => tk.Where(x => x.VaiTro == VaiTro.BanDoc),
            "NhanVien" => tk.Where(x => x.VaiTro != VaiTro.BanDoc),
            "TatCa" => tk,
            _ => tk.Where(x => false)
        };

        var ids = await (from t in tk
                         join c in _db.CaiDatThongBaos.AsNoTracking() on t.MaTaiKhoan equals c.MaTaiKhoan into g
                         from c in g.DefaultIfEmpty()
                         where c == null || (c.BatThongBaoWeb && c.NhomHeThong)
                         select t.MaTaiKhoan).ToListAsync(ct);
        if (ids.Count == 0) return ServiceResult<int>.Error(nameof(input.NhomNhan), "Không có người nhận phù hợp.");

        var khoa = "HeThong_" + Guid.NewGuid().ToString("N") + "_Broadcast";
        var now = DateTime.UtcNow;
        foreach (var id in ids)
        {
            _db.ThongBaos.Add(new ThongBao
            {
                MaTaiKhoan = id,
                TieuDe = tieuDe,
                NoiDung = noiDung,
                LoaiThongBao = "sys",
                MucDo = input.MucDo,
                Icon = "bell",
                LoaiDoiTuong = "HeThong",
                MaDoiTuong = null,
                KhoaSuKien = khoa,
                NgayTao = now,
                DaDoc = false,
                DangHienThi = true
            });
        }
        await _db.SaveChangesAsync(ct);
        return ServiceResult<int>.Ok(ids.Count);
    }
}