using System.Text.Json.Serialization;

namespace Libs.Result
{
    // Base class for errors
    public class ErrorModel
    {
        public ErrorCode ErrorCode { get; }
        public string Message { get; }
        public object? AdditionalData { get; }

        // Constructor requiring the errorCode and message, with optional additional data
        public ErrorModel(ErrorCode errorCode, string message, object? additionalData = null)
        {
            ErrorCode = errorCode;
            Message = message;
            AdditionalData = additionalData;
        }
    }

    public class NoResultsErrorModel : ErrorModel
    {
        public string Query { get; }

        // Constructor that passes values to the base class
        public NoResultsErrorModel(string query)
            : base(ErrorCode.NoResults, "No Results could be found", new { Query = query })
        {
            Query = query;
        }
    }

    // ResourceNotFoundErrorModel (e.g., for when a resource is not found)
    public class ResourceNotFoundErrorModel : ErrorModel
    {
        public string ResourceName { get; }
        public string ResourceId { get; }

        // Constructor that passes values to the base class
        public ResourceNotFoundErrorModel(string resourceName, string resourceId)
            : base(ErrorCode.ResourceNotFound, $"{resourceName} with id {resourceId} was not found.", resourceName)
        {
            ResourceName = resourceName;
            ResourceId = resourceId;
        }
    }

    // BusinessLogicErrorModel (e.g., for errors related to business rules)
    public class BusinessLogicErrorModel : ErrorModel
    {
        public string BusinessRule { get; }

        // Constructor that passes values to the base class
        public BusinessLogicErrorModel(string businessRule, string message)
            : base(ErrorCode.BusinessLogicError, message, businessRule)
        {
            BusinessRule = businessRule;
        }
    }

    // ThirdPartyErrorModel (e.g., for errors from external services)
    public class ThirdPartyErrorModel : ErrorModel
    {
        public string ServiceName { get; }
        public int StatusCode { get; }

        // Constructor that passes values to the base class
        public ThirdPartyErrorModel(string serviceName, int statusCode, string thirdPartyMessage)
            : base(ErrorCode.ThirdPartyError, thirdPartyMessage)
        {
            ServiceName = serviceName;
            StatusCode = statusCode;
        }
    }

    // ValidationErrorModel (e.g., for errors related to validation failures)
    public class ValidationErrorModel : ErrorModel
    {
        public string Field { get; }
        public string ErrorMessage { get; }

        // Constructor that passes values to the base class
        public ValidationErrorModel(string field, string errorMessage)
            : base(ErrorCode.ValidationError, "A validation error occurred.", new { Field = field, ErrorMessage = errorMessage })
        {
            Field = field;
            ErrorMessage = errorMessage;
        }
    }

    // InternalError for system/internal exceptions
    public class InternalErrorModel : ErrorModel
    {
        [JsonIgnore]
        public Exception Exception { get; }

        // Constructor that passes values to the base class
        public InternalErrorModel(Exception exception)
            : base(ErrorCode.InternalError, exception.Message, new { ExceptionType = exception.GetType().Name })
        {
            Exception = exception;
        }

        // Overload that allows for a custom message
        public InternalErrorModel(Exception exception, string customMessage)
            : base(ErrorCode.InternalError, customMessage, new { ExceptionType = exception.GetType().Name, ExceptionMessage = exception.Message })
        {
            Exception = exception;
        }
    }

    public class ForbiddenErrorModel : ErrorModel
    {
        public string Reason { get; }

        public ForbiddenErrorModel(string reason)
            : base(ErrorCode.Forbidden, reason)
        {
            Reason = reason;
        }
    }
}


public enum ErrorCode
{
    BusinessLogicError,
    ResourceNotFound,
    NoResults,
    ThirdPartyError,
    ValidationError,
    InternalError,
    Forbidden,
    Authentication
}
