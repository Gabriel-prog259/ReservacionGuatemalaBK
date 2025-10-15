using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace HotelReservas
{
    public static class DataStore
    {
        public static string BaseDir { get; private set; } = "";
        public static string HabitacionesDir { get; private set; } = "";
        public static string ClientesDir { get; private set; } = "";
        public static string FacturasDir { get; private set; } = "";

        public static string HabitacionesFile => Path.Combine(HabitacionesDir, "Habitaciones.txt");
        public static string ClientesFile => Path.Combine(ClientesDir, "Clientes.txt");
        public static string FacturasFile => Path.Combine(FacturasDir, "Facturas.txt");

        public static string HabitacionesErrFile => Path.Combine(HabitacionesDir, "Habitaciones_Errores.txt");
        public static string ClientesErrFile => Path.Combine(ClientesDir, "Clientes_Errores.txt");

        public static void Init()
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            BaseDir = Path.Combine(desktop, "Reservas Guatemala");
            HabitacionesDir = Path.Combine(BaseDir, "Habitaciones");
            ClientesDir = Path.Combine(BaseDir, "Clientes");
            FacturasDir = Path.Combine(BaseDir, "Facturas");

            Directory.CreateDirectory(BaseDir);
            Directory.CreateDirectory(HabitacionesDir);
            Directory.CreateDirectory(ClientesDir);
            Directory.CreateDirectory(FacturasDir);

            if (!File.Exists(HabitacionesFile)) File.WriteAllText(HabitacionesFile, "");
            if (!File.Exists(ClientesFile)) File.WriteAllText(ClientesFile, "");
            if (!File.Exists(FacturasFile)) File.WriteAllText(FacturasFile, "");
        }

        public static void SaveHabitaciones(Habitacion[] rooms)
        {
            if (rooms == null) return;
            using var sw = new StreamWriter(HabitacionesFile, false);
            foreach (var h in rooms)
            {
                sw.WriteLine($"{h.Numero}|{(int)h.Tipo}|{h.Precio.ToString(CultureInfo.InvariantCulture)}|{(int)h.Estado}");
            }
        }

        public static Habitacion[] LoadHabitaciones()
        {
            if (!File.Exists(HabitacionesFile)) return Array.Empty<Habitacion>();
            var lines = File.ReadAllLines(HabitacionesFile);
            var list = new List<Habitacion>();
            var errores = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                var ln = lines[i];
                if (string.IsNullOrWhiteSpace(ln)) continue;

                try
                {
                    var parts = ln.Split('|');
                    if (parts.Length < 4) throw new FormatException("se esperaban 4 columnas");

                    int num = int.Parse(parts[0]);
                    var tipo = (TipoHabitacion)int.Parse(parts[1]);
                    decimal precio = decimal.Parse(parts[2], CultureInfo.InvariantCulture);
                    var est = (Estado)int.Parse(parts[3]);

                    list.Add(new Habitacion
                    {
                        Numero = num,
                        Tipo = tipo,
                        Precio = precio,
                        Estado = est
                    });
                }
                catch (Exception ex)
                {
                    errores.Add($"Línea {i + 1}: \"{ln}\" -> {ex.Message}");
                }
            }

            if (errores.Count > 0)
            {
                File.AppendAllText(HabitacionesErrFile,
                    $"[{DateTime.Now}] Errores al cargar Habitaciones.txt{Environment.NewLine}" +
                    string.Join(Environment.NewLine, errores) + Environment.NewLine +
                    new string('-', 60) + Environment.NewLine);
            }

            return list.OrderBy(h => h.Numero).ToArray();
        }

        public static void LoadHabitacionesIntoConfig()
        {
            var rooms = LoadHabitaciones();
            if (rooms.Length > 0)
            {
                ConfigHabitaciones.CargarDesdePersistencia(rooms);
            }
        }

        public static void SaveClientes(IEnumerable<Cliente> clientes)
        {
            using var sw = new StreamWriter(ClientesFile, false);
            foreach (var c in clientes)
            {
                int dias = c.DiasEstancia;
                string hab = c.HabitacionNumero?.ToString() ?? "";
                decimal price = 0m;
                if (c.Estancias != null && c.Estancias.Count > 0)
                {
                    var last = c.Estancias.Last();
                    price = last.PrecioNoche;
                }
                sw.WriteLine($"{Esc(c.NombreCompleto)}|{Esc(c.DPI)}|{Esc(c.Nit)}|{Esc(c.Telefono)}|{dias}|{hab}|{price.ToString(CultureInfo.InvariantCulture)}");
            }
        }

        public static List<Cliente> LoadClientes()
        {
            var res = new List<Cliente>();
            if (!File.Exists(ClientesFile)) return res;

            var errores = new List<string>();
            var lines = File.ReadAllLines(ClientesFile);

            for (int i = 0; i < lines.Length; i++)
            {
                var ln = lines[i];
                if (string.IsNullOrWhiteSpace(ln)) continue;

                try
                {
                    var p = SplitSafe(ln);
                    if (p.Length < 7) throw new FormatException("se esperaban 7 columnas");

                    var c = new Cliente
                    {
                        NombreCompleto = UnEsc(p[0]),
                        DPI = UnEsc(p[1]),
                        Nit = UnEsc(p[2]),
                        Telefono = UnEsc(p[3]),
                        DiasEstancia = int.TryParse(p[4], out var d) ? d : 0
                    };

                    if (int.TryParse(p[5], out var hab))
                        c.HabitacionNumero = hab;
                    else
                        c.HabitacionNumero = null;

                    decimal precioNoche = 0m;
                    _ = decimal.TryParse(p[6], NumberStyles.Number, CultureInfo.InvariantCulture, out precioNoche);

                    c.Estancias = new List<MovimientoEstancia>();
                    if (c.DiasEstancia > 0 && c.HabitacionNumero != null)
                    {
                        c.Estancias.Add(new MovimientoEstancia
                        {
                            Concepto = "Reserva importada",
                            Dias = c.DiasEstancia,
                            PrecioNoche = precioNoche
                        });
                    }

                    res.Add(c);
                }
                catch (Exception ex)
                {
                    errores.Add($"Línea {i + 1}: \"{ln}\" -> {ex.Message}");
                }
            }

            if (errores.Count > 0)
            {
                File.AppendAllText(ClientesErrFile,
                    $"[{DateTime.Now}] Errores al cargar Clientes.txt{Environment.NewLine}" +
                    string.Join(Environment.NewLine, errores) + Environment.NewLine +
                    new string('-', 60) + Environment.NewLine);
            }

            return res;
        }

        public static void LoadClientesIntoModule()
        {
            var list = LoadClientes();
            if (list.Count > 0)
            {
                Clientes.CargarDesdePersistencia(list);
            }
        }

        public static string AppendFactura(string facturaTexto, string clienteNombre, DateTime fecha, string tipo)
        {
            Directory.CreateDirectory(FacturasDir);

            using (var sw = new StreamWriter(FacturasFile, true))
            {
                sw.WriteLine(facturaTexto);
                sw.WriteLine();
                sw.WriteLine(new string('=', 70));
                sw.WriteLine();
            }

            var safeClient = SanitizeFileName(clienteNombre);
            var stamp = fecha.ToString("yyyyMMdd-HHmmss");
            var filename = $"{safeClient}_{tipo.ToUpperInvariant()}_{stamp}.txt";
            var fullPath = Path.Combine(FacturasDir, filename);
            File.WriteAllText(fullPath, facturaTexto);
            return fullPath;
        }

        private static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("|", "\\p");
        private static string UnEsc(string s) => (s ?? "").Replace("\\p", "|").Replace("\\\\", "\\");

        private static string[] SplitSafe(string line)
        {
            var parts = new List<string>();
            var cur = "";
            bool slash = false;
            foreach (var ch in line)
            {
                if (slash)
                {
                    if (ch == 'p') { cur += "|"; }
                    else { cur += "\\" + ch; }
                    slash = false;
                }
                else
                {
                    if (ch == '\\') slash = true;
                    else if (ch == '|') { parts.Add(cur); cur = ""; }
                    else cur += ch;
                }
            }
            parts.Add(cur);
            return parts.ToArray();
        }

        private static string SanitizeFileName(string s)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string((s ?? "").Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            if (string.IsNullOrWhiteSpace(cleaned)) cleaned = "Cliente";
            return cleaned.Length > 60 ? cleaned.Substring(0, 60) : cleaned;
        }
    }
}
