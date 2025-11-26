// Program.cs  ← COPIA Y PEGA EXACTAMENTE ESTE
using ContabilidadOrquestador.Services;

var builder = WebApplication.CreateBuilder(args);

// === SERVICIOS ===
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<IContabilidadService, ContabilidadService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// === APP ===
var app = builder.Build();

// PUERTO DINÁMICO DE RAILWAY (OBLIGATORIO)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Clear();
app.Urls.Add($"http://0.0.0.0:{port}");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Contabilidad Orquestador v1");
    c.RoutePrefix = "swagger"; // ← para que swagger esté en /swagger
});

app.UseCors("AllowAll");

// QUITAMOS HTTPS REDIRECTION (Railway no lo soporta internamente)
app.UseRouting();
app.MapControllers();

// Health check rápido para que Railway sepa que está vivo
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/health", () => new { status = "OK", time = DateTime.UtcNow });

app.Logger.LogInformation("Orquestador Contabilidad iniciado correctamente en puerto {Port}", port);

app.Run();
