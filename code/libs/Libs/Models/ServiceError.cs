namespace Libs.Models;
/// <summary>
/// 
/// </summary>
public class ServiceError
{
    /// <summary>
    /// 
    /// </summary>
    /// <value></value>
    public string ErrorMessage { get; }

    public object? ObjectInQuestion { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="errorMessage"></param>
    /// <param name="objectInQuestion"></param>
    public ServiceError(string errorMessage, object? objectInQuestion = null)
    {
        ErrorMessage = errorMessage;
        ObjectInQuestion = objectInQuestion;
    }

    public override string ToString()
    {
        return "[" + ObjectInQuestion + "] " + ErrorMessage;
    }
}
