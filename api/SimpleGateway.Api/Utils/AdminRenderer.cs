using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SimpleGateway.Api.Data;
using SimpleGateway.Api.Models;

namespace SimpleGateway.Api.Utils
{
    public static class AdminRenderer
    {
        public static string ReadTemplate(string contentRootPath, string relativePath)
        {
            var root = Path.Combine(contentRootPath, "wwwroot", "admin");
            var full = Path.Combine(root, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(full) ? File.ReadAllText(full) : string.Empty;
        }

        public static async Task<string> BuildServicesListHtml(GatewayDbContext db, string contentRootPath)
        {
            var services = await db.Services.OrderBy(s => s.Name).ToListAsync();
            var rows = new StringBuilder();
            foreach (var s in services)
            {
                rows.AppendLine("<tr>");
                rows.AppendLine($"  <td class=\"px-4 py-2 text-sm text-gray-700\">{System.Net.WebUtility.HtmlEncode(s.Id)}</td>");
                rows.AppendLine($"  <td class=\"px-4 py-2 text-sm text-gray-700\">{System.Net.WebUtility.HtmlEncode(s.Name)}</td>");
                rows.AppendLine($"  <td class=\"px-4 py-2 text-sm text-gray-700\">{System.Net.WebUtility.HtmlEncode(s.Url)}</td>");
                rows.AppendLine("  <td class=\"px-4 py-2 text-sm text-right\">\n" +
                                $"    <button class=\"mr-2 px-2 py-1 bg-yellow-500 text-white rounded\" hx-get=\"/admin/partials/services/form/{System.Net.WebUtility.UrlEncode(s.Id)}\" hx-target=\"#main\" hx-swap=\"innerHTML\">Edit</button>\n" +
                                $"    <button class=\"px-2 py-1 bg-red-600 text-white rounded\" hx-delete=\"/admin/services/{System.Net.WebUtility.UrlEncode(s.Id)}\" hx-confirm=\"Are you sure?\" hx-target=\"#main\" hx-swap=\"innerHTML\">Delete</button>\n"
                                );
                rows.AppendLine("</tr>");
            }
            var tpl = ReadTemplate(contentRootPath, "partials/services-list.html");
            return tpl.Replace("{{rows}}", rows.ToString());
        }

        public static async Task<string> BuildEndpointsListHtml(GatewayDbContext db, string contentRootPath)
        {
            var endpoints = await db.Endpoints.OrderBy(e => e.Path).ToListAsync();
            var services = await db.Services.ToDictionaryAsync(s => s.Id, s => s.Name);
            var rows = new StringBuilder();
            foreach (var e in endpoints)
            {
                var svcName = services.ContainsKey(e.ServiceId) ? System.Net.WebUtility.HtmlEncode(services[e.ServiceId]) : System.Net.WebUtility.HtmlEncode(e.ServiceId);
                rows.AppendLine("<tr>");
                rows.AppendLine($"  <td class=\"px-4 py-2 text-sm text-gray-700\">{System.Net.WebUtility.HtmlEncode(e.Id)}</td>");
                rows.AppendLine($"  <td class=\"px-4 py-2 text-sm text-gray-700\">{svcName}</td>");
                rows.AppendLine($"  <td class=\"px-4 py-2 text-sm text-gray-700\">{System.Net.WebUtility.HtmlEncode(e.Path)}</td>");
                rows.AppendLine($"  <td class=\"px-4 py-2 text-sm text-gray-700\">{System.Net.WebUtility.HtmlEncode(e.Method)}</td>");
                rows.AppendLine("  <td class=\"px-4 py-2 text-sm text-right\">\n" +
                                $"    <button class=\"mr-2 px-2 py-1 bg-yellow-500 text-white rounded\" hx-get=\"/admin/partials/endpoints/form/{System.Net.WebUtility.UrlEncode(e.Id)}\" hx-target=\"#main\" hx-swap=\"innerHTML\">Edit</button>\n" +
                                $"    <button class=\"px-2 py-1 bg-red-600 text-white rounded\" hx-delete=\"/admin/endpoints/{System.Net.WebUtility.UrlEncode(e.Id)}\" hx-confirm=\"Are you sure?\" hx-target=\"#main\" hx-swap=\"innerHTML\">Delete</button>\n"
                                );
                rows.AppendLine("</tr>");
            }
            var tpl = ReadTemplate(contentRootPath, "partials/endpoints-list.html");
            return tpl.Replace("{{rows}}", rows.ToString());
        }
    }
}
