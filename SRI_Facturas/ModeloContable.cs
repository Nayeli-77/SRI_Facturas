using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SRI_Facturas
{
    public class ModeloContable
    {
        public enum TipoDocumento
        {
            Emitida,
            Recibida
        }

        public class CompraExcel
        {
            public TipoDocumento Tipo { get; set; }
            public string RazonSocial { get; set; }
            public DateTime FechaEmision { get; set; }
            public decimal ValorSinImpuestos { get; set; }
            public decimal Iva { get; set; }
            public decimal ImporteTotal { get; set; }
        }

    }
}