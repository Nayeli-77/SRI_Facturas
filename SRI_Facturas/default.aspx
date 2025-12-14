<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="default.aspx.cs" Inherits="SRI_Facturas._default" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Proyecto SRI Facturas</title>
    <link href="Resources/css/styles.css" rel="stylesheet" />
</head>
<body>
    <form id="form1" runat="server">

        <header class="header-espe">
            <div class="header-content">
                <div class="logo-space">
                    <img id="logoESPE" src="Resources/img/LogoEspe.png" alt="Logo ESPE" />
                </div>

                <h1 class="title">Proyecto SRI Facturas</h1>
            </div>
        </header>

        <main class="main-content">
            <div class="center-graphic">
                <img src="Resources/img/Espe.jpeg" class="img-espe" />
            </div>

            <div class="buttons-container">
                <asp:Button ID="btnEmitidas" CssClass="btn-action" Text="Descargar Emitidas"
                    runat="server" OnClick="btnEmitidas_Click" />

                <asp:Button ID="btnRecibidas" CssClass="btn-action" Text="Descargar Recibidas"
                    runat="server" OnClick="btnRecibidas_Click" />
            </div>
        </main>

        <footer class="footer">
            Autor: Sthefany Padilla
        </footer>


    </form>
</body>
</html>
