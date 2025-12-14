
using PlaywrightWorkerService;
using PlaywrightWorkerService.Jobs;
using PlaywrightWorkerService.Services;



var builder = Host.CreateApplicationBuilder(args);

// --- CONFIGURAR PLAYWRIGHT ---
Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", @"D:\Nayeli\SRI\PlaywrightBrowsers");

// Registrar servicio PlaywrightJob como Hosted Service
//builder.Services.AddHostedService<PlaywrightJob>();
// Inyección de dependencias
builder.Services.AddSingleton<SRIService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

