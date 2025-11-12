using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using System;

namespace Facturar.Components.Data
{
    public class ServicioFactura
    {
        private List<FacturaItem> items = new List<FacturaItem>();
        private readonly String ruta = "mibase.db";

        public async Task<List<FacturaItem>> ObtenerItems()
        {
            items.Clear();
            using var conexion = new SqliteConnection($"Datasource={ruta}");
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = "SELECT Identificador, Producto, Cantidad, PrecioUnitario FROM FacturaItem";
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                items.Add(new FacturaItem
                {
                    Identificador = lector.GetInt32(0),
                    Producto = lector.GetString(1),
                    Cantidad = lector.GetInt32(2),
                    PrecioUnitario = lector.GetDecimal(3)
                });
            }
            return items;
        }

        public async Task AgregarItem(FacturaItem item)
        {
            using var conexion = new SqliteConnection($"Datasource={ruta}");
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = @"
                INSERT INTO FacturaItem (Identificador, Producto, Cantidad, PrecioUnitario) 
                VALUES(@IDENTIFICADOR, @PRODUCTO, @CANTIDAD, @PRECIO)";
            comando.Parameters.AddWithValue("@IDENTIFICADOR", item.Identificador);
            comando.Parameters.AddWithValue("@PRODUCTO", item.Producto);
            comando.Parameters.AddWithValue("@CANTIDAD", item.Cantidad);
            comando.Parameters.AddWithValue("@PRECIO", item.PrecioUnitario);
            await comando.ExecuteNonQueryAsync();
        }

        public async Task ActualizarItem(FacturaItem item)
        {
            using var conexion = new SqliteConnection($"Datasource={ruta}");
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = @"
                UPDATE FacturaItem 
                SET Producto = @PRODUCTO, 
                    Cantidad = @CANTIDAD, 
                    PrecioUnitario = @PRECIO
                WHERE Identificador = @IDENTIFICADOR";
            comando.Parameters.AddWithValue("@PRODUCTO", item.Producto);
            comando.Parameters.AddWithValue("@CANTIDAD", item.Cantidad);
            comando.Parameters.AddWithValue("@PRECIO", item.PrecioUnitario);
            comando.Parameters.AddWithValue("@IDENTIFICADOR", item.Identificador);
            await comando.ExecuteNonQueryAsync();
        }

        public async Task EliminarItem(int identificador)
        {
            using var conexion = new SqliteConnection($"Datasource={ruta}");
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = "DELETE FROM FacturaItem WHERE Identificador = @IDENTIFICADOR";
            comando.Parameters.AddWithValue("@IDENTIFICADOR", identificador);
            await comando.ExecuteNonQueryAsync();
        }

        public async Task<string> ObtenerValorConfig(string clave)
        {
            using var conexion = new SqliteConnection($"Datasource={ruta}");
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = "SELECT valor FROM configuracion WHERE clave = @CLAVE";
            comando.Parameters.AddWithValue("@CLAVE", clave);
            var resultado = await comando.ExecuteScalarAsync();
            return resultado?.ToString() ?? string.Empty;
        }

        public async Task GuardarValorConfig(string clave, string valor)
        {
            using var conexion = new SqliteConnection($"Datasource={ruta}");
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = @"
                INSERT OR REPLACE INTO configuracion (clave, valor)
                VALUES (@CLAVE, @VALOR)
            ";
            comando.Parameters.AddWithValue("@CLAVE", clave);
            comando.Parameters.AddWithValue("@VALOR", valor ?? string.Empty);
            await comando.ExecuteNonQueryAsync();
        }

