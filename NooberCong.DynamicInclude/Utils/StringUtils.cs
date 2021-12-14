using System.Text;
using NooberCong.DynamicInclude.Exceptions;

namespace NooberCong.DynamicInclude.Utils;

internal static class StringUtils
{
    internal static (string, IEnumerable<string>, int?, int?) ParseIncludeExpression(string expression)
    {
        var propNameAndOrderBys = expression;
        int? startIdx = default;
        int? endIdx = default;
        string[]? splitParts;

        if (expression.Contains('['))
        {
            splitParts = expression.Split('[');

            if (splitParts.Length != 2)
            {
                throw new Exception("Only one square bracket is expected when include expression has skip/take");
            }

            propNameAndOrderBys = splitParts[0];
            var skipTake = splitParts[1].Trim(']').Split(':', StringSplitOptions.TrimEntries);

            if (skipTake.Length != 2)
            {
                throw new Exception(
                    "Invalid skip/take exception syntax: [skip:skip+take] eg: [3:5] to skip 3 and take 2");
            }

            if (int.TryParse(skipTake[0], out var tmp) && tmp >= 0)
            {
                startIdx = tmp;
            }
            else if (!string.IsNullOrWhiteSpace(skipTake[0]))
            {
                throw new Exception("Skip must be a positive integer");
            }

            if (int.TryParse(skipTake[1], out tmp) && tmp >= 0)
            {
                endIdx = tmp;
            }
            else if (!string.IsNullOrWhiteSpace(skipTake[1]))
            {
                throw new Exception("Take must be a positive interger");
            }

            if (!startIdx.HasValue && !endIdx.HasValue)
            {
                throw new Exception(
                    "Either skip or take must be specified when include expression has skip/take square brackets");
            }

            if (startIdx.HasValue && endIdx.HasValue && startIdx > endIdx)
            {
                throw new Exception("End index must be greater than start index");
            }
        }

        splitParts = propNameAndOrderBys.Split('<', StringSplitOptions.TrimEntries);

        if (splitParts.Length > 2)
        {
            throw new Exception("Only one < is expected when include has order");
        }

        var orderBys = splitParts.Length > 1
            ? splitParts[1].Trim('>')
                .Split(',', StringSplitOptions.TrimEntries)
            : Array.Empty<string>();

        return (splitParts[0], orderBys, startIdx,
            endIdx.HasValue ? startIdx.HasValue ? endIdx - startIdx : endIdx : null);
    }

    internal static ICollection<IEnumerable<string>> ParseAbsoluteIncludePaths(string expr,
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
        // orderState = number of '<' minus number of '>' used to separate the order expression of an include
        var orderState = 0;
        var sb = new StringBuilder();

        foreach (var ch in expr)
        {
            // End of one outermost include
            if (ch == ',' && braceState == 0 && orderState == 0)
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
                else if (ch == '<')
                {
                    orderState++;
                }
                else if (ch == '>')
                {
                    orderState--;
                }
            }
        }

        if (braceState != 0 || orderState != 0)
        {
            throw new Exception("Number of opening and closing brackets of () and <> must be equal");
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
                if (child[i] == '.' && orderState == 0)
                {
                    includePath.Add(sb.ToString().Trim());
                    ParseAbsoluteIncludePaths(child.Substring(i + 1).Trim(), includePaths, includePath);
                    // Remove above include
                    includePath.RemoveAt(includePath.Count - 1);
                    break;
                }

                sb.Append(child[i]);

                if (child[i] == '<')
                {
                    orderState++;
                }
                else if (child[i] == '>')
                {
                    orderState--;
                }

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
}