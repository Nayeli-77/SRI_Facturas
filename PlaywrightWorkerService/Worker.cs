using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PlaywrightWorkerService.Models;
using PlaywrightWorkerService.Services;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PlaywrightWorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly string _basePath = @"D:\Nayeli\SRI\PlaywrightJobs";
        private readonly SRIService _sriService;

        private readonly string _downloadsPath = @"D:\Nayeli\SRI\XML";


        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _sriService = new SRIService();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio en modo espera. Esperando jobs de la web...");

            Directory.CreateDirectory(_basePath + @"\Pending");
            Directory.CreateDirectory(_basePath + @"\Processing");
            Directory.CreateDirectory(_basePath + @"\Completed");
            Directory.CreateDirectory(_basePath + @"\Error");

            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcesarJobsAsync();
                await Task.Delay(2000, stoppingToken); // revisa cada 2 segundos
            }
        }

        private async Task ProcesarJobsAsync()
        {
            string pending = Path.Combine(_basePath, "Pending");
            string processing = Path.Combine(_basePath, "Processing");
            string completed = Path.Combine(_basePath, "Completed");
            string error = Path.Combine(_basePath, "Error");

            var archivos = Directory.GetFiles(pending, "*.json");

            foreach (var archivo in archivos)
            {
                string fileName = Path.GetFileName(archivo);
                string procesando = Path.Combine(processing, fileName);

                try
                {
                    // Mover job a Processing
                    File.Move(archivo, procesando, true);
                    ActualizarStatus(procesando, "PROCESSING");
                    // Leer el JSON
                    string json = File.ReadAllText(procesando);
                    var job = JsonSerializer.Deserialize<SRIJob>(json);

                    if (job == null)
                        throw new Exception("El archivo JSON no corresponde al modelo SRIJob.");

                    // Carpeta de salida para el job
                    //string destino = Path.Combine(_downloadsPath, job.JobId);
                    string destino = Path.Combine(_downloadsPath);
                    Directory.CreateDirectory(destino);

                    bool ok = false;

                    // =============================
                    //     SELECCIÓN DEL PROCESO
                    // =============================
                    if (job.Tipo?.ToUpper() == "EMITIDAS")
                    {
                        ok = await _sriService.DescargarEmitidasAsync(
                            job.Ruc,
                            job.Ci,
                            job.Clave,
                            job.Fecha,
                            job.Estado,
                            job.TipoComprobante,
                            job.Establecimiento,
                            destino
                        );
                    }

                    else if (job.Tipo?.ToUpper() == "RECIBIDAS")
                    {
                        ok = await _sriService.DescargarRecibidasAsync(
                            job.Ruc,
                            job.Ci,
                            job.Clave,
                            job.Ano,
                            job.Mes,
                            job.Dia ?? "0",
                            job.TipoComprobante,
                            destino
                        );
                    }
                    else
                    {
                        throw new Exception($"Tipo de job no válido: {job.Tipo}. Debe ser EMITIDAS o RECIBIDAS.");
                    }
                    
                    // =============================
                    //     RESULTADO
                    // =============================
                    if (ok)
                    { 
                        ActualizarStatus(procesando, "COMPLETO: ARCHIVO DESCARGADO CON EXITO");
                        File.Move(procesando, Path.Combine(completed, fileName), true);
                    }
                    else
                    { 
                        ActualizarStatus(procesando, "ERROR: Validar consola para detalles");
                    File.Move(procesando, Path.Combine(error, fileName), true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando job");

                    try
                    {
                        File.Move(procesando, Path.Combine(error, fileName), true);
                    }
                    catch { }
                }
            }
        }
        private void ActualizarStatus(string filePath, string status)
        {
            string json = File.ReadAllText(filePath);
            var job = JsonSerializer.Deserialize<SRIJob>(json);

            if (job == null) return;

            job.Status = status;

            json = JsonSerializer.Serialize(job, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);

            _logger.LogInformation($"Status actualizado a: {status} para {filePath}");
        }

    }
}



