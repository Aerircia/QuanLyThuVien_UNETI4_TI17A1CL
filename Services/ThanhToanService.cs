// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng ThanhToanService, xử lý nghiệp vụ thanh toán tiền phạt (online hoặc tại quầy), gồm tạo giao dịch, đối soát kết quả từ cổng thanh toán, hủy giao dịch chờ.
using System.Data;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Payment;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services;

public sealed class ThanhToanService : IThanhToanService
{
    private const int MaxFinesPerPayment = 50;
    private const string ProviderName = "payOS";

    private readonly AppDbContext _db;
    private readonly IPaymentGateway _gateway;
    private readonly PayOsSettings _settings;
    private readonly TimeProvider _time;
    private readonly ILogger<ThanhToanService> _logger;

    public ThanhToanService(
        AppDbContext db,
        IPaymentGateway gateway,
        IOptions<PayOsSettings> settings,
        TimeProvider time,
        ILogger<ThanhToanService> logger)
    {
        _db = db;
        _gateway = gateway;
        _settings = settings.Value;
        _time = time;
        _logger = logger;
    }

    public bool IsOnlineEnabled => _gateway.IsConfigured;

    private DateTime Now() => _time.GetLocalNow().DateTime;

    // ---------------------------------------------------------------- Tạo giao dịch online

