// Services/ContabilidadService.cs
using System.Text.Json;
using ContabilidadOrquestador.Models;

namespace ContabilidadOrquestador.Services
{
    public class ContabilidadService : IContabilidadService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ContabilidadService> _logger;
        private readonly string _baseUrl;

        public ContabilidadService(HttpClient httpClient, ILogger<ContabilidadService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _baseUrl = configuration["ServiciosExternos:BaseUrl"]
                       ?? "https://programacionweb2examen3-production.up.railway.app/api";

            _logger.LogInformation("Servicio externo base URL: {BaseUrl}", _baseUrl);
        }

        public async Task<List<Factura>> ObtenerTodasFacturas()
        {
            try
            {
                var url = $"{_baseUrl}/Facturas/Listar";
                _logger.LogInformation("Consultando facturas → {Url}", url);

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Error al obtener facturas: {Status}", response.StatusCode);
                    return new List<Factura>();
                }

                var json = await response.Content.ReadAsStringAsync();
                var facturas = JsonSerializer.Deserialize<List<Factura>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("Facturas obtenidas: {Count}", facturas?.Count ?? 0);
                return facturas ?? new List<Factura>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al obtener facturas");
                return new List<Factura>();
            }
        }

        public async Task<List<Pago>> ObtenerTodosPagos()
        {
            try
            {
                var url = $"{_baseUrl}/Pagos/Listar";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Error al obtener pagos: {Status}", response.StatusCode);
                    return new List<Pago>();
                }

                var json = await response.Content.ReadAsStringAsync();
                var pagos = JsonSerializer.Deserialize<List<Pago>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("Pagos obtenidos: {Count}", pagos?.Count ?? 0);
                return pagos ?? new List<Pago>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al obtener pagos");
                return new List<Pago>();
            }
        }

        public async Task<List<Cliente>> ObtenerTodosClientes()
        {
            try
            {
                var url = $"{_baseUrl}/Clientes/Listar";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Error al obtener clientes: {Status}", response.StatusCode);
                    return new List<Cliente>();
                }

                var json = await response.Content.ReadAsStringAsync();
                var clientes = JsonSerializer.Deserialize<List<Cliente>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("Clientes obtenidos: {Count}", clientes?.Count ?? 0);
                return clientes ?? new List<Cliente>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al obtener clientes");
                return new List<Cliente>();
            }
        }

        public async Task<DeudaClienteResult> CalcularDeudaCliente(string clienteCi)
        {
            if (!int.TryParse(clienteCi, out int ciInt))
                throw new ArgumentException("El CI debe ser un número válido.");

            var (facturas, pagos, clientes) = await ObtenerDatosParalelo();

            var cliente = clientes.FirstOrDefault(c => c.Ci == clienteCi)
                          ?? throw new KeyNotFoundException($"Cliente con CI {clienteCi} no encontrado.");

            var facturasCliente = facturas.Where(f => f.ClienteCi == ciInt).ToList();

            var pagosCliente = pagos
                .Where(p => facturas.Any(f => f.Codigo == p.FacturaCodigo && f.ClienteCi == ciInt))
                .ToList();

            var totalFacturado = facturasCliente.Sum(f => f.MontoTotal);
            var totalPagado = pagosCliente.Sum(p => p.MontoPagado);
            var deudaActual = totalFacturado - totalPagado;

            var estadoDeuda = deudaActual <= 0 ? "Al día" :
                              deudaActual <= 1000 ? "En observación" : "Moroso";

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

        public async Task<object> GenerarReporteMorosidad()
        {
            var (facturas, pagos, clientes) = await ObtenerDatosParalelo();

            var resultado = new List<object>();

            foreach (var cliente in clientes)
            {
                if (!int.TryParse(cliente.Ci, out int ciInt)) continue;

                var facturasCli = facturas.Where(f => f.ClienteCi == ciInt).ToList();
                var pagosCli = pagos
                    .Where(p => facturas.Any(f => f.Codigo == p.FacturaCodigo && f.ClienteCi == ciInt))
                    .ToList();

                var facturado = facturasCli.Sum(f => f.MontoTotal);
                var pagado = pagosCli.Sum(p => p.MontoPagado);
                var deuda = facturado - pagado;

                var estado = deuda <= 0 ? "Al día" :
                             deuda <= 1000 ? "En observación" : "Moroso";

                resultado.Add(new
                {
                    ClienteCi = cliente.Ci,
                    NombreCliente = cliente.Nombre,
                    Categoria = cliente.Categoria,
                    TotalFacturado = facturado,
                    TotalPagado = pagado,
                    Deuda = deuda,
                    Estado = estado,
                    CantidadFacturas = facturasCli.Count
                });
            }

            var morosos = resultado.Where(x => ((dynamic)x).Estado == "Moroso").ToList();

            return new
            {
                FechaGeneracion = DateTime.UtcNow,
                TotalClientes = clientes.Count,
                TotalDeudaGeneral = resultado.Sum(x => (decimal)((dynamic)x).Deuda),
                Resumen = new
                {
                    AlDia = resultado.Count(x => ((dynamic)x).Estado == "Al día"),
                    EnObservacion = resultado.Count(x => ((dynamic)x).Estado == "En observación"),
                    Morosos = morosos.Count
                },
                Top5Morosos = morosos.OrderByDescending(x => (decimal)((dynamic)x).Deuda).Take(5),
                Detalle = resultado
            };
        }

        private async Task<(List<Factura> facturas, List<Pago> pagos, List<Cliente> clientes)> ObtenerDatosParalelo()
        {
            var t1 = ObtenerTodasFacturas();
            var t2 = ObtenerTodosPagos();
            var t3 = ObtenerTodosClientes();

            await Task.WhenAll(t1, t2, t3);

            return (t1.Result, t2.Result, t3.Result);
        }
    }
}
