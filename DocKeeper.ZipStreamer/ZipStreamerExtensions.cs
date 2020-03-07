using System;
using DocKeeper.ZipStreamer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DocKeeper
{
    public static class ZipStreamerExtensions
    {
        public static IApplicationBuilder UseZipStreamer(this IApplicationBuilder builder, Action<ZipStreamerOptions> setup = null)
        {
            var options = builder.ApplicationServices.GetService<IOptions<ZipStreamerOptions>>()?.Value ??
                          new ZipStreamerOptions();
            setup?.Invoke(options);
            return builder.UseMiddleware<ZipStreamerMiddleware>(options);
        }

        public static IServiceCollection ConfigureZipStreamer(this IServiceCollection services, IConfiguration topLevel)
        {
            return services.Configure<ZipStreamerOptions>(topLevel.GetSection("ZipStreamer"));
        }
    }
}