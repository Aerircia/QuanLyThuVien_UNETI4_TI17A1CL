// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: Service truy vấn, lọc, sắp xếp, phân trang và xử lý nghiệp vụ phiếu mượn

using System.Data;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services;

public sealed class PhieuMuonService : IPhieuMuonService
{
    private readonly AppDbContext _db;
    private readonly ICauHinhThuVienService _cauHinh;
    private readonly TimeProvider _time;

    public PhieuMuonService(
        AppDbContext db,
        ICauHinhThuVienService cauHinh,
        TimeProvider time)
    {
        _db = db;
        _cauHinh = cauHinh;
        _time = time;
    }

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    public async Task<IReadOnlyList<TheLoaiOption>> GetTheLoaiOptionsAsync(CancellationToken ct = default)
    {
        return await _db.TheLoais
            .AsNoTracking()
            .OrderBy(t => t.TenTheLoai)
            .Select(t => new TheLoaiOption(t.MaTheLoai, t.TenTheLoai))
            .ToListAsync(ct);
    }

    public async Task<PagedResult<PhieuMuonListItem>> GetListAsync(
        PhieuMuonFilter filter, CancellationToken ct = default)
    {
        var query = _db.PhieuMuons.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var keyword = filter.Search.Trim();
            query = query.Where(p =>
                (p.BanDoc != null && p.BanDoc.HoTen.Contains(keyword)) ||
                (p.Sach != null && p.Sach.TenSach.Contains(keyword)));
        }

        if (filter.MaTheLoai.HasValue)
            query = query.Where(p => p.Sach != null && p.Sach.MaTheLoai == filter.MaTheLoai.Value);

        if (filter.TrangThai.HasValue)
            query = query.Where(p => p.TrangThai == filter.TrangThai.Value);

        if (filter.TuNgay.HasValue)
            query = query.Where(p => p.NgayDangKy >= filter.TuNgay.Value);

        if (filter.DenNgay.HasValue)
        {
            // Dùng mốc loại trừ để không bỏ sót ngày DenNgay
            var denNgayExclusive = filter.DenNgay.Value.AddDays(1);
            query = query.Where(p => p.NgayDangKy < denNgayExclusive);
        }

        if (filter.QuaHan)
        {
            var today = Today;
            query = query.Where(p =>
                p.TrangThai == TrangThaiPhieuMuon.DangMuon &&
                p.HanTra.HasValue && p.HanTra.Value < today);
        }

        // Luôn sắp xếp ổn định bằng MaPhieuMuon để phân trang không nhảy dòng
        var ordered = filter.SapXep switch
        {
            "ban_doc" => query.OrderBy(p => p.BanDoc!.HoTen)
                .ThenBy(p => p.MaPhieuMuon),
            "ngay_tang" => query.OrderBy(p => p.NgayDangKy)
                .ThenBy(p => p.MaPhieuMuon),
            "han_tang" => query.OrderBy(p => p.HanTra)
                .ThenBy(p => p.MaPhieuMuon),
            "han_giam" => query.OrderByDescending(p => p.HanTra)
                .ThenByDescending(p => p.MaPhieuMuon),
            _ => query.OrderByDescending(p => p.NgayDangKy)
                .ThenByDescending(p => p.MaPhieuMuon)
        };

        var projected = ordered.Select(p => new PhieuMuonListItem(
            p.MaPhieuMuon,
            p.BanDoc!.HoTen,
            p.BanDoc.MaSoThe,
            p.Sach!.TenSach,
            p.Sach.TheLoai!.TenTheLoai,
            p.NgayDangKy,
            p.NgayMuon,
            p.HanTra,
            p.TrangThai));

