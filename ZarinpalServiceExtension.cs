using Microsoft.Extensions.DependencyInjection;

namespace Zarinpal.Api
{
    public static class ZarinpalServiceExtension
    {
        public static IServiceCollection AddZarinpal(this IServiceCollection service)
        {
            service.AddScoped<IZarinpalProvider, ZarinpalProvider>();
            return service;
        }
    }
}