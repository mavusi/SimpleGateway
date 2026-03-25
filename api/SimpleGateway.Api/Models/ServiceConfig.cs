using System.ComponentModel.DataAnnotations;

namespace SimpleGateway.Api.Models
{
    public class ServiceConfig
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
