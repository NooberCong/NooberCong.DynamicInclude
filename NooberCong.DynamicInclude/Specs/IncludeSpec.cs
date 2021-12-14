using System.Collections;
using System.Linq.Expressions;
using NooberCong.DynamicInclude.Utils;

namespace NooberCong.DynamicInclude.Specs;

internal class IncludeSpec
{
    internal Type Type { get; set; }
    internal bool IsEnumerable { get; set; }
    internal Expression IncludeExpression { get; set; }

    private IncludeSpec()
    {
    }

    internal static IncludeSpec Parse(Type declarerType, string expr)
    {
        var (propName, orderBys, skip, take) = StringUtils.ParseIncludeExpression(expr);

        var includePropInfo = ReflectionUtils.FindInstancePropertyByNameIgnoreCase(declarerType, propName);
        var isEnumerable = includePropInfo.PropertyType.GetInterface(nameof(IEnumerable)) != null;

        var includePropType = includePropInfo.PropertyType.TryGetInnerType();

        // OrderBys are only applicable to collection types
        if (!isEnumerable && orderBys.Any())
        {
            throw new Exception();
        }

        var orderSpecs = orderBys.Select(ob => OrderSpec.Parse(includePropType, ob));

        var declarerParam = Expression.Parameter(declarerType);
        var includePropExpression = Expression.Property(declarerParam, includePropInfo);

        Expression curExpression = includePropExpression;
        bool isFirstOrder = true;
        foreach (var spec in orderSpecs)
        {
            var orderMethodName = isFirstOrder ? spec.IsAscending ? nameof(Enumerable.OrderBy) :
                nameof(Enumerable.OrderByDescending) :
                spec.IsAscending ? nameof(Enumerable.ThenBy) : nameof(Enumerable.ThenByDescending);

            curExpression = curExpression.Order(spec.OrderExpression, orderMethodName);
            isFirstOrder = false;
        }

        if (skip.HasValue)
        {
            curExpression = curExpression.Skip(includePropType, skip.Value);
        }

        if (take.HasValue)
        {
            curExpression = curExpression.Take(includePropType, take.Value);
        }

        var finalType = curExpression switch
        {
            MemberExpression => includePropInfo.PropertyType,
            MethodCallExpression metExr => metExr.Method.ReturnType,
            _ => throw new Exception()
        };

        curExpression = Expression.Lambda(
            typeof(Func<,>).MakeGenericType(declarerType, finalType),
            curExpression,
            declarerParam
        );

        return new IncludeSpec
        {
            Type = includePropType,
            IsEnumerable = isEnumerable,
            IncludeExpression = curExpression
        };
    }
}