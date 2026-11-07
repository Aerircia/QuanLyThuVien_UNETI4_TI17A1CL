// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: gia hạn phiếu mượn: kiểm tra điều kiện gia hạn, tạo yêu cầu, duyệt và cập nhật hạn trả của phiếu.

using System.Data;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services;

public class GiaHanMuonService : IGiaHanMuonService
{
    private const string MsgNotFound = "Không tìm thấy yêu cầu gia hạn.";
    private const string MsgPhieuNotFound = "Không tìm thấy phiếu mượn.";
    private const string MsgSaveFailed = "Không thể lưu thay đổi. Vui lòng thử lại.";

    private readonly AppDbContext _db;
    private readonly TimeProvider _time;

    public GiaHanMuonService(AppDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    private sealed record PhieuInfo(int MaPhieuMuon, int MaBanDoc, int MaSach, TrangThaiPhieuMuon TrangThai, DateOnly? HanTra);

// Danh sách 
    public async Task<PagedResult<GiaHanMuonListItem>> GetListAsync(GiaHanMuonFilter filter, CancellationToken ct = default)
    {
        var query = _db.GiaHanMuons.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            query = query.Where(g => g.PhieuMuon!.BanDoc!.HoTen.Contains(s) ||
                                     g.PhieuMuon!.Sach!.TenSach.Contains(s));
        }

        if (filter.TrangThai is { } trangThai)
            query = query.Where(g => g.TrangThai == trangThai);
        var projected = query
            .OrderBy(g => g.TrangThai == TrangThaiGiaHan.ChoDuyet ? 0 : 1)
            .ThenByDescending(g => g.NgayYeuCau)
            .ThenByDescending(g => g.MaGiaHan)
            .Select(g => new GiaHanMuonListItem(
                g.MaGiaHan,
                g.MaPhieuMuon,
                g.PhieuMuon!.BanDoc!.HoTen,
                g.PhieuMuon!.Sach!.TenSach,
                g.HanTraCu,
                g.HanTraMoi,
                g.LyDo,
                g.NgayYeuCau,
                g.TrangThai,
                g.NgayXuLy));

        return await PagedResult<GiaHanMuonListItem>.CreateAsync(projected, filter.Page, filter.PageSize, ct);
    }

//Bạn đọc gửi yêu cầu
    public async Task<GiaHanCreateInfo?> GetCreateInfoAsync(int maPhieuMuon, int maBanDoc, CancellationToken ct = default)
    {
        var phieu = await LoadPhieuAsync(maPhieuMuon, ct);
        if (phieu is null || phieu.MaBanDoc != maBanDoc)
            return null;

        var cfg = await GetConfigAsync(ct);
        var canhBao = await CheckEligibilityAsync(phieu, cfg, 0, ct);
        var soLan = await CountApprovedAsync(phieu.MaPhieuMuon, ct);
        var tenSach = await _db.Sachs.AsNoTracking()
            .Where(s => s.MaSach == phieu.MaSach)
            .Select(s => s.TenSach)
            .FirstAsync(ct);

        return new GiaHanCreateInfo(
            phieu.MaPhieuMuon, tenSach, phieu.HanTra, soLan, cfg.SoLanGiaHanToiDa, cfg.SoNgayGiaHanToiDa, canhBao);
    }

    public async Task<ServiceResult<int>> CreateAsync(CreateGiaHanInput input, int maBanDoc, CancellationToken ct = default)
    {
        var phieu = await LoadPhieuAsync(input.MaPhieuMuon, ct);
        if (phieu is null || phieu.MaBanDoc != maBanDoc)
            return ServiceResult<int>.Error(string.Empty, MsgPhieuNotFound);

        var cfg = await GetConfigAsync(ct);
        var result = new ServiceResult<int>();

        foreach (var loi in await CheckEligibilityAsync(phieu, cfg, 0, ct))
            result.AddError(string.Empty, loi);

        var lyDo = (input.LyDo ?? string.Empty).Trim();
        if (lyDo.Length == 0)
            result.AddError(nameof(input.LyDo), "Vui lòng nhập lý do gia hạn.");
        else if (lyDo.Length > 100)
            result.AddError(nameof(input.LyDo), "Lý do tối đa 100 ký tự.");

        if (phieu.HanTra is { } hanTraCu)
        {
            var toiDa = hanTraCu.AddDays(cfg.SoNgayGiaHanToiDa);
            if (input.HanTraMoi <= hanTraCu)
                result.AddError(nameof(input.HanTraMoi), "Hạn trả mới phải sau hạn trả hiện tại.");
            else if (input.HanTraMoi > toiDa)
                result.AddError(nameof(input.HanTraMoi),
                    $"Mỗi lần gia hạn tối đa {cfg.SoNgayGiaHanToiDa} ngày (hạn trả mới không quá {toiDa:dd/MM/yyyy}).");
        }

        if (!result.Succeeded)
            return result;

        var giaHan = new GiaHanMuon
        {
            MaPhieuMuon = phieu.MaPhieuMuon,
            HanTraCu = phieu.HanTra!.Value,   
            HanTraMoi = input.HanTraMoi,
            LyDo = lyDo,
            NgayYeuCau = Today,     
            TrangThai = TrangThaiGiaHan.ChoDuyet
        };
        _db.GiaHanMuons.Add(giaHan);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return ServiceResult<int>.Error(string.Empty, "Phiếu này đã có yêu cầu gia hạn đang chờ duyệt.");
        }

