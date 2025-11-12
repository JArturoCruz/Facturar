using System;
using System.Collections.Generic;

namespace Facturar.Components.Data
{
    public class Factura
    {
        public int FacturaID { get; set; }
        public string NombreFactura { get; set; }
        public DateTime FechaCreacion { get; set; }
        public decimal Total { get; set; }
        public string NombreUsuario { get; set; }
        public List<FacturaItem> Items { get; set; } = new List<FacturaItem>();
    }
}