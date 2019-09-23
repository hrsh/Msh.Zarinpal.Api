using System;
using System.Threading.Tasks;
using Zarinpal.Api.Models;

namespace Zarinpal.Api
{
    public interface IZarinpalProvider
    {
        [Obsolete("This method is obsolete. Use PayAsync instead")]
        ZarinpalResult<ZarinpalPaymentResponseModel> InvokePayment(ZarinpalPaymentRequestModel model);

        [Obsolete("This method is obsolete. Use PayAsync instead")]
        Task<ZarinpalResult<ZarinpalPaymentResponseModel>> InvokePaymentAsync(ZarinpalPaymentRequestModel model);

        Task<ZarinpalResult<ZarinpalPaymentResponseModel>> PayAsync(ZarinpalPaymentRequestModel model);
        Task<ZarinpalResult<ZarinpalVerificationResponseModel>> VerifyAsync(ZarinpalPaymentVerificationModel model);

        [Obsolete("This method is obsolete. Use VerifyAsync instead")]
        ZarinpalResult<ZarinpalVerificationResponseModel> InvokePaymentVerification(ZarinpalPaymentVerificationModel model);

        [Obsolete("This method is obsolete. Use VerifyAsync instead")]
        Task<ZarinpalResult<ZarinpalVerificationResponseModel>> InvokePaymentVerificationAsync(ZarinpalPaymentVerificationModel model);
    }
}