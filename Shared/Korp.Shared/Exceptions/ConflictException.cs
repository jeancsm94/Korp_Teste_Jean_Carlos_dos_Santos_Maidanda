using Microsoft.AspNetCore.Http;

namespace Korp.Shared.Exceptions
{
    public class ConflictException : DomainException
    {
        public ConflictException(string message) : base(message)
        {
        }

        public override int StatusCode => StatusCodes.Status409Conflict;

        public override string Title => "Conflito";
    }
}
