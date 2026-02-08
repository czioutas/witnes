namespace Libs.Exceptions;

///<Summary>
/// Exception Type for handled cases.
///</Summary>
public class RequirementException : Exception
{
    public RequirementException() : base("requirements.not-met") { }

    public RequirementException(string key) : base(key) { }
}