        return ServiceResult<int>.Ok(giaHan.MaGiaHan);
    }

//Thủ thư xem 

    public async Task<GiaHanMuonDetail?> GetAsync(int maGiaHan, CancellationToken ct = default)
    {
        var row = await _db.GiaHanMuons.AsNoTracking()
            .Where(g => g.MaGiaHan == maGiaHan)
            .Select(g => new
            {
                MaGiaHan = g.MaGiaHan,
                MaPhieuMuon = g.MaPhieuMuon,
                MaBanDoc = g.PhieuMuon!.MaBanDoc,
                MaSach = g.PhieuMuon!.MaSach,
                TenBanDoc = g.PhieuMuon!.BanDoc!.HoTen,
                TenSach = g.PhieuMuon!.Sach!.TenSach,
                TrangThaiPhieu = g.PhieuMuon!.TrangThai,
                HanTraHienTai = g.PhieuMuon!.HanTra,
                HanTraCu = g.HanTraCu,
                HanTraMoi = g.HanTraMoi,
                LyDo = g.LyDo,
                NgayYeuCau = g.NgayYeuCau,
                TrangThai = g.TrangThai,
                NgayXuLy = g.NgayXuLy,
                NhanXet = g.NhanXet
            })
            .FirstOrDefaultAsync(ct);

        if (row is null)
            return null;

        var cfg = await GetConfigAsync(ct);
        var soLan = await CountApprovedAsync(row.MaPhieuMuon, ct);
        var conLai = await SoLuongConLaiAsync(row.MaSach, ct);

        IReadOnlyList<string> canhBao = Array.Empty<string>();
        if (row.TrangThai == TrangThaiGiaHan.ChoDuyet)
        {
            var phieu = new PhieuInfo(row.MaPhieuMuon, row.MaBanDoc, row.MaSach, row.TrangThaiPhieu, row.HanTraHienTai);
            canhBao = await CheckApprovalAsync(phieu, row.MaGiaHan, row.HanTraCu, row.HanTraMoi, cfg, ct);
        }

        return new GiaHanMuonDetail(
            row.MaGiaHan, row.MaPhieuMuon, row.MaBanDoc, row.TenBanDoc, row.TenSach, row.TrangThaiPhieu,
            row.HanTraHienTai, row.HanTraCu, row.HanTraMoi, row.LyDo, row.NgayYeuCau, row.TrangThai,
            row.NgayXuLy, row.NhanXet, soLan, cfg.SoLanGiaHanToiDa, cfg.SoNgayGiaHanToiDa, conLai, canhBao);
    }

    public async Task<ServiceResult> ProcessAsync(ProcessGiaHanInput input, CancellationToken ct = default)
    {
        var nhanXet = string.IsNullOrWhiteSpace(input.NhanXet) ? null : input.NhanXet.Trim();
        if (nhanXet is { Length: > 100 })
            return ServiceResult.Fail(nameof(input.NhanXet), "Nhận xét tối đa 100 ký tự.");
        if (!input.Duyet && nhanXet is null)
            return ServiceResult.Fail(nameof(input.NhanXet), "Vui lòng nhập nhận xét khi từ chối.");

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var giaHan = await _db.GiaHanMuons
            .Include(g => g.PhieuMuon)
            .FirstOrDefaultAsync(g => g.MaGiaHan == input.MaGiaHan, ct);

        if (giaHan is null || giaHan.PhieuMuon is null)
            return ServiceResult.Fail(MsgNotFound);

        if (giaHan.TrangThai != TrangThaiGiaHan.ChoDuyet)
            return ServiceResult.Fail("Yêu cầu gia hạn này đã được xử lý.");

        var phieuMuon = giaHan.PhieuMuon;

        if (input.Duyet)
        {
            var cfg = await GetConfigAsync(ct);
            var phieu = new PhieuInfo(
                phieuMuon.MaPhieuMuon, phieuMuon.MaBanDoc, phieuMuon.MaSach, phieuMuon.TrangThai, phieuMuon.HanTra);

            var loi = await CheckApprovalAsync(phieu, giaHan.MaGiaHan, giaHan.HanTraCu, giaHan.HanTraMoi, cfg, ct);
            if (loi.Count > 0)
            {
                var fail = new ServiceResult();
                foreach (var m in loi) fail.AddError(string.Empty, m);
                return fail;
            }

            giaHan.TrangThai = TrangThaiGiaHan.DaDuyet;
            phieuMuon.HanTra = giaHan.HanTraMoi;  
        }
        else
        {
            giaHan.TrangThai = TrangThaiGiaHan.TuChoi;
        }

        giaHan.NgayXuLy = Today;
        giaHan.NhanXet = nhanXet;

        try
        {
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Fail(MsgSaveFailed);
        }

        return ServiceResult.Success();
    }

