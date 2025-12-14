using System;
using System.IO;
using System.Text.Json;
using System.Web.UI;
using SRI_Facturas;

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

    }
}
