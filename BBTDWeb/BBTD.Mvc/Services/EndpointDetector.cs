using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Identity.Client;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace BBTD.Mvc.Services
{
    public interface IEndpointDetector
    {
        string GetEndpoint();
    }

    public class EndpointDetector : IEndpointDetector
    {
        private readonly IServer _server;
        private readonly IWiFiInterfaceDetector _wiFiInterfaceDetector;

        public EndpointDetector(IServer server, IWiFiInterfaceDetector wiFiInterfaceDetector)
        {
            _server = server;
            _wiFiInterfaceDetector = wiFiInterfaceDetector;
        }

        public string GetEndpoint() =>
            $"http://{GetWiFiAddress()}:{GetPort()}";

        private string GetWiFiAddress() =>
            _wiFiInterfaceDetector.GetWiFiAddress();

        private string GetPort()
        {
            var addressFeature = _server.Features.Get<IServerAddressesFeature>();
            var firstAddress = addressFeature.Addresses.FirstOrDefault();

            if (firstAddress == null)
                return null;

            var port = firstAddress.Split(":").Last();
            return port;
        }
    }
}