// Quy tắc dùng chung
//Điều kiện để một phiếu được gia hạn thêm, rỗng = hợp lệ.
    private async Task<List<string>> CheckEligibilityAsync(
        PhieuInfo phieu, CauHinhThuVien cfg, int excludeMaGiaHan, CancellationToken ct)
    {
        var errors = new List<string>();

        if (phieu.TrangThai != TrangThaiPhieuMuon.DangMuon)
        {
            errors.Add("Chỉ phiếu đang mượn mới được gia hạn.");
            return errors;
        }

// Quá hạn = Đang mượn và HanTra < hôm nay
        if (phieu.HanTra is null || phieu.HanTra.Value < Today)
            errors.Add("Phiếu đã quá hạn trả nên không thể gia hạn.");

        var soLan = await CountApprovedAsync(phieu.MaPhieuMuon, ct);
        if (soLan >= cfg.SoLanGiaHanToiDa)
            errors.Add($"Phiếu đã được gia hạn tối đa {cfg.SoLanGiaHanToiDa} lần.");

        var coYeuCauKhac = await _db.GiaHanMuons.AnyAsync(g =>
            g.MaPhieuMuon == phieu.MaPhieuMuon &&
            g.TrangThai == TrangThaiGiaHan.ChoDuyet &&
            g.MaGiaHan != excludeMaGiaHan, ct);
        if (coYeuCauKhac)
            errors.Add("Phiếu đang có một yêu cầu gia hạn khác chờ duyệt.");

// Sách hết mà có bạn đọc chờ mượn thì không gia hạn.
        if (await SoLuongConLaiAsync(phieu.MaSach, ct) <= 0)
        {
            var coNguoiCho = await _db.PhieuMuons.AnyAsync(p =>
                p.MaSach == phieu.MaSach &&
                p.TrangThai == TrangThaiPhieuMuon.ChoDuyet &&
                p.MaBanDoc != phieu.MaBanDoc, ct);
            if (coNguoiCho)
                errors.Add("Sách đã hết bản còn lại và đang có bạn đọc khác chờ mượn nên không thể gia hạn.");
        }

        return errors;
    }

 //Điều kiện duyệt = điều kiện chung + kiểm tra riêng cho yêu cầu đã lưu
    private async Task<List<string>> CheckApprovalAsync(
        PhieuInfo phieu, int maGiaHan, DateOnly hanTraCu, DateOnly hanTraMoi, CauHinhThuVien cfg, CancellationToken ct)
    {
        var errors = await CheckEligibilityAsync(phieu, cfg, maGiaHan, ct);

        if (phieu.HanTra != hanTraCu)
            errors.Add("Hạn trả hiện tại của phiếu đã thay đổi so với lúc bạn đọc gửi yêu cầu.");

        if (hanTraMoi <= hanTraCu)
            errors.Add("Hạn trả mới phải sau hạn trả cũ.");
        else if (hanTraMoi.DayNumber - hanTraCu.DayNumber > cfg.SoNgayGiaHanToiDa)
            errors.Add($"Mỗi lần gia hạn tối đa {cfg.SoNgayGiaHanToiDa} ngày.");

        return errors;
    }

    private Task<int> CountApprovedAsync(int maPhieuMuon, CancellationToken ct) =>
        _db.GiaHanMuons.CountAsync(g => g.MaPhieuMuon == maPhieuMuon && g.TrangThai == TrangThaiGiaHan.DaDuyet, ct);

// Số bản còn lại = SoLuongTong - (phiếu Đã duyệt + Đang mượn) - sách mất .
// Sách mất không quay lại kho trừ vĩnh viễn 
    private async Task<int> SoLuongConLaiAsync(int maSach, CancellationToken ct)
    {
        var so = await _db.Sachs.AsNoTracking()
            .Where(s => s.MaSach == maSach)
            .Select(s => new
            {
                SoLuongTong = s.SoLuongTong,
                DangGiu = s.PhieuMuons.Count(p =>
                    p.TrangThai == TrangThaiPhieuMuon.DaDuyet || p.TrangThai == TrangThaiPhieuMuon.DangMuon),
                DaMat = s.PhieuMuons.Count(p =>
                    p.TrangThai == TrangThaiPhieuMuon.DaTra &&
                    p.TraSach != null &&
                    p.TraSach.TinhTrangSach == TinhTrangSach.Mat)
            })
            .FirstOrDefaultAsync(ct);

        return so is null ? 0 : so.SoLuongTong - so.DangGiu - so.DaMat;
    }

    private Task<PhieuInfo?> LoadPhieuAsync(int maPhieuMuon, CancellationToken ct) =>
        _db.PhieuMuons.AsNoTracking()
            .Where(p => p.MaPhieuMuon == maPhieuMuon)
            .Select(p => new PhieuInfo(p.MaPhieuMuon, p.MaBanDoc, p.MaSach, p.TrangThai, p.HanTra))
            .FirstOrDefaultAsync(ct);

    private async Task<CauHinhThuVien> GetConfigAsync(CancellationToken ct) =>
        await _db.CauHinhThuViens.AsNoTracking().FirstOrDefaultAsync(ct) ?? new CauHinhThuVien();
}