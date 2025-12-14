<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="DescargaSRIEmitidas.aspx.cs" Inherits="SRIWebDownloader.Pages.DescargaSRI" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Descarga SRI</title>
    <link href="Resources/css/descargaEmitidas.css" rel="stylesheet" />
    <script src="Resources/js/vallidacionEmitidas.js"></script>
</head>

<body>
    <form id="form1" runat="server">

        <!-- HEADER (igual formato que default.aspx) -->
        <header class="header-espe">
            <div class="header-content">
                <div class="logo-space">
                    <img src="Resources/img/LogoEspe.png" alt="Logo ESPE" />
                </div>

                <h1 class="title">Descarga de Comprobantes SRI</h1>

                <!-- BOTÓN INICIO (casita) en la cabecera, alineado a la derecha -->
                <div class="header-actions">
                    <a class="btn-home" href="default.aspx" title="Ir al inicio">🏠 Inicio</a>
                </div>
            </div>
        </header>

        <!-- MAIN -->
        <main class="main-section">

            <div class="container">

                <h2 class="sub-title">Parámetros de Descarga</h2>

                <div class="input">
                    <label>Cédula / RUC</label>
                    <asp:TextBox ID="txtCedula" runat="server" CssClass="textbox"  ClientIDMode="Static"></asp:TextBox>
                </div>

                <div class="input">
                    <label>Clave de acceso</label>
                    <asp:TextBox ID="txtClave" runat="server" CssClass="textbox" TextMode="Password" ClientIDMode="Static"></asp:TextBox>
                </div>

                <div class="input">
                    <label>Fecha</label>
                    <!-- TextMode Date (renderiza input type=date en navegadores modernos) -->
                    <asp:TextBox ID="txtFecha" runat="server" CssClass="textbox-small" TextMode="Date" ClientIDMode="Static"></asp:TextBox>
                </div>

                <div class="input">
                    <label>Estado</label>
                    <asp:DropDownList ID="ddlEstado" runat="server" CssClass="textbox-small" ClientIDMode="Static">
                        <asp:ListItem Value="">-- Seleccione --</asp:ListItem>
                        <asp:ListItem Value="Autorizados">Autorizados</asp:ListItem>
                        <asp:ListItem Value="No Autorizados">No Autorizados</asp:ListItem>
                        <asp:ListItem Value="Por Procesar">Por Procesar</asp:ListItem>
                    </asp:DropDownList>
                </div>

                <div class="input">
                    <label>Tipo</label>
                    <asp:DropDownList ID="dpTipo" runat="server" CssClass="textbox" ClientIDMode="Static">
                        <asp:ListItem Value="">-- Seleccione --</asp:ListItem>
                        <asp:ListItem Value="Factura">Factura</asp:ListItem>
                        <asp:ListItem Value="Notas de Débito">Notas de Débito</asp:ListItem>
                        <asp:ListItem Value="Comprobante de Retención">Comprobante de Retención</asp:ListItem>
                        <asp:ListItem Value="Liquidación de compra de bienes y servicios">Liquidación de compra de bienes y servicios</asp:ListItem>
                    </asp:DropDownList>
                </div>

                <!-- ScriptManager (UpdatePanel funciona) -->
                <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePartialRendering="true"></asp:ScriptManager>

                <!-- Botón: llama a la validación JS externa usando ClientID -->
                <asp:Button ID="btnEnviarJob" runat="server" CssClass="btn-action"
                    Text="Enviar Job"
                    OnClientClick="return validarFormularioSRI();"
                    OnClick="btnEnviarJob_Click" />

                <h3 class="sub-title">Estado del Proceso</h3>

                <asp:UpdatePanel ID="UpdatePanel1" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>

                        <asp:Label ID="lblMsg" runat="server" CssClass="msg"></asp:Label>

                        <asp:TextBox ID="txtLog" runat="server" TextMode="MultiLine"
                            Rows="10" CssClass="log-box" ReadOnly="true"></asp:TextBox>

                        <asp:Timer ID="tmRefresh" runat="server" Interval="5000"
                            OnTick="tmRefresh_Tick" Enabled="false"></asp:Timer>

                    </ContentTemplate>
                    <Triggers>
                        <asp:AsyncPostBackTrigger ControlID="tmRefresh" EventName="Tick" />
                    </Triggers>
                </asp:UpdatePanel>

            </div>

        </main>

        <!-- FOOTER -->
        <footer class="footer">
            Autor: Sthefany Padilla 
        </footer>

    </form>
</body>
</html>
