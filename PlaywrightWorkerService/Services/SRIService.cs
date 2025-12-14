using ClosedXML.Excel;
using Microsoft.Playwright;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;



namespace PlaywrightWorkerService.Services
{
    public class SRIService
    {
        public async Task<bool> DescargarEmitidasAsync(
            string ruc,
            string ci,
            string clave,
            string fecha,
            string estado,
            string tipoComprobante,
            string establecimiento,
            string carpetaBase,
            Action<string>? log = null)
        {
            try
            {
                log ??= Console.WriteLine;
                Console.OutputEncoding = System.Text.Encoding.UTF8;

                // === Generar carpeta con fecha ===
                string fechaCarpeta = DateTime.Now.ToString("yyyy-MM-dd");
                string carpetaDestino = Path.Combine(carpetaBase, fechaCarpeta);
                Directory.CreateDirectory(carpetaDestino);

                log($"📁 Carpeta destino: {carpetaDestino}");

                using var playwright = await Playwright.CreateAsync();
                var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = false
                });

                var context = await browser.NewContextAsync(new BrowserNewContextOptions
                {
                    AcceptDownloads = true
                });

                var page = await context.NewPageAsync();

                log("➡️ Accediendo a SRI…");

                bool ok = await GotoWithRetryAsync(
                    page,
                    "https://srienlinea.sri.gob.ec/sri-en-linea/inicio/NAT",
                    log
                );

                if (!ok)
                {
                    log("❌ Error crítico: No se pudo abrir el SRI.");
                    return false;
                }


                // Login
                await page.GetByRole(AriaRole.Link, new() { Name = "Ir a iniciar sesión" }).ClickAsync();
                await page.GetByRole(AriaRole.Textbox, new() { Name = "*RUC / C.I. / Pasaporte" }).FillAsync(ruc);
                await page.GetByRole(AriaRole.Textbox, new() { Name = "C.I. adicional" }).FillAsync(ci);
                await page.GetByRole(AriaRole.Textbox, new() { Name = "*Clave" }).FillAsync(clave);
                await page.GetByRole(AriaRole.Button, new() { Name = "Ingresar" }).ClickAsync();

                log("✅ Login realizado");

                // Cerrar modal si aparece
                try { await page.Locator("sri-titulo-modal-mat div").Nth(2).ClickAsync(); } catch { }

                // Navegación
                await page.GetByRole(AriaRole.Button, new() { Name = "Abrir o cerrar menu desplegado" }).ClickAsync();
                await page.GetByRole(AriaRole.Link, new() { Name = "  FACTURACIÓN ELECTRÓNICA" }).ClickAsync();
                await page.GetByRole(AriaRole.Link, new() { Name = " Producción" }).ClickAsync();

                await page.GetByRole(AriaRole.Listitem)
                    .Filter(new() { HasTextRegex = new Regex("^Consultas$") })
                    .GetByRole(AriaRole.Link).ClickAsync();

                await page.GetByRole(AriaRole.Link, new() { Name = "Comprobantes electrónicos emitidos" }).ClickAsync();

                log("📄 Módulo de emitidas cargado");

                // FILTROS
                DateTime fechaAux = DateTime.ParseExact(fecha, "yyyy-MM-dd", null);
                string fechaFormateada = fechaAux.ToString("dd/MM/yyyy");
                await page.Locator("#frmPrincipal\\:calendarFechaDesde_input").FillAsync(fechaFormateada);
                await page.Locator("#frmPrincipal\\:cmbEstadoAutorizacion").SelectOptionAsync(estado);
                await page.Locator("#frmPrincipal\\:cmbTipoComprobante").SelectOptionAsync(tipoComprobante);
                await page.Locator("#frmPrincipal\\:cmbEstablecimiento").SelectOptionAsync(establecimiento);

                await page.GetByRole(AriaRole.Button, new() { Name = "Consultar" }).ClickAsync();
                await page.WaitForTimeoutAsync(3500);

                // Mensaje del sistema
                string mensajeSistema = "";
                try
                {
                    mensajeSistema = await page.Locator("[id='formMessages:messages'] div").TextContentAsync();
                }
                catch { }

                if (!string.IsNullOrWhiteSpace(mensajeSistema))
                {
                    log("⚠️ Mensaje del SRI: " + mensajeSistema.Trim());
                    await Logout(page);
                    return false;
                }

                // Verificar filas
                var filas = page.Locator("#frmPrincipal\\:tablaCompEmitidos tr");
                int filasCount = await filas.CountAsync();

