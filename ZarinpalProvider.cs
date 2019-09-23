using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Zarinpal.Api.Models;

namespace Zarinpal.Api
{
    public class ZarinpalProvider : IZarinpalProvider
    {
        private readonly ZarinpalConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _requestUrl;
        private readonly string _verifyUrl;

        public ZarinpalProvider(IOptionsSnapshot<ZarinpalConfiguration> options, HttpClient httpClient)
        {
            _httpClient = httpClient;

            if (_httpClient == null)
                throw new ArgumentNullException(nameof(_httpClient));

            var zarinpalConfiguration = options;
            if (zarinpalConfiguration == null)
                throw new ArgumentNullException(nameof(zarinpalConfiguration));

            _configuration = zarinpalConfiguration.Value;
            if (_configuration == null)
                throw new ArgumentNullException(nameof(_configuration));

            _requestUrl = ZarinpalUrlConfig.GetPaymentRequestUrl(_configuration.UseSandbox);
            _verifyUrl = ZarinpalUrlConfig.GetVerificationUrl(_configuration.UseSandbox);
        }

        private static ZarinpalResult<string> StartPayment<T>(T model, string method, string url)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = method;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                if (method.Equals(ZarinpalRequestMethod.Post))
                {
                    var json = JsonConvert.SerializeObject(model);
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
            }

            HttpWebResponse httpResponse;
            try
            {
                httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            }
            catch
            {
                return ZarinpalResult<string>.Failed(new ZarinpalError
                {
                    Code = "-1000",
                    Description = "Server not found!"
                });
            }

            using (var streamReader = new StreamReader(httpResponse.GetResponseStream() ?? throw new InvalidOperationException()))
            {
                var result = streamReader.ReadToEnd();
                return ZarinpalResult<string>.Invoke(result);
            }
        }

        private static async Task<ZarinpalResult<string>> StartPaymentAsync<T>(T model, string method, string url)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = method;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                if (method.Equals(ZarinpalRequestMethod.Post))
                {
                    var json = JsonConvert.SerializeObject(model);
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
            }
            HttpWebResponse httpResponse;
            try
            {
                httpResponse = (HttpWebResponse)await httpWebRequest.GetResponseAsync();
            }
            catch (Exception e)
            {
                return ZarinpalResult<string>.Failed(new ZarinpalError
                {
                    Code = "-1000",
                    Description = $"Server not found! {e}"
                });
            }

