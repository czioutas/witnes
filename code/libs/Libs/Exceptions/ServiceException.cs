
using Libs.Models;

namespace Libs.Exceptions;

/// <summary>
/// 
/// </summary>
public class ServiceException : Exception
{
    private readonly List<ServiceError> _serviceErrors;

    /// <summary>
    /// Initializes a new instance of ServiceException with error message and object in question
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <param name="objectInQuestion">The object that caused the error</param>
    public ServiceException(string errorMessage, object? objectInQuestion) : base("Service Exception")
    {
        _serviceErrors = [new ServiceError(errorMessage, objectInQuestion)];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceError"></param>
    /// <returns></returns>
    public ServiceException(ServiceError serviceError) : base("Service Exception")
    {
        _serviceErrors = [serviceError];
    }

    /// <summary>
    /// Initializes a new instance of ServiceException with no errors
    /// </summary>
    public ServiceException() : base("Service Exception")
    {
        _serviceErrors = [];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceErrors"></param>
    /// <returns></returns>
    public ServiceException(IEnumerable<ServiceError> serviceErrors) : base("Service Exception")
    {
        _serviceErrors = serviceErrors.ToList();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string[] GetErrors()
    {
        return _serviceErrors.Select(x => x.ToString()).ToArray();
    }

    /// <summary>
    /// Adds a new Service Error to the list of Errors.
    /// </summary>
    /// <param name="serviceError">The new service error</param>
    public void AddServiceError(ServiceError serviceError)
    {
        _serviceErrors.Add(serviceError);
    }
}
