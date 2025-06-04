using dizparc_elevate;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;

namespace dizparc_elevate
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();

                    webBuilder.ConfigureKestrel(serverOptions =>
                    {
                        serverOptions.AddServerHeader = false; // Remove server header
                        serverOptions.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB limit
                        serverOptions.Limits.MaxRequestHeadersTotalSize = 32 * 1024; // 32KB limit (updated property name)
                        serverOptions.Limits.MaxRequestLineSize = 8 * 1024; // 8KB limit
                        serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
                        serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
                    });
                });
    }
}