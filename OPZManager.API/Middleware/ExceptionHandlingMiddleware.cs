using System.Net;
using System.Text.Json;
using OPZManager.API.DTOs.Common;
using OPZManager.API.Exceptions;

namespace OPZManager.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var errorDto = new ApiErrorDto();

            switch (exception)
            {
                case NotFoundException notFoundEx:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorDto.StatusCode = response.StatusCode;
                    errorDto.Message = notFoundEx.Message;
                    break;

                case AppValidationException validationEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorDto.StatusCode = response.StatusCode;
                    errorDto.Message = validationEx.Message;
                    errorDto.Errors = validationEx.Errors;
                    break;

                case BusinessRuleException businessEx:
                    response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                    errorDto.StatusCode = response.StatusCode;
                    errorDto.Message = businessEx.Message;
                    break;

                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorDto.StatusCode = response.StatusCode;
                    errorDto.Message = "Brak autoryzacji.";
                    break;

                default:
                    _logger.LogError(exception, "Nieobsługiwany wyjątek");
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorDto.StatusCode = response.StatusCode;
                    errorDto.Message = "Wystąpił wewnętrzny błąd serwera.";
                    break;
            }

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await response.WriteAsync(JsonSerializer.Serialize(errorDto, options));
        }
    }
}
