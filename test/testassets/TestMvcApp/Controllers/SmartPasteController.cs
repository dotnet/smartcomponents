using Microsoft.AspNetCore.Mvc;

namespace TestMvcApp.Controllers;

public class SmartPasteController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}