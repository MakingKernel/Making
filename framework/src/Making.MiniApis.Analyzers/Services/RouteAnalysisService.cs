using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using Making.MiniApis.Analyzers.Constants;

namespace Making.MiniApis.Analyzers.Services
{
    internal static class RouteAnalysisService
    {
        public static (string httpMethod, string route) DetermineHttpMethodAndRoute(IMethodSymbol method)
        {
            var httpMethodAttr = method.GetAttributes().FirstOrDefault(a => 
                a.AttributeClass != null && 
                MiniApiConstants.HttpMethodAttributes.Contains(a.AttributeClass.Name) &&
                a.AttributeClass.ContainingNamespace?.ToDisplayString() == "Microsoft.AspNetCore.Mvc");

            if (httpMethodAttr != null)
            {
                var attrName = httpMethodAttr.AttributeClass!.Name;
                if (MiniApiConstants.MvcHttpMethodAttributes.TryGetValue(attrName, out var httpMethod))
                {
                    var route = httpMethodAttr.ConstructorArguments.FirstOrDefault().Value as string ?? 
                               NormalizeMethodName(method.Name);
                    return (httpMethod, route);
                }
            }

            return InferHttpMethodFromName(method.Name);
        }

        private static (string httpMethod, string route) InferHttpMethodFromName(string methodName)
        {
            var cleanName = methodName;
            if (cleanName.EndsWith(MiniApiConstants.AsyncSuffix, StringComparison.OrdinalIgnoreCase))
            {
                cleanName = cleanName.Substring(0, cleanName.Length - MiniApiConstants.AsyncSuffix.Length);
            }
            if (cleanName.EndsWith(MiniApiConstants.ServiceSuffix, StringComparison.OrdinalIgnoreCase))
            {
                cleanName = cleanName.Substring(0, cleanName.Length - MiniApiConstants.ServiceSuffix.Length);
            }
            
            var lowerName = cleanName.ToLowerInvariant();
            string routePart;
            string httpMethod;
            
            if (lowerName.StartsWith("get"))
            {
                httpMethod = "Get";
                routePart = cleanName.Substring(3);
            }
            else if (lowerName.StartsWith("remove") || lowerName.StartsWith("delete"))
            {
                httpMethod = "Delete";
                routePart = lowerName.StartsWith("remove") ? cleanName.Substring(6) : cleanName.Substring(6);
            }
            else if (lowerName.StartsWith("post") || lowerName.StartsWith("create") || 
                     lowerName.StartsWith("add") || lowerName.StartsWith("insert"))
            {
                httpMethod = "Post";
                routePart = lowerName.StartsWith("post") ? cleanName.Substring(4) :
                           lowerName.StartsWith("create") ? cleanName.Substring(6) :
                           lowerName.StartsWith("add") ? cleanName.Substring(3) : cleanName.Substring(6);
            }
            else if (lowerName.StartsWith("put") || lowerName.StartsWith("update") || lowerName.StartsWith("modify"))
            {
                httpMethod = "Put";
                routePart = lowerName.StartsWith("put") ? cleanName.Substring(3) :
                           lowerName.StartsWith("update") ? cleanName.Substring(6) : cleanName.Substring(6);
            }
            else if (lowerName.StartsWith("patch"))
            {
                httpMethod = "Patch";
                routePart = cleanName.Substring(5);
            }
            else
            {
                httpMethod = "Post";
                routePart = cleanName;
            }
            
            if (string.IsNullOrEmpty(routePart))
            {
                return (httpMethod, "");
            }
            
            var route = "/" + routePart.ToLowerInvariant();
            return (httpMethod, route);
        }

        private static string NormalizeMethodName(string methodName)
        {
            var normalized = methodName;
            
            if (normalized.EndsWith(MiniApiConstants.AsyncSuffix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(0, normalized.Length - MiniApiConstants.AsyncSuffix.Length);
            }

            if (normalized.EndsWith(MiniApiConstants.ServiceSuffix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(0, normalized.Length - MiniApiConstants.ServiceSuffix.Length);
            }

            if (!normalized.StartsWith("/"))
                normalized = "/" + normalized;

            return normalized;
        }
    }
}