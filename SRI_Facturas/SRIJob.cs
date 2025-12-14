using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SRI_Facturas
{
    public class SRIJob
    {
        public string JobId { get; set; }
        public string Tipo { get; set; }    // "EMITIDAS" o "RECIBIDAS"
        public string Status { get; set; }
        public string Ruc { get; set; }
        public string Ci { get; set; }
        public string Ano { get; set; }
        public string Mes { get; set; }
        public string Dia { get; set; }
        public string Clave { get; set; }
        public string Fecha { get; set; }          // dd/mm/yyyy
        public string Estado { get; set; }         // AUT,NAT,PPR
        public string TipoComprobante { get; set; } // 1..6 (tu script usa "1".."6")
        public string Establecimiento { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}