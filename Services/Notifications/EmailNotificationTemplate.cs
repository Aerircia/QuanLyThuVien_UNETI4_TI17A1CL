// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: Xây dựng template email thông báo
using System.Net;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Notifications;

public static class EmailNotificationTemplate
{
    public static string Build(string name, string title, string message, string? url)
    {
        var safeName = WebUtility.HtmlEncode(name); var safeTitle = WebUtility.HtmlEncode(title);
        var safeMessage = WebUtility.HtmlEncode(message).Replace("\r\n", "<br>").Replace("\n", "<br>");
        var link = string.IsNullOrWhiteSpace(url) ? "/ThongBao" : url;
        return $"""<!doctype html><html lang="vi"><body style="margin:0;background:#f4f7f2;font-family:Arial,sans-serif;color:#263326"><table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="padding:28px 10px"><tr><td align="center"><table role="presentation" width="600" cellpadding="0" cellspacing="0" style="max-width:600px;background:#fff;border-radius:16px;overflow:hidden"><tr><td style="background:#477b43;padding:24px 30px;color:#fff"><div style="font-size:20px;font-weight:700">THƯ VIỆN UNETI</div><div style="font-size:12px;margin-top:5px;opacity:.9">KẾT NỐI TRI THỨC – LAN TỎA TƯƠNG LAI</div></td></tr><tr><td style="padding:30px"><p style="font-size:15px">Xin chào <b>{safeName}</b>,</p><h2 style="color:#315f32;font-size:22px">{safeTitle}</h2><div style="font-size:15px;line-height:1.7;color:#4b5848">{safeMessage}</div><p style="margin:28px 0"><a href="{WebUtility.HtmlEncode(link)}" style="display:inline-block;background:#477b43;color:#fff;text-decoration:none;padding:12px 22px;border-radius:8px;font-weight:700">XEM CHI TIẾT</a></p><p style="font-size:12px;color:#879184">Đây là email tự động từ Thư viện UNETI. Vui lòng không trả lời email này.</p></td></tr></table></td></tr></table></body></html>""";
    }
}
