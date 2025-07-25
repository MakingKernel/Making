using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace Making.MiniApis.Analyzers.Services
{
    internal static class TypeAnalysisService
    {
        public static bool IsSimpleType(ITypeSymbol type, Compilation compilation)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Char:
                case SpecialType.System_String:
                    return true;
            }
            
            if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                var genericTypeDef = namedType.ConstructedFrom;
                if (genericTypeDef.SpecialType == SpecialType.System_Nullable_T)
                {
                    return IsSimpleType(namedType.TypeArguments[0], compilation);
                }
            }
            
            var typeName = type.ToDisplayString();
            return typeName == "System.DateTime" || 
                   typeName == "System.DateTimeOffset" ||
                   typeName == "System.TimeSpan" ||
                   typeName == "System.Guid" ||
                   typeName == "System.Uri";
        }

        public static List<IPropertySymbol> GetPublicProperties(ITypeSymbol type)
        {
            return type.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public && 
                           p.GetMethod != null && 
                           p.SetMethod != null &&
                           !p.IsStatic)
                .ToList();
        }

        public static bool CanInstantiate(ITypeSymbol type)
        {
            if (type.TypeKind == TypeKind.Interface)
                return false;
            
            if (type.IsAbstract)
                return false;
            
            if (type.IsStatic)
                return false;
            
            if (type.TypeKind == TypeKind.Delegate)
                return false;
            
            if (type.TypeKind == TypeKind.Enum)
                return false;
            
            if (type is INamedTypeSymbol namedType)
            {
                var constructors = namedType.Constructors
                    .Where(c => c.DeclaredAccessibility == Accessibility.Public)
                    .ToList();
                
                if (!constructors.Any())
                    return false;
                
                return constructors.Any(c => c.Parameters.Length == 0 || 
                                       c.Parameters.All(p => p.HasExplicitDefaultValue));
            }
            
            return true;
        }
    }
}