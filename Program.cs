using ContabilidadOrquestador.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HttpClient con timeout
builder.Services.AddHttpClient<IContabilidadService, ContabilidadService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// CRÍTICO PARA RAILWAY: Bind al puerto dinámico
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}"); // Escucha en todas las IPs y puerto asignado

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection(); // Quita si da problemas en Railway (usa HTTP)
app.UseAuthorization();
app.MapControllers();

// Log de inicio para depurar en Railway
app.Logger.LogInformation("Contabilidad Orquestador iniciado en puerto {Port} en {Environment}", port, app.Environment.EnvironmentName);

app.Run();