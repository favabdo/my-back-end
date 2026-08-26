namespace NileTechno.Application.Common.Exceptions;

public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException() : base("ليس لديك صلاحية للقيام بهذه العملية.") { }
}
