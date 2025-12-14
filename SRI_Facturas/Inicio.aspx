<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Inicio.aspx.cs" Inherits="TuProyecto.Inicio" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Descargas SRI</title>
    <style>
        .container {
            width: 400px;
            margin: 80px auto;
            padding: 25px;
            border: 1px solid #ccc;
            border-radius: 12px;
            text-align: center;
            font-family: Arial;
        }
        .btn {
            width: 250px;
            padding: 12px;
            margin: 10px 0;
            font-size: 16px;
            cursor: pointer;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <div class="container">
        <h2>Descargas SRI</h2>

        <asp:Button ID="btnEmitidas" CssClass="btn" Text="Descargar Emitidas"
            runat="server" OnClick="btnEmitidas_Click" />

        <asp:Button ID="btnRecibidas" CssClass="btn" Text="Descargar Recibidas"
            runat="server" OnClick="btnRecibidas_Click" />
    </div>
    </form>
</body>
</html>
