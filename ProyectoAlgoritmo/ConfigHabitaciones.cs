using System;
using System.Globalization;
using System.Collections.Generic;

namespace HotelReservas
{
    public enum TipoHabitacion { Sencilla = 1, Doble = 2, Suite = 3 }
    public enum Estado { Libre = 1, Ocupada = 2 }

    public struct Habitacion
    {
        public int Numero;
        public TipoHabitacion Tipo;
        public decimal Precio;
        public Estado Estado;
    }

    public static class ConfigHabitaciones
    {
        private static Habitacion[] _habitaciones = Array.Empty<Habitacion>();
        private static bool _inicializado => _habitaciones.Length > 0;

        public static bool HayHabitaciones => _habitaciones.Length > 0;
        public static Habitacion[] ObtenerHabitaciones() => _habitaciones;
        public static void CargarDesdePersistencia(Habitacion[] rooms) => _habitaciones = rooms ?? Array.Empty<Habitacion>();

        public static bool ExisteHabitacion(int numero) =>
            numero >= 1 && numero <= _habitaciones.Length;

        public static bool EstaLibre(int numero)
        {
            if (!ExisteHabitacion(numero)) return false;
            return _habitaciones[numero - 1].Estado == Estado.Libre;
        }

        public static bool Ocupar(int numero)
        {
            if (!ExisteHabitacion(numero)) return false;
            int i = numero - 1;
            if (_habitaciones[i].Estado == Estado.Ocupada) return false;
            var h = _habitaciones[i];
            h.Estado = Estado.Ocupada;
            _habitaciones[i] = h;
            DataStore.SaveHabitaciones(_habitaciones);
            return true;
        }

        public static bool Liberar(int numero)
        {
            if (!ExisteHabitacion(numero)) return false;
            int i = numero - 1;
            var h = _habitaciones[i];
            h.Estado = Estado.Libre;
            _habitaciones[i] = h;
            DataStore.SaveHabitaciones(_habitaciones);
            return true;
        }

        public static decimal PrecioPorNoche(int numero)
        {
            if (!ExisteHabitacion(numero)) return 0m;
            return _habitaciones[numero - 1].Precio;
        }

        public static bool HayLibres()
        {
            foreach (var h in _habitaciones)
                if (h.Estado == Estado.Libre) return true;
            return false;
        }

        public static void MostrarMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine(" === Configuracion de Habitaciones === ");
                Console.WriteLine("1) Registrar habitaciones");
                Console.WriteLine("2) Listado de habitaciones (solo consulta)");
                Console.WriteLine("3) Editar Informacion de habitación (tipo / precio)");
                Console.WriteLine("0) Volver");
                Console.Write("Selecciona una opción: ");