        public async Task GuardarFacturaCompletaAsync(DateTime fechaFactura, string nombreUsuario, List<FacturaItem> itemsDraft)
        {
            using var conexion = new SqliteConnection($"Datasource={ruta}");
            await conexion.OpenAsync();
            using var transaccion = conexion.BeginTransaction();

            try
            {
                var total = itemsDraft.Sum(i => i.Subtotal);
                var fecha = fechaFactura.ToString("yyyy-MM-dd HH:mm:ss");

                var cmdFactura = conexion.CreateCommand();
                cmdFactura.Transaction = transaccion;
                cmdFactura.CommandText = @"
                    INSERT INTO Factura (NombreFactura, FechaCreacion, Total, NombreUsuario)
                    VALUES (@NOMBRE, @FECHA, @TOTAL, @USUARIO);
                    SELECT last_insert_rowid();
                ";
                cmdFactura.Parameters.AddWithValue("@NOMBRE", "TEMP_ID");
                cmdFactura.Parameters.AddWithValue("@FECHA", fecha);
                cmdFactura.Parameters.AddWithValue("@TOTAL", total);
                cmdFactura.Parameters.AddWithValue("@USUARIO", nombreUsuario);

                long nuevoFacturaID = (long)await cmdFactura.ExecuteScalarAsync();

                var cmdUpdateName = conexion.CreateCommand();
                cmdUpdateName.Transaction = transaccion;
                cmdUpdateName.CommandText = "UPDATE Factura SET NombreFactura = @NOMBRE WHERE FacturaID = @ID";
                cmdUpdateName.Parameters.AddWithValue("@NOMBRE", nuevoFacturaID.ToString());
                cmdUpdateName.Parameters.AddWithValue("@ID", nuevoFacturaID);
                await cmdUpdateName.ExecuteNonQueryAsync();

                foreach (var item in itemsDraft)
                {
                    var cmdItem = conexion.CreateCommand();
                    cmdItem.Transaction = transaccion;
                    cmdItem.CommandText = @"
                        INSERT INTO FacturaItemHistorico (FacturaID, Producto, Cantidad, PrecioUnitario)
                        VALUES (@FACTURA_ID, @PRODUCTO, @CANTIDAD, @PRECIO)
                    ";
                    cmdItem.Parameters.AddWithValue("@FACTURA_ID", nuevoFacturaID);
                    cmdItem.Parameters.AddWithValue("@PRODUCTO", item.Producto);
                    cmdItem.Parameters.AddWithValue("@CANTIDAD", item.Cantidad);
                    cmdItem.Parameters.AddWithValue("@PRECIO", item.PrecioUnitario);
                    await cmdItem.ExecuteNonQueryAsync();
                }

                var cmdLimpiar = conexion.CreateCommand();
                cmdLimpiar.Transaction = transaccion;
                cmdLimpiar.CommandText = "DELETE FROM FacturaItem";
                await cmdLimpiar.ExecuteNonQueryAsync();

                await transaccion.CommitAsync();
            }
            catch
            {
                await transaccion.RollbackAsync();
                throw;
            }
        }

