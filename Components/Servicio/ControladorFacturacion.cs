using Facturar.Components.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Facturar.Components.Servicio
{
    public class ControladorFacturacion
    {
        private readonly ServicioFactura _servicioFactura;

        public FacturaItem DraftItem { get; set; } = new FacturaItem { Cantidad = 1, PrecioUnitario = 0.01m };

        public ControladorFacturacion(ServicioFactura servicioFacturacion)
        {
            _servicioFactura = servicioFacturacion;
        }

        public async Task CargarDraftItemAsync()
        {
            DraftItem.Producto = await _servicioFactura.ObtenerValorConfig("DraftProducto");

            int.TryParse(await _servicioFactura.ObtenerValorConfig("DraftCantidad"), out int cantidad);
            DraftItem.Cantidad = cantidad > 0 ? cantidad : 1;

            decimal.TryParse(await _servicioFactura.ObtenerValorConfig("DraftPrecioUnitario"), out decimal precio);
            DraftItem.PrecioUnitario = precio > 0 ? precio : 0.01m;
        }

        public async Task GuardarDraftItemAsync()
        {
            await _servicioFactura.GuardarValorConfig("DraftProducto", DraftItem.Producto ?? string.Empty);
            await _servicioFactura.GuardarValorConfig("DraftCantidad", DraftItem.Cantidad.ToString());
            await _servicioFactura.GuardarValorConfig("DraftPrecioUnitario", DraftItem.PrecioUnitario.ToString());
        }

        public async Task GuardarCambiosDraftItemAsync()
        {
            if (DraftItem.Identificador == 0)
            {
                DraftItem.Identificador = await GenerarNuevoID();
                await _servicioFactura.AgregarItem(DraftItem);
            }
            else
            {
                await _servicioFactura.ActualizarItem(DraftItem);
            }

            DraftItem = new FacturaItem { Cantidad = 1, PrecioUnitario = 0.01m };
            await GuardarDraftItemAsync();
        }

        public void CargarItemParaEdicion(FacturaItem item)
        {
            DraftItem = new FacturaItem
            {
                Identificador = item.Identificador,
                Producto = item.Producto,
                Cantidad = item.Cantidad,
                PrecioUnitario = item.PrecioUnitario
            };
        }

        public async Task CancelarEdicionAsync()
        {
            DraftItem = new FacturaItem { Cantidad = 1, PrecioUnitario = 0.01m };
            await GuardarDraftItemAsync();
        }

        public async Task<List<FacturaItem>> ObtenerItems()
        {
            return await _servicioFactura.ObtenerItems();
        }

        public async Task EliminarItem(int identificador)
        {
            await _servicioFactura.EliminarItem(identificador);
        }

        private async Task<int> GenerarNuevoID()
        {
            var items = await this.ObtenerItems();
            return items.Any() ? items.Max(i => i.Identificador) + 1 : 1;
        }
    }
}