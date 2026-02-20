namespace OPZManager.API.DTOs.Common
{
    public class ApiErrorDto
    {
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
