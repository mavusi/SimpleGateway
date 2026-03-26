using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using SimpleGateway.Api.Data;
using SimpleGateway.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;

namespace SimpleGateway.Api.Controllers
{
    [ApiController]
    [Route("admin")]
    public class AdminController : ControllerBase
    {
        private readonly GatewayDbContext _db;

        public AdminController(GatewayDbContext db)
        {
            _db = db;
        }

        // ----- Services -----
        [HttpGet("services")]
        public async Task<IActionResult> GetServices()
        {
            
            var services = await _db.Services.Include(s => s.Endpoints).ToListAsync();
            return Ok(services);
        }

        [HttpGet("services/{id}")]
        public async Task<IActionResult> GetService(string id)
        {
            
            var svc = await _db.Services.Include(s => s.Endpoints).FirstOrDefaultAsync(s => s.Id == id);
            if (svc == null) return NotFound();
            return Ok(svc);
        }

        [HttpPost("services")]
        public async Task<IActionResult> CreateService([FromBody] GatewayService service)
        {
            
            if (service == null) return BadRequest();
            if (string.IsNullOrWhiteSpace(service.Id)) service.Id = Guid.NewGuid().ToString("N");

            _db.Services.Add(service);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetService), new { id = service.Id }, service);
        }

        [HttpPut("services/{id}")]
        public async Task<IActionResult> UpdateService(string id, [FromBody] GatewayService updated)
        {
            
            var svc = await _db.Services.FirstOrDefaultAsync(s => s.Id == id);
            if (svc == null) return NotFound();

            svc.Name = updated.Name;
            svc.Url = updated.Url;
            svc.Path = updated.Path;

            _db.Services.Update(svc);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("services/{id}")]
        public async Task<IActionResult> DeleteService(string id)
        {
            
            var svc = await _db.Services.Include(s => s.Endpoints).FirstOrDefaultAsync(s => s.Id == id);
            if (svc == null) return NotFound();

            _db.Services.Remove(svc);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // ----- Endpoints -----
        [HttpGet("endpoints")]
        public async Task<IActionResult> GetEndpoints()
        {

            var endpoints = await _db.Endpoints.ToListAsync();
            return Ok(endpoints);
        }

        [HttpGet("endpoints/{id}")]
        public async Task<IActionResult> GetEndpoint(string id)
        {

            var ep = await _db.Endpoints.FirstOrDefaultAsync(e => e.Id == id);
            if (ep == null) return NotFound();
            return Ok(ep);
        }

        [HttpPost("endpoints")]
        public async Task<IActionResult> CreateEndpoint([FromBody] GatewayEndpoint endpoint)
        {
            
            if (endpoint == null) return BadRequest();
            if (string.IsNullOrWhiteSpace(endpoint.Id)) endpoint.Id = Guid.NewGuid().ToString("N");

            // ensure service exists
            var svc = await _db.Services.FirstOrDefaultAsync(s => s.Id == endpoint.ServiceId);
            if (svc == null) return BadRequest("ServiceId does not reference an existing service");

            _db.Endpoints.Add(endpoint);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEndpoint), new { id = endpoint.Id }, endpoint);
        }

        [HttpPut("endpoints/{id}")]
        public async Task<IActionResult> UpdateEndpoint(string id, [FromBody] GatewayEndpoint updated)
        {
            
            var ep = await _db.Endpoints.FirstOrDefaultAsync(e => e.Id == id);
            if (ep == null) return NotFound();

            // if ServiceId changed, ensure it exists
            if (!string.Equals(ep.ServiceId, updated.ServiceId, StringComparison.OrdinalIgnoreCase))
            {
                var svc = await _db.Services.FirstOrDefaultAsync(s => s.Id == updated.ServiceId);
                if (svc == null) return BadRequest("ServiceId does not reference an existing service");
                ep.ServiceId = updated.ServiceId;
            }

            ep.Method = updated.Method;
            ep.Path = updated.Path;

            _db.Endpoints.Update(ep);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("endpoints/{id}")]
        public async Task<IActionResult> DeleteEndpoint(string id)
        {
            
            var ep = await _db.Endpoints.FirstOrDefaultAsync(e => e.Id == id);
            if (ep == null) return NotFound();

            _db.Endpoints.Remove(ep);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
