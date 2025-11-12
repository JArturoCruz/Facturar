using System.Collections.Generic;
using System.Linq;

namespace Facturar.Components.Data.Reportes
{
    public class ReporteMes
    {
        public int NumeroMes { get; set; }
        public string NombreMes { get; set; }
        public List<Factura> Facturas { get; set; } = new List<Factura>();
        public decimal TotalMes => Facturas.Sum(f => f.Total);
    }
}