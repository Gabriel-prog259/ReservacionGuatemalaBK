using System;

namespace HotelReservas
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            DataStore.Init();

            // 👇 NUEVO: cargar datos existentes desde los TXT al iniciar
            DataStore.LoadHabitacionesIntoConfig();
            DataStore.LoadClientesIntoModule();

            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== SISTEMA DE RESERVAS GUATEMALA ===");
                Console.WriteLine("1) Habitaciones");
                Console.WriteLine("2) Clientes");
                Console.WriteLine("3) Administración");
                Console.WriteLine("0) Salir");
                Console.Write("Selecciona una opción: ");
                var op = Console.ReadLine()?.Trim();

                switch (op)
                {
                    case "1":
                        ConfigHabitaciones.MostrarMenu();
                        break;
                    case "2":
                        Clientes.MostrarMenu();
                        break;
                    case "3":
                        Administracion.LoginYMenu();
                        break;
                    case "0":
                        Console.WriteLine("¡Hasta luego!");
                        return;
                    default:
                        Pausa("Opción inválida.");
                        break;
                }
            }
        }

        public static void Pausa(string? mensaje = null)
        {
            if (!string.IsNullOrWhiteSpace(mensaje))
                Console.WriteLine(mensaje);
            Console.WriteLine("Presiona una tecla para continuar...");
            Console.ReadKey(true);
        }
    }
}
