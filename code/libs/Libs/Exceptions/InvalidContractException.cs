namespace Libs.Exceptions;

/// <summary>
/// 
/// </summary>
public class InvalidContractException : Exception
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public InvalidContractException() : base()
    {
    }

    ///<Summary>
    /// Message only constructor
    ///</Summary>
    public InvalidContractException(string message) : base(message) { }

    /// <summary>
    /// Message and inner exception constructor
    /// </summary>
    public InvalidContractException(string message, Exception inner) : base(message, inner) { }
}
