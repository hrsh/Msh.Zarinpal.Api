using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Zarinpal.Api;
using Zarinpal.Api.Models;
using Zarinpal.Test.WebApp.Models;

namespace Zarinpal.Test.WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IOptionsSnapshot<ZarinpalConfiguration> _options;
        private readonly IZarinpalProvider _zarinpal;

        public HomeController(IOptionsSnapshot<ZarinpalConfiguration> options, IZarinpalProvider zarinpal)
        {
            _options = options;
            _zarinpal = zarinpal;
        }

        public IActionResult Index(string referenceId = null)
        {
            if (string.IsNullOrWhiteSpace(referenceId))
                return View();
            return Content(referenceId);
        }

        public async Task<IActionResult> Privacy()
        {
            var model = new ZarinpalPaymentRequestModel
            {
                Amount = 2000,
                Description = "tozihat",
                CallbackUrl = "https://localhost:44343/home/PaymentResult",
                Email = "name@email.com",
                Mobile = "*****"
            };
            var t = await _zarinpal.InvokePaymentAsync(model);

            if (t.Succeeded)
                return Redirect(t.Result.PaymentUrl);

            return BadRequest(t.Errors);
        }

        public IActionResult PaymentResult(string authority, string status)
        {
            var t = long.Parse(authority);
            var result = _zarinpal.InvokePaymentVerification(new ZarinpalPaymentVerificationModel(2000, authority));

            if (result.Succeeded)
                return RedirectToAction("Index", new { referenceId = result.Result.ReferenceId });

            return BadRequest(result.Errors);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
