// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng PayOsGateway, dùng SDK payOS (NuGet "payOS" 2.x) để tạo liên kết thanh toán, hủy liên kết, truy vấn trạng thái thanh toán và xác thực webhook.
// LƯU Ý: đây là file DUY NHẤT dùng SDK payOS (NuGet "payOS" 2.x). Tên thành viên SDK dưới đây viết theo
// tài liệu công khai; nếu trình biên dịch báo khác (vd GetAsync/CancelAsync/Webhooks.VerifyAsync), chỉ sửa tại đây.
using System.Text.Json;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Payment;

public sealed class PayOsGateway : IPaymentGateway
{
    private readonly PayOsSettings _settings;
    private readonly Lazy<PayOSClient> _client;

    public PayOsGateway(IOptions<PayOsSettings> options)
    {
        _settings = options.Value;
        _client = new Lazy<PayOSClient>(() => new PayOSClient(new PayOSOptions
        {
            ClientId = _settings.ClientId!,
            ApiKey = _settings.ApiKey!,
            ChecksumKey = _settings.ChecksumKey!
        }));
    }

    public bool IsConfigured => _settings.IsConfigured;

    public async Task<CreateLinkResult> CreateLinkAsync(CreateLinkRequest request, CancellationToken ct = default)
    {
        // Hết hạn do ứng dụng tự xử lý (hủy liên kết khi quá NgayHetHan). Có thể bổ sung ExpiredAt của SDK ở đây.
        var response = await _client.Value.PaymentRequests.CreateAsync(new CreatePaymentLinkRequest
        {
            OrderCode = request.OrderCode,
            Amount = request.Amount,
            Description = request.Description,
            ReturnUrl = request.ReturnUrl,
            CancelUrl = request.CancelUrl
        });
        return new CreateLinkResult(response.CheckoutUrl, response.PaymentLinkId);
    }

    public async Task<GatewayPaymentInfo?> GetPaymentAsync(long orderCode, CancellationToken ct = default)
    {
        var info = await _client.Value.PaymentRequests.GetAsync(orderCode);
        if (info is null) return null;

        var state = info.Status.ToString().ToUpperInvariant() switch
        {
            "PAID" => GatewayPaymentState.Paid,
            "UNDERPAID" => GatewayPaymentState.Underpaid,
            "CANCELLED" => GatewayPaymentState.Cancelled,
            "EXPIRED" => GatewayPaymentState.Expired,
            "FAILED" => GatewayPaymentState.Failed,
            _ => GatewayPaymentState.Pending   // PENDING, PROCESSING, ...
        };
        return new GatewayPaymentInfo(state, Convert.ToInt64(info.AmountPaid));
    }

    public async Task CancelLinkAsync(long orderCode, string? reason, CancellationToken ct = default)
        => await _client.Value.PaymentRequests.CancelAsync(orderCode, reason);

    public async Task<long?> VerifyWebhookAsync(string rawBody, CancellationToken ct = default)
    {
        try
        {
            var webhook = JsonSerializer.Deserialize<Webhook>(rawBody, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (webhook is null) return null;
            var data = await _client.Value.Webhooks.VerifyAsync(webhook);   // ném lỗi nếu sai chữ ký
            return data.OrderCode;
        }
        catch
        {
            return null;
        }
    }
}
