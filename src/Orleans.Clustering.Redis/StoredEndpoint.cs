using System.Net;

namespace Orleans.Clustering.Redis
{
    public class StoredEndpoint
    {
        public static implicit operator IPEndPoint(StoredEndpoint endpoint)
        {
            return new IPEndPoint(IPAddress.Parse(endpoint.Address), endpoint.Port);
        }

        public static implicit operator StoredEndpoint(IPEndPoint endPoint)
        {
            return new StoredEndpoint
            {
                Address = endPoint.Address.ToString(),
                Port = endPoint.Port
            };
        }

        public string Address { get; set; }
        public int Port { get; set; }
    }
}