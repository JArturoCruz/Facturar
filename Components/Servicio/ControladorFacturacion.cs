using Factura.Components.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Factura.Components.Servicio
{
    public class ControladorFacturacion
    {
        private readonly ServicioFacturacion _servicioFacturacion;

        public ControladorFacturacion(ServicioFacturacion servicioFacturacion)
        {
            _servicioFacturacion = servicioFacturacion;
        }

        public async Task<List<FacturaItem>> ObtenerItems()
        {
            return await _servicioFacturacion.ObtenerItems();
        }

        public async Task AgregarItem(FacturaItem item)
        {
            item.Identificador = await GenerarNuevoID();
            await _servicioFacturacion.AgregarItem(item);
        }

        public async Task EliminarItem(int identificador)
        {
            await _servicioFacturacion.EliminarItem(identificador);
        }

        private async Task<int> GenerarNuevoID()
        {
            var items = await _servicioFacturacion.ObtenerItems();
            return items.Any() ? items.Max(i => i.Identificador) + 1 : 1;
        }
    }
}