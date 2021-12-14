using System.Linq.Expressions;
using NooberCong.DynamicInclude.Utils;

namespace NooberCong.DynamicInclude.Specs;

internal class OrderSpec
{
    public Expression OrderExpression { get; set; }
    public bool IsAscending { get; set; }

    private OrderSpec()
    {
    }

    internal static OrderSpec Parse(Type declarerType, string expr)
    {
        if (string.IsNullOrWhiteSpace(expr) || expr.Trim() == "-" || expr.Trim() == "+")
        {
            throw new Exception("Order expression must not be empty");
        }

        var isAscending = expr.First() != '-';

        return new OrderSpec
        {
            OrderExpression = ReflectionUtils.GenerateGetterExpression(declarerType, expr.Trim('-', '+')),
            IsAscending = isAscending
        };
    }
}