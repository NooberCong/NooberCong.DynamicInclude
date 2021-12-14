using NooberCong.DynamicInclude.Specs;
using NooberCong.DynamicInclude.Utils;

namespace NooberCong.DynamicInclude.Extensions;

public static class DynamicIncludeQueryableExtensions
{
    public static IQueryable<T> DynamicInclude<T>(this IQueryable<T> query, string include) where T : class
    {
        var allIncludePathSpecs = ParseInlude<T>(include);
        object curQuery = query;

        foreach (var includePathSpecs in allIncludePathSpecs)
        {
            var isFirstInclude = true;
            var prevEnumerable = false;
            Type prevType = typeof(T);

            foreach (var includeSpec in includePathSpecs)
            {
                var includeType = includeSpec.IncludeExpression.GetType().GenericTypeArguments[0]
                    .GenericTypeArguments[1];

                var includeMethod = isFirstInclude
                    ? ReflectionUtils.GetIncludeMethodInfo(typeof(T), includeType)
                    : prevEnumerable
                        ? ReflectionUtils.GetThenIncludeAfterEnumerableMethodInfo(typeof(T), prevType,
                            includeType)
                        : ReflectionUtils.GetThenIncludeAfterReferenceMethodInfo(typeof(T), prevType,
                            includeType);

                curQuery = includeMethod.Invoke(null,
                    new object[]
                        {curQuery, includeSpec.IncludeExpression});


                prevType = includeSpec.Type;
                prevEnumerable = includeSpec.IsEnumerable;
                isFirstInclude = false;
            }
        }

        return (IQueryable<T>) curQuery;
    }

    private static IEnumerable<IEnumerable<IncludeSpec>> ParseInlude<T>(
        string include)
    {
        var allPathSpecs = new List<IEnumerable<IncludeSpec>>();

        foreach (var absoluteIncludePathExprs in StringUtils.ParseAbsoluteIncludePaths(include))
        {
            var includePathSpecs = new List<IncludeSpec>();

            var curType = typeof(T);

            foreach (var expr in absoluteIncludePathExprs)
            {
                var includeSpec = IncludeSpec.Parse(curType, expr);
                includePathSpecs.Add(includeSpec);

                curType = includeSpec.Type;
            }

            allPathSpecs.Add(includePathSpecs);
        }

        return allPathSpecs;
    }
}