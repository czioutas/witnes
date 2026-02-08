namespace Libs.Exceptions;

///<Summary>
/// Exception Type for handled cases.
///</Summary>
public class ResourceNotFoundException : Exception
{
    ///<Summary>
    /// Parameter-less constructor
    ///</Summary>
    public ResourceNotFoundException() : base("Resource not found.") { }

    ///<Summary>
    /// Specify the resource that was not found
    ///</Summary>
    public ResourceNotFoundException(string? resourceSpecific, string id) : base($"Resource: [{resourceSpecific}] with {id} not found.") { }

    ///<Summary>
    /// Specify the resource that was not found
    ///</Summary>
    public ResourceNotFoundException(string? resourceSpecific, Guid id) : base($"Resource: [{resourceSpecific}] with {id.ToString()} not found.") { }

    public static void ThrowIfNull(object? o)
    {
        throw new ResourceNotFoundException();
    }
}
