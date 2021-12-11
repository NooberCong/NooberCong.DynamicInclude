namespace NooberCong.DynamicInclude.Exceptions
{
    public class InvalidIncludeExpressionException : Exception
    {
        public InvalidIncludeExpressionException(Type declarerType, string propName) : base($"{propName} is not an includable property of {declarerType.Name}")
        {
        }
    }

    public class InvalidIncludeExpressionException<T> : InvalidIncludeExpressionException where T : class
    {
        public InvalidIncludeExpressionException(string propName) : base(typeof(T), propName)
        {
            
        }
    }
}
