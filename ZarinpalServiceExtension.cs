using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Zarinpal.Api
{
    public static class ZarinpalServiceExtension
    {
        public static IServiceCollection AddZarinpal(this IServiceCollection service)
        {
            service.AddHttpClient<IZarinpalProvider, ZarinpalProvider>();
            service.AddSingleton<JsonSerializer>();
            return service;
        }
    }
}