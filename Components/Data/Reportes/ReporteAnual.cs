using System.Collections.Generic;
using System.Linq;

namespace Facturar.Components.Data.Reportes
{
    public class ReporteAnual
    {
        public int Anio { get; set; }
        public List<ReporteMes> Meses { get; set; } = new List<ReporteMes>();
        public decimal TotalAnual => Meses.Sum(m => m.TotalMes);
    }
}