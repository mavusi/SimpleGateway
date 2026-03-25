using System.Collections.Generic;

namespace SimpleGateway.Api.Models
{
    public class GatewayService
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string Path { get; set; }

        // Navigation property for related endpoints
        public ICollection<GatewayEndpoint> Endpoints { get; set; }
            = new List<GatewayEndpoint>();

    }
}
