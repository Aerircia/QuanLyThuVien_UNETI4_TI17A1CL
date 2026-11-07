// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng IPaymentGateway, định nghĩa các phương thức tạo liên kết thanh toán, hủy liên kết, truy vấn trạng thái thanh toán và xác thực webhook.
namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

public sealed record CreateLinkRequest(long OrderCode, int Amount, string Description, string ReturnUrl, string CancelUrl);

public sealed record CreateLinkResult(string CheckoutUrl, string PaymentLinkId);

public enum GatewayPaymentState { Pending, Paid, Underpaid, Cancelled, Expired, Failed }

public sealed record GatewayPaymentInfo(GatewayPaymentState State, long AmountPaid);

/// <summary>Cổng thanh toán. Mọi chi tiết SDK nằm trong implementation (PayOsGateway).</summary>
public interface IPaymentGateway
{
    bool IsConfigured { get; }

    Task<CreateLinkResult> CreateLinkAsync(CreateLinkRequest request, CancellationToken ct = default);

    /// <summary>Hỏi trực tiếp cổng thanh toán về trạng thái đơn (nguồn tin cậy duy nhất).</summary>
    Task<GatewayPaymentInfo?> GetPaymentAsync(long orderCode, CancellationToken ct = default);

    Task CancelLinkAsync(long orderCode, string? reason, CancellationToken ct = default);

    /// <summary>Kiểm tra chữ ký webhook; trả về orderCode nếu hợp lệ, null nếu sai chữ ký.</summary>
    Task<long?> VerifyWebhookAsync(string rawBody, CancellationToken ct = default);
}
