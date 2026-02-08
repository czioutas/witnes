namespace Libs.Result;

public sealed record Result<T>
{
    private object? Value { get; set; }

    public T GetValue
    {
        get { return Value is null ? throw new Exception("Value is null") : (T)Value; }
    }

    // Non-nullable ErrorModel with null! to satisfy the compiler
    public ErrorModel ErrorModel { get; private set; } = null!;

    public bool IsFailure => ErrorModel is not null;

    // Constructor for successful results
    public Result(T value)
    {
        Value = value;
        // ErrorModel remains null! but we don't explicitly set it
    }

    // Constructor for failed results with an ErrorModel
    public Result(ErrorModel errorModel)
    {
        Value = null;
        ErrorModel = errorModel;
    }

    public static Result<T> Ok(T value) => new(value);

    public static Result<T> Failure(ErrorModel errorModel) => new(errorModel);

    // Static methods for predefined error results

    public static Result<T> NoResults(string query)
    {
        var errorModel = new NoResultsErrorModel(query);
        return new Result<T>(errorModel);
    }

    public static Result<T> NotFound(string resourceName, string resourceId)
    {
        var errorModel = new ResourceNotFoundErrorModel(resourceName, resourceId);
        return new Result<T>(errorModel);
    }

    public static Result<T> BusinessError(string businessRule, string message)
    {
        var errorModel = new BusinessLogicErrorModel(businessRule, message);
        return new Result<T>(errorModel);
    }

    public static Result<T> InfaError(string message, Exception ex)
    {
        var errorModel = new InternalErrorModel(ex, message);
        return new Result<T>(errorModel);
    }

    public static Result<T> ThirdPartyError(string serviceName, int statusCode, string thirdPartyMessage)
    {
        var errorModel = new ThirdPartyErrorModel(serviceName, statusCode, thirdPartyMessage);
        return new Result<T>(errorModel);
    }

    // Static method for ValidationError
    public static Result<T> ValidationError(string field, string errorMessage)
    {
        var errorModel = new ValidationErrorModel(field, errorMessage);
        return new Result<T>(errorModel);
    }

    public static Result<T> Forbidden(string reason)
    {
        var errorModel = new ForbiddenErrorModel(reason);
        return new Result<T>(errorModel);
    }

    public Result<TResult> Map<TResult>(Func<T, TResult> onSuccess)
    {
        if (!IsFailure)
            return Result<TResult>.Ok(onSuccess(GetValue));

        return Result<TResult>.Failure(ErrorModel);
    }

    public Result<T> MapError(Func<ErrorModel, ErrorModel> errorTransformer)
    {
        if (!IsFailure)
            return this;

        return Result<T>.Failure(errorTransformer(ErrorModel));
    }

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<ErrorModel, TResult> onFailure)
    {
        return !IsFailure
            ? onSuccess(GetValue)
            : onFailure(ErrorModel);
    }

    /// <summary>
    /// Converts a failed Result of one type to a failed Result of another type,
    /// preserving the error. Useful for bubbling up errors through layers.
    /// </summary>
    public Result<TNew> ToErrorResult<TNew>()
    {
        if (!IsFailure)
            throw new InvalidOperationException("Cannot convert a successful result using ToErrorResult");

        return Result<TNew>.Failure(ErrorModel);
    }

    /// <summary>
    /// Returns the error as a Result of a different type if this result is a failure, otherwise returns null.
    /// Useful for early returns: if (result.ReturnIfError() is var error) return error;
    /// </summary>
    public Result<TNew>? ReturnIfError<TNew>()
    {
        return IsFailure ? Result<TNew>.Failure(ErrorModel) : null;
    }
}
