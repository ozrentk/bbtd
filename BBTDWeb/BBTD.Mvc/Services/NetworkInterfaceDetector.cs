using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Identity.Client;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace BBTD.Mvc.Services
{
    public interface INetworkInterfaceDetector
    {
        string GetWiFiAddress();
        string GetPort();
        string GetAuthority();
    }

    public class NetworkInterfaceDetector : INetworkInterfaceDetector
    {
        private readonly IServer _server;

        public NetworkInterfaceDetector(IServer server)
        {
            _server = server;
        }

        public string GetAuthority() =>
            $"http://{GetWiFiAddress()}:{GetPort()}";

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

        public string GetPort()
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