        public async Task<List<Factura>> ObtenerFacturasGuardadasAsync()
        {
            var facturas = new List<Factura>();
            using var conexion = new SqliteConnection($"Datasource={ruta}");
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = "SELECT FacturaID, NombreFactura, FechaCreacion, Total, NombreUsuario FROM Factura ORDER BY FechaCreacion DESC";

            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                facturas.Add(new Factura
                {
                    FacturaID = lector.GetInt32(0),
                    NombreFactura = lector.GetString(1),
                    FechaCreacion = DateTime.Parse(lector.GetString(2)),
                    Total = lector.GetDecimal(3),
                    NombreUsuario = lector.GetString(4)
                });
            }
            return facturas;
        }

        public async Task<Factura> ObtenerDetalleFacturaAsync(int facturaID)
        {
            Factura factura = null;
            using var conexion = new SqliteConnection($"Datasource={ruta}");
            await conexion.OpenAsync();

            var cmdFactura = conexion.CreateCommand();
            cmdFactura.CommandText = "SELECT FacturaID, NombreFactura, FechaCreacion, Total, NombreUsuario FROM Factura WHERE FacturaID = @ID";
            cmdFactura.Parameters.AddWithValue("@ID", facturaID);

            using (var lectorF = await cmdFactura.ExecuteReaderAsync())
            {
                if (await lectorF.ReadAsync())
                {
                    factura = new Factura
                    {
                        FacturaID = lectorF.GetInt32(0),
                        NombreFactura = lectorF.GetString(1),
                        FechaCreacion = DateTime.Parse(lectorF.GetString(2)),
                        Total = lectorF.GetDecimal(3),
                        NombreUsuario = lectorF.GetString(4)
                    };
                }
            }

            if (factura == null) return null;

            var cmdItems = conexion.CreateCommand();
            cmdItems.CommandText = "SELECT Producto, Cantidad, PrecioUnitario FROM FacturaItemHistorico WHERE FacturaID = @ID";
            cmdItems.Parameters.AddWithValue("@ID", facturaID);

            using (var lectorI = await cmdItems.ExecuteReaderAsync())
            {
                while (await lectorI.ReadAsync())
                {
                    factura.Items.Add(new FacturaItem
                    {
                        Producto = lectorI.GetString(0),
                        Cantidad = lectorI.GetInt32(1),
                        PrecioUnitario = lectorI.GetDecimal(2)
                    });
                }
            }
            return factura;
        }

        public async Task RecargarFacturaEnBorradorAsync(int facturaID)
        {
            using var conexion = new SqliteConnection($"Datasource={ruta}");
            await conexion.OpenAsync();
            using var transaccion = conexion.BeginTransaction();

            try
            {
                var cmdLimpiar = conexion.CreateCommand();
                cmdLimpiar.Transaction = transaccion;
                cmdLimpiar.CommandText = "DELETE FROM FacturaItem";
                await cmdLimpiar.ExecuteNonQueryAsync();

                var cmdObtener = conexion.CreateCommand();
                cmdObtener.Transaction = transaccion;
                cmdObtener.CommandText = "SELECT Producto, Cantidad, PrecioUnitario FROM FacturaItemHistorico WHERE FacturaID = @ID";
                cmdObtener.Parameters.AddWithValue("@ID", facturaID);

                var itemsHistoricos = new List<FacturaItem>();
                using (var lector = await cmdObtener.ExecuteReaderAsync())
                {
                    while (await lector.ReadAsync())
                    {
                        itemsHistoricos.Add(new FacturaItem
                        {
                            Producto = lector.GetString(0),
                            Cantidad = lector.GetInt32(1),
                            PrecioUnitario = lector.GetDecimal(2)
                        });
                    }
                }

                int proximoId = 1;
                foreach (var item in itemsHistoricos)
                {
                    var cmdInsertar = conexion.CreateCommand();
                    cmdInsertar.Transaction = transaccion;
                    cmdInsertar.CommandText = @"
                        INSERT INTO FacturaItem (Identificador, Producto, Cantidad, PrecioUnitario) 
                        VALUES(@IDENTIFICADOR, @PRODUCTO, @CANTIDAD, @PRECIO)";

                    cmdInsertar.Parameters.AddWithValue("@IDENTIFICADOR", proximoId++);
                    cmdInsertar.Parameters.AddWithValue("@PRODUCTO", item.Producto);
                    cmdInsertar.Parameters.AddWithValue("@CANTIDAD", item.Cantidad);
                    cmdInsertar.Parameters.AddWithValue("@PRECIO", item.PrecioUnitario);
                    await cmdInsertar.ExecuteNonQueryAsync();
                }

                await transaccion.CommitAsync();
            }
            catch
            {
                await transaccion.RollbackAsync();
                throw;
            }
        }

        public async Task ActualizarFacturaCompletaAsync(int facturaID, DateTime fechaFactura, string nombreUsuario, List<FacturaItem> itemsDraft)
        {
            using var conexion = new SqliteConnection($"Datasource={ruta}");
            await conexion.OpenAsync();

            using var transaccion = conexion.BeginTransaction();
            try
            {
                var total = itemsDraft.Sum(i => i.Subtotal);
                var fecha = fechaFactura.ToString("yyyy-MM-dd HH:mm:ss");

                var cmdFactura = conexion.CreateCommand();
                cmdFactura.Transaction = transaccion;
                cmdFactura.CommandText = @"
                    UPDATE Factura 
                    SET FechaCreacion = @FECHA, 
                        Total = @TOTAL,
                        NombreUsuario = @USUARIO
                    WHERE FacturaID = @ID";
                cmdFactura.Parameters.AddWithValue("@FECHA", fecha);
                cmdFactura.Parameters.AddWithValue("@TOTAL", total);
                cmdFactura.Parameters.AddWithValue("@USUARIO", nombreUsuario);
                cmdFactura.Parameters.AddWithValue("@ID", facturaID);
                await cmdFactura.ExecuteNonQueryAsync();

                var cmdBorrarItems = conexion.CreateCommand();
                cmdBorrarItems.Transaction = transaccion;
                cmdBorrarItems.CommandText = "DELETE FROM FacturaItemHistorico WHERE FacturaID = @ID";
                cmdBorrarItems.Parameters.AddWithValue("@ID", facturaID);
                await cmdBorrarItems.ExecuteNonQueryAsync();

                foreach (var item in itemsDraft)
                {
                    var cmdItem = conexion.CreateCommand();
                    cmdItem.Transaction = transaccion;
                    cmdItem.CommandText = @"
                        INSERT INTO FacturaItemHistorico (FacturaID, Producto, Cantidad, PrecioUnitario)
                        VALUES (@FACTURA_ID, @PRODUCTO, @CANTIDAD, @PRECIO)
                    ";
                    cmdItem.Parameters.AddWithValue("@FACTURA_ID", facturaID);
                    cmdItem.Parameters.AddWithValue("@PRODUCTO", item.Producto);
                    cmdItem.Parameters.AddWithValue("@CANTIDAD", item.Cantidad);
                    cmdItem.Parameters.AddWithValue("@PRECIO", item.PrecioUnitario);
                    await cmdItem.ExecuteNonQueryAsync();
                }

                var cmdLimpiarBorrador = conexion.CreateCommand();
                cmdLimpiarBorrador.Transaction = transaccion;
                cmdLimpiarBorrador.CommandText = "DELETE FROM FacturaItem";
                await cmdLimpiarBorrador.ExecuteNonQueryAsync();

                await transaccion.CommitAsync();
            }
            catch
            {
                await transaccion.RollbackAsync();
                throw;
            }
        }

        public async Task EliminarFacturaGuardadaAsync(int facturaID)
        {
            using var conexion = new SqliteConnection($"Datasource={ruta}");
            await conexion.OpenAsync();
            using var transaccion = conexion.BeginTransaction();

            try
            {
                var cmdItems = conexion.CreateCommand();
                cmdItems.Transaction = transaccion;
                cmdItems.CommandText = "DELETE FROM FacturaItemHistorico WHERE FacturaID = @ID";
                cmdItems.Parameters.AddWithValue("@ID", facturaID);
                await cmdItems.ExecuteNonQueryAsync();

                var cmdFactura = conexion.CreateCommand();
                cmdFactura.Transaction = transaccion;
                cmdFactura.CommandText = "DELETE FROM Factura WHERE FacturaID = @ID";
                cmdFactura.Parameters.AddWithValue("@ID", facturaID);
                await cmdFactura.ExecuteNonQueryAsync();

                await transaccion.CommitAsync();
            }
            catch
            {
                await transaccion.RollbackAsync();
                throw;
            }
        }

        public async Task LimpiarBorradorCompletoAsync()
        {
            using var conexion = new SqliteConnection($"Datasource={ruta}");
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = "DELETE FROM FacturaItem";
            await comando.ExecuteNonQueryAsync();
        }

        public async Task<List<Factura>> ObtenerFacturasPorAnioAsync(int anio)
        {
            var facturas = new List<Factura>();
            using var conexion = new SqliteConnection($"Datasource={ruta}");
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = @"
                SELECT FacturaID, NombreFactura, FechaCreacion, Total, NombreUsuario
                FROM Factura 
                WHERE strftime('%Y', FechaCreacion) = @ANIO
                ORDER BY FechaCreacion";
            comando.Parameters.AddWithValue("@ANIO", anio.ToString());

            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                facturas.Add(new Factura
                {
                    FacturaID = lector.GetInt32(0),
                    NombreFactura = lector.GetString(1),
                    FechaCreacion = DateTime.Parse(lector.GetString(2)),
                    Total = lector.GetDecimal(3),
                    NombreUsuario = lector.GetString(4)
                });
            }
            return facturas;
        }
    }
}