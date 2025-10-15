SISTEMA DE RESERVAS GUATEMALA
================================

Proyecto en C# (.NET Console App) para la gestión de habitaciones, clientes y facturación
de un pequeño hotel o sistema de hospedaje. El sistema almacena la información en archivos
.txt para mantener los datos entre compilaciones y ejecuciones.



------------------------------------------------------------
FUNCIONALIDADES PRINCIPALES
------------------------------------------------------------

GESTIÓN DE HABITACIONES
- Crear nuevas habitaciones con tipo, precio, estado y descripción.
- Editar información de habitaciones existentes.
- Marcar habitaciones como ocupadas, libres o en mantenimiento.
- Consultar listado general de habitaciones.
- Datos almacenados en Habitaciones.txt.

GESTIÓN DE CLIENTES
- Registrar nuevos clientes con nombre, DPI, teléfono y dirección.
- Editar o eliminar clientes existentes.
- Consultar listado de todos los clientes.
- Datos almacenados en Clientes.txt.

ADMINISTRACIÓN
- Login administrativo (con usuario y contraseña).
- Generación de facturas por habitación.
- Extensión de reservas con actualización automática de factura.
- Búsqueda de facturas por habitación.
- Integración con módulos de habitaciones y clientes.



------------------------------------------------------------
PERSISTENCIA DE DATOS
------------------------------------------------------------

El sistema guarda automáticamente los datos en archivos de texto (.txt) dentro del directorio raíz del proyecto:

- Habitaciones.txt → información de todas las habitaciones.
- Clientes.txt → información de todos los clientes.

Estos archivos se actualizan cada vez que se:
- Agrega, modifica o elimina una habitación o cliente.
- Genera o actualiza una factura.

CARGA AUTOMÁTICA AL INICIAR
Desde la última actualización, ya no es necesario volver a crear los registros cada vez que se recompila el proyecto.
Al iniciar el programa, se ejecutan automáticamente:

    DataStore.LoadHabitacionesIntoConfig();
    DataStore.LoadClientesIntoModule();

Esto carga a memoria toda la información existente en los archivos .txt para su uso inmediato.



------------------------------------------------------------
ESTRUCTURA DEL PROYECTO
------------------------------------------------------------

HotelReservas/
├── Program.cs              # Punto de entrada principal
├── DataStore.cs            # Manejo de persistencia (lectura/escritura de archivos)
├── ConfigHabitaciones.cs   # Gestión de habitaciones
├── Clientes.cs             # Gestión de clientes
├── Administracion.cs       # Módulo de login y facturación
├── Habitaciones.txt        # (Generado automáticamente)
├── Clientes.txt            # (Generado automáticamente)
└── bin/ / obj/             # Archivos de compilación (generados por .NET)



------------------------------------------------------------
FLUJO GENERAL DEL SISTEMA
------------------------------------------------------------

1. Al ejecutar el programa:
   - Se inicializa DataStore.
   - Se cargan los datos de habitaciones y clientes desde los .txt.
2. Se muestra el menú principal:

   === SISTEMA DE RESERVAS GUATEMALA ===
   1) Habitaciones
   2) Clientes
   3) Administración
   0) Salir

3. El usuario navega entre módulos y realiza operaciones.
4. Todos los cambios se guardan automáticamente en los archivos de texto.



------------------------------------------------------------
REQUISITOS
------------------------------------------------------------

- .NET 6.0 o superior
- Sistema operativo Windows, macOS o Linux



------------------------------------------------------------
EJECUCIÓN
------------------------------------------------------------

1. Clonar el proyecto o copiar los archivos fuente.
2. Compilar el proyecto con:
   dotnet build
3. Ejecutar el programa con:
   dotnet run



------------------------------------------------------------
NOTAS TÉCNICAS
------------------------------------------------------------

- Los archivos .txt se crean automáticamente si no existen.
- Se usa DataStore como clase central para la persistencia y carga de datos.
- No se requiere base de datos: todo se maneja mediante lectura/escritura de texto.
- La estructura es modular: cada archivo gestiona una parte independiente del sistema.
