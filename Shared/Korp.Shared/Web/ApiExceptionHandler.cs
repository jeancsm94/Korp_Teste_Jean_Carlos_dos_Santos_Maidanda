using Korp.Shared.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Korp.Shared.Web
{
    public sealed class ApiExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly ILogger<ApiExceptionHandler> _logger;

        public ApiExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ApiExceptionHandler> logger)
        {
            _problemDetailsService = problemDetailsService;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not DomainException domainException)
            {
                return false;
            }

            _logger.LogWarning(domainException, "Erro de domínio tratado: {Message}", domainException.Message);

            httpContext.Response.StatusCode = domainException.StatusCode;

            var problemDetails = new ProblemDetails
            {
                Status = domainException.StatusCode,
                Title = domainException.Title,
                Detail = domainException.Message,
                Instance = httpContext.Request.Path
            };

            if (domainException is InsufficientStockException { Items: not null } insufficientStock)
            {
                problemDetails.Extensions["items"] = insufficientStock.Items;
            }

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = domainException,
                ProblemDetails = problemDetails
            });
        }
    }
}
