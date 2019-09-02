using System.Threading.Tasks;
using Zarinpal.Api.Models;

namespace Zarinpal.Api
{
    public interface IZarinpalProvider
    {
        ZarinpalResult<ZarinpalPaymentResponseModel> InvokePayment(ZarinpalPaymentRequestModel model);
        Task<ZarinpalResult<ZarinpalPaymentResponseModel>> InvokePaymentAsync(ZarinpalPaymentRequestModel model);

        ZarinpalResult<ZarinpalVerificationResponseModel> InvokePaymentVerification(ZarinpalPaymentVerificationModel model);
        Task<ZarinpalResult<ZarinpalVerificationResponseModel>> InvokePaymentVerificationAsync(ZarinpalPaymentVerificationModel model);
    }
}