using Facturar.Components.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Facturar.Components.Servicio
{
    public class ControladorFacturacion
    {
        private readonly ServicioFactura _servicioFactura;

        public FacturaItem DraftItem { get; set; } = new FacturaItem { Cantidad = 1, PrecioUnitario = 0.01m };

        public int? FacturaIDEnModificacion { get; set; } = null;
        public string NombreFacturaEnModificacion { get; set; } = string.Empty;

        public string NombreFacturaEnBorrador { get; set; } = string.Empty;

        public ControladorFacturacion(ServicioFactura servicioFacturacion)
        {
            _servicioFactura = servicioFacturacion;
        }

        public async Task CargarEstadoBorradorAsync()
        {
            DraftItem.Producto = await _servicioFactura.ObtenerValorConfig("DraftProducto");
            int.TryParse(await _servicioFactura.ObtenerValorConfig("DraftCantidad"), out int cantidad);
            DraftItem.Cantidad = cantidad > 0 ? cantidad : 1;
            decimal.TryParse(await _servicioFactura.ObtenerValorConfig("DraftPrecioUnitario"), out decimal precio);
            DraftItem.PrecioUnitario = precio > 0 ? precio : 0.01m;

            NombreFacturaEnBorrador = await _servicioFactura.ObtenerValorConfig("DraftNombreFactura");

            FacturaIDEnModificacion = int.TryParse(await _servicioFactura.ObtenerValorConfig("DraftModifyingID"), out int id) && id != 0 ? id : null;
            NombreFacturaEnModificacion = await _servicioFactura.ObtenerValorConfig("DraftModifyingName");
        }

        public async Task GuardarDraftItemAsync()
        {
            await _servicioFactura.GuardarValorConfig("DraftProducto", DraftItem.Producto ?? string.Empty);
            await _servicioFactura.GuardarValorConfig("DraftCantidad", DraftItem.Cantidad.ToString());
            await _servicioFactura.GuardarValorConfig("DraftPrecioUnitario", DraftItem.PrecioUnitario.ToString());
        }

        public async Task GuardarNombreFacturaEnBorradorAsync()
        {
            await _servicioFactura.GuardarValorConfig("DraftNombreFactura", NombreFacturaEnBorrador);
        }

        public async Task GuardarEstadoModificacionAsync()
        {
            await _servicioFactura.GuardarValorConfig("DraftModifyingID", FacturaIDEnModificacion?.ToString() ?? "0");
            await _servicioFactura.GuardarValorConfig("DraftModifyingName", NombreFacturaEnModificacion);
        }

        public async Task GuardarCambiosDraftItemAsync()
        {
            if (DraftItem.Identificador == 0)
            {
                var itemsActuales = await _servicioFactura.ObtenerItems();
                var itemExistente = itemsActuales.FirstOrDefault(i =>
                    i.Producto.Equals(DraftItem.Producto, StringComparison.OrdinalIgnoreCase));

                if (itemExistente != null)
                {
                    itemExistente.Cantidad += DraftItem.Cantidad;
                    itemExistente.PrecioUnitario = DraftItem.PrecioUnitario;
                    await _servicioFactura.ActualizarItem(itemExistente);
                }
                else
                {
                    DraftItem.Identificador = await GenerarNuevoID();
                    await _servicioFactura.AgregarItem(DraftItem);
                }
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

        private async Task LimpiarConfigBorradorAsync()
        {
            DraftItem = new FacturaItem { Cantidad = 1, PrecioUnitario = 0.01m };
            await GuardarDraftItemAsync();

            NombreFacturaEnBorrador = "";
            await GuardarNombreFacturaEnBorradorAsync();

            FacturaIDEnModificacion = null;
            NombreFacturaEnModificacion = "";
            await GuardarEstadoModificacionAsync();
        }

        public async Task GuardarFacturaActualAsync(string nombreFactura, List<FacturaItem> itemsDraft)
        {
            if (string.IsNullOrWhiteSpace(nombreFactura))
            {
                throw new System.Exception("El nombre de la factura es obligatorio.");
            }
            if (!itemsDraft.Any())
            {
                throw new System.Exception("No se puede guardar una factura vacía.");
            }

            await _servicioFactura.GuardarFacturaCompletaAsync(nombreFactura, itemsDraft);
            await LimpiarConfigBorradorAsync();
        }

        public async Task ActualizarFacturaGuardadaAsync(int facturaID, string nombreFactura, List<FacturaItem> itemsDraft)
        {
            if (string.IsNullOrWhiteSpace(nombreFactura))
            {
                throw new System.Exception("El nombre de la factura es obligatorio.");
            }
            if (!itemsDraft.Any())
            {
                throw new System.Exception("No se puede guardar una factura vacía.");
            }

            await _servicioFactura.ActualizarFacturaCompletaAsync(facturaID, nombreFactura, itemsDraft);
            await LimpiarConfigBorradorAsync();
        }

        public async Task<List<Factura>> ObtenerFacturasGuardadasAsync()
        {
            return await _servicioFactura.ObtenerFacturasGuardadasAsync();
        }

        public async Task<Factura> ObtenerDetalleFacturaAsync(int facturaID)
        {
            return await _servicioFactura.ObtenerDetalleFacturaAsync(facturaID);
        }

        public async Task RecargarFacturaEnBorradorAsync(int facturaID)
        {
            await _servicioFactura.RecargarFacturaEnBorradorAsync(facturaID);
        }

        public async Task EliminarFacturaGuardadaAsync(int facturaID)
        {
            await _servicioFactura.EliminarFacturaGuardadaAsync(facturaID);
        }

        public async Task LimpiarEstadoBorradorAsync()
        {
            await _servicioFactura.LimpiarBorradorCompletoAsync();
            await LimpiarConfigBorradorAsync();
        }
    }
}