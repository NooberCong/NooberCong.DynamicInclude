namespace NooberCong.DynamicInclude.Exceptions;

public class NotAPropertyException : Exception
{
    public NotAPropertyException(Type type, string propName) : base($"{propName} is not a property of {type.Name}")
    {
    }
}