using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleGateway.Api.Data;
using SimpleGateway.Api.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleGateway.Api.Controllers
{
    public class ServicesController : Controller
    {
        private readonly GatewayDbContext _db;

        public ServicesController(GatewayDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var services = await _db.Services.Include(s => s.Endpoints).ToListAsync();
            return View(services);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var svc = await _db.Services.Include(s => s.Endpoints).FirstOrDefaultAsync(s => s.Id == id);
            if (svc == null) return NotFound();
            return View(svc);
        }

        public IActionResult Create()
        {
            return View(new GatewayService());
        }

        [HttpPost]
        public async Task<IActionResult> Create(GatewayService service)
        {
            if (!ModelState.IsValid) return View(service);
            if (string.IsNullOrWhiteSpace(service.Id)) service.Id = Guid.NewGuid().ToString("N");
            _db.Services.Add(service);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var svc = await _db.Services.Include(s => s.Endpoints).FirstOrDefaultAsync(s => s.Id == id);
            if (svc == null) return NotFound();
            return View(svc);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, GatewayService updated)
        {
            if (!ModelState.IsValid) return View(updated);
            var svc = await _db.Services.FirstOrDefaultAsync(s => s.Id == id);
            if (svc == null) return NotFound();
            svc.Name = updated.Name;
            svc.Url = updated.Url;
            svc.Path = updated.Path;
            _db.Services.Update(svc);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var svc = await _db.Services.Include(s => s.Endpoints).FirstOrDefaultAsync(s => s.Id == id);
            if (svc == null) return NotFound();
            return View(svc);
        }

        [HttpPost]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var svc = await _db.Services.Include(s => s.Endpoints).FirstOrDefaultAsync(s => s.Id == id);
            if (svc == null) return NotFound();
            _db.Services.Remove(svc);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
