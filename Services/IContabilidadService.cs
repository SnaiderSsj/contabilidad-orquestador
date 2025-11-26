using ContabilidadOrquestador.Models;

namespace ContabilidadOrquestador.Services
{
    public interface IContabilidadService
    {
        Task<DeudaClienteResult> CalcularDeudaCliente(string clienteCi);
        Task<object> GenerarReporteMorosidad();
        Task<List<Factura>> ObtenerTodasFacturas();
        Task<List<Pago>> ObtenerTodosPagos();
        Task<List<Cliente>> ObtenerTodosClientes();
    }
}