                var op = Console.ReadLine()?.Trim();
                switch (op)
                {
                    case "1":
                        CrearHabitaciones();
                        break;
                    case "2":
                        ListarHabitaciones(pausar: true);
                        break;
                    case "3":
                        EditarHabitacion();
                        break;
                    case "0":
                        return;
                    default:
                        Program.Pausa("Opción inválida.");
                        break;
                }
            }
        }

        private static void CrearHabitaciones()
        {
            Console.Clear();
            Console.WriteLine(" === Opciones para habitación === ");

            bool agregar = false;
            if (_inicializado)
            {
                Console.WriteLine("Ya existen habitaciones.");
                Console.WriteLine("1) Conservar y AGREGAR más");
                Console.WriteLine("2) REEMPLAZAR todo el listado (se conservarán las ocupadas)");
                Console.WriteLine("0) Cancelar y volver");
                Console.Write("Elige una opción: ");
                var resp = Console.ReadLine()?.Trim();

                if (resp == "0")
                {
                    Program.Pausa("Operación cancelada.");
                    return;
                }
                else if (resp == "1")
                {
                    agregar = true;
                }
                else if (resp == "2")
                {
                    agregar = false;
                }
                else
                {
                    Program.Pausa("Opción inválida. Operación cancelada.");
                    return;
                }
            }

            int cantidad = LeerEnteroPositivo(agregar
                ? "¿Cuántas habitaciones ADICIONALES deseas registrar? "
                : "¿Cuántas habitaciones deseas registrar? ");

            if (agregar)
            {
                int old = _habitaciones.Length;
                var nuevas = new Habitacion[old + cantidad];
                Array.Copy(_habitaciones, nuevas, old);

                Console.WriteLine();
                Console.WriteLine("Tipos: 1) Sencilla  2) Doble  3) Suite");
                Console.WriteLine();

                for (int i = old; i < nuevas.Length; i++)
                {
                    Console.WriteLine($"--- Habitación #{i + 1} ---");
                    var tipo = LeerTipoHabitacion("Tipo (1-3): ");
                    var precio = LeerDecimalPositivo("Precio (ej. 1200.50): ");

                    nuevas[i] = new Habitacion
                    {
                        Numero = i + 1,
                        Tipo = tipo,
                        Precio = precio,
                        Estado = Estado.Libre
                    };
                    Console.WriteLine();
                }

                _habitaciones = nuevas;
                DataStore.SaveHabitaciones(_habitaciones);
                Program.Pausa("Habitaciones agregadas correctamente.");
                return;
            }

            var ocupadas = new List<Habitacion>();
            var mapOldToNew = new Dictionary<int, int>();

            if (_inicializado)
            {
                foreach (var h in _habitaciones)
                    if (h.Estado == Estado.Ocupada)
                        ocupadas.Add(h);

                ocupadas.Sort((a, b) => a.Numero.CompareTo(b.Numero));
            }

            int tot = ocupadas.Count + cantidad;
            var resultado = new Habitacion[tot];

            for (int i = 0; i < ocupadas.Count; i++)
            {
                var h = ocupadas[i];
                int nuevoNumero = i + 1;
                mapOldToNew[h.Numero] = nuevoNumero;
                h.Numero = nuevoNumero;
                resultado[i] = h;
            }

            Console.WriteLine();
            Console.WriteLine("Tipos: 1) Sencilla  2) Doble  3) Suite");
            Console.WriteLine();

            for (int i = ocupadas.Count; i < tot; i++)
            {
                Console.WriteLine($"--- Habitación #{i + 1} ---");
                var tipo = LeerTipoHabitacion("Tipo (1-3): ");
                var precio = LeerDecimalPositivo("Precio (ej. 1200.50): ");

                resultado[i] = new Habitacion
                {
                    Numero = i + 1,
                    Tipo = tipo,
                    Precio = precio,
                    Estado = Estado.Libre
                };
                Console.WriteLine();
            }

            _habitaciones = resultado;

            if (mapOldToNew.Count > 0)
                Clientes.AplicarRemapeoHabitaciones(mapOldToNew);

            DataStore.SaveHabitaciones(_habitaciones);
            DataStore.SaveClientes(Clientes.ObtenerClientesParaGuardar());

            Program.Pausa("Proceso completado (ocupadas conservadas y nuevas libres).");
        }

        private static void EditarHabitacion()
        {
            if (!_inicializado)
            {
                Program.Pausa("Aún no hay habitaciones para editar.");
                return;
            }

            Console.Clear();
            Console.WriteLine("=== EDITAR HABITACIÓN ===");
            ListarHabitaciones(pausar: false);

            Console.Write("Ingresa el número de la habitación a editar (0 = cancelar): ");
            if (!int.TryParse(Console.ReadLine(), out int num) || num < 0 || num > _habitaciones.Length)
            {
                Program.Pausa("Entrada inválida.");
                return;
            }
            if (num == 0) return;

            int idx = num - 1;
            var h = _habitaciones[idx];

            if (h.Estado == Estado.Ocupada)
            {
                Program.Pausa("La habitación está OCUPADA y no puede ser modificada.");
                return;
            }

            Console.WriteLine($"Editando Habitación #{h.Numero} (Tipo actual: {h.Tipo}, Precio actual: {h.Precio:C2})");
            Console.WriteLine("Deja vacío para conservar el valor actual.");

            Console.Write("Nuevo tipo (1=Sencilla, 2=Doble, 3=Suite): ");
            var tipoTxt = Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(tipoTxt) && (tipoTxt == "1" || tipoTxt == "2" || tipoTxt == "3"))
            {
                h.Tipo = (TipoHabitacion)int.Parse(tipoTxt);
            }

            Console.Write("Nuevo precio por noche: ");
            var precioTxt = Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(precioTxt))
            {
                if (decimal.TryParse(precioTxt, NumberStyles.Number, CultureInfo.CurrentCulture, out var v1) && v1 >= 0)
                    h.Precio = v1;
                else if (decimal.TryParse(precioTxt, NumberStyles.Number, CultureInfo.InvariantCulture, out var v2) && v2 >= 0)
                    h.Precio = v2;
                else
                {
                    Program.Pausa("Precio inválido. No se cambió el precio.");
                }
            }

            _habitaciones[idx] = h;
            DataStore.SaveHabitaciones(_habitaciones);
            Program.Pausa("Habitación actualizada.");
        }

        private static void ListarHabitaciones(bool pausar)
        {
            Console.WriteLine("No.  | Tipo      | Precionoche       | Estado");
            Console.WriteLine("----------------------------------------------");
            foreach (var h in _habitaciones)
            {
                Console.WriteLine($"{h.Numero,3}  | {h.Tipo,-9} | {h.Precio,10:C2} | {h.Estado}");
            }
            Console.WriteLine();
            if (pausar) Program.Pausa();
        }

        private static int LeerEnteroPositivo(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                var txt = Console.ReadLine();
                if (int.TryParse(txt, out int v) && v > 0)
                    return v;
                Console.WriteLine("Valor inválido. Ingresa un entero positivo.");
            }
        }

        private static decimal LeerDecimalPositivo(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                var txt = Console.ReadLine();

                if (decimal.TryParse(txt, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal v) && v >= 0)
                    return v;
                if (decimal.TryParse(txt, NumberStyles.Number, CultureInfo.InvariantCulture, out v) && v >= 0)
                    return v;

                Console.WriteLine("Valor inválido. Ingresa un número decimal (ej. 1200.50).");
            }
        }

        private static TipoHabitacion LeerTipoHabitacion(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                var txt = Console.ReadLine();
                if (txt is "1" or "2" or "3")
                    return (TipoHabitacion)int.Parse(txt!);

                Console.WriteLine("Opción inválida. Usa 1, 2 o 3.");
            }
        }
    }
}