                if (filasCount == 0)
                {
                    log("⚠️ No hay datos para exportar.");
                    await Logout(page);
                    return false;
                }

                log($"📌 {filasCount} filas encontradas.");
                log("📊 Exportando tabla a CSV...");

                // Extraer encabezados
                var encabezados = await page.EvaluateAsync<string[]>(@"
                    () => {
                        return Array.from(document.querySelectorAll('#frmPrincipal\\:tablaCompEmitidos thead th'))
                                    .map(th => th.innerText.trim());
                    }
                    ");

                                    // Extraer filas
                                    var rows = await page.EvaluateAsync<string[][]>(@"
                    () => {
                        const trs = Array.from(document.querySelectorAll('#frmPrincipal\\:tablaCompEmitidos_data tr'));
                        return trs.map(tr => {
                            return Array.from(tr.querySelectorAll('td')).map(td => td.innerText.trim());
                        });
                    }
                    ");

                // === Generar CSV ===
                var sb = new StringBuilder();

                // Encabezados
                sb.AppendLine(string.Join(";", encabezados.Select(c => c.Replace(";", ","))));

                // Información
                foreach (var row in rows)
                    sb.AppendLine(string.Join(";", row.Select(c => c.Replace(";", ","))));

                // === Crear nombre de archivo incremental ===
                int num = 1;
                string archivoCsv;

                do
                {
                    archivoCsv = Path.Combine(carpetaDestino, $"emitidas_{num}.csv");
                    num++;
                }
                 while (File.Exists(archivoCsv));

                await File.WriteAllTextAsync(archivoCsv, sb.ToString(), Encoding.UTF8);

                log("📄 CSV guardado en: " + archivoCsv);

                await Logout(page);
                return true;
            }
            catch (Exception ex)
            {
                log("❌ Error general: " + ex.Message);
                return false;
            }
        }

        private async Task Logout(IPage page)
        {
           
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                var cerrarSesion = page.GetByRole(AriaRole.Link, new() { Name = " Cerrar sesión" });
                if (await cerrarSesion.IsVisibleAsync())
                    await cerrarSesion.ClickAsync();

                var continuar = page.GetByRole(AriaRole.Button, new() { Name = "Continuar" });
                if (await continuar.IsVisibleAsync())
                    await continuar.ClickAsync();

                Console.WriteLine("📄 Gestión generada con éxito");
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚠ Error durante Logout: " + ex.Message);
                // NO throw -> que no bloquee el return true
            }
        }
        //metodo para descargar recibidas 
        public async Task<bool> DescargarRecibidasAsync(
            string ruc,
            string ci,
            string clave,
            string ano,
            string mes,
            string dia,
            string tipoComprobante,
            string carpetaBase,
            Action<string>? log = null)
        {
            try
            {
                log ??= Console.WriteLine;
                Console.OutputEncoding = Encoding.UTF8;

                // ================== CARPETAS ==================
                string fechaCarpeta = DateTime.Now.ToString("yyyy-MM-dd");
                string carpetaDestino = Path.Combine(carpetaBase, fechaCarpeta);
                Directory.CreateDirectory(carpetaDestino);

                string carpetaXml = Path.Combine(carpetaDestino, "XML"+ fechaCarpeta);
                Directory.CreateDirectory(carpetaXml);

                log($"📁 Carpeta destino: {carpetaDestino}");
                log($"📂 Carpeta XML: {carpetaXml}");

                using var playwright = await Playwright.CreateAsync();
                var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = false
                });

                var context = await browser.NewContextAsync(new BrowserNewContextOptions
                {
                    AcceptDownloads = true
                });

                var page = await context.NewPageAsync();

                // ================== ACCESO ==================
                log("➡️ Accediendo a SRI…");

                bool ok = await GotoWithRetryAsync(
                    page,
                    "https://srienlinea.sri.gob.ec/sri-en-linea/inicio/NAT",
                    log
                );

                if (!ok)
                {
                    log("❌ No se pudo abrir el SRI.");
                    return false;
                }

                // ================== LOGIN ==================
                await page.GetByRole(AriaRole.Link, new() { Name = "Ir a iniciar sesión" }).ClickAsync();
                await page.GetByRole(AriaRole.Textbox, new() { Name = "*RUC / C.I. / Pasaporte" }).FillAsync(ruc);
                await page.GetByRole(AriaRole.Textbox, new() { Name = "C.I. adicional" }).FillAsync(ci);
                await page.GetByRole(AriaRole.Textbox, new() { Name = "*Clave" }).FillAsync(clave);
                await page.GetByRole(AriaRole.Button, new() { Name = "Ingresar" }).ClickAsync();

