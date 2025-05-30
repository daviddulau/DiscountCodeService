using DiscountCodeService.Loggers;
using DiscountCodeService.Repositories;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace DiscountCodeService;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

		builder.WebHost.ConfigureKestrel(o =>
		{
			o.ListenAnyIP(5000, p => p.Protocols = HttpProtocols.Http2);
			// generous limits for high‑throughput traffic
			o.Limits.MaxRequestHeaderCount = 128;
			o.AddServerHeader = false;
		});

		builder.Services.AddGrpc(options =>
		{
			options.MaxReceiveMessageSize = 4 * 1024 * 1024; // 4 MB
			options.IgnoreUnknownServices = true;
		});

		// Add services to the container.

		builder.Services.AddSingleton<IDiscountRepository, LiteDbDiscountRepository>();

		builder.Services.AddLogging(cfg =>
		{
			cfg.ClearProviders();
			cfg.AddProvider(new LoggerProvider("logs/app.log"));
		});

            var app = builder.Build();

		app.MapGrpcService<Services.DiscountService>();
		app.MapGet("/", () => "DiscountCodes gRPC service – use a gRPC client.");

		await app.RunAsync();
	}
    }
