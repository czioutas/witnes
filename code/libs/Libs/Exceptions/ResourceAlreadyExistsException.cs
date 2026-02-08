namespace Libs.Exceptions;

///<Summary>
/// Exception Type.
///</Summary>
public class ResourceAlreadyExistsException : Exception
{
    ///<Summary>
    /// Message only constructor
    ///</Summary>
    public ResourceAlreadyExistsException() : base() { }

    ///<Summary>
    /// Message only constructor
    ///</Summary>
    public ResourceAlreadyExistsException(string message) : base(message) { }
}
