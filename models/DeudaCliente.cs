using System.ComponentModel.DataAnnotations.Schema; // Para Column(TypeName)

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
        [Column(TypeName = "int")] // Fuerza int para clienteCi
        public int ClienteCiInt { get; set; } // Temporal para deserializar
        [NotMapped] // No se mapea a DB, solo para runtime
        public string ClienteCi
        {
            get => ClienteCiInt.ToString();
            set => ClienteCiInt = int.TryParse(value, out var ci) ? ci : 0;
        }

        public string Fecha { get; set; } // String como llega
        [Column(TypeName = "int")] // Fuerza int
        public int MontoTotalInt { get; set; }
        [NotMapped]
        public decimal MontoTotal
        {
            get => MontoTotalInt;
            set => MontoTotalInt = (int)value;
        }

        public bool Pagada { get; set; }
    }

    public class Pago
    {
        public int Codigo { get; set; }
        public int FacturaCodigo { get; set; }
        public string FechaPago { get; set; } // String como llega
        public decimal MontoPagado { get; set; } // Ya es decimal, pero confirma en deserialización
    }

    public class Cliente
    {
        public string Nombre { get; set; }
        public string Ci { get; set; }
        public string Categoria { get; set; }
    }
}