using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ContabilidadOrquestador.Services;

namespace ContabilidadOrquestador.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosticoController : ControllerBase
    {
        private readonly IContabilidadService _contabilidadService;
        private readonly HttpClient _httpClient;

        public DiagnosticoController(IContabilidadService contabilidadService, HttpClient httpClient)
        {
            _contabilidadService = contabilidadService;
            _httpClient = httpClient;
        }

        [HttpGet("estructura-datos")]
        public async Task<ActionResult> GetEstructuraDatos()
        {
            try
            {
                var facturasResponse = await _httpClient.GetAsync("https://programacionweb2examen3-production.up.railway.app/api/Facturas/Listar");
                var pagosResponse = await _httpClient.GetAsync("https://programacionweb2examen3-production.up.railway.app/api/Pagos/Listar");
                var clientesResponse = await _httpClient.GetAsync("https://programacionweb2examen3-production.up.railway.app/api/Clientes/Listar");

                var resultado = new
                {
                    Facturas = new
                    {
                        StatusCode = facturasResponse.StatusCode,
                        Content = facturasResponse.IsSuccessStatusCode ?
                            await facturasResponse.Content.ReadAsStringAsync() : "ERROR",
                        Estructura = "Por analizar"
                    },
                    Pagos = new
                    {
                        StatusCode = pagosResponse.StatusCode,
                        Content = pagosResponse.IsSuccessStatusCode ?
                            await pagosResponse.Content.ReadAsStringAsync() : "ERROR",
                        Estructura = "Por analizar"
                    },
                    Clientes = new
                    {
                        StatusCode = clientesResponse.StatusCode,
                        Content = clientesResponse.IsSuccessStatusCode ?
                            await clientesResponse.Content.ReadAsStringAsync() : "ERROR",
                        Estructura = "Por analizar"
                    }
                };

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}