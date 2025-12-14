using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PlaywrightWorkerService.Jobs
{
    public class PlaywrightJob : BackgroundService
    {
        private readonly ILogger<PlaywrightJob> _logger;

        public PlaywrightJob(ILogger<PlaywrightJob> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Iniciando JOB del SRI...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await EjecutarProcesoSRI();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error ejecutando el proceso del SRI");
                }

                _logger.LogInformation("JOB completado. Esperando 1 minuto...");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task EjecutarProcesoSRI()
        {
            _logger.LogInformation("Iniciando Playwright...");

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            var page = await browser.NewPageAsync();

            // 1. Ir al SRI
            _logger.LogInformation("Accediendo al SRI...");
            await page.GotoAsync("https://srienlinea.sri.gob.ec/sri-en-linea/consultas/comprobantes-electronicos");

            // 2. Cargar archivo XML (aquí defines tu archivo)
            string xmlPath = @"D:\Nayeli\SRI\SRI\Pruebas\factura.xml";
            _logger.LogInformation("Cargando archivo: " + xmlPath);

            await page.SetInputFilesAsync("input[type='file']", xmlPath);

            // 3. Hacer clic en “Consultar”
            await page.ClickAsync("#botonConsultar");

            // 4. Esperar resultado
            await page.WaitForSelectorAsync("#resultadoConsulta");

            string respuesta = await page.InnerTextAsync("#resultadoConsulta");

            _logger.LogInformation("Respuesta del SRI: " + respuesta);

            // 5. Guardar resultado en archivo
            string logFile = @"D:\Nayeli\SRI\SRI\Logs\resultado_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
            File.WriteAllText(logFile, respuesta);

            _logger.LogInformation("Resultado guardado en: " + logFile);
        }
    }
}
