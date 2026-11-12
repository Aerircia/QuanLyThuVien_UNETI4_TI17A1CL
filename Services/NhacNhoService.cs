// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: kiểm tra điều kiện rồi gửi thông báo nhắc trả sách / nhắc gia hạn / nhắc nộp phạt cho bạn đọc

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services;

public class NhacNhoService : INhacNhoService
{
    //Hạn nộp phạt = ngày trả sách + số ngày này
    private const int SoNgayHanNopPhat = 7;

    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    private readonly AppDbContext _db;
    private readonly TimeProvider _time;
    private readonly IThongBaoService _thongBao;
    private readonly ITienPhatTreHanService _phatTre;

    public NhacNhoService(AppDbContext db, TimeProvider time, IThongBaoService thongBao, ITienPhatTreHanService phatTre)
    {
        _db = db;
        _time = time;
        _thongBao = thongBao;
        _phatTre = phatTre;
    }

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);
    private static string Tien(decimal v) => v.ToString("N0", Vi) + " ₫";
    private static ServiceResult<string> Loi(string message) => ServiceResult<string>.Error(string.Empty, message);

    // Nhắc trả sách

    public async Task<ServiceResult<string>> NhacTraSachAsync(int maPhieuMuon, CancellationToken ct = default)
    {
        var p = await LayPhieuAsync(maPhieuMuon, ct);
        if (p is null) return Loi("Không tìm thấy phiếu mượn.");
        if (p.TrangThai != TrangThaiPhieuMuon.DangMuon || p.HanTra is null)
            return Loi("Chỉ phiếu đang mượn mới nhắc trả sách được.");

        var today = Today;
        var han = p.HanTra.Value;
        var soNgayTre = _phatTre.TinhSoNgayTre(han, today);

        string tieuDe, noiDung, mucDo;
        if (soNgayTre > 0)
        {
            var cfg = await _db.CauHinhThuViens.AsNoTracking().FirstOrDefaultAsync(ct) ?? new CauHinhThuVien();
            mucDo = "er";
            tieuDe = $"Phiếu #{p.MaPhieuMuon} đã quá hạn {soNgayTre} ngày";
            // Phiếu đã quá hạn không gia hạn được nên chỉ nhắc trả sách
            noiDung = $"Sách \"{p.TenSach}\" (phiếu #{p.MaPhieuMuon}) đã quá hạn {soNgayTre} ngày, " +
                      $"tiền phạt tạm tính {Tien(_phatTre.TinhTienPhat(cfg, soNgayTre))}. Vui lòng trả sách sớm tại quầy thư viện.";
        }
        else
        {
            var conLai = han.DayNumber - today.DayNumber;
            var khiNao = conLai == 0 ? "hôm nay" : conLai == 1 ? "ngày mai" : $"sau {conLai} ngày nữa";
            mucDo = "wa";
            tieuDe = $"Phiếu #{p.MaPhieuMuon} sắp đến hạn trả";
            noiDung = $"Sách \"{p.TenSach}\" (phiếu #{p.MaPhieuMuon}) đến hạn trả {khiNao} ({han:dd/MM/yyyy}). " +
                      "Vui lòng trả sách đúng hạn, hoặc gửi yêu cầu gia hạn nếu phiếu còn lượt gia hạn.";
        }

        return await GuiAsync(p.MaTaiKhoan, p.TenBanDoc,
            $"PhieuMuon_{p.MaPhieuMuon}_NhacTra_{today:yyyyMMdd}", "due", mucDo,
            tieuDe, noiDung, $"/PhieuMuon/Details/{p.MaPhieuMuon}", "PhieuMuon", p.MaPhieuMuon, ct);
    }

    public async Task<ServiceResult<string>> NhacGiaHanAsync(int maPhieuMuon, CancellationToken ct = default)
    {
        var p = await LayPhieuAsync(maPhieuMuon, ct);
        if (p is null) return Loi("Không tìm thấy phiếu mượn.");
        if (p.TrangThai != TrangThaiPhieuMuon.DangMuon || p.HanTra is null)
            return Loi("Chỉ phiếu đang mượn mới nhắc gia hạn được.");

        var today = Today;
        var han = p.HanTra.Value;
        if (han < today)
            return Loi("Phiếu đã quá hạn nên không gia hạn được. Hãy dùng \"Nhắc trả sách\".");

        var cfg = await _db.CauHinhThuViens.AsNoTracking().FirstOrDefaultAsync(ct) ?? new CauHinhThuVien();
        var daGiaHan = await _db.GiaHanMuons.CountAsync(g => g.MaPhieuMuon == maPhieuMuon && g.TrangThai == TrangThaiGiaHan.DaDuyet, ct);
        if (daGiaHan >= cfg.SoLanGiaHanToiDa)
            return Loi($"Phiếu đã gia hạn đủ {cfg.SoLanGiaHanToiDa} lần, không nhắc gia hạn nữa.");

        if (await _db.GiaHanMuons.AnyAsync(g => g.MaPhieuMuon == maPhieuMuon && g.TrangThai == TrangThaiGiaHan.ChoDuyet, ct))
            return Loi("Phiếu đang có một yêu cầu gia hạn chờ duyệt, không cần nhắc.");

        var conLai = han.DayNumber - today.DayNumber;
        var khiNao = conLai == 0 ? "hôm nay" : conLai == 1 ? "ngày mai" : $"sau {conLai} ngày nữa";
        var noiDung = $"Sách \"{p.TenSach}\" (phiếu #{p.MaPhieuMuon}) đến hạn trả {khiNao} ({han:dd/MM/yyyy}). " +
                      $"Nếu cần thêm thời gian, bạn có thể gửi yêu cầu gia hạn tối đa {cfg.SoNgayGiaHanToiDa} ngày " +
                      $"(còn {cfg.SoLanGiaHanToiDa - daGiaHan} lượt gia hạn). Hệ thống sẽ kiểm tra điều kiện khi bạn gửi.";

        return await GuiAsync(p.MaTaiKhoan, p.TenBanDoc,
            $"PhieuMuon_{p.MaPhieuMuon}_NhacGiaHan_{today:yyyyMMdd}", "extension", "in",
            $"Bạn có thể gia hạn phiếu #{p.MaPhieuMuon}", noiDung,
            $"/GiaHanMuon/Create/{p.MaPhieuMuon}", "PhieuMuon", p.MaPhieuMuon, ct);
    }
    public async Task<ServiceResult<string>> NhacNopPhatAsync(int maTra, CancellationToken ct = default)
    {
        var t = await _db.TraSachs.AsNoTracking()
            .Where(x => x.MaTra == maTra)
            .Select(x => new
            {
                x.MaTra,
                x.MaPhieuMuon,
                x.TrangThaiPhat,
                x.TienPhat,
                x.NgayTra,
                MaTaiKhoan = x.PhieuMuon!.BanDoc!.MaTaiKhoan,
                TenBanDoc = x.PhieuMuon!.BanDoc!.HoTen,
                TenSach = x.PhieuMuon!.Sach!.TenSach
            })
            .FirstOrDefaultAsync(ct);

        if (t is null) return Loi("Không tìm thấy bản ghi trả sách.");
        if (t.TrangThaiPhat != TrangThaiPhat.ChuaNop) return Loi("Khoản phạt này đã được nộp hoặc không có phạt, không cần nhắc.");

        var today = Today;
        var hanNop = t.NgayTra.AddDays(SoNgayHanNopPhat);
        var quaHanNop = today.DayNumber - hanNop.DayNumber;
        var hanText = quaHanNop > 0 ? $"Đã quá hạn nộp {quaHanNop} ngày." : $"Hạn nộp phạt: {hanNop:dd/MM/yyyy}.";

        var noiDung = $"Sách \"{t.TenSach}\" (phiếu #{t.MaPhieuMuon}) có khoản phạt {Tien(t.TienPhat)} chưa nộp. {hanText} " +
                      "Vui lòng nộp phạt tại quầy thư viện hoặc xem chi tiết ở mục Tiền phạt.";

        return await GuiAsync(t.MaTaiKhoan, t.TenBanDoc,
            $"TraSach_{t.MaTra}_NhacNopPhat_{today:yyyyMMdd}", "fine", quaHanNop > 0 ? "er" : "wa",
            $"Bạn có khoản phạt {Tien(t.TienPhat)} chưa nộp", noiDung,
            "/PhieuMuon/MyFines", "TraSach", t.MaTra, ct);
    }
    public async Task<Dictionary<string, TimeOnly>> GetThoiGianDaNhacHomNayAsync(IEnumerable<string> danhSachKhoa, CancellationToken ct = default)
    {
        var listKeys = danhSachKhoa.Distinct().ToList();
        if (!listKeys.Any()) return new Dictionary<string, TimeOnly>();
        var listThongBao = await _db.ThongBaos.AsNoTracking()
            .Where(tb => listKeys.Contains(tb.KhoaSuKien))
            .Select(tb => new { tb.KhoaSuKien, tb.NgayTao })
            .ToListAsync(ct);

        return listThongBao
            .GroupBy(tb => tb.KhoaSuKien)
            .ToDictionary(
                g => g.Key,
                g => TimeOnly.FromDateTime(g.Max(x => x.NgayTao))
            );
}
    private Task<PhieuNhac?> LayPhieuAsync(int maPhieuMuon, CancellationToken ct) =>
        _db.PhieuMuons.AsNoTracking()
            .Where(x => x.MaPhieuMuon == maPhieuMuon)
            .Select(x => new PhieuNhac(x.MaPhieuMuon, x.TrangThai, x.HanTra, x.BanDoc!.MaTaiKhoan, x.BanDoc!.HoTen, x.Sach!.TenSach))
            .FirstOrDefaultAsync(ct);

    private sealed record PhieuNhac(int MaPhieuMuon, TrangThaiPhieuMuon TrangThai, DateOnly? HanTra, int MaTaiKhoan, string TenBanDoc, string TenSach);

    /// <summary>
    /// Mỗi việc chỉ nhắc một lần mỗi ngày: khóa sự kiện có kèm ngày (yyyyMMdd). Sang ngày hôm sau tạo thông báo mới
    /// (hiện lại là chưa đọc và gửi email lại) vì IThongBaoService.CreateAsync khi gặp khóa cũ chỉ cập nhật nội dung,
    /// không đặt lại trạng thái chưa đọc và không gửi email.
    /// </summary>
    private async Task<ServiceResult<string>> GuiAsync(
        int maTaiKhoan, string tenBanDoc, string khoa, string loai, string mucDo,
        string tieuDe, string noiDung, string duongDan, string loaiDoiTuong, int maDoiTuong, CancellationToken ct)
    {
        // Phải bật thông báo trên website thì mới có bản ghi để chống nhắc lặp; nếu bạn đọc tắt thì báo cho thủ thư biết
        var st = await _thongBao.GetSettingsAsync(maTaiKhoan, ct);
        var nhomBat = loai switch
        {
            "extension" => st.NhomGiaHan,
            "fine" => st.NhomTienPhat,
            _ => st.NhomMuonTra
        };
        if (!st.BatThongBaoWeb || !nhomBat)
            return Loi($"Bạn đọc {tenBanDoc} đã tắt nhận thông báo loại này trên website nên hệ thống không gửi nhắc được. Vui lòng liên hệ trực tiếp.");

        if (await _db.ThongBaos.AnyAsync(x => x.MaTaiKhoan == maTaiKhoan && x.KhoaSuKien == khoa, ct))
            return Loi($"Hôm nay đã nhắc bạn đọc {tenBanDoc} về việc này. Có thể nhắc lại vào ngày mai.");

        await _thongBao.CreateAsync(maTaiKhoan, tieuDe, noiDung, khoa, loai, mucDo, duongDan, loaiDoiTuong, maDoiTuong, ct);
        return ServiceResult<string>.Ok($"Đã gửi nhắc nhở đến bạn đọc {tenBanDoc}.");
    }
}