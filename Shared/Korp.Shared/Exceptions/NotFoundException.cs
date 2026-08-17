using Microsoft.AspNetCore.Http;

namespace Korp.Shared.Exceptions
{
    public sealed class NotFoundException : DomainException
    {
        public NotFoundException(string message) : base(message)
        {
        }

        public override int StatusCode => StatusCodes.Status404NotFound;

        public override string Title => "Recurso não encontrado";
    }
}
