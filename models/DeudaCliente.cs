namespace ContabilidadOrquestador.Models
{
    public class DeudaClienteResult
    {
        public string ClienteCi { get; set; }
        public string NombreCliente { get; set; }
        public string CategoriaCliente { get; set; }
        public decimal TotalFacturado { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal DeudaActual { get; set; }
        public string EstadoDeuda { get; set; }
        public int CantidadFacturas { get; set; }
        public int CantidadPagos { get; set; }
        public DateTime FechaCalculo { get; set; }
        public List<Factura> Facturas { get; set; } = new List<Factura>();
        public List<Pago> Pagos { get; set; } = new List<Pago>();
    }

    public class Factura
    {
        public int Codigo { get; set; }
        public string ClienteCi { get; set; }
        public DateTime Fecha { get; set; }
        public decimal MontoTotal { get; set; }
        public bool Pagada { get; set; }
    }

    public class Pago
    {
        public int Codigo { get; set; }
        public int FacturaCodigo { get; set; }
        public DateTime FechaPago { get; set; }
        public decimal MontoPagado { get; set; }
    }

    public class Cliente
    {
        public string Nombre { get; set; }
        public string Ci { get; set; }
        public string Categoria { get; set; }
    }
}