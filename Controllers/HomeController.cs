using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
