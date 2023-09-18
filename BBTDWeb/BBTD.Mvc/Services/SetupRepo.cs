using BBTD.Mvc.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Text.Json;

namespace BBTD.Mvc.Services
{
    public interface ISetupRepo
    {
        SetupData GetData(bool isReset = false);
        void SetData(SetupData setupData);
        void ResetData();
        string? GetRawJson();
    }

    public class SetupRepo : ISetupRepo
    {
        private readonly IEndpointDetector _networkInterfaceDetector;
        //private readonly ISerilogInterfaceDetector _serilogInterfaceDetector;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SetupRepo(
            IEndpointDetector networkInterfaceDetector,
            //ISerilogInterfaceDetector serilogInterfaceDetector,
            IHttpContextAccessor httpContextAccessor)
        {
            _networkInterfaceDetector = networkInterfaceDetector;
            //_serilogInterfaceDetector = serilogInterfaceDetector;
            _httpContextAccessor = httpContextAccessor;
        }

        private SetupData? _setupData;

        public SetupData GetData(bool isReset = false)
        {
            var ctx = _httpContextAccessor.HttpContext!;

            if (!isReset)
            {
                // If we already have value, just return it
                if (_setupData != null)
                    return _setupData;

                // If we don't, try to read it from cookie
                ctx.Request.Cookies.TryGetValue("jsonSetup", out string? jsonSetupFromCookie);
                if (jsonSetupFromCookie != null)
                {
                    _setupData = JsonSerializer.Deserialize<SetupData>(jsonSetupFromCookie);
                    return _setupData!;
                }
            }

            // If it's not reset request or we just don't have a cookie value, write cookie and return value
            _setupData = new SetupData
            {
                ServerUrl = _networkInterfaceDetector.GetEndpoint(),
                //GraylogUrl = _serilogInterfaceDetector.GetEndpoint(),
                NumberOfItems = 100,
                BarcodeSize = 200,
                BarcodeType = ZXing.BarcodeFormat.QR_CODE,
                TimeoutMilliseconds = 3000
            };

            var jsonSetup = JsonSerializer.Serialize(_setupData);
            var options = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(30)
            };
            ctx.Response.Cookies.Append("jsonSetup", jsonSetup, options);

            return _setupData;
        }

        public void SetData(SetupData newData)
        {
            var ctx = _httpContextAccessor.HttpContext!;

            _setupData = newData;

            var jsonSetup = JsonSerializer.Serialize(_setupData);
            var options = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(30)
            };
            ctx.Response.Cookies.Append("jsonSetup", jsonSetup, options);
        }

        public void ResetData() => GetData(isReset: true);

        public string? GetRawJson()
        {
            var ctx = _httpContextAccessor.HttpContext!;

            ctx.Request.Cookies.TryGetValue("jsonSetup", out string? jsonSetupFromCookie);

            return jsonSetupFromCookie;
        }
    }
}
