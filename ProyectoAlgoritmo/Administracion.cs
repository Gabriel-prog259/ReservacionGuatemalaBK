using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace HotelReservas
{
    public static class Administracion
    {
        public static void LoginYMenu()
        {
            Console.Clear();
            Console.WriteLine("=== ADMINISTRACIÓN ===");
            Console.Write("Usuario: ");
            string? user = Console.ReadLine();
            Console.Write("Contraseña: ");
            string? pass = LeerPassword();

            if (user != "Admin" || pass != "Admin123")
            {
                Program.Pausa("Credenciales inválidas.");
                return;
            }

            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== ADMINISTRACIÓN ===");
                Console.WriteLine("1) Resumen general (clientes, reservas y habitaciones)");
                Console.WriteLine("2) Comparativa de ingresos/egresos por mes");
                Console.WriteLine("3) Facturas");
                Console.WriteLine("0) Volver");
                Console.Write("Elige una opción: ");
                var op = Console.ReadLine()?.Trim();

                switch (op)
                {
                    case "1":
                        MostrarResumen();
                        break;
                    case "2":
                        MostrarIngresosEgresosMensuales();
                        break;
                    case "3":
                        MenuFacturas();
                        break;
                    case "0":
                        return;
                    default:
                        Program.Pausa("Opción inválida.");
                        break;
                }
            }
        }

        private static void MostrarResumen()
        {
            Console.Clear();
            Console.WriteLine("=== RESUMEN GENERAL ===");

            var clientes = Clientes.ObtenerClientesParaGuardar();
            int totalClientes = clientes.Count;
            int conReserva = clientes.Count(c => c.DiasEstancia > 0 && c.HabitacionNumero != null);
            int sinReserva = totalClientes - conReserva;

            Console.WriteLine("[CLIENTES]");
            Console.WriteLine($"  - Totales         : {totalClientes}");
            Console.WriteLine($"  - Con reserva     : {conReserva}");
            Console.WriteLine($"  - Sin reserva     : {sinReserva}");
            Console.WriteLine();

            Console.WriteLine("[RESERVAS ACTIVAS]");
            if (conReserva == 0)
            {
                Console.WriteLine("  No hay reservas activas.");
            }
            else
            {
                Console.WriteLine("Id  | Cliente                          | Hab | Días | Precio/Noche | Subtotal");
                Console.WriteLine("----+----------------------------------+-----+------+--------------+-------------");
                int idx = 1;
                foreach (var c in clientes.Where(x => x.DiasEstancia > 0 && x.HabitacionNumero != null))
                {
                    int hab = c.HabitacionNumero!.Value;
                    decimal px = ConfigHabitaciones.PrecioPorNoche(hab);
                    decimal sub = px * c.DiasEstancia;
                    Console.WriteLine($"{idx++,3} | {Trunc(c.NombreCompleto, 32),-32} | {hab,3} | {c.DiasEstancia,4} | {px,12:C2} | {sub,11:C2}");
                }
            }
            Console.WriteLine();

            var rooms = ConfigHabitaciones.ObtenerHabitaciones();
            Console.WriteLine("[HABITACIONES]");
            if (rooms.Length == 0)
            {
                Console.WriteLine("  No hay habitaciones configuradas.");
            }
            else
            {
                int totalHab = rooms.Length;
                int libres = rooms.Count(h => h.Estado == Estado.Libre);
                int ocupadas = rooms.Count(h => h.Estado == Estado.Ocupada);
                Console.WriteLine($"  - Totales    : {totalHab}");
                Console.WriteLine($"  - Libres     : {libres}");
                Console.WriteLine($"  - Ocupadas   : {ocupadas}");
                Console.WriteLine();
                Console.WriteLine("No.  | Tipo      | Precio       | Estado");
                Console.WriteLine("-----+-----------+--------------+---------");
                foreach (var h in rooms)
                    Console.WriteLine($"{h.Numero,3}  | {h.Tipo,-9} | {h.Precio,10:C2} | {h.Estado}");
            }

            Console.WriteLine();
            Console.WriteLine("[CLIENTES - LISTA GENERAL]");
            if (totalClientes == 0)
            {
                Console.WriteLine("  No hay clientes registrados.");
            }
            else
            {
                Console.WriteLine("Id  | Nombre                          | DPI            | Teléfono       | Días | Habitación");
                Console.WriteLine("----+---------------------------------+----------------+----------------+------+-----------");
                for (int i = 0; i < clientes.Count; i++)
                {
                    var c = clientes[i];
                    string hab = c.HabitacionNumero?.ToString() ?? "-";
                    Console.WriteLine($"{i + 1,3} | {Trunc(c.NombreCompleto, 33),-33} | {Trunc(c.DPI, 14),-14} | {Trunc(c.Telefono, 14),-14} | {c.DiasEstancia,4} | {hab,9}");
                }
            }

            Program.Pausa();
        }

        private static void MostrarIngresosEgresosMensuales()
        {
            Console.Clear();
            Console.WriteLine("=== INGRESOS Y EGRESOS POR MES ===");
            var porMes = CalcularIngresosPorMes();
            if (porMes.Count == 0)
            {
                Program.Pausa("No se encontraron facturas para calcular ingresos.");
                return;
            }

            Console.WriteLine("Mes        | Ingresos      | Egresos       | Utilidad");
            Console.WriteLine("-----------------------------------------------------------");
            foreach (var kv in porMes.OrderBy(k => k.Key))
            {
                var mes = kv.Key;
                decimal ingresos = kv.Value;
                decimal egresos = Math.Round(ingresos * 0.35m, 2);
                decimal utilidad = ingresos - egresos;
                Console.WriteLine($"{mes,-10} | {ingresos,12:C2} | {egresos,12:C2} | {utilidad,12:C2}");
            }

            Program.Pausa();
        }

        private static Dictionary<string, decimal> CalcularIngresosPorMes()
        {
            var dict = new Dictionary<string, decimal>();

            foreach (var f in EnumerarArchivosFacturaIndividual())
            {
                try
                {
                    var txt = File.ReadAllText(f);
                    var fecha = ExtraerFechaHora(txt);
                    if (fecha == null) continue;
                    string clave = fecha.Value.ToString("yyyy-MM");

                    decimal ingreso = ExtraerIngresoSegunTipo(txt);
                    if (ingreso < 0) ingreso = 0;

                    if (dict.ContainsKey(clave)) dict[clave] += ingreso;
                    else dict[clave] = ingreso;
                }
                catch {  }
            }
            return dict;
        }

        private static decimal ExtraerIngresoSegunTipo(string facturaTexto)
        {
            bool esCancel = facturaTexto.Contains(">>> CANCELACIÓN");
            if (esCancel)
            {
                if (TryFindCurrencyAfterLabel(facturaTexto, "Monto días usados", out decimal usados))
                    return usados;
                return 0m;
            }
            else
            {
                if (TryFindCurrencyAfterLabel(facturaTexto, "TOTAL ESTANCIA", out decimal total))
                    return total;
                return 0m;
            }
        }

        private static void MenuFacturas()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== FACTURAS ===");
                Console.WriteLine("1) Ver facturas (consolidado)");
                Console.WriteLine("2) Buscar facturas individuales");
                Console.WriteLine("0) Volver");
                Console.Write("Elige una opción: ");
                var op = Console.ReadLine()?.Trim();

                switch (op)
                {
                    case "1":
                        VerConsolidado();
                        break;
                    case "2":
                        BuscarFacturasIndividuales();
                        break;
                    case "0":
                        return;
                    default:
                        Program.Pausa("Opción inválida.");
                        break;
                }
            }
        }

        private static void VerConsolidado()
        {
            var path = Path.Combine(GetFacturasDir(), "facturas.txt");
            Console.Clear();
            Console.WriteLine("=== CONSOLIDADO: facturas.txt ===");
            if (!File.Exists(path))
            {
                Program.Pausa("No existe el consolidado facturas.txt");
                return;
            }

            var lineas = File.ReadAllLines(path);
            const int CHUNK = 40;
            for (int i = 0; i < lineas.Length; i += CHUNK)
            {
                Console.Clear();
                Console.WriteLine($"=== CONSOLIDADO facturas.txt  (líneas {i + 1} - {Math.Min(i + CHUNK, lineas.Length)} de {lineas.Length}) ===");
                foreach (var l in lineas.Skip(i).Take(CHUNK))
                    Console.WriteLine(l);
                if (i + CHUNK < lineas.Length) Program.Pausa("Continuar...");
            }
            Program.Pausa("Fin de archivo.");
        }

        private static void BuscarFacturasIndividuales()
        {
            Console.Clear();
            Console.WriteLine("=== BÚSQUEDA DE FACTURAS INDIVIDUALES ===");
            Console.WriteLine("1) Por cliente");
            Console.WriteLine("2) Por mes (YYYY-MM)");
            Console.WriteLine("3) Por habitación");
            Console.WriteLine("0) Volver");
            Console.Write("Elige un filtro: ");
            var op = Console.ReadLine()?.Trim();

            var archivos = EnumerarArchivosFacturaIndividual().ToList();
            if (archivos.Count == 0)
            {
                Program.Pausa("No se encontraron facturas individuales.");
                return;
            }

            IEnumerable<string> encontrados = archivos;

            switch (op)
            {
                case "1":
                    {
                        var nombres = ExtraerNombresClientesDeFacturas(archivos)
                                      .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                                      .ToList();
                        if (nombres.Count == 0)
                        {
                            Program.Pausa("No hay facturas con nombre de cliente.");
                            return;
                        }
                        Console.WriteLine();
                        Console.WriteLine("Clientes con facturas:");
                        for (int i = 0; i < nombres.Count; i++)
                            Console.WriteLine($"{i + 1,3}) {nombres[i]}");
                        Console.Write("Elige un cliente por índice (0 = volver): ");
                        if (!int.TryParse(Console.ReadLine(), out int ixC) || ixC < 0 || ixC > nombres.Count) { Program.Pausa("Índice inválido."); return; }
                        if (ixC == 0) return;
                        string elegido = nombres[ixC - 1];
                        encontrados = archivos.Where(p => ContieneEnArchivo(p, "Cliente", elegido, ignoreCase: true));
                        break;
                    }
                case "2":
                    {
                        var meses = archivos.Select(a => ObtenerMesArchivo(a))
                                            .Where(s => !string.IsNullOrWhiteSpace(s))
                                            .Distinct()
                                            .OrderBy(s => s)
                                            .ToList();
                        if (meses.Count == 0)
                        {
                            Program.Pausa("No hay meses disponibles.");
                            return;
                        }
                        Console.WriteLine();
                        Console.WriteLine("Meses con facturas:");
                        for (int i = 0; i < meses.Count; i++)
                            Console.WriteLine($"{i + 1,3}) {meses[i]}");
                        Console.Write("Elige un mes por índice (0 = volver): ");
                        if (!int.TryParse(Console.ReadLine(), out int ixM) || ixM < 0 || ixM > meses.Count) { Program.Pausa("Índice inválido."); return; }
                        if (ixM == 0) return;
                        string ym = meses[ixM - 1];
                        encontrados = archivos.Where(p => ObtenerMesArchivo(p) == ym);
                        break;
                    }
                case "3":
                    {
                        var firmas = ConstruirFirmasHabitacionDesdeFacturas(archivos);
                        if (firmas.Count == 0)
                        {
                            Program.Pausa("No se pudo extraer información de habitaciones desde las facturas.");
                            return;
                        }
                        Console.WriteLine();
                        Console.WriteLine("Firmas de habitación encontradas en facturas:");
                        for (int i = 0; i < firmas.Count; i++)
                        {
                            var f = firmas[i];
                            Console.WriteLine($"{i + 1,3}) Hab {f.Numero} | Tipo: {f.Tipo} | Precio/Noche: {f.Precio:C2}");
                        }
                        Console.Write("Elige una firma por índice (0 = volver): ");
                        if (!int.TryParse(Console.ReadLine(), out int ixF) || ixF < 0 || ixF > firmas.Count) { Program.Pausa("Índice inválido."); return; }
                        if (ixF == 0) return;
                        var chosen = firmas[ixF - 1];

                        encontrados = archivos.Where(p => CoincideFacturaConFirma(p, chosen));
                        break;
                    }
                case "0":
                    return;
                default:
                    Program.Pausa("Opción inválida.");
                    return;
            }

            var lista = encontrados.ToList();
            if (lista.Count == 0)
            {
                Program.Pausa("No se encontraron coincidencias.");
                return;
            }

            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== RESULTADOS ===");
                for (int i = 0; i < lista.Count; i++)
                {
                    Console.WriteLine($"{i + 1,3}) {Path.GetFileName(lista[i])}");
                }
                Console.Write("Elige un índice para ver (0 = volver): ");
                if (!int.TryParse(Console.ReadLine(), out int idx) || idx < 0 || idx > lista.Count) { Program.Pausa("Índice inválido."); continue; }
                if (idx == 0) return;

                var archivo = lista[idx - 1];
                Console.Clear();
                Console.WriteLine($"=== {Path.GetFileName(archivo)} ===");
                Console.WriteLine(File.ReadAllText(archivo, Encoding.UTF8));
                Program.Pausa();
            }
        }

        private record FirmaHabitacion(int Numero, string Tipo, decimal Precio);

        private static string GetFacturasDir()
        {
            string desk = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            return Path.Combine(desk, "Reservas Guatemala", "facturas");
        }

        private static IEnumerable<string> EnumerarArchivosFacturaIndividual()
        {
            var dir = GetFacturasDir();
            if (!Directory.Exists(dir)) yield break;

            foreach (var f in Directory.EnumerateFiles(dir, "*.txt", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(f);
                if (string.Equals(name, "facturas.txt", StringComparison.OrdinalIgnoreCase))
                    continue; 
                yield return f;
            }
        }

        private static DateTime? ExtraerFechaHora(string facturaTexto)
        {
            var reader = new StringReader(facturaTexto);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.TrimStart().StartsWith("Fecha/Hora"))
                {
                    var parts = line.Split(':');
                    if (parts.Length >= 2)
                    {
                        var val = string.Join(":", parts.Skip(1)).Trim();
                        if (DateTime.TryParse(val, out var dt)) return dt;
                    }
                }
            }
            return null;
        }

        private static bool TryFindCurrencyAfterLabel(string texto, string label, out decimal value)
        {
            value = 0m;
            var reader = new StringReader(texto);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Contains(label))
                {
                    var idx = line.IndexOf(':');
                    string num = idx >= 0 ? line.Substring(idx + 1) : line;
                    num = num.Replace("Q", "", StringComparison.OrdinalIgnoreCase)
                             .Replace("GTQ", "", StringComparison.OrdinalIgnoreCase)
                             .Trim();
                    if (decimal.TryParse(num, NumberStyles.Currency, CultureInfo.CurrentCulture, out var v1)) { value = v1; return true; }
                    if (decimal.TryParse(num, NumberStyles.Currency, CultureInfo.InvariantCulture, out var v2)) { value = v2; return true; }
                }
            }
            return false;
        }

        private static string? ObtenerMesArchivo(string path)
        {
            try
            {
                var txt = File.ReadAllText(path);
                var fecha = ExtraerFechaHora(txt);
                if (fecha == null) return null;
                return fecha.Value.ToString("yyyy-MM");
            }
            catch { return null; }
        }

        private static bool ContieneEnArchivo(string path, string etiqueta, string valor, bool ignoreCase = false)
        {
            try
            {
                var txt = File.ReadAllText(path);
                var reader = new StringReader(txt);
                string? line;
                var comp = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains(etiqueta, StringComparison.OrdinalIgnoreCase) && line.Contains(valor, comp))
                        return true;
                }
            }
            catch { }
            return false;
        }

        private static List<string> ExtraerNombresClientesDeFacturas(IEnumerable<string> archivos)
        {
            var lista = new List<string>();
            foreach (var p in archivos)
            {
                try
                {
                    var txt = File.ReadAllText(p);
                    var reader = new StringReader(txt);
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.TrimStart().StartsWith("Cliente"))
                        {
                            var parts = line.Split(':');
                            if (parts.Length >= 2)
                            {
                                var nombre = string.Join(":", parts.Skip(1)).Trim();
                                if (!string.IsNullOrWhiteSpace(nombre))
                                {
                                    lista.Add(nombre);
                                }
                            }
                            break;
                        }
                    }
                }
                catch { }
            }
            return lista.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static List<FirmaHabitacion> ConstruirFirmasHabitacionDesdeFacturas(IEnumerable<string> archivos)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<FirmaHabitacion>();

            foreach (var p in archivos)
            {
                try
                {
                    var txt = File.ReadAllText(p);
                    int hab = ExtraerNumeroHabitacion(txt);
                    if (hab <= 0) continue;
                    string tipo = ExtraerTipoHabitacion(txt) ?? "N/A";

                    var precios = ExtraerPreciosUnitarios(txt);
                    if (precios.Count == 0)
                    {
                        string key0 = $"{hab}|{tipo}|{0m}";
                        if (set.Add(key0)) list.Add(new FirmaHabitacion(hab, tipo, 0m));
                    }
                    else
                    {
                        foreach (var pr in precios)
                        {
                            string key = $"{hab}|{tipo}|{pr}";
                            if (set.Add(key)) list.Add(new FirmaHabitacion(hab, tipo, pr));
                        }
                    }
                }
                catch { }
            }
            return list.OrderBy(x => x.Numero).ThenBy(x => x.Tipo).ThenBy(x => x.Precio).ToList();
        }

        private static int ExtraerNumeroHabitacion(string texto)
        {
            var reader = new StringReader(texto);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.TrimStart().StartsWith("Habitación"))
                {
                    var parts = line.Split(':');
                    if (parts.Length >= 2)
                    {
                        var val = string.Join(":", parts.Skip(1)).Trim();
                        if (int.TryParse(new string(val.Where(char.IsDigit).ToArray()), out int nro))
                            return nro;
                    }
                    break;
                }
            }
            return -1;
        }

        private static string? ExtraerTipoHabitacion(string texto)
        {
            var reader = new StringReader(texto);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                string l = line.TrimStart();
                if (l.StartsWith("Tipo habitación", StringComparison.OrdinalIgnoreCase) ||
                    l.StartsWith("Tipo", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = l.Split(':');
                    if (parts.Length >= 2)
                    {
                        var tipo = string.Join(":", parts.Skip(1)).Trim();
                        if (!string.IsNullOrWhiteSpace(tipo))
                            return tipo;
                    }
                }
            }
            return null;
        }

        private static List<decimal> ExtraerPreciosUnitarios(string texto)
        {
            var precios = new List<decimal>();
            var reader = new StringReader(texto);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Contains("día(s)") && line.Contains("×"))
                {
                    int idx = line.IndexOf('×');
                    if (idx >= 0)
                    {
                        var resto = line.Substring(idx + 1);
                        var antesIgual = resto.Split('=').FirstOrDefault() ?? resto;
                        var numTxt = antesIgual.Replace("Q", "", StringComparison.OrdinalIgnoreCase)
                                               .Replace("GTQ", "", StringComparison.OrdinalIgnoreCase)
                                               .Trim();
                        numTxt = new string(numTxt.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
                        if (decimal.TryParse(numTxt, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.CurrentCulture, out var v1))
                            precios.Add(v1);
                        else if (decimal.TryParse(numTxt, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out var v2))
                            precios.Add(v2);
                    }
                }
            }
            return precios.Distinct().ToList();
        }

        private static bool CoincideFacturaConFirma(string path, FirmaHabitacion f)
        {
            try
            {
                var txt = File.ReadAllText(path);
                int hab = ExtraerNumeroHabitacion(txt);
                if (hab != f.Numero) return false;

                string tipo = ExtraerTipoHabitacion(txt) ?? "N/A";
                if (!tipo.Equals(f.Tipo, StringComparison.OrdinalIgnoreCase)) return false;

                var precios = ExtraerPreciosUnitarios(txt);
                if (precios.Count == 0)
                    return f.Precio == 0m;

                return precios.Any(p => p == f.Precio);
            }
            catch
            {
                return false;
            }
        }

        private static string Trunc(string s, int n) => s.Length <= n ? s : s.Substring(0, n - 1) + "…";

        private static string LeerPassword()
        {
            var pwd = new StringBuilder();
            ConsoleKey key;
            while (true)
            {
                var ck = Console.ReadKey(intercept: true);
                key = ck.Key;
                if (key == ConsoleKey.Enter) break;
                if (key == ConsoleKey.Backspace && pwd.Length > 0)
                {
                    pwd.Length--;
                    continue;
                }
                if (!char.IsControl(ck.KeyChar))
                {
                    pwd.Append(ck.KeyChar);
                }
            }
            Console.WriteLine();
            return pwd.ToString();
        }
    }
}
