<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="DescargaSRIRecibidas.aspx.cs" Inherits="SRI_Facturas.DescargaSRIRecibidas" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Descarga SRI Recibidas</title>
    <link href="Resources/css/descargaRecibidas.css" rel="stylesheet" />
    <script src="Resources/js/validacionRecibidas.js"></script>
</head>

<body>
    <form id="form1" runat="server">

        <!-- HEADER -->
        <header class="header-espe">
            <div class="header-content">
                <div class="logo-space">
                    <img src="Resources/img/LogoEspe.png" alt="Logo ESPE" />
                </div>

                <h1 class="title">Descarga de Comprobantes Recibidos</h1>

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
                    <asp:TextBox ID="txtCedula" runat="server" CssClass="textbox" ClientIDMode="Static"></asp:TextBox>
                </div>

                <div class="input">
                    <label>Clave de acceso</label>
                    <asp:TextBox ID="txtClave" runat="server" CssClass="textbox" TextMode="Password" ClientIDMode="Static"></asp:TextBox>
                </div>

                <div class="input">
                    <label>Año</label>
                    <asp:DropDownList ID="dpAnio" runat="server" CssClass="textbox-small" ClientIDMode="Static">
                        <asp:ListItem Value="">-- Seleccione --</asp:ListItem>
                        <asp:ListItem Value="2025">2025</asp:ListItem>
                        <asp:ListItem Value="2024">2024</asp:ListItem>
                        <asp:ListItem Value="2023">2023</asp:ListItem>
                        <asp:ListItem Value="2022">2022</asp:ListItem>
                        <asp:ListItem Value="2021">2021</asp:ListItem>
                    </asp:DropDownList>
                </div>

                <div class="input">
                    <label>Mes</label>
                    <asp:DropDownList ID="ddlMes" runat="server" CssClass="textbox-small" ClientIDMode="Static">
                        <asp:ListItem Value="">-- Seleccione --</asp:ListItem>
                        <asp:ListItem Value="Enero">Enero</asp:ListItem>
                        <asp:ListItem Value="Febrero">Febrero</asp:ListItem>
                        <asp:ListItem Value="Marzo">Marzo</asp:ListItem>
                        <asp:ListItem Value="Abril">Abril</asp:ListItem>
                        <asp:ListItem Value="Mayo">Mayo</asp:ListItem>
                        <asp:ListItem Value="Junio">Junio</asp:ListItem>
                        <asp:ListItem Value="Julio">Julio</asp:ListItem>
                        <asp:ListItem Value="Agosto">Agosto</asp:ListItem>
                        <asp:ListItem Value="Septiembre">Septiembre</asp:ListItem>
                        <asp:ListItem Value="Octubre">Octubre</asp:ListItem>
                        <asp:ListItem Value="Noviembre">Noviembre</asp:ListItem>
                        <asp:ListItem Value="Diciembre">Diciembre</asp:ListItem>
                    </asp:DropDownList>
                </div>

                <div class="input">
                    <label>Día</label>
                            <asp:DropDownList ID="dpDia" runat="server" CssClass="textbox-small" ClientIDMode="Static">
                                <asp:ListItem Value="">-----Seleccionar-----</asp:ListItem>
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

                <!-- ScriptManager -->
                <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePartialRendering="true"></asp:ScriptManager>

                <!-- Botón con validación JS -->
                <asp:Button ID="btnEnviarJob" runat="server" CssClass="btn-action"
                    Text="Enviar Job"
                    OnClientClick="return validarFormularioRecibidas();"
                    OnClick="btnEnviar_Click" />

                <h3 class="sub-title">Estado del Proceso</h3>

                <!-- UPDATE PANEL -->
                <asp:UpdatePanel ID="UpdatePanel1" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>

                        <asp:Label ID="lblMsg" runat="server" CssClass="msg"></asp:Label>

                        <asp:TextBox ID="txtLog" runat="server" TextMode="MultiLine"
                            Rows="10" CssClass="log-box" ReadOnly="true"></asp:TextBox>

                        <!-- TIMER -->
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
