namespace Korp.Shared.Exceptions
{
    public sealed class InvalidInvoiceStateException : ConflictException
    {
        public InvalidInvoiceStateException(string message) : base(message)
        {
        }

        public override string Title => "Estado inválido da nota fiscal";
    }
}