                log("✅ Login realizado");

                // Cerrar modal si aparece
                try { await page.Locator("sri-titulo-modal-mat div").Nth(2).ClickAsync(); } catch { }

                // ================== NAVEGACIÓN ==================
                await page.GetByRole(AriaRole.Button, new() { Name = "Abrir o cerrar menu desplegado" }).ClickAsync();
                await page.GetByRole(AriaRole.Link, new() { Name = "  FACTURACIÓN ELECTRÓNICA" }).ClickAsync();
                await page.GetByRole(AriaRole.Link, new() { Name = " Producción" }).ClickAsync();

                await page.GetByRole(AriaRole.Listitem)
                    .Filter(new() { HasTextRegex = new Regex("^Consultas$") })
                    .GetByRole(AriaRole.Link).ClickAsync();

                await page.GetByRole(AriaRole.Link, new() { Name = "Comprobantes electrónicos recibidos" }).ClickAsync();

                log("📄 Módulo de recibidas cargado");

                // ================== FILTROS ==================
                await page.Locator("#frmPrincipal\\:ano").SelectOptionAsync(ano);
                await page.Locator("#frmPrincipal\\:mes").SelectOptionAsync(mes);
                await page.Locator("#frmPrincipal\\:dia").SelectOptionAsync(dia);
                await page.Locator("#frmPrincipal\\:cmbTipoComprobante").SelectOptionAsync(tipoComprobante);

                var btnConsultar = page.GetByRole(AriaRole.Button, new() { Name = "Consultar" });
                await btnConsultar.ScrollIntoViewIfNeededAsync();
                await btnConsultar.ClickAsync(new() { Force = true });

                await page.WaitForTimeoutAsync(3500);

                log("👉 Resolver CAPTCHA manualmente…");
                await page.WaitForTimeoutAsync(1200);

                // ================== MENSAJES SRI ==================
                try
                {
                    string mensaje = await page.Locator("[id='formMessages:messages'] div").TextContentAsync();
                    if (!string.IsNullOrWhiteSpace(mensaje))
                    {
                        log("⚠️ Mensaje SRI: " + mensaje.Trim());
                        await Logout(page);
                        return false;
                    }
                }
                catch { }

                // ================== FILAS ==================
                var filas = page.Locator("#frmPrincipal\\:tablaCompRecibidos_data tr");
                int filasCount = await filas.CountAsync();

                if (filasCount == 0)
                {
                    log("⚠️ No hay comprobantes.");
                    await Logout(page);
                    return false;
                }

                log($"📌 {filasCount} filas encontradas.");

                // ================== DESCARGA XML (DOCUMENTOS) ==================
                log("⬇️ Descargando XML (columna DOCUMENTOS)...");

                for (int i = 0; i < filasCount; i++)
                {
                    try
                    {
                        var linkDocumento = page.Locator(
                            $"#frmPrincipal\\:tablaCompRecibidos_data tr:nth-child({i + 1}) td:nth-last-child(3) a"
                        );

                        if (await linkDocumento.CountAsync() == 0)
                        {
                            log($"⚠️ Fila {i + 1}: sin XML");
                            continue;
                        }

                        var download = await page.RunAndWaitForDownloadAsync(async () =>
                        {
                            await linkDocumento.First.ClickAsync(new() { Force = true });
                        });

                        string nombreXml = $"recibida_{i + 1}_{DateTime.Now:HHmmss}.xml";
                        await download.SaveAsAsync(Path.Combine(carpetaXml, nombreXml));

                        log($"✅ XML fila {i + 1} descargado");
                        await page.WaitForTimeoutAsync(900);
                        //Lectuta de xml descargados
                        string rutaExcel = Path.Combine(carpetaDestino, "recibidas"+fechaCarpeta+".xlsx");

                        XmlRecibidasToExcel(carpetaXml, rutaExcel, log);
                    }
                    catch (Exception ex)
                    {
                        log($"❌ Error XML fila {i + 1}: {ex.Message}");
                    }
                }

                // ================== CSV ==================
                log("📊 Exportando CSV...");

