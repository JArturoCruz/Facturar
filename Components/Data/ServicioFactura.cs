using Factura.Components.Data;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Factura.Components.Data
{
    public class ServicioFacturacion
    {
        private List<FacturaItem> items = new List<FacturaItem>();
        private readonly String ruta = "mibase.db";

        public async Task<List<FacturaItem>> ObtenerItems()
        {
            if (!items.Any())
            {
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

            items.Add(item);
        }

        public async Task EliminarItem(int identificador)
        {
            using var conexion = new SqliteConnection($"Datasource={ruta}");
            await conexion.OpenAsync();

            var comando = conexion.CreateCommand();
            comando.CommandText = "DELETE FROM FacturaItem WHERE Identificador = @IDENTIFICADOR";
            comando.Parameters.AddWithValue("@IDENTIFICADOR", identificador);

            await comando.ExecuteNonQueryAsync();

            items.RemoveAll(i => i.Identificador == identificador);
        }
    }
}