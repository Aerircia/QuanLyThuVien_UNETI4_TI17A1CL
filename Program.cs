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
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
//Dẫn đường đến thư mục chứa ảnh bìa sách
var bookCoverDirectory = Path.Combine(app.Environment.ContentRootPath, "Uploads", "Book");
Directory.CreateDirectory(bookCoverDirectory);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(bookCoverDirectory),
    RequestPath = "/book-covers"
});

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
