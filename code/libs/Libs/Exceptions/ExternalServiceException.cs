namespace Libs.Exceptions;

///<Summary>
/// Exception Type for external services.
///</Summary>
public class ExternalServiceException : Exception
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public ExternalServiceException(string message) : base(message)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="inner"></param>
    /// <returns></returns>
    public ExternalServiceException(string message, Exception inner) : base(message, inner)
    {
    }
}
