using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using static SRI_Facturas.ModeloContable;

namespace SRI_Facturas
{
    public partial class _default : Page
    {
        protected void btnEmitidas_Click(object sender, EventArgs e)
        {
            Response.Redirect("DescargaSRIEmitidas.aspx");
        }

        protected void btnRecibidas_Click(object sender, EventArgs e)
        {
            Response.Redirect("DescargaSRIRecibidas.aspx");
        }

        protected void btnGenerarContable_Click(object sender, EventArgs e)
        {
            if (!fuCarpetaCsv.HasFiles) return;

            var lista = new List<CompraExcel>();

            foreach (HttpPostedFile file in fuCarpetaCsv.PostedFiles)
            {
                if (file.FileName.EndsWith(".csv"))
                    lista.AddRange(LeerCsvGeneral(file));
            }

            // ORDENAR POR FECHA
            lista = lista.OrderBy(x => x.FechaEmision).ToList();

            var excel = GenerarExcelContable(lista);

            Response.Clear();
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            Response.AddHeader("content-disposition", "attachment;filename=AsientoContable.xlsx");
            Response.BinaryWrite(excel);
            Response.End();
        }

        protected void btnGenerarLibroDiario_Click(object sender, EventArgs e)
        {
            if (!fuCarpetaCsv.HasFiles) return;

            var lista = new List<CompraExcel>();

            foreach (HttpPostedFile file in fuCarpetaCsv.PostedFiles)
            {
                if (file.FileName.EndsWith(".csv"))
                    lista.AddRange(LeerCsvGeneral(file));
            }

            // ORDENAR POR FECHA
            lista = lista.OrderBy(x => x.FechaEmision).ToList();

            var excel = GenerarExelLibroDiario(lista);

            Response.Clear();
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            Response.AddHeader("content-disposition", "attachment;filename=LibroDiario.xlsx");
            Response.BinaryWrite(excel);
            Response.End();
        }

        protected void btnGenerarDebeHaber_Click(object sender, EventArgs e)
        {
            if (!fuCarpetaCsv.HasFiles) return;

            var lista = new List<CompraExcel>();

            foreach (HttpPostedFile file in fuCarpetaCsv.PostedFiles)
            {
                if (file.FileName.EndsWith(".csv"))
                    lista.AddRange(LeerCsvGeneral(file));
            }

            // ORDENAR POR FECHA
            lista = lista.OrderBy(x => x.FechaEmision).ToList();

            var excel = GenerarExcelDebeHaber(lista);

            Response.Clear();
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            Response.AddHeader("content-disposition", "attachment;filename=DebeHaber.xlsx");
            Response.BinaryWrite(excel);
            Response.End();
        }

        protected void btnGenerarResumen_Click(object sender, EventArgs e)
        {
            if (!fuCarpetaCsv.HasFiles) return;

            var lista = new List<CompraExcel>();

            foreach (HttpPostedFile file in fuCarpetaCsv.PostedFiles)
            {
                if (file.FileName.EndsWith(".csv"))
                    lista.AddRange(LeerCsvGeneral(file));
            }

            // ORDENAR POR FECHA
            lista = lista.OrderBy(x => x.FechaEmision).ToList();

            var excel = GenerarExcelResumen(lista);

            Response.Clear();
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            Response.AddHeader("content-disposition", "attachment;filename=Resumen.xlsx");
            Response.BinaryWrite(excel);
            Response.End();
        }

        // ================= CSV =================

        private List<CompraExcel> LeerCsvGeneral(HttpPostedFile archivo)
        {
            var lista = new List<CompraExcel>();

            using (var reader = new StreamReader(archivo.InputStream, Encoding.UTF8))
            {
                var encabezado = reader.ReadLine().Split(';');
                var tipo = DetectarTipo(encabezado);

                while (!reader.EndOfStream)
                {
                    var linea = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(linea)) continue;

                    var c = linea.Split(';');

                    lista.Add(tipo == TipoDocumento.Recibida
                        ? LeerRecibida(c)
                        : LeerEmitida(c));
                }
            }
            return lista;
        }

        private TipoDocumento DetectarTipo(string[] columnas)
        {
            if (columnas.Any(x => x.ToLower().Contains("razón social emisor")))
                return TipoDocumento.Recibida;

            return TipoDocumento.Emitida;
        }

