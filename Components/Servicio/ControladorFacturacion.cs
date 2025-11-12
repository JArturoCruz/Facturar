using Facturar.Components.Data;
using Facturar.Components.Data.Reportes;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Globalization;

namespace Facturar.Components.Servicio
{
    public class ControladorFacturacion
    {
        private readonly ServicioFactura _servicioFactura;

        public FacturaItem DraftItem { get; set; } = new FacturaItem { Cantidad = 1, PrecioUnitario = 0.01m };

        public int? FacturaIDEnModificacion { get; set; } = null;
        public DateTime FechaFacturaEnBorrador { get; set; } = DateTime.Today;

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

            FacturaIDEnModificacion = int.TryParse(await _servicioFactura.ObtenerValorConfig("DraftModifyingID"), out int id) && id != 0 ? id : null;

            var fechaGuardada = await _servicioFactura.ObtenerValorConfig("DraftFechaFactura");
            if (DateTime.TryParse(fechaGuardada, out DateTime fecha))
            {
                FechaFacturaEnBorrador = fecha;
            }
            else
            {
                FechaFacturaEnBorrador = DateTime.Today;
            }
        }

        public async Task GuardarDraftItemAsync()
        {
            await _servicioFactura.GuardarValorConfig("DraftProducto", DraftItem.Producto ?? string.Empty);
            await _servicioFactura.GuardarValorConfig("DraftCantidad", DraftItem.Cantidad.ToString());
            await _servicioFactura.GuardarValorConfig("DraftPrecioUnitario", DraftItem.PrecioUnitario.ToString());
        }

        public async Task GuardarFechaFacturaEnBorradorAsync()
        {
            await _servicioFactura.GuardarValorConfig("DraftFechaFactura", FechaFacturaEnBorrador.ToString("o"));
        }

        public async Task GuardarEstadoModificacionAsync()
        {
            await _servicioFactura.GuardarValorConfig("DraftModifyingID", FacturaIDEnModificacion?.ToString() ?? "0");
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

            FacturaIDEnModificacion = null;
            await GuardarEstadoModificacionAsync();

            FechaFacturaEnBorrador = DateTime.Today;
            await GuardarFechaFacturaEnBorradorAsync();
        }

        public async Task GuardarFacturaActualAsync(DateTime fechaFactura, List<FacturaItem> itemsDraft)
        {
            if (!itemsDraft.Any())
            {
                throw new System.Exception("No se puede guardar una factura vacía.");
            }

            await _servicioFactura.GuardarFacturaCompletaAsync(fechaFactura, itemsDraft);
            await LimpiarConfigBorradorAsync();
        }

        public async Task ActualizarFacturaGuardadaAsync(int facturaID, DateTime fechaFactura, List<FacturaItem> itemsDraft)
        {
            if (!itemsDraft.Any())
            {
                throw new System.Exception("No se puede guardar una factura vacía.");
            }

            await _servicioFactura.ActualizarFacturaCompletaAsync(facturaID, fechaFactura, itemsDraft);
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

        public async Task<ReporteAnual> GenerarReporteAnualAsync(int anio)
        {
            var facturas = await _servicioFactura.ObtenerFacturasPorAnioAsync(anio);
            var reporte = new ReporteAnual { Anio = anio };
            var facturasPorMes = facturas.GroupBy(f => f.FechaCreacion.Month)
                                         .ToDictionary(g => g.Key, g => g.ToList());

            for (int i = 1; i <= 12; i++)
            {
                var nombreMes = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i);
                var reporteMes = new ReporteMes
                {
                    NumeroMes = i,
                    NombreMes = nombreMes.Substring(0, 1).ToUpper() + nombreMes.Substring(1)
                };

                if (facturasPorMes.ContainsKey(i))
                {
                    reporteMes.Facturas = facturasPorMes[i];
                }

                reporte.Meses.Add(reporteMes);
            }

            return reporte;
        }
    }
}