using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    }
}