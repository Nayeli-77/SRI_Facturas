using Microsoft.Playwright;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

        //private async Task Logout(IPage page)
        //{
        //    try
        //    {
        //        await page.GetByRole(AriaRole.Link, new() { Name = " Cerrar sesión" }).ClickAsync();
        //        await page.GetByRole(AriaRole.Button, new() { Name = "Continuar" }).ClickAsync();
        //        Console.WriteLine("📄 Gestion generada con exito: " );

        //    }
        //    catch { }
        //}
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

                // ============ LOGIN ============
                await page.GetByRole(AriaRole.Link, new() { Name = "Ir a iniciar sesión" }).ClickAsync();
                await page.GetByRole(AriaRole.Textbox, new() { Name = "*RUC / C.I. / Pasaporte" }).FillAsync(ruc);
                await page.GetByRole(AriaRole.Textbox, new() { Name = "C.I. adicional" }).FillAsync(ci);
                await page.GetByRole(AriaRole.Textbox, new() { Name = "*Clave" }).FillAsync(clave);
                await page.GetByRole(AriaRole.Button, new() { Name = "Ingresar" }).ClickAsync();

                log("✅ Login realizado");

                // Cerrar modal si aparece
                try { await page.Locator("sri-titulo-modal-mat div").Nth(2).ClickAsync(); } catch { }

                // ============ NAVEGACIÓN ============
                await page.GetByRole(AriaRole.Button, new() { Name = "Abrir o cerrar menu desplegado" }).ClickAsync();
                await page.GetByRole(AriaRole.Link, new() { Name = "  FACTURACIÓN ELECTRÓNICA" }).ClickAsync();
                await page.GetByRole(AriaRole.Link, new() { Name = " Producción" }).ClickAsync();

                await page.GetByRole(AriaRole.Listitem)
                    .Filter(new() { HasTextRegex = new Regex("^Consultas$") })
                    .GetByRole(AriaRole.Link).ClickAsync();

                await page.GetByRole(AriaRole.Link, new() { Name = "Comprobantes electrónicos recibidos" }).ClickAsync();

                log("📄 Módulo de recibidas cargado");

                // ============ FILTROS ============
                await page.Locator("#frmPrincipal\\:ano").SelectOptionAsync(ano);
                await page.Locator("#frmPrincipal\\:mes").SelectOptionAsync(mes);
                await page.Locator("#frmPrincipal\\:dia").SelectOptionAsync(dia);
                await page.Locator("#frmPrincipal\\:cmbTipoComprobante").SelectOptionAsync(tipoComprobante);

                //await page.GetByRole(AriaRole.Button, new() { Name = "Consultar" }).ClickAsync();
                await page.WaitForTimeoutAsync(1200);

                // === CLICK FUERZADO EN CONSULTAR ===

                var btnConsultar = page.GetByRole(AriaRole.Button, new() { Name = "Consultar" });

                try
                {
                    // Asegurar que exista
                    await btnConsultar.WaitForAsync();

                    // Asegurar visibilidad y foco
                    await btnConsultar.ScrollIntoViewIfNeededAsync();
                    await btnConsultar.FocusAsync();

                    // Intento 1 - Click normal
                    await btnConsultar.ClickAsync(new() { Force = true });
                    log("👉 Click en Consultar (force) enviado...");
                }
                catch
                {
                    log("⚠ Click normal falló, intentando JS...");

                    try
                    {
                        // Intento 2 - Click por JavaScript (infalible)
                        await page.EvaluateAsync(@"() => {
                            const btn = document.querySelector('button, input[type=button], input[type=submit], span.ui-button-text');
                            if(btn) btn.click();
                        }");

                        log("👉 Click en Consultar aplicado por JavaScript");
                    }
                    catch (Exception ex2)
                    {
                        log("❌ Falló el click por JS: " + ex2.Message);
                        return false;
                    }
                }

                // CONTINÚA NORMALMENTE…
                await page.WaitForTimeoutAsync(3500);

                log("👉 Se requiere el CAPTCHA manual");

                await page.WaitForTimeoutAsync(1200);
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

                // ============ VERIFICAR FILAS ============
                var filas = page.Locator("#frmPrincipal\\:tablaCompRecibidos_data tr");
                int filasCount = await filas.CountAsync();

                if (filasCount == 0)
                {
                    log("⚠️ No hay datos para exportar.");
                    await Logout(page);
                    return false;
                }

                log($"📌 {filasCount} filas encontradas.");
                log("📊 Exportando tabla a CSV...");

                // ============ LIMPIADORES ============
                string Clean(string s)
                {
                    if (string.IsNullOrEmpty(s)) return "";

                    return s
                        .Replace("\r", " ")
                        .Replace("\n", " ")
                        .Replace("\t", " ")
                        .Replace(";", ",")
                        .Replace("  ", " ")
                        .Trim();
                }

                string FixExcel(string s)
                {
                    if (string.IsNullOrEmpty(s)) return s;

                    // Evitar notación científica en RUC y claves largas
                    if (s.All(char.IsDigit) && s.Length >= 10)
                        return "'" + s;

                    return s;
                }

                // ============ EXTRAER ENCABEZADOS ============
                var encabezados = await page.EvaluateAsync<string[]>(@"
                    () => {
                        return Array.from(document.querySelectorAll('#frmPrincipal\\:tablaCompRecibidos thead th'))
                                    .map(th => th.innerText.trim());
                    }
                ");

                        // ============ EXTRAER FILAS ============
                        var rows = await page.EvaluateAsync<string[][]>(@"
                    () => {
                        const trs = Array.from(document.querySelectorAll('#frmPrincipal\\:tablaCompRecibidos_data tr'));
                        return trs.map(tr => {
                            return Array.from(tr.querySelectorAll('td')).map(td => td.innerText.trim());
                        });
                    }
                ");

                // ============ GENERAR CSV ============
                var sb = new StringBuilder();

                // Encabezados
                sb.AppendLine(string.Join(";", encabezados.Select(c => Clean(c))));

                // Filas
                foreach (var row in rows)
                {
                    sb.AppendLine(string.Join(";", row.Select(c => FixExcel(Clean(c)))));
                }

                // ============ NOMBRE DE ARCHIVO ============
                int num = 1;
                string archivoCsv;

                do
                {
                    archivoCsv = Path.Combine(carpetaDestino, $"recibidas_{num}.csv");
                    num++;
                }
                while (File.Exists(archivoCsv));

                await File.WriteAllTextAsync(archivoCsv, sb.ToString(), Encoding.UTF8);

                log("📄 CSV guardado en: " + archivoCsv);

                //await Logout(page);
                //return true;

                try
                {
                    await Logout(page);
                }
                catch (Exception ex)
                {
                    log($"⚠️ Logout falló: {ex.Message} (ignorado)");
                }

                return true; // <-- AHORA SIEMPRE LLEGA
            }
            catch (Exception ex)
            {
                log("❌ Error general: " + ex.Message);
                return false;
            }
        }

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
                        Timeout = 20000, // 20 segundos
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
