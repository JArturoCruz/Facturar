using Facturar.Components.Data;
using Facturar.Components.Servicio;
using Facturar.Components;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<ControladorFacturacion>();
builder.Services.AddSingleton<ServicioFactura>();

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


String ruta = "mibase.db";
using var conexion = new SqliteConnection($"DataSource={ruta}");
conexion.Open();
var comando = conexion.CreateCommand();

comando.CommandText = @"
    -- Tabla de borrador 
    CREATE TABLE IF NOT EXISTS
    FacturaItem(
        Identificador INTEGER,
        Producto TEXT,
        Cantidad INTEGER,
        PrecioUnitario REAL 
    );

    -- Tabla de configuración 
    CREATE TABLE IF NOT EXISTS
    configuracion( clave TEXT PRIMARY KEY, valor TEXT);

    -- NUEVA: Tabla para guardar las facturas
    CREATE TABLE IF NOT EXISTS
    Factura(
        FacturaID INTEGER PRIMARY KEY AUTOINCREMENT,
        NombreFactura TEXT NOT NULL,
        FechaCreacion TEXT NOT NULL,
        Total REAL NOT NULL
    );

    --Tabla para guardar los items de las facturas guardadas
    CREATE TABLE IF NOT EXISTS
    FacturaItemHistorico(
        ItemID INTEGER PRIMARY KEY AUTOINCREMENT,
        FacturaID INTEGER NOT NULL,
        Producto TEXT,
        Cantidad INTEGER,
        PrecioUnitario REAL,
        FOREIGN KEY(FacturaID) REFERENCES Factura(FacturaID)
    );

    -- Valores iniciales para el borrador del formulario
    INSERT OR IGNORE INTO configuracion (clave, valor) VALUES ('DraftProducto', '');
    INSERT OR IGNORE INTO configuracion (clave, valor) VALUES ('DraftCantidad', '1');
    INSERT OR IGNORE INTO configuracion (clave, valor) VALUES ('DraftPrecioUnitario', '0.01');
";
comando.ExecuteNonQuery();
conexion.Close();

app.Run();