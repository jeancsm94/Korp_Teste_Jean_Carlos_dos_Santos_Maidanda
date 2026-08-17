namespace Korp.Shared.Exceptions
{
    public abstract class DomainException : Exception
    {
        protected DomainException(string message) : base(message)
        {
        }

        public abstract int StatusCode { get; }

        public virtual string Title => "Erro de domínio";
    }
}
