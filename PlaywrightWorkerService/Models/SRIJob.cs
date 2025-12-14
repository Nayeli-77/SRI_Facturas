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

    public class FacturaDetalle
    {
        public string RucEmisor { get; set; } 
        public string RazonSocialEmisor { get; set; }
        public string ClaveAcceso { get; set; }
        public string FechaEmision { get; set; }

        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public decimal Subtotal { get; set; }
        public decimal IVA { get; set; }
    }


}
