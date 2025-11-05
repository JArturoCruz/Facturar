using Factura.Components;
// Importamos los nuevos namespaces
using Factura.Components.Data;
using Factura.Components.Servicio;
using Facturar.Components;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Registramos los nuevos servicios como en tu proyecto original
builder.Services.AddSingleton<ControladorFacturacion>();
builder.Services.AddSingleton<ServicioFacturacion>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// --- LÓGICA DE LA BASE DE DATOS ---
String ruta = "mibase.db";
using var conexion = new SqliteConnection($"DataSource={ruta}");
conexion.Open();
var comando = conexion.CreateCommand();

comando.CommandText = @"
    -- Mantenemos tu tabla original por si la necesitas
    CREATE TABLE IF NOT EXISTS
    juego( identificador integer, nombre text, jugado integer);

    -- Mantenemos tu tabla de configuración
    CREATE TABLE IF NOT EXISTS
    configuracion( clave TEXT PRIMARY KEY, valor TEXT);

    -- --- TABLA NUEVA PARA FACTURAS ---
    CREATE TABLE IF NOT EXISTS
    FacturaItem(
        Identificador INTEGER,
        Producto TEXT,
        Cantidad INTEGER,
        PrecioUnitario REAL 
    );
";
comando.ExecuteNonQuery();
conexion.Close();

app.Run();