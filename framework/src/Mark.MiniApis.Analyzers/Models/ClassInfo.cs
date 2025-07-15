using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Mark.MiniApis.Analyzers.Models
{
    internal sealed class ClassInfo
    {
        public string Namespace { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string? Tags { get; set; }
        public List<AttributeData> AuthorizeAttributes { get; set; } = new();
        public List<AttributeData> FilterAttributes { get; set; } = new();
        public List<IMethodSymbol> Methods { get; set; } = new();
    }
}