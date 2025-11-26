using System.Text.Json;
using ContabilidadOrquestador.Models;

namespace ContabilidadOrquestador.Services
{
    public class ContabilidadService : IContabilidadService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ContabilidadService> _logger;
        private readonly string _baseUrl = "https://programacionweb2examen3-production.up.railway.app/api";

        public ContabilidadService(HttpClient httpClient, ILogger<ContabilidadService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<Factura>> ObtenerTodasFacturas()
        {
            try
            {
                _logger.LogInformation("Obteniendo todas las facturas...");
                var response = await _httpClient.GetAsync($"{_baseUrl}/Facturas/Listar");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Respuesta Facturas: {content}");

                    var facturas = JsonSerializer.Deserialize<List<Factura>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return facturas ?? new List<Factura>();
                }

                _logger.LogWarning($"Error al obtener facturas: {response.StatusCode}");
                return new List<Factura>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo facturas");
                return new List<Factura>();
            }
        }

        public async Task<List<Pago>> ObtenerTodosPagos()
        {
            try
            {
                _logger.LogInformation("Obteniendo todos los pagos...");
                var response = await _httpClient.GetAsync($"{_baseUrl}/Pagos/Listar");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Respuesta Pagos: {content}");

                    var pagos = JsonSerializer.Deserialize<List<Pago>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return pagos ?? new List<Pago>();
                }

                _logger.LogWarning($"Error al obtener pagos: {response.StatusCode}");
                return new List<Pago>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo pagos");
                return new List<Pago>();
            }
        }

        public async Task<List<Cliente>> ObtenerTodosClientes()
        {
            try
            {
                _logger.LogInformation("Obteniendo todos los clientes...");
                var response = await _httpClient.GetAsync($"{_baseUrl}/Clientes/Listar");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Respuesta Clientes: {content}");

                    var clientes = JsonSerializer.Deserialize<List<Cliente>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return clientes ?? new List<Cliente>();
                }

                _logger.LogWarning($"Error al obtener clientes: {response.StatusCode}");
                return new List<Cliente>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo clientes");
                return new List<Cliente>();
            }
        }

        public async Task<DeudaClienteResult> CalcularDeudaCliente(string clienteCi)
        {
            try
            {
                _logger.LogInformation($"Calculando deuda para cliente CI: {clienteCi}");

                // Obtener todos los datos
                var facturasTask = ObtenerTodasFacturas();
                var pagosTask = ObtenerTodosPagos();
                var clientesTask = ObtenerTodosClientes();

                await Task.WhenAll(facturasTask, pagosTask, clientesTask);

                var todasFacturas = await facturasTask;
                var todosPagos = await pagosTask;
                var todosClientes = await clientesTask;

                // Buscar información del cliente por CI
                var cliente = todosClientes.FirstOrDefault(c => c.Ci == clienteCi);

                if (cliente == null)
                {
                    throw new Exception($"Cliente con CI {clienteCi} no encontrado");
                }

                // Filtrar por cliente CI
                var facturasCliente = todasFacturas.Where(f => f.ClienteCi == clienteCi).ToList();

                // Para pagos, necesitamos buscar las facturas asociadas
                var pagosCliente = new List<Pago>();
                foreach (var pago in todosPagos)
                {
                    var facturaAsociada = todasFacturas.FirstOrDefault(f => f.Codigo == pago.FacturaCodigo);
                    if (facturaAsociada?.ClienteCi == clienteCi)
                    {
                        pagosCliente.Add(pago);
                    }
                }

                // Calcular métricas
                var totalFacturado = facturasCliente.Sum(f => f.MontoTotal);
                var totalPagado = pagosCliente.Sum(p => p.MontoPagado);
                var deudaActual = totalFacturado - totalPagado;

                // Determinar estado de deuda
                var estadoDeuda = deudaActual switch
                {
                    <= 0 => "Al día",
                    > 0 and <= 1000 => "En observación",
                    _ => "Moroso"
                };

                return new DeudaClienteResult
                {
                    ClienteCi = clienteCi,
                    NombreCliente = cliente.Nombre,
                    CategoriaCliente = cliente.Categoria,
                    TotalFacturado = totalFacturado,
                    TotalPagado = totalPagado,
                    DeudaActual = deudaActual,
                    EstadoDeuda = estadoDeuda,
                    CantidadFacturas = facturasCliente.Count,
                    CantidadPagos = pagosCliente.Count,
                    FechaCalculo = DateTime.UtcNow,
                    Facturas = facturasCliente,
                    Pagos = pagosCliente
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculando deuda para cliente CI: {clienteCi}");
                throw;
            }
        }

        public async Task<object> GenerarReporteMorosidad()
        {
            try
            {
                _logger.LogInformation("Generando reporte de morosidad...");

                var facturasTask = ObtenerTodasFacturas();
                var pagosTask = ObtenerTodosPagos();
                var clientesTask = ObtenerTodosClientes();

                await Task.WhenAll(facturasTask, pagosTask, clientesTask);

                var todasFacturas = await facturasTask;
                var todosPagos = await pagosTask;
                var todosClientes = await clientesTask;

                // Calcular deuda por cliente
                var deudaPorCliente = new List<object>();

                foreach (var cliente in todosClientes)
                {
                    var facturasCliente = todasFacturas.Where(f => f.ClienteCi == cliente.Ci).ToList();

                    var pagosCliente = new List<Pago>();
                    foreach (var pago in todosPagos)
                    {
                        var facturaAsociada = todasFacturas.FirstOrDefault(f => f.Codigo == pago.FacturaCodigo);
                        if (facturaAsociada?.ClienteCi == cliente.Ci)
                        {
                            pagosCliente.Add(pago);
                        }
                    }

                    var totalFacturado = facturasCliente.Sum(f => f.MontoTotal);
                    var totalPagado = pagosCliente.Sum(p => p.MontoPagado);
                    var deuda = totalFacturado - totalPagado;

                    var estado = deuda switch
                    {
                        <= 0 => "Al día",
                        > 0 and <= 1000 => "En observación",
                        _ => "Moroso"
                    };

                    deudaPorCliente.Add(new
                    {
                        ClienteCi = cliente.Ci,
                        NombreCliente = cliente.Nombre,
                        Categoria = cliente.Categoria,
                        TotalFacturado = totalFacturado,
                        TotalPagado = totalPagado,
                        Deuda = deuda,
                        Estado = estado,
                        CantidadFacturas = facturasCliente.Count
                    });
                }

                var clientesMorosos = deudaPorCliente.Where(c => ((dynamic)c).Estado == "Moroso").ToList();
                var clientesObservacion = deudaPorCliente.Where(c => ((dynamic)c).Estado == "En observación").ToList();

                return new
                {
                    FechaGeneracion = DateTime.UtcNow,
                    TotalClientes = deudaPorCliente.Count,
                    TotalDeudaGeneral = deudaPorCliente.Sum(c => (decimal)((dynamic)c).Deuda),
                    ResumenEstados = new
                    {
                        AlDia = deudaPorCliente.Count(c => ((dynamic)c).Estado == "Al día"),
                        EnObservacion = clientesObservacion.Count,
                        Morosos = clientesMorosos.Count
                    },
                    TopClientesMorosos = clientesMorosos.Take(10),
                    DeudaPorCliente = deudaPorCliente,
                    MetricasGenerales = new
                    {
                        TotalFacturado = todasFacturas.Sum(f => f.MontoTotal),
                        TotalPagado = todosPagos.Sum(p => p.MontoPagado),
                        TotalFacturas = todasFacturas.Count,
                        TotalPagos = todosPagos.Count,
                        TotalClientesRegistrados = todosClientes.Count
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte de morosidad");
                throw;
            }
        }
    }
}