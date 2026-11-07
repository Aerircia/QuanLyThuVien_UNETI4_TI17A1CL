// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: service tạo nội dung mô tả thanh toán tiền phạt gửi payOS, đảm bảo không vượt quá 25 ký tự, có thể liệt kê mã phiếu hoặc mã giao dịch.
namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Payment;

/// <summary>
/// Tạo nội dung chuyển khoản gửi payOS. payOS giới hạn mô tả (25 ký tự với tài khoản chưa liên kết),
/// vì vậy: đủ chỗ thì liệt kê MỌI mã phiếu ("Phat PM12,15,18"); không đủ thì dùng mã giao dịch
/// ("Phat GD45 x7") và danh sách đầy đủ xem ở trang chi tiết giao dịch. Hàm thuần, kết quả luôn tái lập được
/// từ (MaGiaoDich, các MaPhieuMuon) nên không cần lưu thêm cột. Chỉ dùng ASCII.
/// </summary>
public static class PayDescriptionBuilder
{
    public const int GatewayMaxLength = 25;

    public static string ForGateway(int maGiaoDich, IEnumerable<int> maPhieuMuons)
    {
        var ids = Normalize(maPhieuMuons);
        var full = "Phat PM" + string.Join(",", ids);
        return full.Length <= GatewayMaxLength ? full : $"Phat GD{maGiaoDich} x{ids.Count}";
    }

    /// <summary>Câu đầy đủ, không bị cắt, dùng để hiển thị trên giao diện.</summary>
    public static string FullText(IEnumerable<int> maPhieuMuons)
        => "Thanh toán tiền phạt mã phiếu " + string.Join(", ", Normalize(maPhieuMuons));

    private static List<int> Normalize(IEnumerable<int> ids) => ids.Distinct().OrderBy(x => x).ToList();
}
