using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using NooberCong.DynamicInclude.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace NooberCong.DynamicInclude.Extensions;

public static class IQueryableExtensions
{
    public static IQueryable<T> DynamicInclude<T>(this IQueryable<T> query, string include) where T : class
    {
        var allIncludePathSpecs = ParseInlude<T>(include);
        object curQuery = query;

        foreach (var includePathSpecs in allIncludePathSpecs)
        {
            var isFirstInclude = true;
            var prevCollection = false;
            Type prevType = typeof(T);
            foreach (var (type, propName, isCollection) in includePathSpecs)
            {
                var includeMethod = isFirstInclude ? GetIncludeMethodInfo(typeof(T), propName) :
                    prevCollection ? GetThenIncludeAfterEnumerableMethodInfo(typeof(T), prevType, propName) :
                    GetThenIncludeAfterReferenceMethodInfo(typeof(T), prevType, propName);

                curQuery = includeMethod.Invoke(null,
                    new object[] {curQuery, GenerateGetterExpression(prevType, propName)});

                prevType = type;
                prevCollection = isCollection;
                isFirstInclude = false;
            }
        }

        return (IQueryable<T>) curQuery;
    }

    private static IEnumerable<ICollection<(Type Type, string PropertyName, bool IsCollection)>> ParseInlude<T>(
        string include)
    {
        var allPathSpecs = new List<ICollection<(Type, string, bool)>>();

        foreach (var absoluteIncludePathPropNames in ParseAbsoluteIncludePaths(include))
        {
            var includePathSpecs = new List<(Type, string, bool)>();

            var curType = typeof(T);

            foreach (var propName in absoluteIncludePathPropNames)
            {
                EnsureIncludable(curType, propName);

                var propInfo = FindInstancePropertyByNameIgnoreCase(curType, propName);

                var isCollection = propInfo.PropertyType.GetInterface(nameof(IEnumerable)) != null;
                // Get inner generic type if property is a collection
                var propType = isCollection ? propInfo.PropertyType.GenericTypeArguments[0] : propInfo.PropertyType;

                includePathSpecs.Add((propType, propInfo.Name, isCollection));

                curType = propType;
            }

            allPathSpecs.Add(includePathSpecs);
        }

        return allPathSpecs;
    }

    private static void EnsureIncludable(Type declarerType, string propertyName)
    {
        // Get inner type if declarer is a collection
        if (declarerType.GetInterface(nameof(IEnumerable)) != null)
        {
            declarerType = declarerType.GenericTypeArguments[0];
        }

        var propInfo = FindInstancePropertyByNameIgnoreCase(declarerType, propertyName);

        // Not a property or is not includable
        if (propInfo == null || propInfo.GetCustomAttribute<NotMappedAttribute>() != null)
        {
            throw new InvalidIncludeExpressionException(declarerType, propertyName);
        }
    }

    private static PropertyInfo? FindInstancePropertyByNameIgnoreCase(Type entityType, string propName)
    {
        return entityType.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
    }

    private static ICollection<IEnumerable<string>> ParseAbsoluteIncludePaths(string expr,
        ICollection<IEnumerable<string>> includePaths = null, IList<string> includePath = null)
    {
        includePaths ??= new List<IEnumerable<string>>();
        includePath ??= new List<string>();

        // Remove braces of multi-include, eg: (prop1, prop2) -> prop1, prop2
        if (expr.StartsWith('('))
        {
            expr = expr.Substring(1, expr.Length - 2);
        }

        var children = new List<string>();
        // braceState = number of '(' minus number of ')', used to find outermost includes
        var braceState = 0;
        var sb = new StringBuilder();

        foreach (var ch in expr)
        {
            // End of one outermost include
            if (ch == ',' && braceState == 0)
            {
                var child = sb.ToString().Trim();
                if (child.Length > 0)
                {
                    children.Add(child);
                    sb.Clear();
                }
            }
            else
            {
                sb.Append(ch);
                if (ch == '(')
                {
                    braceState++;
                }
                else if (ch == ')')
                {
                    braceState--;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(sb.ToString()))
        {
            children.Add(sb.ToString().Trim());
        }

        // Process each child include
        foreach (var child in children)
        {
            sb.Clear();
            for (int i = 0; i < child.Length; i++)
            {
                // Nested include separator
                if (child[i] == '.')
                {
                    includePath.Add(sb.ToString().Trim());
                    ParseAbsoluteIncludePaths(child.Substring(i + 1).Trim(), includePaths, includePath);
                    // Remove above include
                    includePath.RemoveAt(includePath.Count - 1);
                    break;
                }

                sb.Append(child[i]);

                // End of include path
                if (i == child.Length - 1)
                {
                    includePath.Add(sb.ToString().Trim());
                    includePaths.Add(includePath.ToArray());
                    includePath.RemoveAt(includePath.Count - 1);
                }
            }
        }

        return includePaths;
    }

    private static object GenerateGetterExpression(Type entityType, string propName)
    {
        var propInfo = entityType.GetProperty(propName);
        var objParameterExpr = Expression.Parameter(entityType);
        var propertyExpr = Expression.Property(objParameterExpr, propInfo);
        return Expression.Lambda(typeof(Func<,>).MakeGenericType(entityType, propInfo.PropertyType), propertyExpr,
            objParameterExpr);
    }

    private static MethodInfo GetIncludeMethodInfo(Type entityType, string propertyName)
    {
        var propInfo = entityType.GetProperty(propertyName);

        return typeof(EntityFrameworkQueryableExtensions)
            .GetTypeInfo().GetDeclaredMethods(nameof(EntityFrameworkQueryableExtensions.Include))
            .Single(
                mi =>
                    mi.GetGenericArguments().Count() == 2
                    && mi.GetParameters().Any(
                        pi => pi.Name == "navigationPropertyPath" && pi.ParameterType != typeof(string)))
            .MakeGenericMethod(entityType, propInfo.PropertyType);
    }

    private static MethodInfo GetThenIncludeAfterReferenceMethodInfo(Type entityType, Type previousType,
        string propertyName)
    {
        var propInfo = previousType.GetProperty(propertyName);

        return typeof(EntityFrameworkQueryableExtensions)
            .GetTypeInfo().GetDeclaredMethods(nameof(EntityFrameworkQueryableExtensions.ThenInclude))
            .Single(
                mi => mi.GetGenericArguments().Count() == 3
                      && mi.GetParameters()[0].ParameterType.GenericTypeArguments[1].IsGenericParameter)
            .MakeGenericMethod(entityType, previousType, propInfo.PropertyType);
    }

    private static MethodInfo GetThenIncludeAfterEnumerableMethodInfo(Type entityType, Type previousType,
        string propertyName)
    {
        var propInfo = previousType.GetProperty(propertyName);

        return typeof(EntityFrameworkQueryableExtensions)
            .GetTypeInfo().GetDeclaredMethods(nameof(EntityFrameworkQueryableExtensions.ThenInclude))
            .Single(
                mi => mi.GetGenericArguments().Count() == 3
                      && mi.GetParameters()[0].ParameterType.GenericTypeArguments[1].IsGenericParameter)
            .MakeGenericMethod(entityType, previousType, propInfo.PropertyType);
    }
}