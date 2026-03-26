namespace SimpleGateway.DataAccess.Models
{
    public class GatewayEndpoint
    {
        public string Id { get; set; }
        public string ServiceId { get; set; }
        public string Method { get; set; }
        public string Path { get; set; }
    }
}