            using (var streamReader = new StreamReader(httpResponse.GetResponseStream() ?? throw new InvalidOperationException()))
            {
                var result = streamReader.ReadToEnd();
                return ZarinpalResult<string>.Invoke(result);
            }
        }

        private async Task<ZarinpalResult<T>> PostRequestBase<T, TU>(TU model, string url) where T : class
        {
            try
            {
                var t = await _httpClient.PostAsync(url, 
                    new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json"));
                var f = await t.Content.ReadAsAsync<T>();
                return ZarinpalResult<T>.Invoke(f);
            }
            catch
            {
                return ZarinpalResult<T>.Failed(new ZarinpalError
                {
                    Code = "8000",
                    Description = "Could not send request!"
                });
            }
        }

        [Obsolete("This method is obsolete. Use Pay instead")]
        public ZarinpalResult<ZarinpalPaymentResponseModel> InvokePayment(ZarinpalPaymentRequestModel model)
        {
            var errors = new List<ZarinpalError>();
            model.ValidateModel(errors);
            if (errors.Any())
                return ZarinpalResult<ZarinpalPaymentResponseModel>.Failed(errors.ToArray());

            model.MerchantId = _configuration.Token;
            var request = StartPayment(model, ZarinpalRequestMethod.Post, _requestUrl);
            if (!request.Succeeded)
                return ZarinpalResult<ZarinpalPaymentResponseModel>.Failed(request.Errors.ToArray());

            var response = JsonConvert.DeserializeObject<ZarinpalPaymentResponseModel>(request.Result);
            if (response == null)
                return ZarinpalResult<ZarinpalPaymentResponseModel>.Failed();
            response.PaymentUrl = ZarinpalUrlConfig.GetPaymenGatewayUrl(response.Authority, _configuration.UseSandbox);
            var t = ZarinpalResult<ZarinpalPaymentResponseModel>.Invoke(response);
            t.Result.Validate(errors);

            return errors.Any() ? ZarinpalResult<ZarinpalPaymentResponseModel>.Failed(errors.ToArray()) : t;
        }

        [Obsolete("This method is obsolete. Use PayAsync instead")]
        public async Task<ZarinpalResult<ZarinpalPaymentResponseModel>> InvokePaymentAsync(ZarinpalPaymentRequestModel model)
        {
            var errors = new List<ZarinpalError>();
            model.ValidateModel(errors);
            if (errors.Any())
                return ZarinpalResult<ZarinpalPaymentResponseModel>.Failed(errors.ToArray());

            model.MerchantId = _configuration.Token;

            //var x = await PostRequestBase<ZarinpalPaymentResponseModel, ZarinpalPaymentRequestModel>(model, _requestUrl);
            
            var request = await StartPaymentAsync(model, ZarinpalRequestMethod.Post, _requestUrl);
            if (!request.Succeeded)
                return ZarinpalResult<ZarinpalPaymentResponseModel>.Failed(request.Errors.ToArray());

            var response = JsonConvert.DeserializeObject<ZarinpalPaymentResponseModel>(request.Result);
            if (response == null)
                return ZarinpalResult<ZarinpalPaymentResponseModel>.Failed();
            response.PaymentUrl = ZarinpalUrlConfig.GetPaymenGatewayUrl(response.Authority, _configuration.UseSandbox);
            var t = ZarinpalResult<ZarinpalPaymentResponseModel>.Invoke(response);
            t.Result.Validate(errors);

            return errors.Any() ? ZarinpalResult<ZarinpalPaymentResponseModel>.Failed(errors.ToArray()) : t;
        }

        public async Task<ZarinpalResult<ZarinpalVerificationResponseModel>> VerifyAsync(ZarinpalPaymentVerificationModel model)
        {
            var errors = new List<ZarinpalError>();

            model = new ZarinpalPaymentVerificationModel(_configuration.Token, model.Amount, model.Authority);
            model.ValidateModel(errors);

            if (errors.Any())
                return ZarinpalResult<ZarinpalVerificationResponseModel>.Failed(errors.ToArray());

            var t = await PostRequestBase<ZarinpalVerificationResponseModel, ZarinpalPaymentVerificationModel>(
                model,
                _verifyUrl);

            return !t.Succeeded ? 
                ZarinpalResult<ZarinpalVerificationResponseModel>.Failed(errors.ToArray()) : 
                ZarinpalResult<ZarinpalVerificationResponseModel>.Invoke(t.Result);
        }

        [Obsolete("This method is obsolete. Use Verify instead")]
        public ZarinpalResult<ZarinpalVerificationResponseModel> InvokePaymentVerification(ZarinpalPaymentVerificationModel model)
        {
            var errors = new List<ZarinpalError>();
            model = new ZarinpalPaymentVerificationModel(_configuration.Token, model.Amount, model.Authority);
            model.ValidateModel(errors);

            if (errors.Any())
                return ZarinpalResult<ZarinpalVerificationResponseModel>.Failed(errors.ToArray());

            var response = StartPayment(model, ZarinpalRequestMethod.Post, _verifyUrl);

            if (string.IsNullOrWhiteSpace(response.Result))
                return ZarinpalResult<ZarinpalVerificationResponseModel>.Failed(new ZarinpalError { Code = "-6000", Description = "Could not fetch response." });

            if (!response.Succeeded)
                return ZarinpalResult<ZarinpalVerificationResponseModel>.Failed(new ZarinpalError { Code = "-7000", Description = "could not read response." });

            var verificationResult = JsonConvert.DeserializeObject<ZarinpalVerificationResponseModel>(response.Result);
            return ZarinpalResult<ZarinpalVerificationResponseModel>.Invoke(verificationResult);
        }

        [Obsolete("This method is obsolete. Use VerifyAsync instead")]
        public async Task<ZarinpalResult<ZarinpalVerificationResponseModel>> InvokePaymentVerificationAsync(ZarinpalPaymentVerificationModel model)
        {
            var errors = new List<ZarinpalError>();

            model = new ZarinpalPaymentVerificationModel(_configuration.Token, model.Amount, model.Authority);
            model.ValidateModel(errors);

            if (errors.Any())
                return ZarinpalResult<ZarinpalVerificationResponseModel>.Failed(errors.ToArray());

            var response = await StartPaymentAsync(model, ZarinpalRequestMethod.Post, _verifyUrl);

            if (string.IsNullOrWhiteSpace(response.Result))
                return ZarinpalResult<ZarinpalVerificationResponseModel>.Failed();

            if (!response.Succeeded)
                return ZarinpalResult<ZarinpalVerificationResponseModel>.Failed();

            var verificationResult = JsonConvert.DeserializeObject<ZarinpalVerificationResponseModel>(response.Result);
            return ZarinpalResult<ZarinpalVerificationResponseModel>.Invoke(verificationResult);
        }

        public async Task<ZarinpalResult<ZarinpalPaymentResponseModel>> PayAsync(ZarinpalPaymentRequestModel model)
        {
            var errors = new List<ZarinpalError>();
            model.ValidateModel(errors);
            if (errors.Any())
                return ZarinpalResult<ZarinpalPaymentResponseModel>.Failed(errors.ToArray());

            model.MerchantId = _configuration.Token;
            var t = await PostRequestBase<ZarinpalPaymentResponseModel, ZarinpalPaymentRequestModel>(
                model,
                _requestUrl);

            if(!t.Succeeded)
            {
                return ZarinpalResult<ZarinpalPaymentResponseModel>.Failed(errors.ToArray());
            }

            t.Result.PaymentUrl = ZarinpalUrlConfig.GetPaymenGatewayUrl(t.Result.Authority, false);
            t.Result.Validate(errors);

            return errors.Any()
                ? ZarinpalResult<ZarinpalPaymentResponseModel>.Failed(errors.ToArray())
                : ZarinpalResult<ZarinpalPaymentResponseModel>.Invoke(t.Result);
        }
    }
}