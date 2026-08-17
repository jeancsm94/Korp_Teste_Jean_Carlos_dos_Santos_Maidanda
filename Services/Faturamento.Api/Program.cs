using Faturamento.Api.Clients;
using Faturamento.Api.Data;
using Faturamento.Api.Services;
using Faturamento.Api.Web;
using Korp.Shared.Web;
using Microsoft.EntityFrameworkCore;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<FaturamentoDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("FaturamentoDb")));

builder.Services.AddScoped<IInvoiceService, InvoiceService>();

builder.Services.AddHttpClient<IEstoqueClient, EstoqueClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Estoque:BaseUrl"]!);
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.Retry.Delay = TimeSpan.FromMilliseconds(200);
    options.Retry.UseJitter = true;

    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);

    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
    options.CircuitBreaker.FailureRatio = 0.5;
    options.CircuitBreaker.MinimumThroughput = 2;
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(10);

    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<EstoqueUnavailableExceptionHandler>();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

const string CorsPolicy = "Frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();

app.UseCors(CorsPolicy);

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
