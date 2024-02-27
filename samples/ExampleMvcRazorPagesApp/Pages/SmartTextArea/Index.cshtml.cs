using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExampleMvcRazorPagesApp.Pages.SmartTextArea
{
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
            ViewData["Title"] = "Smart TextArea";
        }
    }
}
