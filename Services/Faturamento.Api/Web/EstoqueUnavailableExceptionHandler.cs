using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Faturamento.Api.Web
{
    // Registrado ANTES do ApiExceptionHandler compartilhado: traduz falhas de comunicação
    // com o serviço de Estoque (circuito aberto, timeout, HTTP genérico) em um 503 claro,
    // em vez de deixar a exceção estourar como erro 500 não tratado.
    public sealed class EstoqueUnavailableExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly ILogger<EstoqueUnavailableExceptionHandler> _logger;

        public EstoqueUnavailableExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<EstoqueUnavailableExceptionHandler> logger)
        {
            _problemDetailsService = problemDetailsService;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not (BrokenCircuitException or TimeoutRejectedException or HttpRequestException))
            {
                return false;
            }

            _logger.LogWarning(exception, "Falha de comunicação com o serviço de Estoque.");

            httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Serviço indisponível",
                    Detail = "Serviço de estoque está indisponível no momento. Tente novamente em instantes.",
                    Instance = httpContext.Request.Path
                }
            });
        }
    }
}
