using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using Newtonsoft.Json;

namespace SRI_Facturas
{
    public partial class Inicio : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }
        protected void btnEmitidas_Click(object sender, EventArgs e)
        {
            Response.Redirect("DescargaSRIEmitidas.aspx");
        }

        protected void btnRecibidas_Click(object sender, EventArgs e)
        {
            Response.Redirect("DescargaSRIRecibidas.aspx");
        }
    }
}

