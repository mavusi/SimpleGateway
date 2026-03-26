using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SimpleGateway.Api.Data;
using SimpleGateway.Api.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleGateway.Api.Controllers
{
    public class EndpointsController : Controller
    {
        private readonly GatewayDbContext _db;

        public EndpointsController(GatewayDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var endpoints = await _db.Endpoints.ToListAsync();
            return View(endpoints);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var ep = await _db.Endpoints.FirstOrDefaultAsync(e => e.Id == id);
            if (ep == null) return NotFound();
            return View(ep);
        }

        public async Task<IActionResult> Create(string serviceId = null)
        {
            ViewBag.Services = await _db.Services.ToListAsync();
            return View(new GatewayEndpoint { ServiceId = serviceId });
        }

        [HttpPost]
        public async Task<IActionResult> Create(GatewayEndpoint endpoint)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Services = await _db.Services.ToListAsync();
                return View(endpoint);
            }

            if (string.IsNullOrWhiteSpace(endpoint.Id)) endpoint.Id = Guid.NewGuid().ToString("N");

            // ensure service exists
            var svc = await _db.Services.FirstOrDefaultAsync(s => s.Id == endpoint.ServiceId);
            if (svc == null)
            {
                ModelState.AddModelError("ServiceId", "Service not found");
                ViewBag.Services = await _db.Services.ToListAsync();
                return View(endpoint);
            }

            _db.Endpoints.Add(endpoint);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var ep = await _db.Endpoints.FirstOrDefaultAsync(e => e.Id == id);
            if (ep == null) return NotFound();
            ViewBag.Services = await _db.Services.ToListAsync();
            return View(ep);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, GatewayEndpoint updated)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Services = await _db.Services.ToListAsync();
                return View(updated);
            }

            var ep = await _db.Endpoints.FirstOrDefaultAsync(e => e.Id == id);
            if (ep == null) return NotFound();

            // if ServiceId changed, ensure it exists
            if (!string.Equals(ep.ServiceId, updated.ServiceId, StringComparison.OrdinalIgnoreCase))
            {
                var svc = await _db.Services.FirstOrDefaultAsync(s => s.Id == updated.ServiceId);
                if (svc == null)
                {
                    ModelState.AddModelError("ServiceId", "Service not found");
                    ViewBag.Services = await _db.Services.ToListAsync();
                    return View(updated);
                }
                ep.ServiceId = updated.ServiceId;
            }

            ep.Method = updated.Method;
            ep.Path = updated.Path;

            _db.Endpoints.Update(ep);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var ep = await _db.Endpoints.FirstOrDefaultAsync(e => e.Id == id);
            if (ep == null) return NotFound();
            return View(ep);
        }

        [HttpPost]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var ep = await _db.Endpoints.FirstOrDefaultAsync(e => e.Id == id);
            if (ep == null) return NotFound();
            _db.Endpoints.Remove(ep);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
