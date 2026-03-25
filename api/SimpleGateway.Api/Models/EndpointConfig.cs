using System.ComponentModel.DataAnnotations;

namespace SimpleGateway.Api.Models
{
    public class EndpointConfig
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        public string ServiceId { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
    }
}
