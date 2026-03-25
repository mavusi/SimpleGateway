namespace SimpleGateway.Api.Models
{
    public class GatewayEndpoint
    {
        public string Id { get; set; }
        public string ServiceId { get; set; }
        public string Method { get; set; }
        public string Path { get; set; }
        
        // Navigation property back to service (optional)
        public GatewayService Service { get; set; }
    }
}