        private CompraExcel LeerRecibida(string[] c)
        {
            try
            {
                return new CompraExcel
                {
                    Tipo = TipoDocumento.Recibida,
                    RazonSocial = c[1],
                    FechaEmision = SafeDate(c[5]),
                    ValorSinImpuestos = SafeDecimal(c[6]),
                    Iva = SafeDecimal(c[7]),
                    ImporteTotal = SafeDecimal(c[8])
                };
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }


        private CompraExcel LeerEmitida(string[] c)
        {
            try
            {
                return new CompraExcel
                {
                    Tipo = TipoDocumento.Emitida,
                    RazonSocial = c[1],
                    FechaEmision = SafeDate(c[4]),
                    ValorSinImpuestos = SafeDecimal(c[5]),
                    Iva = SafeDecimal(c[6]),
                    ImporteTotal = SafeDecimal(c[7])
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }


        private decimal SafeDecimal(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return 0;

            valor = valor.Replace(",", ".").Trim();

            decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result);
            return result;
        }

        private DateTime SafeDate(string valor)
        {
            DateTime.TryParse(valor, out DateTime fecha);
            return fecha;
        }

        // ================= EXCEL =================
        private byte[] GenerarExcelContable(List<CompraExcel> datos)
        {
            try
            {
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Asiento Contable");

                    int fila = 1;
                    int nro = 1;

                    // ================= ENCABEZADOS =================
                    ws.Cell("A1").Value = "Fecha";
                    ws.Cell("B1").Value = "Detalle";
                    ws.Cell("D1").Value = "Debe";
                    ws.Cell("E1").Value = "Haber";
                    ws.Range("A1:E1").Style.Font.Bold = true;
                    fila++;

                    var compras = new List<decimal>();
                    var ivaCompras = new List<decimal>();
                    var ventas = new List<decimal>();
                    var ivaVentas = new List<decimal>();
                    var bancosDebe = new List<decimal>();
                    var bancosHaber = new List<decimal>();

                    // ================= ASIENTOS =================
                    foreach (var d in datos.OrderBy(x => x.FechaEmision))
                    {
                        ws.Cell(fila, 1).Value = d.FechaEmision;
                        ws.Cell(fila, 1).Style.DateFormat.Format = "dd/MM/yyyy";
                        ws.Cell(fila, 2).Value = nro;
                        fila++;

                        if (d.Tipo == TipoDocumento.Recibida)
                        {
                            ws.Cell(fila, 2).Value = "Compras";
                            ws.Cell(fila, 4).Value = d.ValorSinImpuestos;
                            compras.Add(d.ValorSinImpuestos);
                            fila++;

                            ws.Cell(fila, 2).Value = "IVA Compras";
                            ws.Cell(fila, 4).Value = d.Iva;
                            ivaCompras.Add(d.Iva);
                            fila++;

                            ws.Cell(fila, 2).Value = "Bancos";
                            ws.Cell(fila, 5).Value = d.ImporteTotal;
                            bancosHaber.Add(d.ImporteTotal);
                            fila++;
                        }
                        else
                        {
                            ws.Cell(fila, 2).Value = "Bancos";
                            ws.Cell(fila, 4).Value = d.ImporteTotal;
                            bancosDebe.Add(d.ImporteTotal);
                            fila++;

                            ws.Cell(fila, 2).Value = "Ventas";
                            ws.Cell(fila, 5).Value = d.ValorSinImpuestos;
                            ventas.Add(d.ValorSinImpuestos);
                            fila++;

                            ws.Cell(fila, 2).Value = "IVA Ventas";
                            ws.Cell(fila, 5).Value = d.Iva;
                            ivaVentas.Add(d.Iva);
                            fila++;
                        }

                        fila++;
                        nro++;
                    }

                    // ================= TABLA DE CÁLCULOS =================
                    int filaCalc = 1;

                    // ---- COMPRAS ----
                    ws.Range("G1:H1").Merge().Value = "COMPRAS";
                    ws.Cell("G1").Style.Font.Bold = true;
                    ws.Cell("G2").Value = "Debe";
                    ws.Cell("H2").Value = "Haber";
                    filaCalc = 3;
                    foreach (var v in compras)
                        ws.Cell(filaCalc++, 7).Value = v;
                    ws.Cell(filaCalc, 7).Value = compras.Sum();
                    ws.Cell(filaCalc, 7).Style.Font.Bold = true;
                    filaCalc += 2;

                    // ---- IVA COMPRAS ----
                    ws.Range("J1:K1").Merge().Value = "IVA COMPRAS";
                    ws.Cell("J1").Style.Font.Bold = true;
                    ws.Cell("J2").Value = "Debe";
                    ws.Cell("K2").Value = "Haber";
                    int filaIvaCompras = 3;
                    foreach (var v in ivaCompras)
                        ws.Cell(filaIvaCompras++, 10).Value = v;
                    ws.Cell(filaIvaCompras, 10).Value = ivaCompras.Sum();
                    ws.Cell(filaIvaCompras, 10).Style.Font.Bold = true;

                    // ---- VENTAS ----
                    int inicioVentas = Math.Max(filaCalc, filaIvaCompras) + 2;
                    ws.Range($"G{inicioVentas}:H{inicioVentas}").Merge().Value = "VENTAS";
                    ws.Cell($"G{inicioVentas}").Style.Font.Bold = true;
                    ws.Cell(inicioVentas + 1, 7).Value = "Debe";
                    ws.Cell(inicioVentas + 1, 8).Value = "Haber";
                    int filaVentas = inicioVentas + 2;
                    foreach (var v in ventas)
                        ws.Cell(filaVentas++, 8).Value = v;
                    ws.Cell(filaVentas, 8).Value = ventas.Sum();
                    ws.Cell(filaVentas, 8).Style.Font.Bold = true;

                    // ---- IVA VENTAS ----
                    ws.Range($"J{inicioVentas}:K{inicioVentas}").Merge().Value = "IVA VENTAS";
                    ws.Cell($"J{inicioVentas}").Style.Font.Bold = true;
                    ws.Cell(inicioVentas + 1, 10).Value = "Debe";
                    ws.Cell(inicioVentas + 1, 11).Value = "Haber";
                    int filaIvaVentas = inicioVentas + 2;
                    foreach (var v in ivaVentas)
                        ws.Cell(filaIvaVentas++, 11).Value = v;
                    ws.Cell(filaIvaVentas, 11).Value = ivaVentas.Sum();
                    ws.Cell(filaIvaVentas, 11).Style.Font.Bold = true;

                    // ---- BANCOS ----
                    int filaBancos = Math.Max(filaVentas, filaIvaVentas) + 2;
                    ws.Range($"G{filaBancos}:H{filaBancos}").Merge().Value = "BANCOS";
                    ws.Cell($"G{filaBancos}").Style.Font.Bold = true;
                    ws.Cell(filaBancos + 1, 7).Value = "Debe";
                    ws.Cell(filaBancos + 1, 8).Value = "Haber";

                    int filaBancosValores = filaBancos + 2;
                    int maxBancos = Math.Max(bancosDebe.Count, bancosHaber.Count);
                    for (int i = 0; i < maxBancos; i++)
                    {
                        if (i < bancosDebe.Count)
                            ws.Cell(filaBancosValores, 7).Value = bancosDebe[i];
                        if (i < bancosHaber.Count)
                            ws.Cell(filaBancosValores, 8).Value = bancosHaber[i];
                        filaBancosValores++;
                    }

                    ws.Cell(filaBancosValores, 7).Value = bancosDebe.Sum();
                    ws.Cell(filaBancosValores, 8).Value = bancosHaber.Sum();
                    ws.Range(filaBancosValores, 7, filaBancosValores, 8).Style.Font.Bold = true;

                    // ================= ESTADO DE RESULTADOS =================
                    int filaEstado = filaBancosValores + 3;

                    ws.Range(filaEstado, 7, filaEstado, 10).Merge().Value = "ESTADO DE RESULTADOS";
                    ws.Cell(filaEstado, 7).Style.Font.Bold = true;
                    ws.Cell(filaEstado, 7).Style.Font.FontSize = 12;
                    filaEstado++;

                    var fecha = datos.First().FechaEmision;
                    var fechaFinMes = new DateTime(
                        fecha.Year,
                        fecha.Month,
                        DateTime.DaysInMonth(fecha.Year, fecha.Month)
                    );

                    ws.Range(filaEstado, 7, filaEstado, 10).Merge().Value = fechaFinMes;
                    ws.Cell(filaEstado, 7).Style.DateFormat.Format = "dd/MM/yyyy";
                    filaEstado += 2;

                    ws.Cell(filaEstado, 7).Value = "Ingresos";
                    ws.Cell(filaEstado, 7).Style.Font.Bold = true;
                    filaEstado++;

                    ws.Cell(filaEstado, 8).Value = "Venta";
                    ws.Cell(filaEstado, 10).Value = ventas.Sum();
                    ws.Cell(filaEstado, 10).Style.NumberFormat.Format = "$ #,##0.00";
                    filaEstado += 2;

                    ws.Cell(filaEstado, 7).Value = "Gastos";
                    ws.Cell(filaEstado, 7).Style.Font.Bold = true;
                    filaEstado++;

                    ws.Cell(filaEstado, 8).Value = "Compra";
                    ws.Cell(filaEstado, 10).Value = compras.Sum();
                    ws.Cell(filaEstado, 10).Style.NumberFormat.Format = "$ #,##0.00";
                    filaEstado += 2;

                    decimal utilidad = ventas.Sum() - compras.Sum();

                    ws.Cell(filaEstado, 7).Value = "(=) Utilidad del Ejercicio";
                    ws.Cell(filaEstado, 7).Style.Font.Bold = true;
                    ws.Cell(filaEstado, 10).Value = utilidad;
                    ws.Cell(filaEstado, 10).Style.Font.Bold = true;
                    ws.Cell(filaEstado, 10).Style.NumberFormat.Format = "$ #,##0.00";

                    ws.Columns().AdjustToContents();

                    using (var ms = new MemoryStream())
                    {
                        wb.SaveAs(ms);
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al generar el archivo contable", ex);
            }
        }

        private byte[] GenerarExelLibroDiario(List<CompraExcel> datos)
        {
            try
            {
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Libro Diario");

                    int fila = 1;
                    int nro = 1;

                    // ================= ENCABEZADOS =================
                    ws.Cell("A1").Value = "Fecha";
                    ws.Cell("B1").Value = "Detalle";
                    ws.Cell("D1").Value = "Debe";
                    ws.Cell("E1").Value = "Haber";
                    ws.Range("A1:E1").Style.Font.Bold = true;
                    fila++;

                    var compras = new List<decimal>();
                    var ivaCompras = new List<decimal>();
                    var ventas = new List<decimal>();
                    var ivaVentas = new List<decimal>();
                    var bancosDebe = new List<decimal>();
                    var bancosHaber = new List<decimal>();

                    // ================= ASIENTOS =================
                    foreach (var d in datos.OrderBy(x => x.FechaEmision))
                    {
                        ws.Cell(fila, 1).Value = d.FechaEmision;
                        ws.Cell(fila, 1).Style.DateFormat.Format = "dd/MM/yyyy";
                        ws.Cell(fila, 2).Value = nro;
                        fila++;

                        if (d.Tipo == TipoDocumento.Recibida)
                        {
                            ws.Cell(fila, 2).Value = "Compras";
                            ws.Cell(fila, 4).Value = d.ValorSinImpuestos;
                            compras.Add(d.ValorSinImpuestos);
                            fila++;

                            ws.Cell(fila, 2).Value = "IVA Compras";
                            ws.Cell(fila, 4).Value = d.Iva;
                            ivaCompras.Add(d.Iva);
                            fila++;

                            ws.Cell(fila, 2).Value = "Bancos";
                            ws.Cell(fila, 5).Value = d.ImporteTotal;
                            bancosHaber.Add(d.ImporteTotal);
                            fila++;
                        }
                        else
                        {
                            ws.Cell(fila, 2).Value = "Bancos";
                            ws.Cell(fila, 4).Value = d.ImporteTotal;
                            bancosDebe.Add(d.ImporteTotal);
                            fila++;

                            ws.Cell(fila, 2).Value = "Ventas";
                            ws.Cell(fila, 5).Value = d.ValorSinImpuestos;
                            ventas.Add(d.ValorSinImpuestos);
                            fila++;

                            ws.Cell(fila, 2).Value = "IVA Ventas";
                            ws.Cell(fila, 5).Value = d.Iva;
                            ivaVentas.Add(d.Iva);
                            fila++;
                        }

                        fila++;
                        nro++;
                    }

                    using (var ms = new MemoryStream())
                    {
                        wb.SaveAs(ms);
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al generar el archivo contable", ex);
            }
        }

        private byte[] GenerarExcelDebeHaber(List<CompraExcel> datos)
        {
            try
            {
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Cálculos");

                    var compras = new List<decimal>();
                    var ivaCompras = new List<decimal>();
                    var ventas = new List<decimal>();
                    var ivaVentas = new List<decimal>();

                    // ================= RECOLECCIÓN DE DATOS =================
                    foreach (var d in datos)
                    {
                        if (d.Tipo == TipoDocumento.Recibida)
                        {
                            compras.Add(d.ValorSinImpuestos);
                            ivaCompras.Add(d.Iva);
                        }
                        else
                        {
                            ventas.Add(d.ValorSinImpuestos);
                            ivaVentas.Add(d.Iva);
                        }
                    }

                    int fila = 1;

                    // ================= COMPRAS =================
                    ws.Range("A1:B1").Merge().Value = "COMPRAS";
                    ws.Cell("A1").Style.Font.Bold = true;

                    ws.Cell("A2").Value = "Debe";
                    ws.Cell("B2").Value = "Haber";

                    fila = 3;
                    foreach (var v in compras)
                        ws.Cell(fila++, 1).Value = v;

                    ws.Cell(fila, 1).Value = compras.Sum();
                    ws.Cell(fila, 1).Style.Font.Bold = true;

                    // ================= IVA COMPRAS =================
                    ws.Range("D1:E1").Merge().Value = "IVA COMPRAS";
                    ws.Cell("D1").Style.Font.Bold = true;

                    ws.Cell("D2").Value = "Debe";
                    ws.Cell("E2").Value = "Haber";

                    fila = 3;
                    foreach (var v in ivaCompras)
                        ws.Cell(fila++, 4).Value = v;

                    ws.Cell(fila, 4).Value = ivaCompras.Sum();
                    ws.Cell(fila, 4).Style.Font.Bold = true;

                    // ================= VENTAS =================
                    ws.Range("A7:B7").Merge().Value = "VENTAS";
                    ws.Cell("A7").Style.Font.Bold = true;

                    ws.Cell("A8").Value = "Debe";
                    ws.Cell("B8").Value = "Haber";

                    fila = 9;
                    foreach (var v in ventas)
                        ws.Cell(fila++, 2).Value = v;

                    ws.Cell(fila, 2).Value = ventas.Sum();
                    ws.Cell(fila, 2).Style.Font.Bold = true;

                    // ================= IVA VENTAS =================
                    ws.Range("D7:E7").Merge().Value = "IVA VENTAS";
                    ws.Cell("D7").Style.Font.Bold = true;

                    ws.Cell("D8").Value = "Debe";
                    ws.Cell("E8").Value = "Haber";

                    fila = 9;
                    foreach (var v in ivaVentas)
                        ws.Cell(fila++, 5).Value = v;

                    ws.Cell(fila, 5).Value = ivaVentas.Sum();
                    ws.Cell(fila, 5).Style.Font.Bold = true;

                    ws.Columns().AdjustToContents();

                    using (var ms = new MemoryStream())
                    {
                        wb.SaveAs(ms);
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al generar el archivo de cálculos", ex);
            }
        }

        private byte[] GenerarExcelResumen(List<CompraExcel> datos)
        {
            try
            {
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Resumen Bancos");

                    var bancosDebe = new List<decimal>();
                    var bancosHaber = new List<decimal>();

                    // ================= RECOLECCIÓN =================
                    foreach (var d in datos)
                    {
                        if (d.Tipo == TipoDocumento.Recibida)
                        {
                            // Compras → Bancos al HABER
                            bancosHaber.Add(d.ImporteTotal);
                        }
                        else
                        {
                            // Ventas → Bancos al DEBE
                            bancosDebe.Add(d.ImporteTotal);
                        }
                    }

                    int fila = 1;

                    // ================= BANCOS =================
                    ws.Range(fila, 1, fila, 2).Merge().Value = "BANCOS";
                    ws.Cell(fila, 1).Style.Font.Bold = true;
                    fila++;

                    ws.Cell(fila, 1).Value = "Debe";
                    ws.Cell(fila, 2).Value = "Haber";
                    ws.Range(fila, 1, fila, 2).Style.Font.Bold = true;
                    fila++;

                    int maxFilas = Math.Max(bancosDebe.Count, bancosHaber.Count);

                    for (int i = 0; i < maxFilas; i++)
                    {
                        if (i < bancosDebe.Count)
                            ws.Cell(fila, 1).Value = bancosDebe[i];

                        if (i < bancosHaber.Count)
                            ws.Cell(fila, 2).Value = bancosHaber[i];

                        fila++;
                    }

                    // ================= TOTALES =================
                    ws.Cell(fila, 1).Value = bancosDebe.Sum();
                    ws.Cell(fila, 2).Value = bancosHaber.Sum();
                    ws.Range(fila, 1, fila, 2).Style.Font.Bold = true;

                    ws.Columns().AdjustToContents();

                    using (var ms = new MemoryStream())
                    {
                        wb.SaveAs(ms);
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al generar el resumen de bancos", ex);
            }
        }

    }
}
