using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NooberCong.DynamicInclude.Exceptions;
using NooberCong.DynamicInclude.Specs;

namespace NooberCong.DynamicInclude.Utils;

internal static class ReflectionUtils
{
    internal static PropertyInfo? FindInstancePropertyByNameIgnoreCase(Type entityType, string propName)
    {
        return entityType.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
    }

    internal static Type TryGetInnerType(this Type type)
    {
        return type.GetInterface(nameof(IEnumerable)) == null ? type : type.GenericTypeArguments[0];
    }

    internal static Expression GenerateGetterExpression(Type declarerType, string navigationPath)
    {
        Type curType = declarerType;
        ParameterExpression paramExpr = Expression.Parameter(declarerType);
        Expression curExpr = paramExpr;

        foreach (var propName in navigationPath.Split('.',
                     StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var propInfo = curType.GetProperty(propName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (propInfo == null)
            {
                throw new NotAPropertyException(curType, propName);
            }

            curExpr = Expression.Property(curExpr, propInfo);
            curType = propInfo.PropertyType;
        }

        return Expression.Lambda(typeof(Func<,>).MakeGenericType(declarerType, curType), curExpr, paramExpr);
    }

    internal static MethodInfo GetIncludeMethodInfo(Type entityType, Type includeType)
    {
        return typeof(EntityFrameworkQueryableExtensions)
            .GetTypeInfo().GetDeclaredMethods(nameof(EntityFrameworkQueryableExtensions.Include))
            .Single(
                mi =>
                    mi.GetGenericArguments().Count() == 2
                    && mi.GetParameters().Any(
                        pi => pi.Name == "navigationPropertyPath" && pi.ParameterType != typeof(string)))
            .MakeGenericMethod(entityType, includeType);
    }

    internal static MethodInfo GetThenIncludeAfterReferenceMethodInfo(Type entityType, Type previousType,
        Type includeType)
    {
        return typeof(EntityFrameworkQueryableExtensions)
            .GetTypeInfo().GetDeclaredMethods(nameof(EntityFrameworkQueryableExtensions.ThenInclude))
            .Single(
                mi => mi.GetGenericArguments().Count() == 3
                      && mi.GetParameters()[0].ParameterType.GenericTypeArguments[1].IsGenericParameter)
            .MakeGenericMethod(entityType, previousType, includeType);
    }

    internal static MethodInfo GetThenIncludeAfterEnumerableMethodInfo(Type entityType, Type previousType,
        Type includeType)
    {
        return typeof(EntityFrameworkQueryableExtensions)
            .GetTypeInfo().GetDeclaredMethods(nameof(EntityFrameworkQueryableExtensions.ThenInclude))
            .Where(mi => mi.GetGenericArguments().Count() == 3)
            .Single(
                mi =>
                {
                    var typeInfo = mi.GetParameters()[0].ParameterType.GenericTypeArguments[1];
                    return typeInfo.IsGenericType
                           && typeInfo.GetGenericTypeDefinition() == typeof(IEnumerable<>);
                }).MakeGenericMethod(entityType, previousType, includeType);
    }

    internal static Expression Order(this Expression sourceExpression, Expression orderExpression,
        string orderMethodName)
    {
        return Expression.Call(
            typeof(Enumerable),
            orderMethodName,
            new Type[]
            {
                orderExpression.GetType().GenericTypeArguments[0].GenericTypeArguments[0],
                orderExpression.GetType().GenericTypeArguments[0].GenericTypeArguments[1]
            },
            sourceExpression,
            orderExpression
        );
    }

    internal static Expression Skip(this Expression sourceExpression, Type sourceType, int count)
    {
        return Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Skip),
            new Type[]
            {
                sourceType
            },
            sourceExpression,
            Expression.Constant(count, typeof(int))
        );
    }

    internal static Expression Take(this Expression sourceExpression, Type sourceType, int count)
    {
        return Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Take),
            new Type[]
            {
                sourceType
            },
            sourceExpression,
            Expression.Constant(count, typeof(int))
        );
    }
}