                var encabezados = await page.EvaluateAsync<string[]>(@"
            () => Array.from(document.querySelectorAll('#frmPrincipal\\:tablaCompRecibidos thead th'))
                       .map(th => th.innerText.trim());
        ");

                var rows = await page.EvaluateAsync<string[][]>(@"
            () => Array.from(document.querySelectorAll('#frmPrincipal\\:tablaCompRecibidos_data tr'))
                       .map(tr => Array.from(tr.querySelectorAll('td'))
                       .map(td => td.innerText.trim()));
        ");

                var sb = new StringBuilder();
                sb.AppendLine(string.Join(";", encabezados));

                foreach (var row in rows)
                    sb.AppendLine(string.Join(";", row));

                string archivoCsv = Path.Combine(carpetaDestino, "recibidas.csv");
                await File.WriteAllTextAsync(archivoCsv, sb.ToString(), Encoding.UTF8);

                log("📄 CSV generado");

                await Logout(page);
                return true;
            }
            catch (Exception ex)
            {
                log("❌ Error general: " + ex.Message);
                return false;
            }
        }


        //fin del metodo

        //Metodo para leer el xml y pasarlo aun archivo excel
        public void XmlRecibidasToExcel(string carpetaXml, string rutaExcel, Action<string>? log = null)
        {
            log ??= Console.WriteLine;

            var archivos = Directory.GetFiles(carpetaXml, "*.xml");
            if (archivos.Length == 0)
            {
                log("⚠️ No hay XML");
                return;
            }

            var filas = new List<Dictionary<string, string>>();
            var columnas = new HashSet<string>();

            foreach (var archivo in archivos)
            {
                var fila = new Dictionary<string, string>();
                var sriXml = XDocument.Load(archivo);

                // ================= DATOS SRI =================
                void Add(string k, string? v)
                {
                    if (!string.IsNullOrWhiteSpace(v))
                    {
                        fila[k] = v.Trim();
                        columnas.Add(k);
                    }
                }

                Add("estado", sriXml.Descendants().FirstOrDefault(x => x.Name.LocalName == "estado")?.Value);
                Add("fechaAutorizacion", sriXml.Descendants().FirstOrDefault(x => x.Name.LocalName == "fechaAutorizacion")?.Value);
                Add("numeroAutorizacion", sriXml.Descendants().FirstOrDefault(x => x.Name.LocalName == "numeroAutorizacion")?.Value);
                Add("ambiente", sriXml.Descendants().FirstOrDefault(x => x.Name.LocalName == "ambiente")?.Value);

                // ================= XML INTERNO =================
                var comprobante = sriXml.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "comprobante")?.Value;

                if (string.IsNullOrWhiteSpace(comprobante))
                    continue;

                var facturaXml = XDocument.Parse(comprobante);

                // ================= INFO TRIBUTARIA =================
                foreach (var el in facturaXml.Descendants("infoTributaria").Elements())
                    Add(el.Name.LocalName, el.Value);

                // ================= INFO FACTURA =================
                foreach (var el in facturaXml.Descendants("infoFactura").Elements())
                    Add(el.Name.LocalName, el.Value);

                filas.Add(fila);
            }

            // ================= CREAR EXCEL =================
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Recibidas");

            var headers = columnas.OrderBy(x => x).ToList();

            for (int c = 0; c < headers.Count; c++)
            {
                ws.Cell(1, c + 1).Value = headers[c];
                ws.Cell(1, c + 1).Style.Font.Bold = true;
            }

            for (int r = 0; r < filas.Count; r++)
            {
                for (int c = 0; c < headers.Count; c++)
                {
                    if (filas[r].TryGetValue(headers[c], out var v))
                        ws.Cell(r + 2, c + 1).Value = v;
                }
            }

            ws.Columns().AdjustToContents();
            wb.SaveAs(rutaExcel);

            log($"✅ Excel correcto generado: {rutaExcel}");
        }


        //fin de metodo
        private async Task<bool> GotoWithRetryAsync(IPage page, string url, Action<string>? log = null)
        {
            log ??= Console.WriteLine;

            const int maxIntentos = 2;
            int intento = 1;

            while (intento <= maxIntentos)
            {
                try
                {
                    log($"🌐 Intento {intento} de {maxIntentos} → Navegando a: {url}");

                    var resp = await page.GotoAsync(url, new PageGotoOptions
                    {
                        Timeout = 40000, // 20 segundos
                        WaitUntil = WaitUntilState.DOMContentLoaded
                    });

                    if (resp != null && resp.Ok)
                    {
                        log("✅ Página cargada correctamente");
                        return true;
                    }

                    log("⚠️ Respuesta inválida. Retentando…");
                }
                catch (TimeoutException)
                {
                    log("⏳ Timeout cargando la página. Intentando nuevamente…");
                }
                catch (Exception ex)
                {
                    log($"❌ Error en intento {intento}: {ex.Message}");
                }

                await Task.Delay(2000);
                intento++;
            }

            log("❌ No se pudo acceder al SRI después de varios intentos.");
            return false;
        }

    }

}
