using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Services;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// MVC + Database
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddLibraryServices(
    builder.Configuration,
    builder.Environment);

var app = builder.Build();

// Seed database
await DbSeeder.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Loi");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Mọi mã lỗi không có nội dung (404 khi gõ sai đường dẫn, NotFound() trong controller...) được trả về trang Error.
app.UseStatusCodePagesWithReExecute("/Loi", "?statusCode={0}");

app.UseHttpsRedirection();

// Ảnh bìa upload lúc runtime không nằm trong static assets manifest (build-time),
// nên MapStaticAssets không serve được -> serve thư mục uploads bằng StaticFiles middleware.
var thuMucUploads = Path.Combine(
    app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"),
    "uploads", "bia");
Directory.CreateDirectory(thuMucUploads); // PhysicalFileProvider yêu cầu thư mục phải tồn tại
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(thuMucUploads),
    RequestPath = "/uploads/bia"
});

app.UseRouting();
app.UseSession();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapGet("/", () => Results.Redirect("/TrangChu"));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
