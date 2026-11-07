// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Bộ lọc chặn bạn đọc chưa có thẻ thư viện đang hoạt động (chưa có thẻ, chờ duyệt, bị từ chối, bị khóa) khỏi phiếu mượn, gia hạn, tiền phạt. Không ảnh hưởng thủ thư / Admin.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;

/// <summary>
/// Dùng cùng [AuthorizeRole]: [AuthorizeRole(VaiTro.BanDoc)] [RequireActiveCard].
/// Chỉ tác động khi người dùng là Bạn đọc; người chưa đăng nhập vẫn do AuthorizeRole xử lý.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequireActiveCardAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        var current = http.RequestServices.GetRequiredService<ICurrentUserService>();

        if (!current.IsInRole(VaiTro.BanDoc))
            return;

        if (await current.HasActiveBanDocAsync(http.RequestAborted))
            return;

        if (http.Request.Headers.XRequestedWith == "XMLHttpRequest")
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            return;
        }

        var tempData = http.RequestServices.GetRequiredService<ITempDataDictionaryFactory>().GetTempData(http);
        tempData["Loi"] = "Bạn cần có thẻ thư viện đang hoạt động để dùng chức năng này.";
        context.Result = new RedirectToActionResult("MyProfile", "BanDoc", null);
    }
}
