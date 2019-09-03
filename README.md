#### Msh.Zarinpal.Api
A light weight library *with respect to (zarinpal-dotNet)[https://github.com/ZarinPal-Lab/zarinpal-dotNet]* to help online payment using *[Zarinpal](https://www.zarinpal.com)* for `.NET core 2.2` web application.

#### Installation
You can download the latest version of `Msh.Zarinpal.Api` from [Github repository](https://github.com/hrsh/Msh.Zarinpal.Api).
To install via `nuget`:
```
Install-Package Msh.Zarinpal.Api -Version 1.0.0
```
Install from [Nuget](https://www.nuget.org/packages/Msh.Zarinpal.Api/) directly.

#### How to use
Add these codes to `startup.cs` file:
``` C#
using Zarinpal.Api;
using Zarinpal.Api.Models;
...
    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<ZarinpalConfiguration>(opt => Configuration.GetSection("ZarinpalConfig").Bind(opt));
        ...
        services.AddZarinpal();
    }
...
```
and some where in `appsettings.json`
``` json
  "ZarinpalConfig": {
    "Token": "Put your api token here",
    "UseSandbox": false
  }
```
in `controller`:
``` C#
using Zarinpal.Api;
using Zarinpal.Api.Models;
...
    public class HomeController : Controller
    {
        private readonly IZarinpalProvider _zarinpal;

        public HomeController(IZarinpalProvider zarinpal)
        {
            _zarinpal = zarinpal;
        }

        public IActionResult Index(string referenceId = null)
        {
            if (string.IsNullOrWhiteSpace(referenceId))
                return View();
            return Content(referenceId);
        }

        public async Task<IActionResult> StartPayment()
        {
            var model = new ZarinpalPaymentRequestModel
            {
                Amount = 2000,
                Description = "Some description goes here",
                CallbackUrl = "https://<domain>.com/PaymentResult",
                Email = "Client email", 
                Mobile = "Client mobile"
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
    }
...
```
