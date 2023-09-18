using Microsoft.Extensions.Configuration;
using System.Linq;

//namespace BBTD.Mvc.Services
//{
//    public interface ISerilogInterfaceDetector
//    {
//        string GetEndpoint();
//    }

//    public class SerilogInterfaceDetector : ISerilogInterfaceDetector
//    {
//        private readonly IWiFiInterfaceDetector _wiFiInterfaceDetector;
//        private readonly IConfiguration _configuration;

//        public SerilogInterfaceDetector(IWiFiInterfaceDetector wiFiInterfaceDetector, IConfiguration configuration)
//        {
//            _wiFiInterfaceDetector = wiFiInterfaceDetector;
//            _configuration = configuration;
//        }

//        public string GetEndpoint() =>
//            $"{GetWiFiAddress()}:{GetPort()}";

//        private string GetWiFiAddress() =>
//            _wiFiInterfaceDetector.GetWiFiAddress();

//        private string GetPort()
//        {
//            var graylogPort = 
//                _configuration
//                    .GetSection("Serilog:WriteTo")
//                    .GetChildren()
//                    .FirstOrDefault(section => section["Name"] == "Graylog")
//                    ?.GetSection("Args")["port"] ?? "12201";

//            return graylogPort;
//        }
//    }
//}
