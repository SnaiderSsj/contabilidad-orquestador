// Services/ContabilidadService.cs
using System.Text.Json;
using ContabilidadOrquestador.Models;

namespace ContabilidadOrquestador.Services
{
    public class ContabilidadService : IContabilidadService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ContabilidadService> _logger;
        private readonly string _facturasUrl;
        private readonly string _pagosUrl;
        private readonly string _clientesUrl;

        public ContabilidadService(HttpClient httpClient, ILogger<ContabilidadService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;

            var baseUrl = configuration["ServiciosExternos:BaseUrl"]
                          ?? "https://programacionweb2examen3-production.up.railway.app/api";

            _facturasUrl = $"{baseUrl}/Facturas/Listar";
            _pagosUrl = $"{baseUrl}/Pagos/Listar";
            _clientesUrl = $"{baseUrl}/Clientes/Listar";

            _logger.LogInformation("URLs configuradas:");
            _logger.LogInformation($"Facturas: {_facturasUrl}");
            _logger.LogInformation($"Pagos: {_pagosUrl}");
            _logger.LogInformation($"Clientes: {_clientesUrl}");
        }

        public async Task<List<Factura>> ObtenerTodasFacturas()
        {
            try
            {
                _logger.LogInformation("Obteniendo facturas desde {Url}", _facturasUrl);
                var response = await _httpClient.GetAsync(_facturasUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Error HTTP al obtener facturas: {StatusCode}", response.StatusCode);
                    return new List<Factura>();
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Respuesta facturas: {Content}", content.Substring(0, Math.Min(content.Length, 500)));

                var facturas = JsonSerializer.Deserialize<List<Factura>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("Facturas deserializadas: {Count}", facturas?.Count ?? 0);
                return facturas ?? new List<Factura>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción al obtener facturas");
                return new List<Factura>();
            }
        }

        public async Task<List<Pago>> ObtenerTodosPagos()
        {
            try
            {
                _logger.LogInformation("Obteniendo pagos desde {Url}", _pagosUrl);
                var response = await _httpClient.GetAsync(_pagosUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Error HTTP al obtener pagos: {StatusCode}", response.StatusCode);
                    return new List<Pago>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var pagos = JsonSerializer.Deserialize<List<Pago>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("Pagos deserializados: {Count}", pagos?.Count ?? 0);
                return pagos ?? new List<Pago>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción al obtener pagos");
                return new List<Pago>();
            }
        }

        public async Task<List<Cliente>> ObtenerTodosClientes()
        {
            try
            {
                _logger.LogInformation("Obteniendo clientes desde {Url}", _clientesUrl);
                var response = await _httpClient.GetAsync(_clientesUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Error HTTP al obtener clientes: {StatusCode}", response.StatusCode);
                    return new List<Cliente>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var clientes = JsonSerializer.Deserialize<List<Cliente>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("Clientes deserializados: {Count}", clientes?.Count ?? 0);
                return clientes ?? new List<Cliente>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción al obtener clientes");
                return new List<Cliente>();
            }
        }

        public async Task<DeudaClienteResult> CalcularDeudaCliente(string clienteCi)
        {
            _logger.LogInformation("Calculando deuda para cliente CI: {Ci}", clienteCi);

            // Validar que el CI sea número (los reales son int)
            if (!int.TryParse(clienteCi, out int ciInt))
            {
                throw new ArgumentException("El CI debe ser un número válido");
            }

            var (facturas, pagos, clientes) = await ObtenerDatosParalelo();

            var cliente = clientes.FirstOrDefault(c => c.Ci == clienteCi);
            if (cliente == null)
                throw new KeyNotFoundException($"Cliente con CI {clienteCi} no encontrado");

            var facturasCliente = facturas.Where(f => f.ClienteCiInt == ciInt).ToList();

            var pagosCliente = pagos
                .Where(p => facturas.Any(f => f.Codigo == p.FacturaCodigo && f.ClienteCiInt == ciInt))
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

            var reporte = clientes.Select(cliente =>
            {
                int ciInt = int.TryParse(cliente.Ci, out var ci) ? ci : 0;
                var facturasCliente = facturas.Where(f => f.ClienteCiInt == ciInt).ToList();
                var pagosCliente = pagos
                    .Where(p => facturas.Any(f => f.Codigo == p.FacturaCodigo && f.ClienteCiInt == ciInt))
                    .ToList();

                var totalFacturado = facturasCliente.Sum(f => f.MontoTotal);
                var totalPagado = pagosCliente.Sum(p => p.MontoPagado);
                var deuda = totalFacturado - totalPagado;

                var estado = deuda <= 0 ? "Al día" :
                             deuda <= 1000 ? "En observación" : "Moroso";

                return new
                {
                    ClienteCi = cliente.Ci,
                    NombreCliente = cliente.Nombre,
                    Categoria = cliente.Categoria,
                    TotalFacturado = totalFacturado,
                    TotalPagado = totalPagado,
                    Deuda = deuda,
                    Estado = estado,
                    CantidadFacturas = facturasCliente.Count
                };
            }).ToList();

            var morosos = reporte.Where(r => r.Estado == "Moroso").Cast<dynamic>().ToList();

            return new
            {
                FechaGeneracion = DateTime.UtcNow,
                TotalClientes = clientes.Count,
                TotalDeudaGeneral = reporte.Sum(r => r.Deuda),
                ResumenEstados = new
                {
                    AlDia = reporte.Count(r => r.Estado == "Al día"),
                    EnObservacion = reporte.Count(r => r.Estado == "En observación"),
                    Morosos = morosos.Count
                },
                Top5Morosos = morosos.OrderByDescending(m => m.Deuda).Take(5),
                DetalleCompleto = reporte
            };
        }

        // Método auxiliar para traer todo en paralelo (más rápido)
        private async Task<(List<Factura> facturas, List<Pago> pagos, List<Cliente> clientes)> ObtenerDatosParalelo()
        {
            var facturasTask = ObtenerTodasFacturas();
            var pagosTask = ObtenerTodosPagos();
            var clientesTask = ObtenerTodosClientes();

            await Task.WhenAll(facturasTask, pagosTask, clientesTask);

            return (await facturasTask, await pagosTask, await clientesTask);
        }
    }
}