        // PagedResult tự Count, clamp Page và Skip/Take ở SQL
        return await PagedResult<PhieuMuonListItem>.CreateAsync(
            projected, filter.Page, filter.PageSize, ct);
    }

    public async Task<PhieuMuonDetail?> GetDetailsAsync(int id, CancellationToken ct = default)
    {
        var phieu = await _db.PhieuMuons
            .AsNoTracking()
            .Include(p => p.BanDoc)
            .Include(p => p.Sach).ThenInclude(s => s!.TheLoai)
            .FirstOrDefaultAsync(p => p.MaPhieuMuon == id, ct);

        if (phieu?.BanDoc is null || phieu.Sach?.TheLoai is null)
            return null;

        var soLuongConLai = await GetSoLuongConLaiAsync(phieu.MaSach, ct);
        var canhBao = phieu.TrangThai == TrangThaiPhieuMuon.ChoDuyet
            ? await KiemTraDieuKienDuyetAsync(phieu, ct)
            : new List<string>();

        var lichSu = await _db.PhieuMuonLichSus
            .AsNoTracking()
            .Where(h => h.MaPhieuMuon == id)
            .OrderByDescending(h => h.NgayThucHien)
            .ThenByDescending(h => h.MaLichSu)
            .Select(h => new PhieuMuonHistoryItem(
                h.TrangThaiCu,
                h.TrangThaiMoi,
                h.NgayThucHien,
                h.NguoiThucHien != null ? h.NguoiThucHien.HoTen : "Không xác định",
                h.GhiChu))
            .ToListAsync(ct);

        return new PhieuMuonDetail(
            phieu.MaPhieuMuon,
            phieu.MaBanDoc,
            phieu.BanDoc.HoTen,
            phieu.BanDoc.MaSoThe,
            phieu.MaSach,
            phieu.Sach.TenSach,
            phieu.Sach.TheLoai.TenTheLoai,
            phieu.Sach.SoLuongTong,
            soLuongConLai,
            phieu.NgayDangKy,
            phieu.NgayMuon,
            phieu.HanTra,
            phieu.GhiChu,
            phieu.TrangThai,
            phieu.NgayXuLy,
            phieu.NhanXetThuThu,
            canhBao,
            lichSu);
    }

    public async Task<ServiceResult> ProcessAsync(
        ProcessPhieuMuonInput input, int maNguoiThucHien, CancellationToken ct = default)
    {
        var nhanXet = string.IsNullOrWhiteSpace(input.NhanXetThuThu)
            ? null
            : input.NhanXetThuThu.Trim();

        if (nhanXet is { Length: > 100 })
            return ServiceResult.Fail(nameof(input.NhanXetThuThu), "Nhận xét thủ thư tối đa 100 ký tự.");

        if (!input.Duyet && nhanXet is null)
            return ServiceResult.Fail(nameof(input.NhanXetThuThu), "Vui lòng nhập nhận xét khi từ chối phiếu.");

        // Serializable giúp hạn chế hai thủ thư cùng duyệt vượt số lượng còn lại
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var phieu = await _db.PhieuMuons
            .Include(p => p.BanDoc)
            .Include(p => p.Sach)
            .FirstOrDefaultAsync(p => p.MaPhieuMuon == input.MaPhieuMuon, ct);

        if (phieu is null)
            return ServiceResult.Fail("Không tìm thấy phiếu mượn.");

        var trangThaiMoi = input.Duyet
            ? TrangThaiPhieuMuon.DaDuyet
            : TrangThaiPhieuMuon.TuChoi;

        if (!ChoPhepChuyenTrangThai(phieu.TrangThai, trangThaiMoi))
            return ServiceResult.Fail("Chỉ phiếu Chờ duyệt mới được duyệt hoặc từ chối.");

        if (input.Duyet)
        {
            var canhBao = await KiemTraDieuKienDuyetAsync(phieu, ct);
            if (canhBao.Count > 0)
            {
                var failed = new ServiceResult();
                foreach (var message in canhBao)
                    failed.AddError(string.Empty, message);
                return failed;
            }
        }

        var trangThaiCu = phieu.TrangThai;
        phieu.TrangThai = trangThaiMoi;
        phieu.NgayXuLy = Today;
        phieu.NhanXetThuThu = nhanXet;
        ThemLichSu(phieu.MaPhieuMuon, trangThaiCu, trangThaiMoi, maNguoiThucHien,
            nhanXet ?? (input.Duyet ? "Thủ thư duyệt phiếu." : "Thủ thư từ chối phiếu."));

        try
        {
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Fail("Không thể lưu kết quả xử lý phiếu. Hãy kiểm tra dữ liệu và thử lại.");
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> HandOverAsync(
        int id, int maNguoiThucHien, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var phieu = await _db.PhieuMuons
            .Include(p => p.BanDoc)
            .Include(p => p.Sach)
            .FirstOrDefaultAsync(p => p.MaPhieuMuon == id, ct);

        if (phieu is null)
            return ServiceResult.Fail("Không tìm thấy phiếu mượn.");

        if (!ChoPhepChuyenTrangThai(phieu.TrangThai, TrangThaiPhieuMuon.DangMuon))
            return ServiceResult.Fail("Chỉ phiếu Đã duyệt mới được giao sách.");

        if (phieu.BanDoc is null || phieu.Sach is null)
            return ServiceResult.Fail("Phiếu thiếu thông tin bạn đọc hoặc đầu sách.");

        var today = Today;
        if (phieu.BanDoc.TrangThai != TrangThaiBanDoc.HoatDong)
            return ServiceResult.Fail("Bạn đọc đã ngừng hoạt động, không thể giao sách.");
        if (phieu.BanDoc.NgayHetHanThe < today)
            return ServiceResult.Fail("Thẻ thư viện đã hết hạn, không thể giao sách.");
        if (phieu.Sach.TrangThai != TrangThaiSach.DangPhucVu)
            return ServiceResult.Fail("Đầu sách không còn ở trạng thái Đang phục vụ.");

        var conNo = await _db.TraSachs.AnyAsync(t =>
            t.PhieuMuon != null &&
            t.PhieuMuon.MaBanDoc == phieu.MaBanDoc &&
            t.TrangThaiPhat == TrangThaiPhat.ChuaNop, ct);
        if (conNo)
            return ServiceResult.Fail("Bạn đọc còn khoản phạt chưa nộp, không thể giao sách.");

        var trangThaiCu = phieu.TrangThai;
        var soNgayMuon = await _cauHinh.GetSoNgayMuonAsync(phieu.BanDoc.LoaiBanDoc);
        phieu.NgayMuon = today;
        phieu.HanTra = today.AddDays(soNgayMuon);
        phieu.TrangThai = TrangThaiPhieuMuon.DangMuon;
        phieu.NgayXuLy = today;

        ThemLichSu(phieu.MaPhieuMuon, trangThaiCu, phieu.TrangThai, maNguoiThucHien,
            $"Giao sách; hạn mượn {soNgayMuon} ngày.");

        try
        {
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Fail("Không thể lưu thông tin giao sách. Vui lòng thử lại.");
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> CancelAsync(
        int id, int maNguoiThucHien, string? ghiChu, CancellationToken ct = default)
    {
        var note = string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu.Trim();
        if (note is { Length: > 100 })
            return ServiceResult.Fail(nameof(ghiChu), "Ghi chú hủy phiếu tối đa 100 ký tự.");

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var phieu = await _db.PhieuMuons
            .FirstOrDefaultAsync(p => p.MaPhieuMuon == id, ct);

        if (phieu is null)
            return ServiceResult.Fail("Không tìm thấy phiếu mượn.");

        if (!ChoPhepChuyenTrangThai(phieu.TrangThai, TrangThaiPhieuMuon.DaHuy))
            return ServiceResult.Fail("Chỉ phiếu Chờ duyệt hoặc Đã duyệt chưa giao sách mới được hủy.");

        var trangThaiCu = phieu.TrangThai;
        phieu.TrangThai = TrangThaiPhieuMuon.DaHuy;
        phieu.NgayXuLy = Today;
        if (note is not null)
            phieu.NhanXetThuThu = note;

        ThemLichSu(phieu.MaPhieuMuon, trangThaiCu, phieu.TrangThai,
            maNguoiThucHien, note ?? "Thủ thư hủy phiếu.");

        try
        {
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Fail("Không thể lưu kết quả hủy phiếu. Vui lòng thử lại.");
        }

        return ServiceResult.Success();
    }

    private async Task<List<string>> KiemTraDieuKienDuyetAsync(
        PhieuMuon phieu, CancellationToken ct)
    {
        var warnings = new List<string>();

        if (phieu.TrangThai != TrangThaiPhieuMuon.ChoDuyet)
            warnings.Add("Phiếu không còn ở trạng thái Chờ duyệt.");

        if (phieu.Sach is null)
        {
            warnings.Add("Không tìm thấy đầu sách liên quan.");
        }
        else
        {
            if (phieu.Sach.TrangThai != TrangThaiSach.DangPhucVu)
                warnings.Add("Đầu sách không ở trạng thái Đang phục vụ.");

            if (await GetSoLuongConLaiAsync(phieu.MaSach, ct) <= 0)
                warnings.Add("Đầu sách đã hết bản còn lại.");
        }

        if (phieu.BanDoc is null)
        {
            warnings.Add("Không tìm thấy hồ sơ bạn đọc.");
        }
        else
        {
            if (phieu.BanDoc.TrangThai != TrangThaiBanDoc.HoatDong)
                warnings.Add("Bạn đọc không ở trạng thái Hoạt động.");
            if (phieu.BanDoc.NgayHetHanThe < Today)
                warnings.Add("Thẻ thư viện đã hết hạn.");

            var hasUnpaidFine = await _db.TraSachs.AnyAsync(t =>
                t.PhieuMuon != null &&
                t.PhieuMuon.MaBanDoc == phieu.MaBanDoc &&
                t.TrangThaiPhat == TrangThaiPhat.ChuaNop, ct);
            if (hasUnpaidFine)
                warnings.Add("Bạn đọc còn khoản phạt chưa nộp.");
        }

        return warnings;
    }

    // Công thức dùng chung: số tổng - phiếu Đã duyệt/Đang mượn - phiếu đã trả nhưng sách Mất
    private async Task<int> GetSoLuongConLaiAsync(int maSach, CancellationToken ct)
    {
        var data = await _db.Sachs
            .AsNoTracking()
            .Where(s => s.MaSach == maSach)
            .Select(s => new
            {
                s.SoLuongTong,
                DangGiu = s.PhieuMuons.Count(p =>
                    p.TrangThai == TrangThaiPhieuMuon.DaDuyet ||
                    p.TrangThai == TrangThaiPhieuMuon.DangMuon),
                DaMat = s.PhieuMuons.Count(p =>
                    p.TrangThai == TrangThaiPhieuMuon.DaTra &&
                    p.TraSach != null &&
                    p.TraSach.TinhTrangSach == TinhTrangSach.Mat)
            })
            .FirstOrDefaultAsync(ct);

        return data is null ? 0 : data.SoLuongTong - data.DangGiu - data.DaMat;
    }

    private void ThemLichSu(
        int maPhieuMuon,
        TrangThaiPhieuMuon? trangThaiCu,
        TrangThaiPhieuMuon trangThaiMoi,
        int maNguoiThucHien,
        string? ghiChu)
    {
        _db.PhieuMuonLichSus.Add(new PhieuMuonLichSu
        {
            MaPhieuMuon = maPhieuMuon,
            TrangThaiCu = trangThaiCu,
            TrangThaiMoi = trangThaiMoi,
            NgayThucHien = _time.GetLocalNow().DateTime,
            MaNguoiThucHien = maNguoiThucHien,
            GhiChu = ghiChu
        });
    }

    private static bool ChoPhepChuyenTrangThai(
        TrangThaiPhieuMuon tu, TrangThaiPhieuMuon den) => (tu, den) switch
    {
        (TrangThaiPhieuMuon.ChoDuyet, TrangThaiPhieuMuon.DaDuyet) => true,
        (TrangThaiPhieuMuon.ChoDuyet, TrangThaiPhieuMuon.TuChoi) => true,
        (TrangThaiPhieuMuon.ChoDuyet, TrangThaiPhieuMuon.DaHuy) => true,
        (TrangThaiPhieuMuon.DaDuyet, TrangThaiPhieuMuon.DangMuon) => true,
        (TrangThaiPhieuMuon.DaDuyet, TrangThaiPhieuMuon.DaHuy) => true,
        (TrangThaiPhieuMuon.DangMuon, TrangThaiPhieuMuon.DaTra) => true,
        _ => false
    };
}
