using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Mark.Factory;

/// <summary>
/// 依赖注入注册源生成器。
/// </summary>
[Generator]
public sealed class DependencyInjectionGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
        // 调试时可启用以下行以便附加调试器
        // if (!System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Launch();
#endif
        
        // 创建增量数据源，收集带有特定属性的类声明
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        // 合并编译信息和类声明
        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

        // 注册源生成器
        context.RegisterSourceOutput(compilationAndClasses, static (spc, source) => Execute(source.Left, source.Right, spc));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDecl && classDecl.AttributeLists.Count > 0;
    }

    private static INamedTypeSymbol? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
        return symbol as INamedTypeSymbol;
    }

    private static void Execute(Compilation compilation, ImmutableArray<INamedTypeSymbol?> classes, SourceProductionContext context)
    {
        if (classes.Length == 0)
        {
            return;
        }

        // 获取特性类型符号
        INamedTypeSymbol? singletonAttr = compilation.GetTypeByMetadataName("Mark.SingletonAttribute");
        INamedTypeSymbol? scopedAttr = compilation.GetTypeByMetadataName("Mark.ScopedAttribute");
        INamedTypeSymbol? transientAttr = compilation.GetTypeByMetadataName("Mark.TransientAttribute");
        INamedTypeSymbol? registerServiceAttr = compilation.GetTypeByMetadataName("Mark.RegisterServiceAttribute");

        if (singletonAttr is null && scopedAttr is null && transientAttr is null)
        {
            return; // 未找到特性定义，提前退出
        }

        var registrations = new List<(string Lifetime, string ImplementationType, string? ServiceType)>();
        var seen = new HashSet<string>();

        // 1. 处理收集到的类型
        foreach (var typeSymbol in classes)
        {
            if (typeSymbol is not null)
            {
                TryAdd(typeSymbol);
            }
        }

        // 2. 处理所有引用的程序集（包括自身 Assembly），以支持扫描其他项目/库中的类型
        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            TraverseNamespace(assembly.GlobalNamespace);
        }

        // 也扫描当前 Assembly 内未被收集器捕获的类型
        TraverseNamespace(compilation.Assembly.GlobalNamespace);

        if (registrations.Count == 0)
            return;

        // 获取当前项目名称，用于生成唯一的文件名和方法名
        string projectName = GetProjectName(compilation.AssemblyName);
        GenerateSource(context, registrations, projectName);

        // 本地函数：尝试添加注册项
        void TryAdd(INamedTypeSymbol typeSymbol)
        {
            string? lifetime = GetLifetime(typeSymbol, singletonAttr, scopedAttr, transientAttr);
            if (lifetime is null)
                return;

            string implType = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (seen.Add(implType))
            {
                // 获取服务类型
                string? serviceType = GetServiceType(typeSymbol, registerServiceAttr);
                registrations.Add((lifetime, implType, serviceType));
            }
        }

        // 本地函数：递归遍历命名空间中的所有类型
        void TraverseNamespace(INamespaceSymbol ns)
        {
            foreach (var member in ns.GetMembers())
            {
                if (member is INamespaceSymbol childNs)
                {
                    TraverseNamespace(childNs);
                }
                else if (member is INamedTypeSymbol type)
                {
                    TryAdd(type);
                }
            }
        }
    }

    private static string? GetLifetime(INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? singletonAttr,
        INamedTypeSymbol? scopedAttr,
        INamedTypeSymbol? transientAttr)
    {
        foreach (var attr in typeSymbol.GetAttributes())
        {
            if (singletonAttr is not null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, singletonAttr))
                return "Singleton";
            if (scopedAttr is not null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, scopedAttr))
                return "Scoped";
            if (transientAttr is not null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, transientAttr))
                return "Transient";
        }

        return null;
    }

    private static string? GetServiceType(INamedTypeSymbol typeSymbol, INamedTypeSymbol? registerServiceAttr)
    {
        // 1. 首先检查是否有 RegisterServiceAttribute 指定的服务类型
        if (registerServiceAttr is not null)
        {
            foreach (var attr in typeSymbol.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, registerServiceAttr))
                {
                    if (attr.ConstructorArguments.Length > 0)
                    {
                        var serviceTypeArg = attr.ConstructorArguments[0];
                        if (serviceTypeArg.Value is INamedTypeSymbol serviceType)
                        {
                            return serviceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        }
                    }
                }
            }
        }

        // 2. 自动匹配接口：AService 匹配 IAService（A 匹配 B 的完整后缀名）
        string typeName = typeSymbol.Name;
        foreach (var interfaceType in typeSymbol.AllInterfaces)
        {
            string interfaceName = interfaceType.Name;

            // 检查接口名是否以 'I' 开头，并且去掉 'I' 后与类名完全匹配
            if (interfaceName.StartsWith("I") && interfaceName.Length > 1)
            {
                string expectedTypeName = interfaceName.Substring(1); // 去掉 'I'
                // 要求完整匹配：AService 匹配 IAService，而不是 SomeAService 匹配 IAService
                if (typeName == expectedTypeName)
                {
                    return interfaceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }
            }
        }

        // 3. 没有匹配的接口，返回 null（注册为自身类型）
        return null;
    }

    private static void GenerateSource(SourceProductionContext context,
        List<(string Lifetime, string ImplementationType, string? ServiceType)> regs,
        string projectName)
    {
        // 清理项目名，生成更清晰的类名和方法名
        string cleanProjectName = CleanProjectName(projectName);
        string className = $"Generated{cleanProjectName}ServiceExtensions";
        string methodName = $"Add{cleanProjectName}Services";
        string fileName = $"{className}.g.cs";

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// This code was generated by Mark.Factory.DependencyInjectionGenerator");
        sb.AppendLine();
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("namespace Mark.Factory");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// 由 DependencyInjectionGenerator 为 {cleanProjectName} 项目自动生成的服务注册扩展方法。");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public static class {className}");
        sb.AppendLine("    {");
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// 注册 {cleanProjectName} 项目中源码生成器扫描到的服务。");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        /// <param name=\"services\">服务集合</param>");
        sb.AppendLine($"        /// <returns>服务集合，支持链式调用</returns>");
        sb.AppendLine($"        public static IServiceCollection {methodName}(this IServiceCollection services)");
        sb.AppendLine("        {");

        foreach (var (lifetime, implementationType, serviceType) in regs)
        {
            if (serviceType is not null)
            {
                // 注册为接口和实现类：services.AddScoped<IService, ServiceImpl>();
                sb.AppendLine($"            services.Add{lifetime}<{serviceType}, {implementationType}>();");
            }
            else
            {
                // 注册为自身类型：services.AddScoped<ServiceImpl>();
                sb.AppendLine($"            services.Add{lifetime}<{implementationType}>();");
            }
        }

        sb.AppendLine();
        sb.AppendLine("            return services;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource(fileName, SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static string CleanProjectName(string projectName)
    {
        if (string.IsNullOrEmpty(projectName))
            return "Unknown";

        // 移除常见的分隔符和特殊字符，转换为PascalCase
        var cleanName = projectName
            .Replace(".", "")
            .Replace("-", "")
            .Replace("_", "")
            .Replace(" ", "");

        // 确保以字母开头
        if (cleanName.Length > 0 && !char.IsLetter(cleanName[0]))
        {
            cleanName = "Project" + cleanName;
        }

        return string.IsNullOrEmpty(cleanName) ? "Unknown" : cleanName;
    }

    private static string GetProjectName(string? assemblyName)
    {
        if (string.IsNullOrEmpty(assemblyName))
            return "Unknown";

        // 移除常见的后缀和特殊字符，保留有效的标识符字符
        string projectName = assemblyName;

        // 移除常见后缀
        string[] suffixesToRemove = { ".dll", ".exe" };
        foreach (var suffix in suffixesToRemove)
        {
            if (projectName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                projectName = projectName.Substring(0, projectName.Length - suffix.Length);
            }
        }

        // 替换无效字符为下划线，确保是有效的 C# 标识符
        var validChars = projectName.ToCharArray();
        for (int i = 0; i < validChars.Length; i++)
        {
            if (!char.IsLetterOrDigit(validChars[i]) && validChars[i] != '_')
            {
                validChars[i] = '_';
            }
        }

        projectName = new string(validChars);

        // 确保以字母或下划线开头
        if (projectName.Length > 0 && !char.IsLetter(projectName[0]) && projectName[0] != '_')
        {
            projectName = "_" + projectName;
        }

        return string.IsNullOrEmpty(projectName) ? "Unknown" : projectName;
    }

}