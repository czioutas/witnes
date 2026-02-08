using Api.Application.Models;
using Libs.Result;

namespace Api.Application.Extensions;

public static class ErrorModelExtensions
{
    public static ApplicationProblemDetailsModel ToApplicationProblemDetails(
        this ErrorModel error,
        HttpContext httpContext)
    {
        var statusCode = error.ErrorCode switch
        {
            ErrorCode.ValidationError => StatusCodes.Status400BadRequest,
            ErrorCode.ResourceNotFound => StatusCodes.Status404NotFound,
            ErrorCode.Forbidden => StatusCodes.Status403Forbidden,
            ErrorCode.Authentication => StatusCodes.Status401Unauthorized,
            ErrorCode.ThirdPartyError => StatusCodes.Status502BadGateway,
            ErrorCode.InternalError => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status400BadRequest
        };

        var messages = new List<string>();

        switch (error)
        {
            case ValidationErrorModel ve:
                messages.Add($"{ve.Field}: {ve.ErrorMessage}");
                break;
            case ResourceNotFoundErrorModel rnf:
                messages.Add($"{rnf.ResourceName} ({rnf.ResourceId}) not found.");
                break;
            case BusinessLogicErrorModel be:
                messages.Add($"Business rule violated: {be.BusinessRule}");
                break;
            case ThirdPartyErrorModel tp:
                messages.Add($"External service '{tp.ServiceName}' failed with status {tp.StatusCode}");
                break;
            case ForbiddenErrorModel f:
                messages.Add($"Reason: {f.Reason}");
                break;
            default:
                messages.Add(error.Message);
                break;
        }

        return new ApplicationProblemDetailsModel(
            statusCode: statusCode,
            title: error.ErrorCode.ToString(),
            exceptionType: error.Message,   // becomes Detail
            path: httpContext.Request.Path,
            traceId: httpContext.TraceIdentifier,
            errors: messages.ToArray()
        );
    }
}
