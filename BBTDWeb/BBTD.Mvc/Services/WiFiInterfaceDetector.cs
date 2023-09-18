using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Identity.Client;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace BBTD.Mvc.Services
{
    public interface IWiFiInterfaceDetector
    {
        string GetWiFiAddress();
    }

    public class WiFiInterfaceDetector : IWiFiInterfaceDetector
    {
        private readonly IServer _server;

        public WiFiInterfaceDetector(IServer server)
        {
            _server = server;
        }

        public string GetWiFiAddress()
        {
            var firstUpInterface =
                NetworkInterface
                    .GetAllNetworkInterfaces()
                    .OrderByDescending(c => c.Speed)
                    .FirstOrDefault(c =>
                        c.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 &&
                        c.OperationalStatus == OperationalStatus.Up);

            if (firstUpInterface == null)
                return "127.0.0.1";

            var props = firstUpInterface.GetIPProperties();

            var firstIpV4Address =
                props.UnicastAddresses
                    .Where(c => c.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(c => c.Address)
                    .FirstOrDefault();

            if (firstIpV4Address == null)
                return "127.0.0.1";

            return firstIpV4Address.ToString();
        }
    }
}
