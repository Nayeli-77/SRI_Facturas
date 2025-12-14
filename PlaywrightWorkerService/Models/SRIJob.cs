namespace PlaywrightWorkerService.Models
{
    public class SRIJob
    {
        public string JobId { get; set; }
        public string Tipo { get; set; }    // "EMITIDAS" o "RECIBIDAS"

        public string Status { get; set; }
        public string Ruc { get; set; }
        public string Ci { get; set; }
        public string Clave { get; set; }

        // Emitidas
        public string Fecha { get; set; }
        public string Estado { get; set; }
        public string TipoComprobante { get; set; }
        public string Establecimiento { get; set; }

        // Recibidas
        public string Ano { get; set; }
        public string Mes { get; set; }
        public string Dia { get; set; }

        public DateTime CreatedAt { get; set; }
    }

}
