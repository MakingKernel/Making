using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using Mark.MiniApis.Analyzers.Constants;
using Mark.MiniApis.Analyzers.Models;

namespace Mark.MiniApis.Analyzers.Services
{
    internal static class SymbolAnalysisService
    {
        public static bool HasMiniApiAttribute(INamedTypeSymbol classSymbol)
        {
            var attributes = classSymbol.GetAttributes();
            return attributes.Any(a => 
                a.AttributeClass?.Name == MiniApiConstants.MiniApiAttributeName && 
                a.AttributeClass?.ContainingNamespace?.ToDisplayString() == "Mark.AspNetCore");
        }

        public static bool HasIgnoreRouteAttribute(IMethodSymbol methodSymbol)
        {
            return methodSymbol.GetAttributes().Any(a => 
                a.AttributeClass?.Name == MiniApiConstants.IgnoreRouteAttributeName && 
                a.AttributeClass?.ContainingNamespace?.ToDisplayString() == "Mark.AspNetCore");
        }

        public static string GetFullNamespace(INamespaceSymbol? namespaceSymbol)
        {
            if (namespaceSymbol == null || namespaceSymbol.IsGlobalNamespace)
                return string.Empty;

            var parts = new System.Collections.Generic.Stack<string>();
            var current = namespaceSymbol;
            while (current is { IsGlobalNamespace: false })
            {
                parts.Push(current.Name);
                current = current.ContainingNamespace;
            }

            return string.Join(".", parts);
        }

        public static object? GetAttributeProperty(AttributeData attribute, string propertyName)
        {
            return attribute.NamedArguments.FirstOrDefault(n => n.Key == propertyName).Value.Value;
        }

        public static ClassInfo? CreateClassInfo(INamedTypeSymbol classSymbol)
        {
            var namespaceName = GetFullNamespace(classSymbol.ContainingNamespace);
            var className = classSymbol.Name;
            var attributes = classSymbol.GetAttributes();
            
            var methods = classSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.DeclaredAccessibility == Accessibility.Public && 
                            !m.IsStatic &&
                            m.MethodKind == MethodKind.Ordinary &&
                            !m.IsImplicitlyDeclared)
                .ToList();

            var miniApiAttr = attributes.FirstOrDefault(a => 
                a.AttributeClass?.Name == MiniApiConstants.MiniApiAttributeName && 
                a.AttributeClass?.ContainingNamespace?.ToDisplayString() == "Mark.AspNetCore");

            if (miniApiAttr == null)
                return null;

            var route = GetAttributeProperty(miniApiAttr, "Route") as string ??
                       miniApiAttr.ConstructorArguments.FirstOrDefault().Value as string ??
                       $/"/{className.TrimEnd(MiniApiConstants.ServiceSuffix).ToLowerInvariant()}";

            var tags = GetAttributeProperty(miniApiAttr, "Tags") as string;

            var filterAttributes = attributes.Where(a =>
                a.AttributeClass?.Name == MiniApiConstants.FilterAttributeName &&
                a.AttributeClass?.ContainingNamespace?.ToDisplayString() == "Mark.AspNetCore").ToList();

            var authorizeAttributes = attributes.Where(a => 
                a.AttributeClass?.Name?.StartsWith("Authorize", System.StringComparison.OrdinalIgnoreCase) == true).ToList();

            return new ClassInfo
            {
                Namespace = namespaceName,
                ClassName = className,
                Route = route,
                Tags = tags,
                FilterAttributes = filterAttributes,
                AuthorizeAttributes = authorizeAttributes,
                Methods = methods
            };
        }

        public static bool IsSystemAssembly(string assemblyName)
        {
            return MiniApiConstants.SystemAssemblyPrefixes.Any(prefix => 
                assemblyName.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}