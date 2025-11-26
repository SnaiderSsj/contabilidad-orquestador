// Models/DeudaCliente.cs
namespace ContabilidadOrquestador.Models
{
    public class DeudaClienteResult
    {
        public string ClienteCi { get; set; } = string.Empty;
        public string NombreCliente { get; set; } = string.Empty;
        public string CategoriaCliente { get; set; } = string.Empty;
        public decimal TotalFacturado { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal DeudaActual { get; set; }
        public string EstadoDeuda { get; set; } = string.Empty;
        public int CantidadFacturas { get; set; }
        public int CantidadPagos { get; set; }
        public DateTime FechaCalculo { get; set; }
        public List<Factura> Facturas { get; set; } = new();
        public List<Pago> Pagos { get; set; } = new();
    }

    public class Factura
    {
        public int Codigo { get; set; }
        public int ClienteCi { get; set; }           // ← INT
        public string Fecha { get; set; } = string.Empty;
        public int MontoTotal { get; set; }          // ← INT
        public bool Pagada { get; set; }
    }

    public class Pago
    {
        public int Codigo { get; set; }
        public int FacturaCodigo { get; set; }
        public string FechaPago { get; set; } = string.Empty;
        public decimal MontoPagado { get; set; }
    }

    public class Cliente
    {
        public string Nombre { get; set; } = string.Empty;
        public string Ci { get; set; } = string.Empty;     // ← string (ej: "32320")
        public string Categoria { get; set; } = string.Empty;
    }
}
