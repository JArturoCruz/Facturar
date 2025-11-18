using System.Collections.Generic;

namespace Facturar.Components.Data
{
    public class DashboardEstadisticas
    {
        public decimal IngresosTotales { get; set; }
        public int TotalFacturas { get; set; }
        public decimal TicketPromedio { get; set; }
        public ProductoTop ProductoMasVendido { get; set; } = new ProductoTop();
        public ProductoTop ProductoConMasIngresos { get; set; } = new ProductoTop();
        public MesVenta MesMasExitoso { get; set; } = new MesVenta();
        public UsuarioTop UsuarioQueMasFactura { get; set; } = new UsuarioTop();
        public UsuarioTop UsuarioConMasTransacciones { get; set; } = new UsuarioTop();
        public List<Factura> UltimasFacturas { get; set; } = new List<Factura>();
        public List<VentaAnual> VentasPorAno { get; set; } = new List<VentaAnual>();
    }

    public class ProductoTop
    {
        public string Nombre { get; set; } = "N/A";
        public decimal Valor { get; set; }
    }

    public class MesVenta
    {
        public string MesAno { get; set; } = "N/A";
        public decimal Total { get; set; }
    }

    public class UsuarioTop
    {
        public string Nombre { get; set; } = "N/A";
        public decimal Valor { get; set; }
    }

    public class VentaAnual
    {
        public string Ano { get; set; }
        public decimal Total { get; set; }
    }
}