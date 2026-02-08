namespace Libs.Exceptions;

/// <summary>
/// Exception type for infrastructure services (database, messaging, etc.)
/// </summary>
public class InfrastructureException : Exception
{
    public InfrastructureException(string message) : base(message)
    {
    }

    public InfrastructureException(string message, Exception inner) : base(message, inner)
    {
    }
}
