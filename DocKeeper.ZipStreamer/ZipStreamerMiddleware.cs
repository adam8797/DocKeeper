using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DocKeeper.ZipStreamer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.StaticFiles;

namespace DocKeeper
{
    public class ZipStreamerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ZipStreamerOptions _options;
        private readonly TemplateMatcher _requestMatcher;
        private FileExtensionContentTypeProvider _mime;

        public ZipStreamerMiddleware(RequestDelegate next, ZipStreamerOptions options)
        {
            _next = next;
            _options = options;
            _requestMatcher =
                new TemplateMatcher(TemplateParser.Parse(_options.RouteTemplate), new RouteValueDictionary());
            _mime = new FileExtensionContentTypeProvider();
        }

        // GET: 
        public async Task InvokeAsync(HttpContext context)
        {
            // Is this our request?
            if (!RequestingZipComponent(context.Request, out var zipPath, out var innerPath))
            {
                await _next(context);
                return;
            }

            // Figure out where this zip lives
            var fullZipPath = Path.Combine(_options.LibraryPath, zipPath);
            if (!File.Exists(fullZipPath))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }

            try
            {
                // Read the zip
                var zip = ZipFile.OpenRead(fullZipPath);

                // Entry we want to stream
                ZipArchiveEntry entry = null;

                // Looking for the default document
                if (innerPath == null)
                {
                    // Try each default document
                    foreach (var defDoc in _options.DefaultDocuments)
                    {
                        // Check if it exists.
                        entry = zip.GetEntry(Path.Combine(_options.ZipContentPath, defDoc).Replace('\\', '/'));
                        if (entry != null)
                            break;
                    }
                }
                else
                {
                    entry = zip.GetEntry(Path.Combine(_options.ZipContentPath, innerPath).Replace('\\', '/'));
                }

                // couldn't find entry
                if (entry == null)
                {
                    context.Response.StatusCode = (int) HttpStatusCode.NotFound;
                    return;
                }

                // attempt to get the MIME type
                if (_mime.TryGetContentType(Path.GetFileName(entry.Name), out var contentType))
                {
                    context.Response.ContentType = contentType;
                }
                else
                {
                    // Safe default
                    context.Response.ContentType = "application/octet-stream";
                }

                // Send it back
                context.Response.StatusCode = 200;
                await entry.Open().CopyToAsync(context.Response.Body);
            }
            catch
            {
                // Catch all as internal server error
                context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
            }
        }

        private bool RequestingZipComponent(HttpRequest request, out string zipPath, out string innerPath)
        {
            zipPath = null;
            innerPath = null;

            if (request.Method != "GET")
                return false;

            var routeValues = new RouteValueDictionary();

            if (_requestMatcher.TryMatch(request.Path, routeValues) 
                && routeValues.ContainsKey("package")
                && routeValues.ContainsKey("version")
                && routeValues.ContainsKey("file"))
            {
                zipPath = $"{routeValues["package"]}.{routeValues["version"]}.zip";
                innerPath = routeValues["file"]?.ToString();
                return true;
            }

            return false;

        }
    }
}
