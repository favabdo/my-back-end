namespace NileTechno.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"\"{name}\" بالمعرّف ({key}) غير موجود.") { }

    public NotFoundException(string message) : base(message) { }
}
