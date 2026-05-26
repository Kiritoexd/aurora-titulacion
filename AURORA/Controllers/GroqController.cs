using Microsoft.AspNetCore.Mvc;
using AURORA.Servicios;
using System.Threading.Tasks;

namespace AURORA.Controllers
{
    public class GroqController : Controller
    {
        private readonly GroqService _groqService;
        [HttpPost]
        public async Task<IActionResult> IA(string prompt)
        {
            var result = await _groqService.GetCompletionAsync(prompt);
            ViewBag.Resultado = result;
            return View();
        }

        public GroqController(GroqService groqService)
        {
            _groqService = groqService;
        }

        [HttpGet]
        public IActionResult IA()
        {
            return View();
        }

    }
}