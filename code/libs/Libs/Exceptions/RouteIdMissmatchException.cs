namespace Libs.Exceptions;

///<Summary>
/// Exception Type for handled cases.
///</Summary>
public class RouteIdMissmatchException : Exception
{
    ///<Summary>
    /// Parameter-less constructor
    ///</Summary>
    public RouteIdMissmatchException() : base("Resource conflict") { }

    ///<Summary>
    /// Id missmatch
    ///</Summary>
    public RouteIdMissmatchException(Guid routeId, Guid actualId) : base($"Id missmatch route: {routeId.ToString()} with actual: {actualId.ToString()}") { }

    public static void ThrowIfNull(object? o)
    {
        throw new RouteIdMissmatchException();
    }
}
