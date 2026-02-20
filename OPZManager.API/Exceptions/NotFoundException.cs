namespace OPZManager.API.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
        public NotFoundException(string entityName, object key)
            : base($"{entityName} o identyfikatorze '{key}' nie został znaleziony.") { }
    }
}
