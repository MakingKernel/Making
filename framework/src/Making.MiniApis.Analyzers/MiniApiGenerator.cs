using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Making.MiniApis.Analyzers
{
    [Generator(LanguageNames.CSharp)]
    public class MiniApiGenerator : IIncrementalGenerator
    {
        private const string MiniApiAttributeName = "MiniApiAttribute";
        private const string ServiceSuffix = "Service";
        private const string AsyncSuffix = "Async";

        // 常用的HTTP方法特性名称
        private static readonly HashSet<string> HttpMethodAttributes = new()
        {
            "HttpGetAttribute", "HttpPostAttribute", "HttpPutAttribute",
            "HttpDeleteAttribute", "HttpPatchAttribute", "HttpHeadAttribute", "HttpOptionsAttribute"
        };

        // 需要过滤的系统程序集前缀
        private static readonly HashSet<string> SystemAssemblyPrefixes = new()
        {
            "System", "Microsoft.", "mscorlib", "netstandard", "runtime"
        };

        // MVC HTTP方法特性映射
        private static readonly Dictionary<string, string> MvcHttpMethodAttributes = new()
        {
            { "HttpGetAttribute", "Get" },
            { "HttpPostAttribute", "Post" },
            { "HttpPutAttribute", "Put" },
            { "HttpDeleteAttribute", "Delete" },
            { "HttpPatchAttribute", "Patch" },
            { "HttpHeadAttribute", "Head" },
            { "HttpOptionsAttribute", "Options" }
        };

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 获取编译对象
            var compilationProvider = context.CompilationProvider;

            // 收集当前项目的类信息
            var classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: IsCandidateClass,
                    transform: GetSemanticTargetForGeneration)
                .Where(m => m != null)
                .Collect();

            // 注册源输出
            context.RegisterSourceOutput(
                compilationProvider.Combine(classDeclarations),
                (spc, source) =>
                {
                    var (compilation, classes) = source;

                    // 获取引用的程序集中的类型
                    var referencedClasses = GetReferencedClasses(compilation);

                    // 将当前项目和引用程序集的类合并
                    var allClasses = classes.AddRange(referencedClasses);

                    GenerateSource(spc, compilation, allClasses);
                });
        }

        private static bool IsCandidateClass(SyntaxNode node, CancellationToken _)
        {
            return node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };
        }

        private static ClassInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context,
            CancellationToken cancellationToken)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            var model = context.SemanticModel;

            // 获取类的符号信息
            if (model.GetDeclaredSymbol(classDeclaration, cancellationToken) is not INamedTypeSymbol classSymbol)
                return null;

            // 检查类是否标注了MiniApiAttribute
            if (!HasMiniApiAttribute(classSymbol))
                return null;

            return CreateClassInfo(classSymbol);
        }

        private ImmutableArray<ClassInfo> GetReferencedClasses(Compilation compilation)
        {
            var builder = ImmutableArray.CreateBuilder<ClassInfo>();

            foreach (var reference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assemblySymbol)
                    continue;

                // 过滤掉系统程序集
                if (IsSystemAssembly(assemblySymbol.Name))
                    continue;

                foreach (var type in GetAllTypes(assemblySymbol.GlobalNamespace))
                {
                    // 检查类型是否标注了MiniApiAttribute
                    if (HasMiniApiAttribute(type))
                    {
                        var classInfo = CreateClassInfo(type);
                        if (classInfo != null)
                        {
                            builder.Add(classInfo);
                        }
                    }
                }
            }

            return builder.ToImmutable();
        }

        private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol namespaceSymbol)
        {
            foreach (var member in namespaceSymbol.GetMembers())
            {
                if (member is INamedTypeSymbol namedType)
                {
                    yield return namedType;
                }
                else if (member is INamespaceSymbol childNamespace)
                {
                    foreach (var childType in GetAllTypes(childNamespace))
                    {
                        yield return childType;
                    }
                }
            }
        }

        private static bool IsSystemAssembly(string assemblyName)
        {
            return SystemAssemblyPrefixes.Any(prefix =>
                assemblyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private static bool HasMiniApiAttribute(INamedTypeSymbol classSymbol)
        {
            var attributes = classSymbol.GetAttributes();
            return attributes.Any(a =>
                a.AttributeClass?.Name == MiniApiAttributeName &&
                a.AttributeClass?.ContainingNamespace?.ToDisplayString() == "Making.AspNetCore");
        }

        private static ClassInfo? CreateClassInfo(INamedTypeSymbol classSymbol)
        {
            var namespaceName = GetFullNamespace(classSymbol.ContainingNamespace);
            var className = classSymbol.Name;
            var attributes = classSymbol.GetAttributes();

            // 获取公共非静态方法
            var methods = classSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.DeclaredAccessibility == Accessibility.Public &&
                            !m.IsStatic &&
                            m.MethodKind == MethodKind.Ordinary &&
                            !m.IsImplicitlyDeclared)
                .ToList();

            // 获取 MiniApiAttribute
            var miniApiAttr = attributes.FirstOrDefault(a =>
                a.AttributeClass?.Name == MiniApiAttributeName &&
                a.AttributeClass?.ContainingNamespace?.ToDisplayString() == "Making.AspNetCore");

            if (miniApiAttr == null)
                return null;

            // 获取路由配置
            var route = GetAttributeProperty(miniApiAttr, "Route") as string ??
                        miniApiAttr.ConstructorArguments.FirstOrDefault().Value as string ??
                        $"/{(className.EndsWith(ServiceSuffix) ? className.Substring(0, className.Length - ServiceSuffix.Length) : className).ToLowerInvariant()}";

            // 获取标签配置
            var tags = GetAttributeProperty(miniApiAttr, "Tags") as string;

            // 获取 FilterAttributes
            var filterAttributes = attributes.Where(a =>
                a.AttributeClass?.Name == "FilterAttribute" &&
                a.AttributeClass?.ContainingNamespace?.ToDisplayString() == "Making.AspNetCore").ToList();

            // 获取 AuthorizeAttributes
            var authorizeAttributes = attributes.Where(a =>
                a.AttributeClass?.Name?.StartsWith("Authorize", StringComparison.OrdinalIgnoreCase) == true).ToList();

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

        private static object? GetAttributeProperty(AttributeData attribute, string propertyName)
        {
            return attribute.NamedArguments.FirstOrDefault(n => n.Key == propertyName).Value.Value;
        }

        private static string GetFullNamespace(INamespaceSymbol? namespaceSymbol)
        {
            if (namespaceSymbol == null || namespaceSymbol.IsGlobalNamespace)
                return string.Empty;

            var parts = new Stack<string>();
            var current = namespaceSymbol;
            while (current is { IsGlobalNamespace: false })
            {
                parts.Push(current.Name);
                current = current.ContainingNamespace;
            }

            return string.Join(".", parts);
        }

        private void GenerateSource(SourceProductionContext context, Compilation compilation,
            ImmutableArray<ClassInfo?> classes)
        {
            var classInfos = classes.Where(c => c != null).Select(c => c!).ToList();

            if (!classInfos.Any())
            {
                // 生成一个调试文件来查看是否有类被找到
                var debugBuilder = new StringBuilder();
                debugBuilder.AppendLine("// <auto-generated />");
                debugBuilder.AppendLine("// Debug: No MiniApi classes found");
                debugBuilder.AppendLine($"// Total classes checked: {classes.Length}");
                context.AddSource("MiniApiExtensions.Debug.g.cs",
                    SourceText.From(debugBuilder.ToString(), Encoding.UTF8));
                return;
            }

            // 生成主扩展文件（包含 DI 注册和总的映射方法）
            GenerateMainExtensionsFile(context, classInfos);

            // 为每个类生成单独的扩展文件
            foreach (var classInfo in classInfos)
            {
                GenerateIndividualExtensionFile(context, classInfo, compilation, context.CancellationToken);
            }
        }

        private void GenerateMainExtensionsFile(SourceProductionContext context, List<ClassInfo> classInfos)
        {
            // 生成 DI 注册代码
            var diRegistration = GenerateDiRegistration(classInfos);

            // 构建主扩展文件的源代码
            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine("// <auto-generated />");
            sourceBuilder.AppendLine("// This code was generated by Making MiniApi Source Generator");
            sourceBuilder.AppendLine("#nullable enable");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("using Microsoft.AspNetCore.Builder;");
            sourceBuilder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sourceBuilder.AppendLine("using System.Linq;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("namespace Microsoft.Extensions.DependencyInjection");
            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine("    /// <summary>");
            sourceBuilder.AppendLine("    /// MiniApi extension methods for dependency injection and API mapping");
            sourceBuilder.AppendLine("    /// </summary>");
            sourceBuilder.AppendLine("    public static partial class MiniApiExtensions");
            sourceBuilder.AppendLine("    {");
            sourceBuilder.AppendLine("        /// <summary>");
            sourceBuilder.AppendLine("        /// Register all MiniApi classes with the specified service lifetime");
            sourceBuilder.AppendLine("        /// </summary>");
            sourceBuilder.AppendLine("        /// <param name=\"services\">The service collection</param>");
            sourceBuilder.AppendLine(
                "        /// <param name=\"lifetime\">The service lifetime (default: Scoped)</param>");
            sourceBuilder.AppendLine("        /// <returns>The service collection for chaining</returns>");
            sourceBuilder.AppendLine(
                "        public static IServiceCollection AddMiniApis(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine(diRegistration);
            sourceBuilder.AppendLine("            return services;");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("        /// <summary>");
            sourceBuilder.AppendLine("        /// Map all MiniApi endpoints to the web application");
            sourceBuilder.AppendLine("        /// </summary>");
            sourceBuilder.AppendLine("        /// <param name=\"webApplication\">The web application</param>");
            sourceBuilder.AppendLine("        /// <returns>The web application for chaining</returns>");
            sourceBuilder.AppendLine(
                "        public static WebApplication MapMiniApis(this WebApplication webApplication)");
            sourceBuilder.AppendLine("        {");
            foreach (var classInfo in classInfos.OrderBy(c => c.Namespace).ThenBy(c => c.ClassName))
            {
                sourceBuilder.AppendLine(
                    $"            webApplication.Map{GenerateMapMethodName(classInfo.ClassName)}();");
            }

            sourceBuilder.AppendLine("            return webApplication;");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");

            // 添加主扩展文件
            context.AddSource("MiniApiExtensions.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        private void GenerateIndividualExtensionFile(SourceProductionContext context, ClassInfo classInfo,
            Compilation compilation, CancellationToken cancellationToken)
        {
            var methodName = GenerateMapMethodName(classInfo.ClassName);
            var instanceName = GenerateInstanceName(classInfo.ClassName);
            var fileName = $"MiniApiExtensions.{methodName}.g.cs";

            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine("// <auto-generated />");
            sourceBuilder.AppendLine(
                $"// This code was generated by Making MiniApi Source Generator for {classInfo.ClassName}");
            sourceBuilder.AppendLine("#nullable enable");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("using Microsoft.AspNetCore.Builder;");
            sourceBuilder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sourceBuilder.AppendLine("using Microsoft.AspNetCore.Authorization;");
            sourceBuilder.AppendLine("using System.Linq;");

            // 添加类所在的命名空间引用
            if (!string.IsNullOrEmpty(classInfo.Namespace))
            {
                sourceBuilder.AppendLine($"using {classInfo.Namespace};");
            }

            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("namespace Microsoft.Extensions.DependencyInjection");
            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine("    /// <summary>");
            sourceBuilder.AppendLine($"    /// MiniApi extension methods for {classInfo.ClassName}");
            sourceBuilder.AppendLine("    /// </summary>");
            sourceBuilder.AppendLine("    public static partial class MiniApiExtensions");
            sourceBuilder.AppendLine("    {");
            sourceBuilder.AppendLine("        /// <summary>");
            sourceBuilder.AppendLine($"        /// Map {classInfo.ClassName} endpoints to the web application");
            sourceBuilder.AppendLine("        /// </summary>");
            sourceBuilder.AppendLine("        /// <param name=\"webApplication\">The web application</param>");
            sourceBuilder.AppendLine("        /// <returns>The web application for chaining</returns>");
            sourceBuilder.AppendLine(
                $"        public static WebApplication Map{methodName}(this WebApplication webApplication)");
            sourceBuilder.AppendLine("        {");

            sourceBuilder.AppendLine(
                $"            var {instanceName} = webApplication.MapGroup(\"{classInfo.Route}\"){GenerateClassAttributes(classInfo)};");

            foreach (var method in classInfo.Methods.OrderBy(m => m.Name))
            {
                var methodCode = GenerateMethodMapping(method, classInfo, compilation, instanceName, cancellationToken);
                if (!string.IsNullOrWhiteSpace(methodCode))
                    sourceBuilder.AppendLine(methodCode);
            }

            sourceBuilder.AppendLine("            return webApplication;");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");

            // 添加单独的扩展文件
            context.AddSource(fileName, SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        private static string GenerateDiRegistration(List<ClassInfo> classInfos)
        {
            var sb = new StringBuilder();

            // 将类信息按命名空间分组
            var classInfosByNamespace = classInfos
                .GroupBy(c => c.Namespace)
                .OrderBy(g => g.Key);

            sb.AppendLine("            switch (lifetime)");
            sb.AppendLine("            {");

            // Singleton
            sb.AppendLine("                case ServiceLifetime.Singleton:");
            foreach (var group in classInfosByNamespace)
            {
                if (!string.IsNullOrEmpty(group.Key))
                {
                    sb.AppendLine($"                    // {group.Key}");
                }

                foreach (var classInfo in group.OrderBy(c => c.ClassName))
                {
                    var fullTypeName = string.IsNullOrEmpty(classInfo.Namespace)
                        ? classInfo.ClassName
                        : $"{classInfo.Namespace}.{classInfo.ClassName}";
                    sb.AppendLine($"                    services.AddSingleton<{fullTypeName}>();");
                }
            }

            sb.AppendLine("                    break;");

            // Scoped
            sb.AppendLine("                case ServiceLifetime.Scoped:");
            foreach (var group in classInfosByNamespace)
            {
                if (!string.IsNullOrEmpty(group.Key))
                {
                    sb.AppendLine($"                    // {group.Key}");
                }

                foreach (var classInfo in group.OrderBy(c => c.ClassName))
                {
                    var fullTypeName = string.IsNullOrEmpty(classInfo.Namespace)
                        ? classInfo.ClassName
                        : $"{classInfo.Namespace}.{classInfo.ClassName}";
                    sb.AppendLine($"                    services.AddScoped<{fullTypeName}>();");
                }
            }

            sb.AppendLine("                    break;");

            // Transient
            sb.AppendLine("                case ServiceLifetime.Transient:");
            foreach (var group in classInfosByNamespace)
            {
                if (!string.IsNullOrEmpty(group.Key))
                {
                    sb.AppendLine($"                    // {group.Key}");
                }

                foreach (var classInfo in group.OrderBy(c => c.ClassName))
                {
                    var fullTypeName = string.IsNullOrEmpty(classInfo.Namespace)
                        ? classInfo.ClassName
                        : $"{classInfo.Namespace}.{classInfo.ClassName}";
                    sb.AppendLine($"                    services.AddTransient<{fullTypeName}>();");
                }
            }

            sb.AppendLine("                    break;");

            sb.AppendLine("            }");

            return sb.ToString();
        }

        private static string GenerateMapMethodName(string className)
        {
            var baseName = className.EndsWith(ServiceSuffix) ? className.Substring(0, className.Length - ServiceSuffix.Length) : className;
            return baseName.ToString();
        }

        private static string GenerateInstanceName(string className)
        {
            var baseName = className.EndsWith(ServiceSuffix) ? className.Substring(0, className.Length - ServiceSuffix.Length) : className;
            return char.ToLowerInvariant(baseName[0]) + baseName.Substring(1);
        }

        private string GenerateClassAttributes(ClassInfo classInfo)
        {
            var sb = new StringBuilder();

            // 添加类级别的过滤器
            foreach (var filterAttr in classInfo.FilterAttributes)
            {
                if (filterAttr.ConstructorArguments.Length > 0)
                {
                    var filterTypes = filterAttr.ConstructorArguments.FirstOrDefault().Values;
                    foreach (var filter in filterTypes)
                    {
                        if (filter.Value is INamedTypeSymbol filterType)
                        {
                            sb.Append($".AddEndpointFilter<{filterType.ToDisplayString()}>()");
                        }
                    }
                }
            }

            // 处理授权属性
            if (classInfo.AuthorizeAttributes.Any())
            {
                var authorizationCode = GenerateAuthorizationCode(classInfo.AuthorizeAttributes);
                if (!string.IsNullOrEmpty(authorizationCode))
                {
                    sb.Append(authorizationCode);
                }
            }

            // 添加标签
            if (!string.IsNullOrEmpty(classInfo.Tags))
            {
                sb.Append($".WithTags(\"{classInfo.Tags}\")");
            }

            return sb.ToString();
        }

        private static string GenerateAuthorizationCode(List<AttributeData> authorizeAttributes)
        {
            // 获取 AuthorizeAttribute 的 Roles 属性
            var rolesArg = authorizeAttributes
                .SelectMany(a => a.NamedArguments)
                .FirstOrDefault(n => n.Key == "Roles");

            // 获取 AuthorizeAttribute 的 Policy 属性
            var policyArg = authorizeAttributes
                .SelectMany(a => a.NamedArguments)
                .FirstOrDefault(n => n.Key == "Policy");

            if (!rolesArg.Equals(default) && !policyArg.Equals(default) &&
                rolesArg.Value.Value is string roles && policyArg.Value.Value is string policy)
            {
                return $".RequireAuthorization(p => p.RequireRole(\"{roles}\").RequirePolicy(\"{policy}\"))";
            }

            if (!rolesArg.Equals(default) && rolesArg.Value.Value is string role)
            {
                return $".RequireAuthorization(p => p.RequireRole(\"{role}\"))";
            }

            if (!policyArg.Equals(default) && policyArg.Value.Value is string policyValue)
            {
                return $".RequireAuthorization(\"{policyValue}\")";
            }

            return ".RequireAuthorization()";
        }

        private string GenerateMethodMapping(IMethodSymbol method, ClassInfo classInfo, Compilation compilation,
            string instanceName, CancellationToken cancellationToken)
        {
            // 跳过具有 IgnoreRouteAttribute 的方法
            if (method.GetAttributes().Any(a =>
                    a.AttributeClass?.Name == "IgnoreRouteAttribute" &&
                    a.AttributeClass?.ContainingNamespace?.ToDisplayString() == "Making.AspNetCore"))
                return string.Empty;

            // 确定 HTTP 方法和路由
            var (httpMethod, route) = DetermineHttpMethodAndRoute(method);

            // 获取方法级别的过滤器
            var filterAttributes = method.GetAttributes()
                .Where(a => a.AttributeClass?.Name == "FilterAttribute" &&
                            a.AttributeClass?.ContainingNamespace?.ToDisplayString() == "Making.AspNetCore")
                .ToList();

            var filterExtensions = new StringBuilder();
            foreach (var filterAttr in filterAttributes)
            {
                if (filterAttr.ConstructorArguments.Length > 0)
                {
                    var filterTypes = filterAttr.ConstructorArguments[0].Values;
                    foreach (var filter in filterTypes)
                    {
                        if (filter.Value is INamedTypeSymbol filterType)
                        {
                            filterExtensions.AppendLine(
                                $"                .AddEndpointFilter<{filterType.ToDisplayString()}>()");
                        }
                    }
                }
            }

            // 获取方法参数
            var parameters = method.Parameters;

            // 确定服务实例名称
            var serviceInstance = GenerateInstanceName(classInfo.ClassName);

            // 构建 Lambda 表达式
            var fullTypeName = string.IsNullOrEmpty(classInfo.Namespace)
                ? classInfo.ClassName
                : $"{classInfo.Namespace}.{classInfo.ClassName}";

            string lambda;

            // 检查是否是 GET 请求且有复杂类型参数
            if (httpMethod == "Get" && parameters.Length > 0)
            {
                lambda = GenerateGetMethodLambda(parameters, fullTypeName, serviceInstance, method.Name, compilation);
            }
            else
            {
                // 非 GET 请求或无参数的常规处理
                var parameterList = string.Join(", ", parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));
                var parameterNames = string.Join(", ", parameters.Select(p => p.Name));

                // 检查方法是否为异步方法
                bool isAsync = IsAsyncMethod(method);
                
                if (parameters.Length > 0)
                {
                    if (isAsync)
                    {
                        lambda = $"async ({fullTypeName} {serviceInstance}, {parameterList}) => await {serviceInstance}.{method.Name}({parameterNames})";
                    }
                    else
                    {
                        lambda = $"({fullTypeName} {serviceInstance}, {parameterList}) => {serviceInstance}.{method.Name}({parameterNames})";
                    }
                }
                else
                {
                    if (isAsync)
                    {
                        lambda = $"async ({fullTypeName} {serviceInstance}) => await {serviceInstance}.{method.Name}()";
                    }
                    else
                    {
                        lambda = $"({fullTypeName} {serviceInstance}) => {serviceInstance}.{method.Name}()";
                    }
                }
            }

            // 组合所有部分生成方法代码
            var methodCode = $@"            {instanceName}.Map{httpMethod}(""{route}"",
                {lambda}){filterExtensions};";

            return methodCode;
        }

        private string GenerateGetMethodLambda(ImmutableArray<IParameterSymbol> parameters, string fullTypeName,
            string serviceInstance, string methodName, Compilation compilation)
        {
            var simpleParams = new List<string>();
            var complexParams = new List<(IParameterSymbol param, List<IPropertySymbol> properties)>();
            var diParams = new List<IParameterSymbol>(); // 依赖注入参数

            // 分析参数类型
            foreach (var param in parameters)
            {
                if (IsSimpleType(param.Type, compilation))
                {
                    simpleParams.Add($"{param.Type.ToDisplayString()} {param.Name}");
                }
                else if (CanInstantiate(param.Type))
                {
                    // 复杂类型但可以实例化，获取其属性
                    var properties = GetPublicProperties(param.Type);
                    complexParams.Add((param, properties));
                }
                else
                {
                    // 不能实例化的类型（抽象类、接口等），通过依赖注入获取
                    diParams.Add(param);
                }
            }

            // 构建参数列表
            var allParams = new List<string> { $"{fullTypeName} {serviceInstance}" };
            allParams.AddRange(simpleParams);

            // 添加依赖注入参数
            foreach (var param in diParams)
            {
                allParams.Add($"{param.Type.ToDisplayString()} {param.Name}");
            }

            // 为复杂类型的每个属性添加参数
            foreach (var (param, properties) in complexParams)
            {
                foreach (var prop in properties)
                {
                    // 直接使用属性名称作为参数名称，首字母小写
                    var paramName = char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);
                    allParams.Add($"{prop.Type.ToDisplayString()} {paramName}");
                }
            }

            var parameterList = string.Join(", ", allParams);

            // 构建方法调用参数
            var callParams = new List<string>();

            // 添加简单参数
            foreach (var param in parameters.Where(p => IsSimpleType(p.Type, compilation)))
            {
                callParams.Add(param.Name);
            }

            // 添加依赖注入参数（直接使用）
            foreach (var param in diParams)
            {
                callParams.Add(param.Name);
            }

            // 为复杂参数创建实例
            foreach (var (param, properties) in complexParams)
            {
                var propAssignments = properties.Select(prop =>
                {
                    // 使用属性名称作为参数名称，首字母小写
                    var paramName = char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);
                    return $"{prop.Name} = {paramName}";
                }).ToList();

                var paramTypeName = param.Type.ToDisplayString();
                var newInstance = $"new {paramTypeName} {{ {string.Join(", ", propAssignments)} }}";
                callParams.Add(newInstance);
            }

            // 检查方法是否为异步方法
            var method = parameters.Length > 0 ? 
                compilation.GetSymbolsWithName(methodName).OfType<IMethodSymbol>()
                    .FirstOrDefault(m => m.ContainingType.ToDisplayString() == fullTypeName) : null;
            
            bool isAsync = method != null && IsAsyncMethod(method);
            
            string methodCall;
            string lambdaPrefix;
            
            if (isAsync)
            {
                methodCall = callParams.Any()
                    ? $"await {serviceInstance}.{methodName}({string.Join(", ", callParams)})"
                    : $"await {serviceInstance}.{methodName}()";
                lambdaPrefix = "async ";
            }
            else
            {
                methodCall = callParams.Any()
                    ? $"{serviceInstance}.{methodName}({string.Join(", ", callParams)})"
                    : $"{serviceInstance}.{methodName}()";
                lambdaPrefix = "";
            }

            return $"{lambdaPrefix}({parameterList}) => {methodCall}";
        }

        private static bool IsSimpleType(ITypeSymbol type, Compilation compilation)
        {
            // 检查是否为简单类型（基础类型、字符串、DateTime 等）
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

            // 检查可空类型
            if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                var genericTypeDef = namedType.ConstructedFrom;
                if (genericTypeDef.SpecialType == SpecialType.System_Nullable_T)
                {
                    return IsSimpleType(namedType.TypeArguments[0], compilation);
                }
            }

            // 检查常见的简单类型
            var typeName = type.ToDisplayString();
            return typeName == "System.DateTime" ||
                   typeName == "System.DateTimeOffset" ||
                   typeName == "System.TimeSpan" ||
                   typeName == "System.Guid" ||
                   typeName == "System.Uri";
        }

        private static List<IPropertySymbol> GetPublicProperties(ITypeSymbol type)
        {
            var properties = new List<IPropertySymbol>();
            var visitedProperties = new HashSet<string>();

            // 获取当前类型及所有基类的属性
            var currentType = type;
            while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
            {
                var currentProperties = currentType.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Where(p => p.DeclaredAccessibility == Accessibility.Public &&
                                p.GetMethod != null &&
                                p.SetMethod != null &&
                                !p.IsStatic &&
                                !visitedProperties.Contains(p.Name)); // 避免重复添加被覆写的属性

                foreach (var prop in currentProperties)
                {
                    properties.Add(prop);
                    visitedProperties.Add(prop.Name);
                }

                currentType = currentType.BaseType;
            }

            return properties;
        }

        private static bool CanInstantiate(ITypeSymbol type)
        {
            // 检查是否为接口
            if (type.TypeKind == TypeKind.Interface)
                return false;

            // 检查是否为抽象类
            if (type.IsAbstract)
                return false;

            // 检查是否为静态类
            if (type.IsStatic)
                return false;

            // 检查是否为委托类型
            if (type.TypeKind == TypeKind.Delegate)
                return false;

            // 检查是否为枚举（枚举可以实例化，但通常不需要 new）
            if (type.TypeKind == TypeKind.Enum)
                return false;

            // 检查是否有可访问的构造函数
            if (type is INamedTypeSymbol namedType)
            {
                var constructors = namedType.Constructors
                    .Where(c => c.DeclaredAccessibility == Accessibility.Public)
                    .ToList();

                // 如果没有公共构造函数，不能实例化
                if (!constructors.Any())
                    return false;

                // 如果有无参构造函数或者所有参数都有默认值的构造函数，可以实例化
                return constructors.Any(c => c.Parameters.Length == 0 ||
                                             c.Parameters.All(p => p.HasExplicitDefaultValue));
            }

            return true;
        }

        private static bool IsAsyncMethod(IMethodSymbol method)
        {
            var returnType = method.ReturnType;
            var returnTypeName = returnType.ToDisplayString();
            
            // 检查返回类型是否为Task、Task<T>、ValueTask或ValueTask<T>
            return returnTypeName == "System.Threading.Tasks.Task" ||
                   returnTypeName.StartsWith("System.Threading.Tasks.Task<") ||
                   returnTypeName == "System.Threading.Tasks.ValueTask" ||
                   returnTypeName.StartsWith("System.Threading.Tasks.ValueTask<");
        }

        private (string httpMethod, string route) DetermineHttpMethodAndRoute(IMethodSymbol method)
        {
            // 检查是否有 HTTP 方法特性 (支持 Microsoft.AspNetCore.Mvc 命名空间)
            var httpMethodAttr = method.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass != null &&
                HttpMethodAttributes.Contains(a.AttributeClass.Name) &&
                a.AttributeClass.ContainingNamespace?.ToDisplayString() == "Microsoft.AspNetCore.Mvc");

            if (httpMethodAttr != null)
            {
                var attrName = httpMethodAttr.AttributeClass!.Name;
                if (MvcHttpMethodAttributes.TryGetValue(attrName, out var httpMethod))
                {
                    // 获取路由参数，如果有
                    var route = httpMethodAttr.ConstructorArguments.FirstOrDefault().Value as string ??
                                NormalizeMethodName(method.Name);

                    return (httpMethod, route);
                }
            }

            // 根据方法名推断 HTTP 方法和路由
            return InferHttpMethodFromName(method.Name);
        }

        private static (string httpMethod, string route) InferHttpMethodFromName(string methodName)
        {
            // 先移除 Async 和 Service 后缀，但不添加 / 前缀
            var cleanName = methodName;
            if (cleanName.EndsWith(AsyncSuffix, StringComparison.OrdinalIgnoreCase))
            {
                cleanName = cleanName.Substring(0, cleanName.Length - AsyncSuffix.Length);
            }

            if (cleanName.EndsWith(ServiceSuffix, StringComparison.OrdinalIgnoreCase))
            {
                cleanName = cleanName.Substring(0, cleanName.Length - ServiceSuffix.Length);
            }

            var lowerName = cleanName.ToLowerInvariant();
            string routePart;
            string httpMethod;

            if (lowerName.StartsWith("get"))
            {
                httpMethod = "Get";
                routePart = cleanName.Substring(3);
            }
            else if (lowerName.StartsWith("remove"))
            {
                httpMethod = "Delete";
                routePart = cleanName.Substring(6);
            }
            else if (lowerName.StartsWith("delete"))
            {
                httpMethod = "Delete";
                routePart = cleanName.Substring(6);
            }
            else if (lowerName.StartsWith("post"))
            {
                httpMethod = "Post";
                routePart = cleanName.Substring(4);
            }
            else if (lowerName.StartsWith("create"))
            {
                httpMethod = "Post";
                routePart = cleanName.Substring(6);
            }
            else if (lowerName.StartsWith("add"))
            {
                httpMethod = "Post";
                routePart = cleanName.Substring(3);
            }
            else if (lowerName.StartsWith("insert"))
            {
                httpMethod = "Post";
                routePart = cleanName.Substring(6);
            }
            else if (lowerName.StartsWith("put"))
            {
                httpMethod = "Put";
                routePart = cleanName.Substring(3);
            }
            else if (lowerName.StartsWith("update"))
            {
                httpMethod = "Put";
                routePart = cleanName.Substring(6);
            }
            else if (lowerName.StartsWith("modify"))
            {
                httpMethod = "Put";
                routePart = cleanName.Substring(6);
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

            // 如果截取后的部分为空，直接返回空字符串作为路由
            if (string.IsNullOrEmpty(routePart))
            {
                return (httpMethod, "");
            }

            // 转换为小写并添加 / 前缀
            var route = "/" + routePart.ToLowerInvariant();

            return (httpMethod, route);
        }

        private static string NormalizeMethodName(string methodName)
        {
            var normalized = methodName;

            // 移除 'Async' 后缀
            if (normalized.EndsWith(AsyncSuffix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(0, normalized.Length - AsyncSuffix.Length);
            }

            // 移除 'Service' 后缀
            if (normalized.EndsWith(ServiceSuffix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(0, normalized.Length - ServiceSuffix.Length);
            }

            // 确保路由以 '/' 开头
            if (!normalized.StartsWith("/"))
                normalized = "/" + normalized;

            return normalized;
        }

        private sealed class ClassInfo
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
}