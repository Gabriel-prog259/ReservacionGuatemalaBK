using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace HotelReservas
{
    public class MovimientoEstancia
    {
        public string Concepto { get; set; } = "";
        public int Dias { get; set; }
        public decimal PrecioNoche { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
    }

    public class Cliente
    {
        public string NombreCompleto = "";
        public string DPI = "";
        public string Nit = "";
        public string Telefono = "";

        public int DiasEstancia;
        public int? HabitacionNumero;

        public string UltimoComprobante = "";
        public List<MovimientoEstancia> Estancias = new();
    }

    public static class Clientes
    {
        private static readonly List<Cliente> _clientes = new();

        public static List<Cliente> ObtenerClientesParaGuardar() => _clientes;
        public static void CargarDesdePersistencia(List<Cliente> list)
        {
            _clientes.Clear();
            _clientes.AddRange(list);
        }

        public static void MostrarMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine(" === Opciones para clientes === ");
                Console.WriteLine("1) Registrar cliente");
                Console.WriteLine("2) Listado de clientes");
                Console.WriteLine("0) Volver");
                Console.Write("Selecciona una opción: ");

                var op = Console.ReadLine()?.Trim();
                switch (op)
                {
                    case "1":
                        CrearCliente();
                        break;
                    case "2":
                        ListadoClientesMenu();
                        break;
                    case "0":
                        return;
                    default:
                        Program.Pausa("Opción inválida.");
                        break;
                }
            }
        }

        private static void CrearCliente()
        {
            Console.Clear();
            Console.WriteLine("=== Registrar Cliente ===");

            var c = new Cliente
            {
                NombreCompleto = LeerNoVacio("Nombre completo: "),
                DPI = LeerNoVacio("DPI: "),
                Nit = LeerOpcional("NIT (Enter si no tiene): "),
                Telefono = LeerNoVacio("Teléfono: "),
                DiasEstancia = LeerEnteroNoNegativo("Días de estancia (0 si solo registro): ")
            };
            if (string.IsNullOrWhiteSpace(c.Nit)) c.Nit = "CF";

            if (c.DiasEstancia > 0)
            {
                if (!ConfigHabitaciones.HayHabitaciones || !ConfigHabitaciones.HayLibres())
                {
                    Program.Pausa("No hay habitaciones libres. Se registrará al cliente sin hospedaje.");
                    c.DiasEstancia = 0;
                    c.HabitacionNumero = null;
                }
                else
                {
                    Console.WriteLine();
                    if (ElegirHabitacionLibreConValidacion(out int numHab))
                    {
                        c.HabitacionNumero = numHab;
                        ConfigHabitaciones.Ocupar(numHab);

                        var px = ConfigHabitaciones.PrecioPorNoche(numHab);
                        c.Estancias.Add(new MovimientoEstancia
                        {
                            Concepto = "Reserva inicial",
                            Dias = c.DiasEstancia,
                            PrecioNoche = px
                        });
                    }
                    else
                    {
                        c.DiasEstancia = 0;
                        c.HabitacionNumero = null;
                        Program.Pausa("Asignación cancelada. El cliente quedó sin hospedaje.");
                    }
                }
            }

            _clientes.Add(c);
            DataStore.SaveClientes(_clientes);
            Program.Pausa("Cliente registrado correctamente.");
        }

        private static void ListadoClientesMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== LISTADO DE CLIENTES ===");
                if (_clientes.Count == 0)
                {
                    Console.WriteLine("No hay clientes registrados.");
                }
                else
                {
                    ImprimirTablaClientes();
                }

                Console.WriteLine();
                Console.WriteLine("Acciones:");
                Console.WriteLine("1) Asignar hospedaje");
                Console.WriteLine("2) Cancelar estancia");
                Console.WriteLine("3) Finalizar estancia");
                Console.WriteLine("4) Editar información del cliente");
                Console.WriteLine("5) Eliminar cliente");
                Console.WriteLine("0) Volver");
                Console.Write("Elige una acción: ");
                var op = Console.ReadLine()?.Trim();

                switch (op)
                {
                    case "1":
                        CrearHospedaje();
                        break;
                    case "2":
                        CancelarEstancia();
                        break;
                    case "3":
                        FinalizarEstancia();
                        break;
                    case "4":
                        EditarCliente();
                        break;
                    case "5":
                        EliminarCliente();
                        break;
                    case "0":
                        return;
                    default:
                        Program.Pausa("Opción inválida.");
                        break;
                }
            }
        }

        private static void ImprimirTablaClientes()
        {
            Console.WriteLine("Id | Nombre                          | DPI            | Teléfono       | Días | Habitación");
            Console.WriteLine("-------------------------------------------------------------------------------------------");
            for (int i = 0; i < _clientes.Count; i++)
            {
                var c = _clientes[i];
                string hab = c.HabitacionNumero?.ToString() ?? "-";
                Console.WriteLine($"{i + 1,3} | {Trunc(c.NombreCompleto, 33),-33} | {Trunc(c.DPI, 14),-14} | {Trunc(c.Telefono, 14),-14} | {c.DiasEstancia,4} | {hab,9}");
            }
        }

        private static void EditarCliente()
        {
            if (_clientes.Count == 0)
            {
                Program.Pausa("No hay clientes para editar.");
                return;
            }

            Console.Write("Ingresa el Id del cliente a editar (0 = cancelar): ");
            if (!int.TryParse(Console.ReadLine(), out int idx1) || idx1 < 0 || idx1 > _clientes.Count)
            {
                Program.Pausa("Id inválido.");
                return;
            }
            if (idx1 == 0) return;
            int idx = idx1 - 1;

            var c = _clientes[idx];

            Console.WriteLine($"Editando: {c.NombreCompleto} (DPI: {c.DPI}, Tel: {c.Telefono}, NIT: {c.Nit})");
            Console.WriteLine("Deja vacío para conservar el valor actual.");

            Console.Write("Nuevo nombre: ");
            var nombre = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(nombre)) c.NombreCompleto = nombre.Trim();

            Console.Write("Nuevo DPI: ");
            var dpi = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(dpi)) c.DPI = dpi.Trim();

            Console.Write("Nuevo teléfono: ");
            var tel = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(tel)) c.Telefono = tel.Trim();

            Console.Write("Nuevo NIT (Enter = CF): ");
            var nit = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(nit)) c.Nit = "CF";
            else c.Nit = nit.Trim();

            DataStore.SaveClientes(_clientes);
            Program.Pausa("Cliente actualizado.");
        }

        private static void EliminarCliente()
        {
            if (_clientes.Count == 0)
            {
                Program.Pausa("No hay clientes para eliminar.");
                return;
            }

            Console.Write("Ingresa el Id del cliente a eliminar (0 = cancelar): ");
            if (!int.TryParse(Console.ReadLine(), out int idx1) || idx1 < 0 || idx1 > _clientes.Count)
            {
                Program.Pausa("Id inválido.");
                return;
            }
            if (idx1 == 0) return;
            int idx = idx1 - 1;
            var c = _clientes[idx];

            if (c.DiasEstancia > 0 && c.HabitacionNumero != null)
            {
                Console.WriteLine("El cliente tiene una reserva ACTIVA. Debes elegir una opción:");
                Console.WriteLine("1) Finalizar estancia");
                Console.WriteLine("2) Cancelar estancia (reembolso 75% de lo no usado)");
                Console.WriteLine("0) Volver (no eliminar)");
                Console.Write("Tu elección: ");
                var op = Console.ReadLine()?.Trim();
                if (op == "1")
                {
                    FinalizarEstanciaDeCliente(idx);
                }
                else if (op == "2")
                {
                    CancelarEstanciaDeCliente(idx);
                }
                else
                {
                    Program.Pausa("Operación cancelada. El cliente no fue eliminado.");
                    return;
                }
            }

            _clientes.RemoveAt(idx);
            DataStore.SaveClientes(_clientes);
            Program.Pausa("Cliente eliminado correctamente.");
        }

        private static void CrearHospedaje()
        {
            Console.Write("Ingresa el Id del cliente (0 = cancelar): ");
            if (!int.TryParse(Console.ReadLine(), out int idx1) || idx1 < 0 || idx1 > _clientes.Count)
            {
                Program.Pausa("Id inválido.");
                return;
            }
            if (idx1 == 0) return;
            int idx = idx1 - 1;

            var c = _clientes[idx];

            if (c.DiasEstancia > 0 && c.HabitacionNumero is int habActual)
            {
                Console.WriteLine($"Este cliente ya tiene {c.DiasEstancia} día(s) en la habitación #{habActual}.");
                if (Confirmar("¿Desea aumentar los días de estancia? (S/N): "))
                {
                    int extra = LeerEnteroPositivo("¿Cuántos días desea agregar? ");
                    if (Confirmar("¿Mantener la misma habitación? (S/N): "))
                    {
                        c.DiasEstancia += extra;
                        c.Estancias.Add(new MovimientoEstancia
                        {
                            Concepto = "Extensión",
                            Dias = extra,
                            PrecioNoche = ConfigHabitaciones.PrecioPorNoche(habActual)
                        });
                        DataStore.SaveClientes(_clientes);
                        Program.Pausa("Días de estancia actualizados en la misma habitación.");
                        return;
                    }
                    else
                    {
                        string comp = LeerComprobanteUnico("Ingresa el código de comprobante de la primera reserva: ");

                        if (!ConfigHabitaciones.HayLibres())
                        {
                            Program.Pausa("No hay habitaciones libres para el cambio. Operación cancelada.");
                            return;
                        }

                        decimal totalPagadoHastaAhora = c.Estancias.Sum(m => m.Dias * m.PrecioNoche);
                        var facturaCambio = ConstruirFacturaTexto(
                            c,
                            comp,
                            esCancelacion: false,
                            diasUsados: c.DiasEstancia,
                            diasNoUsados: 0,
                            montoUsados: totalPagadoHastaAhora,
                            reembolso: 0m
                        );
                        var tsCambio = DateTime.Now;
                        var pathCambio = DataStore.AppendFactura(facturaCambio, c.NombreCompleto, tsCambio, "CambioHabitacion");
                        Console.WriteLine($"Factura (cambio de habitación) guardada en: {pathCambio}");

                        Console.WriteLine();
                        if (!ElegirHabitacionLibreConValidacion(out int nuevaHab))
                        {
                            Program.Pausa("Cambio cancelado por el usuario.");
                            return;
                        }

                        ConfigHabitaciones.Liberar(habActual);
                        ConfigHabitaciones.Ocupar(nuevaHab);

                        c.HabitacionNumero = nuevaHab;
                        c.DiasEstancia = extra;
                        c.UltimoComprobante = comp;

                        c.Estancias = new List<MovimientoEstancia>
                        {
                            new MovimientoEstancia
                            {
                                Concepto = "Reserva (cambio de habitación)",
                                Dias = extra,
                                PrecioNoche = ConfigHabitaciones.PrecioPorNoche(nuevaHab)
                            }
                        };

                        DataStore.SaveHabitaciones(ConfigHabitaciones.ObtenerHabitaciones());
                        DataStore.SaveClientes(_clientes);

                        Program.Pausa($"Cambio realizado. Nueva habitación #{nuevaHab}. Días asignados: {c.DiasEstancia}.");
                        return;
                    }
                }
                else
                {
                    Program.Pausa("No se realizaron cambios.");
                    return;
                }
            }

            if (!ConfigHabitaciones.HayHabitaciones || !ConfigHabitaciones.HayLibres())
            {
                Program.Pausa("No hay habitaciones libres para asignar.");
                return;
            }

            c.DiasEstancia = LeerEnteroPositivo("Días de estancia: ");

            if (ElegirHabitacionLibreConValidacion(out int numHab))
            {
                c.HabitacionNumero = numHab;
                ConfigHabitaciones.Ocupar(numHab);

                c.Estancias = new List<MovimientoEstancia>
                {
                    new MovimientoEstancia
                    {
                        Concepto = "Reserva inicial",
                        Dias = c.DiasEstancia,
                        PrecioNoche = ConfigHabitaciones.PrecioPorNoche(numHab)
                    }
                };

                DataStore.SaveHabitaciones(ConfigHabitaciones.ObtenerHabitaciones());
                DataStore.SaveClientes(_clientes);

                Program.Pausa("Hospedaje creado y habitación asignada.");
            }
            else
            {
                c.DiasEstancia = 0;
                c.HabitacionNumero = null;
                c.Estancias.Clear();
                DataStore.SaveClientes(_clientes);
                Program.Pausa("Asignación cancelada. El cliente continúa sin hospedaje.");
            }
        }

        private static void CancelarEstancia()
        {
            if (_clientes.All(x => x.DiasEstancia == 0))
            {
                Program.Pausa("Ningún cliente tiene estancia para cancelar.");
                return;
            }

            Console.Write("Ingresa el Id del cliente (0 = cancelar): ");
            if (!int.TryParse(Console.ReadLine(), out int idx1) || idx1 < 0 || idx1 > _clientes.Count)
            {
                Program.Pausa("Id inválido.");
                return;
            }
            if (idx1 == 0) return;
            int idx = idx1 - 1;

            CancelarEstanciaDeCliente(idx);
        }

        private static void CancelarEstanciaDeCliente(int idx)
        {
            var c = _clientes[idx];
            if (c.DiasEstancia == 0 || c.HabitacionNumero is null)
            {
                Program.Pausa("Ese cliente no tiene una estancia activa.");
                return;
            }

            int usados = LeerEnteroEnRango($"Días usados (0 a {c.DiasEstancia}): ", 0, c.DiasEstancia);
            int noUsados = c.DiasEstancia - usados;

            string comp = LeerComprobanteUnico("Comprobante de pago de los días utilizados: ");

            CalcularMontosUsadosNoUsados(c.Estancias, usados,
                out decimal montoUsados, out decimal montoNoUsados);

            decimal reembolso = montoNoUsados * 0.75m;

            var factura = ConstruirFacturaTexto(c, comp, true, usados, noUsados, montoUsados, reembolso);
            Console.WriteLine(factura);

            var ts = DateTime.Now;
            var path = DataStore.AppendFactura(factura, c.NombreCompleto, ts, "Cancelacion");
            Console.WriteLine($"Factura guardada en: {path}");

            ConfigHabitaciones.Liberar(c.HabitacionNumero.Value);
            c.DiasEstancia = 0;
            c.HabitacionNumero = null;
            c.Estancias.Clear();
            c.UltimoComprobante = comp;

            DataStore.SaveHabitaciones(ConfigHabitaciones.ObtenerHabitaciones());
            DataStore.SaveClientes(_clientes);

            Program.Pausa("Cancelación procesada.");
        }

        private static void FinalizarEstancia()
        {
            var conReserva = _clientes
                .Select((c, i) => new { c, i })
                .Where(x => x.c.DiasEstancia > 0 && x.c.HabitacionNumero != null)
                .ToList();

            if (conReserva.Count == 0)
            {
                Program.Pausa("No hay clientes con reserva activa para finalizar.");
                return;
            }

            Console.WriteLine("=== Cliente con estancia activa ===");
            Console.WriteLine("Id | Nombre                         | Hab | Días | Teléfono");
            Console.WriteLine("------------------------------------------------------------");
            foreach (var x in conReserva)
            {
                Console.WriteLine($"{x.i + 1,3} | {Trunc(x.c.NombreCompleto, 29),-29} | {x.c.HabitacionNumero,3} | {x.c.DiasEstancia,4} | {Trunc(x.c.Telefono, 10)}");
            }
            Console.WriteLine();

            Console.Write("Elige el Id del cliente (0=cancelar): ");
            if (!int.TryParse(Console.ReadLine(), out int idxShown) || idxShown < 0) { Program.Pausa("Id inválido."); return; }
            if (idxShown == 0) return;
            int idxGlobal = idxShown - 1;

            FinalizarEstanciaDeCliente(idxGlobal);
        }

        private static void FinalizarEstanciaDeCliente(int idx)
        {
            var c = _clientes[idx];
            if (c.DiasEstancia == 0 || c.HabitacionNumero is null)
            {
                Program.Pausa("Ese cliente no tiene una estancia activa.");
                return;
            }

            string comp = LeerComprobanteUnico("Comprobante de pago de la habitación: ");

            decimal total = c.Estancias.Sum(m => m.Dias * m.PrecioNoche);

            var factura = ConstruirFacturaTexto(c, comp, false, c.DiasEstancia, 0, total, 0m);
            Console.WriteLine(factura);

            var ts = DateTime.Now;
            var path = DataStore.AppendFactura(factura, c.NombreCompleto, ts, "Finalizacion");
            Console.WriteLine($"Factura guardada en: {path}");

            ConfigHabitaciones.Liberar(c.HabitacionNumero!.Value);
            c.DiasEstancia = 0;
            c.HabitacionNumero = null;
            c.Estancias.Clear();
            c.UltimoComprobante = comp;

            DataStore.SaveHabitaciones(ConfigHabitaciones.ObtenerHabitaciones());
            DataStore.SaveClientes(_clientes);

            Program.Pausa("Estancia finalizada y facturada.");
        }

        private static string LeerComprobanteUnico(string prompt)
        {
            while (true)
            {
                string comp = LeerNoVacio(prompt);
                if (ComprobanteExistente(comp))
                {
                    Console.WriteLine(">> Comprobante ya registrado. Asigna otro comprobante válido.");
                    continue;
                }
                return comp;
            }
        }

        private static bool ComprobanteExistente(string comprobante)
        {
            try
            {
                string desk = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string facturasDir = Path.Combine(desk, "Reservas Guatemala", "facturas");
                if (!Directory.Exists(facturasDir)) return false;

                foreach (var file in Directory.EnumerateFiles(facturasDir, "*.txt", SearchOption.TopDirectoryOnly))
                {
                    string text = File.ReadAllText(file);
                    if (text.Contains("Comprobante") && text.Contains(comprobante))
                        return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static string ConstruirFacturaTexto(
            Cliente c,
            string comprobante,
            bool esCancelacion,
            int diasUsados,
            int diasNoUsados,
            decimal montoUsados,
            decimal reembolso)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine();
            sb.AppendLine("=============================================================");
            sb.AppendLine("                  FACTURA / RECIBO DE ESTANCIA               ");
            sb.AppendLine("=============================================================");
            sb.AppendLine($"Hotel / Razón social  : Reservas Guatemala SA");
            sb.AppendLine($"Folio                 : {GenerarFolio()}");
            sb.AppendLine($"Fecha/Hora            : {DateTime.Now}");
            sb.AppendLine($"Comprobante           : {comprobante}");
            sb.AppendLine("-------------------------------------------------------------");
            sb.AppendLine($"Cliente               : {c.NombreCompleto}");
            sb.AppendLine($"NIT                   : {(!string.IsNullOrWhiteSpace(c.Nit) ? c.Nit : c.DPI)}");
            sb.AppendLine($"Teléfono              : {c.Telefono}");
            sb.AppendLine($"Habitación            : {(c.HabitacionNumero?.ToString() ?? "-")}");
            sb.AppendLine("--------------------------------------------------------------");

            if (c.Estancias.Count == 0)
            {
                sb.AppendLine("No hay movimientos registrados.");
            }
            else
            {
                sb.AppendLine("DETALLE:");
                int i = 1;
                foreach (var m in c.Estancias)
                {
                    decimal sub = m.Dias * m.PrecioNoche;
                    sb.AppendLine($"{i,2}. {m.Concepto,-30}  {m.Dias,3} día(s) × {m.PrecioNoche,10:C2} = {sub,10:C2}");
                    i++;
                }
            }

            decimal total = c.Estancias.Sum(m => m.Dias * m.PrecioNoche);
            sb.AppendLine("--------------------------------------------------------------");
            sb.AppendLine($"TOTAL ESTANCIA        : {total,10:C2}");

            if (esCancelacion)
            {
                sb.AppendLine();
                sb.AppendLine(">>> CANCELACIÓN");
                sb.AppendLine($"Días utilizados       : {diasUsados}");
                sb.AppendLine($"Días sin usar         : {diasNoUsados}");
                sb.AppendLine($"Monto días usados     : {montoUsados,10:C2}");
                sb.AppendLine($"Reembolso (del 75% sobre reservacion no utilizada): {reembolso,10:C2}");
                sb.AppendLine("--------------------------------------------------------------");
                sb.AppendLine($"TOTAL A DEVOLVER      : {reembolso,10:C2}");
            }

            sb.AppendLine("==============================================================");
            sb.AppendLine();
            return sb.ToString();
        }

        private static string GenerarFolio()
        {
            var rnd = new Random();
            return $"{DateTime.Now:yyyyMMdd-HHmmss}-{rnd.Next(100, 999)}";
        }

        private static void CalcularMontosUsadosNoUsados(
            List<MovimientoEstancia> movs,
            int diasUsados,
            out decimal montoUsados,
            out decimal montoNoUsados)
        {
            int restantesUsados = diasUsados;
            montoUsados = 0m;
            decimal total = 0m;

            foreach (var m in movs)
            {
                total += m.Dias * m.PrecioNoche;
                if (restantesUsados <= 0) continue;

                int consumir = Math.Min(restantesUsados, m.Dias);
                montoUsados += consumir * m.PrecioNoche;
                restantesUsados -= consumir;
            }

            montoNoUsados = total - montoUsados;
        }

        private static string LeerNoVacio(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                var s = Console.ReadLine()?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(s)) return s;
                Console.WriteLine("No puede quedar vacío.");
            }
        }

        private static string LeerOpcional(string prompt)
        {
            Console.Write(prompt);
            return (Console.ReadLine() ?? "").Trim();
        }

        private static bool Confirmar(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                var s = (Console.ReadLine() ?? string.Empty).Trim().ToUpperInvariant();
                if (s == "S" || s == "SI" || s == "SÍ") return true;
                if (s == "N" || s == "NO") return false;
                Console.WriteLine("Responde S/N.");
            }
        }

        private static int LeerEnteroPositivo(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                if (int.TryParse(Console.ReadLine(), out int v) && v > 0) return v;
                Console.WriteLine("Ingresa un entero positivo.");
            }
        }

        private static int LeerEnteroNoNegativo(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                if (int.TryParse(Console.ReadLine(), out int v) && v >= 0) return v;
                Console.WriteLine("Ingresa un entero ≥ 0.");
            }
        }

        private static int LeerEnteroEnRango(string prompt, int min, int max)
        {
            while (true)
            {
                Console.Write(prompt);
                if (int.TryParse(Console.ReadLine(), out int v) && v >= min && v <= max) return v;
                Console.WriteLine($"Ingresa un entero entre {min} y {max}.");
            }
        }

        private static bool ElegirHabitacionLibreConValidacion(out int numeroSeleccionado)
        {
            numeroSeleccionado = -1;

            if (!ConfigHabitaciones.HayLibres())
            {
                Console.WriteLine("No hay habitaciones libres.");
                return false;
            }

            while (true)
            {
                MostrarHabitaciones();
                Console.Write("Elige el número de habitación (0 = cancelar): ");
                if (!int.TryParse(Console.ReadLine(), out int num))
                {
                    Console.WriteLine("Entrada inválida.");
                    continue;
                }

                if (num == 0)
                    return false;

                if (!ConfigHabitaciones.ExisteHabitacion(num))
                {
                    Console.WriteLine("No existe esa habitación.");
                    continue;
                }

                if (!ConfigHabitaciones.EstaLibre(num))
                {
                    Console.WriteLine("Habitación ocupada. Elige otra.");
                    continue;
                }

                numeroSeleccionado = num;
                return true;
            }
        }

        private static void MostrarHabitaciones()
        {
            if (!ConfigHabitaciones.HayHabitaciones)
            {
                Console.WriteLine("No hay habitaciones configuradas.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("No.  | Tipo      | Precio       | Estado");
            Console.WriteLine("----------------------------------------");
            foreach (var h in ConfigHabitaciones.ObtenerHabitaciones())
            {
                Console.WriteLine($"{h.Numero,3}  | {h.Tipo,-9} | {h.Precio,10:C2} | {h.Estado}");
            }
            Console.WriteLine();
        }

        private static string Trunc(string s, int n) =>
            s.Length <= n ? s : s.Substring(0, n - 1) + "…";

        public static void AplicarRemapeoHabitaciones(Dictionary<int, int> mapa)
        {
            if (mapa == null || mapa.Count == 0) return;

            for (int i = 0; i < _clientes.Count; i++)
            {
                var c = _clientes[i];
                if (c.HabitacionNumero.HasValue && mapa.TryGetValue(c.HabitacionNumero.Value, out int nuevo))
                {
                    c.HabitacionNumero = nuevo;
                }
            }
            DataStore.SaveClientes(_clientes);
        }
    }
}
