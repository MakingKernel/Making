using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Mark.MiniApis.Analyzers.Models;
using Mark.MiniApis.Analyzers.Services;

namespace Mark.MiniApis.Analyzers.Templates
{
    internal static class MethodMappingTemplate
    {
        public static string Render(IMethodSymbol method, ClassInfo classInfo)
        {
            if (SymbolAnalysisService.HasIgnoreRouteAttribute(method))
                return string.Empty;

            var sb = new StringBuilder();
            var methodName = NamingService.NormalizeMethodName(method.Name);
            var httpMethod = GetHttpMethod(method);
            
            sb.Append($"            {classInfo.ClassName.ToLowerInvariant()}.Map{httpMethod}(\"{methodName}\", ");
            sb.Append($"async ({GenerateParameterList(method)}) => ");
            sb.Append($"await {classInfo.ClassName.ToLowerInvariant()}.{method.Name}({GenerateArgumentList(method)}))");

            // Add method-level attributes
            var methodAttributes = RenderMethodAttributes(method);
            if (!string.IsNullOrEmpty(methodAttributes))
            {
                sb.Append(methodAttributes);
            }

            sb.Append(";");
            
            return sb.ToString();
        }

        private static string GetHttpMethod(IMethodSymbol method)
        {
            var attributes = method.GetAttributes();
            
            foreach (var attr in attributes)
            {
                var attrName = attr.AttributeClass?.Name;
                if (attrName != null && attrName.StartsWith("Http") && attrName.EndsWith("Attribute"))
                {
                    return attrName.Substring(4, attrName.Length - 13); // Remove "Http" prefix and "Attribute" suffix
                }
            }
            
            // Default to POST for methods with parameters, GET for parameterless methods
            return method.Parameters.Length > 0 ? "Post" : "Get";
        }

        private static string GenerateParameterList(IMethodSymbol method)
        {
            if (method.Parameters.Length == 0)
                return "";

            var parameters = new List<string>();
            foreach (var param in method.Parameters)
            {
                parameters.Add($"{param.Type.ToDisplayString()} {param.Name}");
            }
            
            return string.Join(", ", parameters);
        }

        private static string GenerateArgumentList(IMethodSymbol method)
        {
            if (method.Parameters.Length == 0)
                return "";

            var arguments = new List<string>();
            foreach (var param in method.Parameters)
            {
                arguments.Add(param.Name);
            }
            
            return string.Join(", ", arguments);
        }

        private static string RenderMethodAttributes(IMethodSymbol method)
        {
            var sb = new StringBuilder();
            var attributes = method.GetAttributes();

            // Add authorization attributes
            var authorizeAttrs = attributes.Where(a => 
                a.AttributeClass?.Name?.StartsWith("Authorize", System.StringComparison.OrdinalIgnoreCase) == true).ToList();
            
            if (authorizeAttrs.Any())
            {
                foreach (var authAttr in authorizeAttrs)
                {
                    var roles = SymbolAnalysisService.GetAttributeProperty(authAttr, "Roles") as string;
                    var policy = SymbolAnalysisService.GetAttributeProperty(authAttr, "Policy") as string;
                    
                    if (!string.IsNullOrEmpty(roles) && !string.IsNullOrEmpty(policy))
                    {
                        sb.Append($".RequireAuthorization(p => p.RequireRole(\"{roles}\").RequirePolicy(\"{policy}\"))");
                    }
                    else if (!string.IsNullOrEmpty(roles))
                    {
                        sb.Append($".RequireAuthorization(p => p.RequireRole(\"{roles}\"))");
                    }
                    else if (!string.IsNullOrEmpty(policy))
                    {
                        sb.Append($".RequireAuthorization(\"{policy}\")");
                    }
                    else
                    {
                        sb.Append(".RequireAuthorization()");
                    }
                }
            }

            return sb.ToString();
        }
    }
}