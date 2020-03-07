using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocKeeper.Package;
using DocKeeper.ZipStreamer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace DocKeeper.Controllers
{
    [Route("api/library")]
    [ApiController]
    public class LibraryController : ControllerBase
    {
        private readonly IOptions<ZipStreamerOptions> _zipOptions;

        public LibraryController(IOptions<ZipStreamerOptions> zipOptions)
        {
            _zipOptions = zipOptions;
        }

        [HttpGet]
        public async Task<IActionResult> GetLibraries()
        {
            if (!Directory.Exists(_zipOptions.Value.LibraryPath))
                return Problem("Cant read Library Path");

            var items = Directory.EnumerateFiles(_zipOptions.Value.LibraryPath, "*.zip")
                //.Concat(Directory.EnumerateFiles(_zipOptions.Value.LibraryPath, "*.nupkg"))
                .Select(PackageInfo.FromZip)
                .Select(x => new
                {
                    x.PackageId,
                    x.Version,
                    Url = GetPackageUrl(x.PackageId, x.Version),
                    Info = GetInfoUrl(x.PackageId, x.Version)
                })
                .GroupBy(x => x.PackageId)
                .ToDictionary(x => x.Key, x => x.Select(x => new { x.Url, x.Version, x.Info }));

            return Ok(items);
        }

        [HttpGet("{package}/{version}")]
        public async Task<IActionResult> GetLibrary(string package, string version)
        {
            if (!Directory.Exists(_zipOptions.Value.LibraryPath))
                return NotFound();

            var zipPath = Path.Combine(_zipOptions.Value.LibraryPath, $"{package}.{version}.zip");
            if (!System.IO.File.Exists(zipPath))
                return NotFound();

            return Ok(PackageInfo.FromZip(zipPath));
        }



        private string GetPackageUrl(string package, string version)
        {
            return $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}/library/{package}/{version}";
        }

        private string GetInfoUrl(string package, string version)
        {
            return $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}/api/library/{package}/{version}";
        }
    }
}