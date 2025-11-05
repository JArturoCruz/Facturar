using System.ComponentModel.DataAnnotations;

namespace Factura.Components.Data
{
    public class FacturaItem
    {
        public int Identificador { get; set; }

        [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
        public string Producto { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser al menos 1.")]
        public int Cantidad { get; set; } = 1;

        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "El precio debe ser mayor a 0.")]
        public decimal PrecioUnitario { get; set; } = 0.01m;

        // Propiedad calculada
        public decimal Subtotal => Cantidad * PrecioUnitario;
    }
}