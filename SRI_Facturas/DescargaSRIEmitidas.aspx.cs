using SRI_Facturas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Web.UI;
using ClosedXML.Excel;
using System.Globalization;

using System.Web.UI.WebControls;

// Para corregir el error CS0246, asegúrate de que el paquete ClosedXML esté instalado en tu proyecto.
// Si usas NuGet, ejecuta el siguiente comando en la Consola del Administrador de paquetes:
// Install-Package ClosedXML

// Si usas .NET Core/Standard, también puedes ejecutar:
// dotnet add package ClosedXML

// No es necesario modificar el código fuente si el using ya está presente:
// using ClosedXML.Excel;

// Solo asegúrate de que la referencia al ensamblado ClosedXML esté correctamente agregada al proyecto.
namespace SRIWebDownloader.Pages
{
    public partial class DescargaSRI : System.Web.UI.Page
    {
        private string BaseDir => @"D:\Nayeli\SRI\PlaywrightJobs\";

        private string PendingDir => Path.Combine(BaseDir, "Pending");
        private string ProcessingDir => Path.Combine(BaseDir, "Processing");
        private string CompletedDir => Path.Combine(BaseDir, "Completed");
        private string ErrorDir => Path.Combine(BaseDir, "Error");

        protected void Page_Load(object sender, EventArgs e)
        {
            //tmRefresh.Enabled = false;   // 🔥 inicia auto-refresco
        }

        protected void btnEnviarJob_Click(object sender, EventArgs e)
        {
            try
            {
                Directory.CreateDirectory(PendingDir);

                string id = Guid.NewGuid().ToString();
                Session["LastJobId"] = id;

                var job = new SRIJob
                {
                    JobId = id,
                    Tipo = "EMITIDAS",
                    Ruc = txtCedula.Text.Trim(),
                    Ci = "",
                    Clave = txtClave.Text.Trim(),
                    Fecha = txtFecha.Text.Trim(),
                    Estado = ddlEstado.SelectedValue,
                    TipoComprobante = dpTipo.SelectedValue,
                    Establecimiento = "",
                    CreatedAt = DateTime.UtcNow,
                    Status = "PENDING"
                };

                string json = JsonSerializer.Serialize(job, new JsonSerializerOptions { WriteIndented = true });

                string path = Path.Combine(PendingDir, id + ".json");
                File.WriteAllText(path, json);

                lblMsg.Text = "✅ Job enviado. Esperando procesamiento...";
                tmRefresh.Enabled = true;   // 🔥 inicia auto-refresco
                txtLog.Text = json;
            }
            catch (Exception ex)
            {
                lblMsg.Text = "❌ Error enviando job: " + ex.Message;
            }
        }

        // 🔥 SE EJECUTA CADA 5 SEGUNDOS
        protected void tmRefresh_Tick(object sender, EventArgs e)
        {
            LoadJobStatus();
        }
        // --- LEE EL ESTADO DEL JOB EN CUALQUIER CARPETA ---
        private void LoadJobStatus()
        {
            try
            {
                if (Session["LastJobId"] == null)
                    return;

                string id = Session["LastJobId"].ToString();

                string file = FindJobFile(id);
                if (file == null)
                {
                    txtLog.Text = "Job no encontrado.";
                    return;
                }

                txtLog.Text = File.ReadAllText(file);

                string folder = new DirectoryInfo(Path.GetDirectoryName(file)).Name;

                lblMsg.Text = "Estado actual: " + folder.ToUpper();

                if (folder == "Completed" || folder == "Error")
                {
                    tmRefresh.Enabled = false;   // 🔥 detiene auto-refresh cuando termina
                }
            }
            catch (Exception ex)
            {
                txtLog.Text = "Error leyendo estado: " + ex.Message;
            }
        }

        private string FindJobFile(string jobId)
        {
            string fileName = jobId + ".json";

            string pending = Path.Combine(PendingDir, fileName);
            if (File.Exists(pending)) return pending;

            string processing = Path.Combine(ProcessingDir, fileName);
            if (File.Exists(processing)) return processing;

            string completed = Path.Combine(CompletedDir, fileName);
            if (File.Exists(completed)) return completed;

            string error = Path.Combine(ErrorDir, fileName);
            if (File.Exists(error)) return error;

            return null;
        }

        protected void btnProcesarCsv_Click(object sender, EventArgs e)
        {
            if (!fuCsv.HasFile)
            {
                lblMsg.Text = "Debe seleccionar un archivo CSV";
                return;
            }

            if (!Path.GetExtension(fuCsv.FileName).Equals(".csv"))
            {
                lblMsg.Text = "El archivo debe ser CSV";
                return;
            }

            var compras = LeerCsv(fuCsv);
            var excel = GenerarExcelContable(compras);

            Response.Clear();
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            Response.AddHeader("content-disposition", "attachment;filename=AsientoContable.xlsx");
            Response.BinaryWrite(excel);
            Response.End();
        }

        private List<CompraExcel> LeerCsv(FileUpload archivo)
        {
            var lista = new List<CompraExcel>();

            using (var reader = new StreamReader(archivo.FileContent, Encoding.UTF8))
            {
                bool encabezado = true;

                while (!reader.EndOfStream)
                {
                    var linea = reader.ReadLine();

                    if (encabezado)
                    {
                        encabezado = false;
                        continue;
                    }

                    var c = linea.Split(';');

                    lista.Add(new CompraExcel
                    {
                        RazonSocial = c[1], // Tipo y serie
                        FechaEmision = DateTime.Parse(c[4]),
                        ValorSinImpuestos = decimal.Parse(c[5], CultureInfo.InvariantCulture),
                        Iva = decimal.Parse(c[6], CultureInfo.InvariantCulture),
                        ImporteTotal = decimal.Parse(c[7], CultureInfo.InvariantCulture)
                    });
                }
            }

            return lista;
        }
        private byte[] GenerarExcelContable(List<CompraExcel> compras)
        {
            using (var wb = new XLWorkbook())
                
            {
                var ws = wb.Worksheets.Add("Asiento Contable");

                ws.Cell(1, 1).Value = "Fecha";
                ws.Cell(1, 2).Value = "Detalle";
                ws.Cell(1, 4).Value = "Debe";
                ws.Cell(1, 5).Value = "Haber";

                int fila = 2;

                foreach (var c in compras)
                {
                    ws.Cell(fila, 1).Value = c.FechaEmision;
                    ws.Cell(fila, 2).Value = "Compras";
                    ws.Cell(fila, 4).Value = c.ValorSinImpuestos;
                    fila++;

                    ws.Cell(fila, 2).Value = "IVA compras";
                    ws.Cell(fila, 4).Value = c.Iva;
                    fila++;

                    ws.Cell(fila, 2).Value = "Bancos";
                    ws.Cell(fila, 5).Value = c.ImporteTotal;
                    fila++;

                    ws.Cell(fila, 2).Value = "Para registrar compra";
                    fila++;
                }

                ws.Columns().AdjustToContents();

                using (var ms = new MemoryStream())
                {
                    wb.SaveAs(ms);
                    return ms.ToArray();
                }
            }
        }
        public class CompraExcel
        {
            public DateTime FechaEmision { get; set; }
            public string RazonSocial { get; set; }
            public decimal ValorSinImpuestos { get; set; }
            public decimal Iva { get; set; }
            public decimal ImporteTotal { get; set; }
        }


    }
}
