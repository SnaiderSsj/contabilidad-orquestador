using Microsoft.AspNetCore.Mvc;
using ContabilidadOrquestador.Models;
using ContabilidadOrquestador.Services;

namespace ContabilidadOrquestador.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContabilidadController : ControllerBase
    {
        private readonly IContabilidadService _contabilidadService;
        private readonly ILogger<ContabilidadController> _logger;

        public ContabilidadController(IContabilidadService contabilidadService, ILogger<ContabilidadController> logger)
        {
            _contabilidadService = contabilidadService;
            _logger = logger;
        }

        [HttpGet("deuda-cliente/{clienteCi}")]
        public async Task<ActionResult<DeudaClienteResult>> GetDeudaCliente(string clienteCi)
        {
            try
            {
                _logger.LogInformation($"Solicitando deuda para cliente CI: {clienteCi}");
                var resultado = await _contabilidadService.CalcularDeudaCliente(clienteCi);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error procesando solicitud para cliente CI: {clienteCi}");
                return StatusCode(500, new
                {
                    error = "Error interno del servidor",
                    mensaje = ex.Message,
                    clienteCi
                });
            }
        }

        [HttpGet("reporte-morosidad")]
        public async Task<ActionResult> GetReporteMorosidad()
        {
            try
            {
                _logger.LogInformation("Generando reporte de morosidad");
                var reporte = await _contabilidadService.GenerarReporteMorosidad();
                return Ok(reporte);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte de morosidad");
                return StatusCode(500, new
                {
                    error = "Error generando reporte",
                    mensaje = ex.Message
                });
            }
        }

        [HttpGet("facturas")]
        public async Task<ActionResult<List<Factura>>> GetFacturas()
        {
            try
            {
                var facturas = await _contabilidadService.ObtenerTodasFacturas();
                return Ok(facturas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo facturas");
                return StatusCode(500, new { error = "Error obteniendo facturas" });
            }
        }

        [HttpGet("pagos")]
        public async Task<ActionResult<List<Pago>>> GetPagos()
        {
            try
            {
                var pagos = await _contabilidadService.ObtenerTodosPagos();
                return Ok(pagos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo pagos");
                return StatusCode(500, new { error = "Error obteniendo pagos" });
            }
        }

        [HttpGet("clientes")]
        public async Task<ActionResult<List<Cliente>>> GetClientes()
        {
            try
            {
                var clientes = await _contabilidadService.ObtenerTodosClientes();
                return Ok(clientes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo clientes");
                return StatusCode(500, new { error = "Error obteniendo clientes" });
            }
        }

        [HttpGet("health")]
        public ActionResult Health()
        {
            return Ok(new
            {
                status = "Healthy",
                servicio = "Contabilidad Orquestador",
                timestamp = DateTime.UtcNow,
                descripcion = "Microservicio para cálculo de deudas clientes",
                endpoints_consumidos = new[] {
                    "https://programacionweb2examen3-production.up.railway.app/api/Facturas/Listar",
                    "https://programacionweb2examen3-production.up.railway.app/api/Pagos/Listar",
                    "https://programacionweb2examen3-production.up.railway.app/api/Clientes/Listar"
                }
            });
        }

        [HttpGet("datos-reales")]
        public async Task<ActionResult> GetDatosReales()
        {
            try
            {
                var facturas = await _contabilidadService.ObtenerTodasFacturas();
                var pagos = await _contabilidadService.ObtenerTodosPagos();
                var clientes = await _contabilidadService.ObtenerTodosClientes();

                return Ok(new
                {
                    TotalClientes = clientes.Count,
                    TotalFacturas = facturas.Count,
                    TotalPagos = pagos.Count,
                    Clientes = clientes.Take(5),
                    Facturas = facturas.Take(5),
                    Pagos = pagos.Take(5)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}