    public async Task<ServiceResult<string>> CreateOnlineAsync(
        int maBanDoc, IReadOnlyCollection<int> maTras, CancellationToken ct = default)
    {
        if (!_gateway.IsConfigured)
            return Fail<string>("Thanh toán trực tuyến chưa được cấu hình.");

        var ids = (maTras ?? Array.Empty<int>()).Distinct().ToList();
        if (ids.Count == 0)
            return Fail<string>("Vui lòng chọn ít nhất một khoản phạt.");
        if (ids.Count > MaxFinesPerPayment)
            return Fail<string>($"Mỗi lần chỉ thanh toán tối đa {MaxFinesPerPayment} khoản phạt.");

        // Giao dịch quá hạn hoặc đã trả nhưng chưa kịp ghi nhận được xử lý trước khi kiểm tra.
        await ReconcileAsync(maBanDoc, null, ct);

        // Truy vấn theo id VÀ chủ sở hữu; số tiền lấy từ DB, không nhận từ form.
        var fines = await (
            from t in _db.TraSachs
            join p in _db.PhieuMuons on t.MaPhieuMuon equals p.MaPhieuMuon
            where ids.Contains(t.MaTra) && p.MaBanDoc == maBanDoc
            select new { t.MaTra, p.MaPhieuMuon, t.TienPhat, t.TrangThaiPhat }).ToListAsync(ct);

        if (fines.Count != ids.Count)
            return Fail<string>("Có khoản phạt không hợp lệ hoặc không thuộc về bạn.");
        if (fines.Any(f => f.TrangThaiPhat != TrangThaiPhat.ChuaNop || f.TienPhat <= 0))
            return Fail<string>("Có khoản phạt đã được thanh toán hoặc không còn phải nộp.");

        var total = fines.Sum(f => f.TienPhat);
        if (decimal.Truncate(total) != total || total > int.MaxValue)
            return Fail<string>("Số tiền thanh toán không hợp lệ.");

        // Đã có giao dịch chờ cho các khoản này?
        var openIds = await _db.GiaoDichThanhToanChiTiets
            .Where(c => c.DangCho && ids.Contains(c.MaTra))
            .Select(c => c.MaGiaoDich).Distinct().ToListAsync(ct);
        if (openIds.Count > 0)
        {
            if (openIds.Count == 1)
            {
                var existing = await _db.GiaoDichThanhToans.AsNoTracking()
                    .Include(g => g.ChiTiets)
                    .FirstAsync(g => g.MaGiaoDich == openIds[0], ct);
                var same = existing.ChiTiets.Select(c => c.MaTra).OrderBy(x => x)
                    .SequenceEqual(ids.OrderBy(x => x));
                if (same && !string.IsNullOrEmpty(existing.CheckoutUrl))
                    return ServiceResult<string>.Ok(existing.CheckoutUrl);   // tiếp tục giao dịch cũ
            }
            return Fail<string>("Có khoản phạt đang chờ thanh toán ở giao dịch khác. Hãy hoàn tất hoặc hủy giao dịch đó trước.");
        }

        // Lưu giao dịch trước khi gọi payOS; chỉ mục unique OrderCode/MaTra(DangCho) chặn trùng khi chạy song song.
        var now = Now();
        GiaoDichThanhToan? gd = null;
        for (var attempt = 0; attempt < 3 && gd is null; attempt++)
        {
            var code = NewOrderCode();
            var candidate = new GiaoDichThanhToan
            {
                OrderCode = code,
                MaBanDoc = maBanDoc,
                TongTien = total,
                PhuongThuc = PhuongThucThanhToan.Online,
                TrangThai = TrangThaiGiaoDich.ChoThanhToan,
                NhaCungCap = ProviderName,
                NgayTao = now,
                NgayHetHan = now.AddMinutes(_settings.ExpireMinutes),
                ChiTiets = fines.Select(f => new GiaoDichThanhToanChiTiet
                {
                    MaTra = f.MaTra,
                    SoTien = f.TienPhat,
                    DangCho = true
                }).ToList()
            };
            _db.GiaoDichThanhToans.Add(candidate);
            try
            {
                await _db.SaveChangesAsync(ct);
                gd = candidate;
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                if (await _db.GiaoDichThanhToans.AnyAsync(g => g.OrderCode == code, ct))
                    continue;   // trùng OrderCode: thử mã khác
                return Fail<string>("Có khoản phạt đang chờ thanh toán ở giao dịch khác. Vui lòng tải lại trang.");
            }
        }
        if (gd is null)
            return Fail<string>("Không tạo được giao dịch. Vui lòng thử lại.");

        var baseUrl = _settings.PublicBaseUrl!.TrimEnd('/');
        // Nội dung CK chứa mã phiếu của mọi khoản phạt (hoặc mã giao dịch nếu quá dài); xem PayDescriptionBuilder.
        var description = PayDescriptionBuilder.ForGateway(gd.MaGiaoDich, fines.Select(f => f.MaPhieuMuon));
        try
        {
            var link = await _gateway.CreateLinkAsync(new CreateLinkRequest(
                gd.OrderCode!.Value, (int)total, description,
                $"{baseUrl}/ThanhToan/Return", $"{baseUrl}/ThanhToan/Cancel"), ct);

            gd.CheckoutUrl = link.CheckoutUrl;
            gd.MaDonHangNhaCungCap = link.PaymentLinkId;
            await _db.SaveChangesAsync(ct);
            return ServiceResult<string>.Ok(link.CheckoutUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không tạo được liên kết payOS cho giao dịch {MaGiaoDich}", gd.MaGiaoDich);
            await CloseAfterGatewayFailureAsync(gd.MaGiaoDich);
            return Fail<string>("Không tạo được liên kết thanh toán. Vui lòng thử lại sau.");
        }
    }

    private async Task CloseAfterGatewayFailureAsync(int maGiaoDich)
    {
        try
        {
            _db.ChangeTracker.Clear();
            var g = await _db.GiaoDichThanhToans.Include(x => x.ChiTiets).FirstAsync(x => x.MaGiaoDich == maGiaoDich);
            g.TrangThai = TrangThaiGiaoDich.ThatBai;
            g.GhiChu = "Không tạo được liên kết thanh toán";
            foreach (var c in g.ChiTiets) c.DangCho = false;
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không đóng được giao dịch {MaGiaoDich} sau lỗi cổng thanh toán", maGiaoDich);
        }
    }

    private static long NewOrderCode()
    {
        Span<byte> buffer = stackalloc byte[8];
        RandomNumberGenerator.Fill(buffer);
        // 1e12 .. 9e15: nằm trong giới hạn số nguyên an toàn của payOS (2^53 - 1).
        return (long)(BitConverter.ToUInt64(buffer) % 9_000_000_000_000_000UL) + 1_000_000_000_000L;
    }

    // ---------------------------------------------------------------- Đối soát / ghi nhận kết quả

    public Task<ServiceResult<TrangThaiGiaoDich>> SettleAsync(long orderCode, CancellationToken ct = default)
        => SettleCoreAsync(orderCode, ct);

    private async Task<ServiceResult<TrangThaiGiaoDich>> SettleCoreAsync(long orderCode, CancellationToken ct)
    {
        var head = await _db.GiaoDichThanhToans.AsNoTracking()
            .Where(g => g.OrderCode == orderCode && g.PhuongThuc == PhuongThucThanhToan.Online)
            .Select(g => new { g.MaGiaoDich, g.TrangThai, g.NgayHetHan })
            .FirstOrDefaultAsync(ct);
        if (head is null)
            return Fail<TrangThaiGiaoDich>("Không tìm thấy giao dịch.");
        if (head.TrangThai != TrangThaiGiaoDich.ChoThanhToan)
            return ServiceResult<TrangThaiGiaoDich>.Ok(head.TrangThai);   // idempotent

        // Gọi cổng thanh toán NGOÀI transaction để không giữ khóa Serializable trong lúc chờ mạng.
        GatewayPaymentInfo? info;
        try
        {
            info = await _gateway.GetPaymentAsync(orderCode, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không kiểm tra được trạng thái payOS cho đơn {OrderCode}", orderCode);
            return Fail<TrangThaiGiaoDich>("Chưa kiểm tra được trạng thái thanh toán, vui lòng thử lại sau.");
        }

        var state = info?.State ?? GatewayPaymentState.Pending;
        var amountPaid = info?.AmountPaid ?? 0;

        if (state == GatewayPaymentState.Pending && head.NgayHetHan < Now())
        {
            try
            {
                await _gateway.CancelLinkAsync(orderCode, "Hết hạn thanh toán", ct);
                state = GatewayPaymentState.Expired;
            }
            catch (Exception ex)
            {
                // Hủy thất bại có thể do khách vừa thanh toán xong: hỏi lại một lần trước khi quyết định.
                _logger.LogWarning(ex, "Không hủy được liên kết payOS cho đơn {OrderCode}", orderCode);
                try
                {
                    info = await _gateway.GetPaymentAsync(orderCode, ct);
                    state = info?.State ?? GatewayPaymentState.Pending;
                    amountPaid = info?.AmountPaid ?? 0;
                }
                catch { /* giữ trạng thái chờ */ }
            }
        }

        if (state == GatewayPaymentState.Pending)
            return ServiceResult<TrangThaiGiaoDich>.Ok(TrangThaiGiaoDich.ChoThanhToan);

        var now = Now();
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var g = await _db.GiaoDichThanhToans.Include(x => x.ChiTiets)
            .FirstAsync(x => x.MaGiaoDich == head.MaGiaoDich, ct);
        if (g.TrangThai != TrangThaiGiaoDich.ChoThanhToan)
        {
            await tx.CommitAsync(ct);
            return ServiceResult<TrangThaiGiaoDich>.Ok(g.TrangThai);   // đã có luồng khác xử lý
        }

        foreach (var c in g.ChiTiets) c.DangCho = false;

        if (state is GatewayPaymentState.Paid or GatewayPaymentState.Underpaid)
        {
            var maTras = g.ChiTiets.Select(c => c.MaTra).ToList();
            var fines = await _db.TraSachs.Where(t => maTras.Contains(t.MaTra)).ToListAsync(ct);
            var alreadyPaid = fines.Count(t => t.TrangThaiPhat != TrangThaiPhat.ChuaNop);
            var exact = state == GatewayPaymentState.Paid && amountPaid == (long)g.TongTien;

            g.NgayThanhToan = now;
            if (exact && alreadyPaid == 0)
            {
                foreach (var t in fines) t.TrangThaiPhat = TrangThaiPhat.DaNop;
                g.TrangThai = TrangThaiGiaoDich.ThanhCong;
            }
            else if (exact)
            {
                // Tiền đã nhận đủ nhưng có khoản đã được thu tại quầy trước đó: không double-count, cần đối soát.
                foreach (var t in fines.Where(t => t.TrangThaiPhat == TrangThaiPhat.ChuaNop))
                    t.TrangThaiPhat = TrangThaiPhat.DaNop;
                g.TrangThai = TrangThaiGiaoDich.CanDoiSoat;
                g.GhiChu = "Có khoản đã thu tại quầy trước; cần đối soát/hoàn tiền";
            }
            else
            {
                g.TrangThai = TrangThaiGiaoDich.CanDoiSoat;
                g.GhiChu = "Số tiền nhận khác số phải thu; cần đối soát";
            }
        }
        else
        {
            g.TrangThai = state switch
            {
                GatewayPaymentState.Cancelled => TrangThaiGiaoDich.DaHuy,
                GatewayPaymentState.Expired => TrangThaiGiaoDich.HetHan,
                _ => TrangThaiGiaoDich.ThatBai
            };
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ServiceResult<TrangThaiGiaoDich>.Ok(g.TrangThai);
    }

    private async Task ReconcileAsync(int? maBanDoc, IReadOnlyCollection<int>? maTras, CancellationToken ct)
    {
        var query = _db.GiaoDichThanhToans.AsNoTracking()
            .Where(g => g.TrangThai == TrangThaiGiaoDich.ChoThanhToan &&
                        g.PhuongThuc == PhuongThucThanhToan.Online && g.OrderCode != null);
        if (maBanDoc is int owner)
            query = query.Where(g => g.MaBanDoc == owner);
        if (maTras is { Count: > 0 })
        {
            var list = maTras.ToList();
            query = query.Where(g => g.ChiTiets.Any(c => list.Contains(c.MaTra)));
        }

        var codes = await query.Select(g => g.OrderCode!.Value).ToListAsync(ct);
        foreach (var code in codes)
            await SettleCoreAsync(code, ct);   // lỗi mạng chỉ làm giao dịch giữ trạng thái chờ
    }

    public async Task<ServiceResult> CancelOnlineAsync(long orderCode, int maBanDoc, CancellationToken ct = default)
    {
        var owned = await _db.GiaoDichThanhToans.AsNoTracking()
            .AnyAsync(g => g.OrderCode == orderCode && g.MaBanDoc == maBanDoc, ct);
        if (!owned)
            return ServiceResult.Fail("Không tìm thấy giao dịch.");

        // Hỏi payOS trước: nếu khách đã thanh toán thì ghi nhận thành công thay vì hủy.
        var first = await SettleCoreAsync(orderCode, ct);
        if (!first.Succeeded)
            return ServiceResult.Fail(first.FirstError);
        if (first.Value != TrangThaiGiaoDich.ChoThanhToan)
            return first.Value == TrangThaiGiaoDich.ThanhCong
                ? ServiceResult.Fail("Giao dịch đã được thanh toán, không thể hủy.")
                : ServiceResult.Success();

        try
        {
            await _gateway.CancelLinkAsync(orderCode, "Bạn đọc hủy giao dịch", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không hủy được liên kết payOS cho đơn {OrderCode}", orderCode);
            return ServiceResult.Fail("Không hủy được giao dịch, vui lòng thử lại.");
        }

        var second = await SettleCoreAsync(orderCode, ct);
        return second.Succeeded ? ServiceResult.Success() : ServiceResult.Fail(second.FirstError);
    }

    public async Task<bool> HandleWebhookAsync(string rawBody, CancellationToken ct = default)
    {
        var code = await _gateway.VerifyWebhookAsync(rawBody, ct);
        if (code is null)
            return false;

        // Dù webhook đã ký đúng vẫn hỏi lại payOS và so khớp số tiền trong SettleCoreAsync.
        var result = await SettleCoreAsync(code.Value, ct);
        if (!result.Succeeded)
            _logger.LogInformation("Webhook payOS cho đơn {OrderCode}: {Error}", code, result.FirstError);
        return true;
    }

    // ---------------------------------------------------------------- Thu tại quầy

    public async Task<ServiceResult<int>> RecordCounterAsync(
        IReadOnlyCollection<int> maTras, int maNguoiXacNhan, CancellationToken ct = default)
    {
        var ids = (maTras ?? Array.Empty<int>()).Distinct().ToList();
        if (ids.Count == 0)
            return Fail<int>("Vui lòng chọn ít nhất một khoản phạt.");
        if (ids.Count > MaxFinesPerPayment)
            return Fail<int>($"Mỗi lần chỉ thu tối đa {MaxFinesPerPayment} khoản phạt.");

        var owners = await (
            from t in _db.TraSachs
            join p in _db.PhieuMuons on t.MaPhieuMuon equals p.MaPhieuMuon
            where ids.Contains(t.MaTra)
            select new { t.MaTra, p.MaBanDoc }).ToListAsync(ct);
        if (owners.Count != ids.Count)
            return Fail<int>("Không tìm thấy khoản phạt.");
        if (owners.Select(o => o.MaBanDoc).Distinct().Count() != 1)
            return Fail<int>("Mỗi lần chỉ thu phạt cho một bạn đọc.");
        var maBanDoc = owners[0].MaBanDoc;

        // Quy tắc chống thanh toán trùng: đối soát giao dịch online đang chờ (có thể đã trả/đã hết hạn) rồi mới thu.
        await ReconcileAsync(null, ids, ct);

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var fines = await _db.TraSachs.Where(t => ids.Contains(t.MaTra)).ToListAsync(ct);
        if (fines.Any(t => t.TrangThaiPhat != TrangThaiPhat.ChuaNop || t.TienPhat <= 0))
            return Fail<int>("Có khoản phạt đã được thanh toán hoặc không còn phải nộp.");

        if (await _db.GiaoDichThanhToanChiTiets.AnyAsync(c => c.DangCho && ids.Contains(c.MaTra), ct))
            return Fail<int>("Khoản phạt đang có giao dịch trực tuyến chờ thanh toán. " +
                             "Hãy đợi giao dịch hết hạn hoặc để bạn đọc hủy trước khi thu tại quầy.");

        var now = Now();
        var gd = new GiaoDichThanhToan
        {
            MaBanDoc = maBanDoc,
            TongTien = fines.Sum(t => t.TienPhat),
            PhuongThuc = PhuongThucThanhToan.TaiQuay,
            TrangThai = TrangThaiGiaoDich.ThanhCong,
            NgayTao = now,
            NgayThanhToan = now,
            MaNguoiXacNhan = maNguoiXacNhan,
            ChiTiets = fines.Select(t => new GiaoDichThanhToanChiTiet
            {
                MaTra = t.MaTra,
                SoTien = t.TienPhat,
                DangCho = false
            }).ToList()
        };
        foreach (var t in fines) t.TrangThaiPhat = TrangThaiPhat.DaNop;

        _db.GiaoDichThanhToans.Add(gd);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ServiceResult<int>.Ok(gd.MaGiaoDich);
    }

    // ---------------------------------------------------------------- Truy vấn

    public async Task<HashSet<int>> GetPendingFineIdsAsync(int maBanDoc, CancellationToken ct = default)
    {
        await ReconcileAsync(maBanDoc, null, ct);
        var ids = await _db.GiaoDichThanhToanChiTiets.AsNoTracking()
            .Where(c => c.DangCho && c.GiaoDich!.MaBanDoc == maBanDoc)
            .Select(c => c.MaTra).ToListAsync(ct);
        return ids.ToHashSet();
    }

    public async Task<PagedResult<GiaoDichListItem>> GetListAsync(
        ThanhToanFilter filter, int? maBanDoc, CancellationToken ct = default)
    {
        var query = _db.GiaoDichThanhToans.AsNoTracking().AsQueryable();

        if (maBanDoc is int owner)
            query = query.Where(g => g.MaBanDoc == owner);
        if (filter.PhuongThuc is { } pt)
            query = query.Where(g => g.PhuongThuc == pt);
        if (filter.TrangThai is { } tt)
            query = query.Where(g => g.TrangThai == tt);
        if (filter.Tu is { } tu)
        {
            var from = tu.ToDateTime(TimeOnly.MinValue);
            query = query.Where(g => g.NgayTao >= from);
        }
        if (filter.Den is { } den)
        {
            var to = den.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(g => g.NgayTao < to);
        }
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            long? code = long.TryParse(s, out var parsed) ? parsed : null;
            query = query.Where(g => g.BanDoc!.HoTen.Contains(s) ||
                                     (g.BanDoc.MaSoThe != null && g.BanDoc.MaSoThe.Contains(s)) ||
                                     (code != null && g.OrderCode == code));
        }

        var projected = (filter.Sort?.ToLowerInvariant() switch
        {
            "oldest" => query.OrderBy(g => g.NgayTao).ThenBy(g => g.MaGiaoDich),
            "amount_asc" => query.OrderBy(g => g.TongTien).ThenByDescending(g => g.NgayTao),
            "amount_desc" => query.OrderByDescending(g => g.TongTien).ThenByDescending(g => g.NgayTao),
            _ => query.OrderByDescending(g => g.NgayTao).ThenByDescending(g => g.MaGiaoDich)
        })
            .Select(g => new GiaoDichListItem(
                g.MaGiaoDich,
                g.OrderCode,
                g.MaBanDoc,
                g.BanDoc!.HoTen,
                g.BanDoc.MaSoThe,
                g.TongTien,
                g.ChiTiets.Count,
                g.PhuongThuc,
                g.TrangThai,
                g.NgayTao,
                g.NgayThanhToan,
                g.NguoiXacNhan != null ? g.NguoiXacNhan.HoTen : null));

        return await PagedResult<GiaoDichListItem>.CreateAsync(projected, filter.Page, filter.PageSize, ct);
    }

    public Task<GiaoDichDetail?> GetAsync(int maGiaoDich, int? maBanDoc, CancellationToken ct = default)
        => LoadDetailAsync(g => g.MaGiaoDich == maGiaoDich, maBanDoc, ct);

    public Task<GiaoDichDetail?> GetByOrderCodeAsync(long orderCode, int? maBanDoc, CancellationToken ct = default)
        => LoadDetailAsync(g => g.OrderCode == orderCode, maBanDoc, ct);

    private async Task<GiaoDichDetail?> LoadDetailAsync(
        System.Linq.Expressions.Expression<Func<GiaoDichThanhToan, bool>> predicate, int? maBanDoc, CancellationToken ct)
    {
        var query = _db.GiaoDichThanhToans.AsNoTracking().Where(predicate);
        if (maBanDoc is int owner)
            query = query.Where(g => g.MaBanDoc == owner);   // bạn đọc chỉ xem giao dịch của chính mình

        var h = await query.Select(g => new
        {
            g.MaGiaoDich, g.OrderCode, g.MaBanDoc,
            HoTen = g.BanDoc!.HoTen, MaSoThe = g.BanDoc.MaSoThe,
            g.TongTien, g.PhuongThuc, g.TrangThai, g.NhaCungCap, g.CheckoutUrl,
            g.NgayTao, g.NgayHetHan, g.NgayThanhToan,
            NguoiXacNhan = g.NguoiXacNhan != null ? g.NguoiXacNhan.HoTen : null,
            g.GhiChu
        }).FirstOrDefaultAsync(ct);
        if (h is null) return null;

        var lines = await (
            from c in _db.GiaoDichThanhToanChiTiets.AsNoTracking()
            where c.MaGiaoDich == h.MaGiaoDich
            join t in _db.TraSachs on c.MaTra equals t.MaTra
            join p in _db.PhieuMuons on t.MaPhieuMuon equals p.MaPhieuMuon
            orderby c.MaChiTiet
            select new GiaoDichLine(
                c.MaTra, p.MaPhieuMuon, p.Sach!.TenSach, c.SoTien,
                t.NgayTra, t.TinhTrangSach, t.SoNgayTre, t.TrangThaiPhat)).ToListAsync(ct);

        return new GiaoDichDetail(
            h.MaGiaoDich, h.OrderCode, h.MaBanDoc, h.HoTen, h.MaSoThe, h.TongTien, h.PhuongThuc, h.TrangThai,
            h.NhaCungCap, h.CheckoutUrl, h.NgayTao, h.NgayHetHan, h.NgayThanhToan, h.NguoiXacNhan, h.GhiChu, lines);
    }

    private static ServiceResult<T> Fail<T>(string message) => ServiceResult<T>.Error(string.Empty, message);
}
