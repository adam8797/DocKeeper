using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocKeeper.ZipStreamer
{
    public class ZipStreamerOptions
    {
        public string RouteTemplate { get; set; } = "/library/{package}/{version}/{*file}";

        public string LibraryPath { get; set; } = "Library";

        public string[] DefaultDocuments { get; set; }

        public string ZipContentPath { get; set; } = "site";
    }
}
