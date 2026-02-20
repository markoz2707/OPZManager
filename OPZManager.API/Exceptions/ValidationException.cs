namespace OPZManager.API.Exceptions
{
    public class AppValidationException : Exception
    {
        public Dictionary<string, string[]> Errors { get; }

        public AppValidationException(string message) : base(message)
        {
            Errors = new Dictionary<string, string[]>();
        }

        public AppValidationException(Dictionary<string, string[]> errors)
            : base("Wystąpiły błędy walidacji.")
        {
            Errors = errors;
        }
    }
}
