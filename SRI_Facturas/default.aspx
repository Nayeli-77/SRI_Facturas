<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="default.aspx.cs" Inherits="SRI_Facturas._default" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Proyecto SRI Facturas</title>
    <link href="Resources/css/styles.css" rel="stylesheet" />
</head>
<body>
    <form id="form1" runat="server">

        <!-- HEADER -->
        <header class="header-espe">
            <div class="header-content">
                <div class="logo-space">
                    <img id="logoESPE" src="Resources/img/LogoEspe.png" alt="Logo ESPE" />
                </div>

                <h1 class="title">Proyecto SRI Facturas</h1>
            </div>
        </header>

        <!-- MAIN -->
        <main class="main-content">

            <div class="center-graphic">
                <img src="Resources/img/Espe.jpeg" class="img-espe" />
            </div>

            <!-- BOTONES EXISTENTES -->
            <div class="buttons-container">

                <asp:Button ID="btnEmitidas" CssClass="btn-action"
                    Text="Descargar Emitidas"
                    runat="server"
                    OnClick="btnEmitidas_Click" />

                <asp:Button ID="btnRecibidas" CssClass="btn-action"
                    Text="Descargar Recibidas"
                    runat="server"
                    OnClick="btnRecibidas_Click" />

            </div>

            <!-- NUEVA SECCIÓN CONTABLE -->
            <div class="contable-wrapper">

                <div class="contable-section">

                    <div class="contable-title">
                        Generación de Archivo Contable
                    </div>

                    <div class="file-upload-wrapper">
                        <asp:FileUpload ID="fuCarpetaCsv"
                            runat="server"
                            AllowMultiple="true" />
                    </div>

                    <!-- Habilita selección de carpeta -->
                    <script>
                        document.getElementById('<%= fuCarpetaCsv.ClientID %>')
                            .setAttribute('webkitdirectory', '');
                    </script>

                    <div class="help-text">
                        Seleccione una carpeta que contenga los archivos CSV de Emitidas y Recibidas
                    </div>

                    <asp:Button ID="btnGenerarContable"
                        CssClass="btn-action btn-contable"
                        Text="Generar Archivo Contable"
                        runat="server"
                        OnClick="btnGenerarContable_Click" />

                    <asp:Button ID="btnGenerarLibroDiario"
                        CssClass="btn-action btn-contable"
                        Text="Generar Libro Diario"
                        runat="server"
                        OnClick="btnGenerarLibroDiario_Click" />

                    <asp:Button ID="btnGenerarDebeHaber"
                        CssClass="btn-action btn-contable"
                        Text="Generar Debe Haber"
                        runat="server"
                        OnClick="btnGenerarDebeHaber_Click" />

                    <asp:Button ID="btnGenerarResumen"
                        CssClass="btn-action btn-contable"
                        Text="Generar Resumen"
                        runat="server"
                        OnClick="btnGenerarResumen_Click" />

                </div>

            </div>

        </main>

        <!-- FOOTER -->
        <footer class="footer">
            Autor: Sthefany Padilla
        </footer>

    </form>
</body>
</html>
