using System;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;

namespace DocKeeper.Package
{
    public class PackageInfo
    {
        public PackageInfo()
        {

        }

        public static PackageInfo FromZip(string zipPath)
        {
            var zip = ZipFile.OpenRead(zipPath);

            var jsonEntry = zip.GetEntry("package.json");

            if (jsonEntry == null)
                return null;

            using var reader = new StreamReader(jsonEntry.Open());
            return JsonConvert.DeserializeObject<PackageInfo>(reader.ReadToEnd());
        }

        public string PackageId { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Version { get; set; }
    }
}
