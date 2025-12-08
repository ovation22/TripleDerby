using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;
using Xunit.Abstractions;
using TripleDerby.Core.Specifications;

namespace TripleDerby.Tests.Architecture;

public class SearchSpecificationUsageTests
{
    private readonly ITestOutputHelper _output;

    public SearchSpecificationUsageTests(ITestOutputHelper output) => _output = output;

    private static bool IsSubclassOfGeneric(Type type, Type genericBase)
    {
        var current = type;
        while (current != null && current != typeof(object))
        {
            var curDef = current.IsGenericType ? current.GetGenericTypeDefinition() : current;
            if (curDef == genericBase) return true;
            current = current.BaseType;
        }
        return false;
    }

    [Fact]
    public void NoDirectInstantiationsOrCtorCallsOfGenericSearchSpecification_When_SpecificSpecificationExists()
    {
        // 1) discover derived SearchSpecification<T> types and their entity type names
        var coreAssembly = typeof(SearchSpecification<>).Assembly;
        var allCoreTypes = coreAssembly.GetTypes();

        var derivedSpecs = allCoreTypes
            .Where(t => t.IsClass && !t.IsAbstract && IsSubclassOfGeneric(t, typeof(SearchSpecification<>)))
            .ToList();

        _output.WriteLine("Derived search specs discovered:");
        foreach (var d in derivedSpecs) _output.WriteLine($" - {d.FullName}");

        var protectedEntityTypeFullNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var protectedEntityTypeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var derived in derivedSpecs)
        {
            var baseType = derived.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(SearchSpecification<>))
                {
                    var arg = baseType.GetGenericArguments()[0];
                    if (arg.FullName != null)
                    {
                        protectedEntityTypeFullNames.Add(arg.FullName);
                        protectedEntityTypeNames.Add(arg.Name); // e.g. "Horse"
                    }
                    break;
                }
                baseType = baseType.BaseType;
            }
        }

        if (!protectedEntityTypeFullNames.Any())
        {
            _output.WriteLine("No derived SearchSpecification<> types found - skipping rule.");
            return;
        }

        // 2) Source-level scan (quick and reliable) - search for 'new SearchSpecification<...>' in repo
        var repoRoot = FindRepoRoot(Path.GetDirectoryName(typeof(SearchSpecificationUsageTests).Assembly.Location) ?? Environment.CurrentDirectory);
        if (repoRoot != null)
            _output.WriteLine($"Repo root for source scan: {repoRoot}");
        else
            _output.WriteLine("Repo root not found; source scan will probe test output folder.");

        var sourceOffenders = new List<string>();
        if (repoRoot != null)
        {
            var csFiles = Directory.EnumerateFiles(repoRoot, "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) && !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar));

            var regexTemplate = new Regex(@"\bnew\s+SearchSpecification\s*<\s*{0}\s*>", RegexOptions.Compiled);
            foreach (var file in csFiles)
            {
                var text = File.ReadAllText(file);
                foreach (var entityName in protectedEntityTypeNames)
                {
                    var regex = new Regex(@"\bnew\s+SearchSpecification\s*<\s*" + Regex.Escape(entityName) + @"\s*>", RegexOptions.Singleline);
                    if (regex.IsMatch(text))
                    {
                        sourceOffenders.Add($"{Path.GetRelativePath(repoRoot, file)} contains 'new SearchSpecification<{entityName}>'");
                    }
                    // also catch usages like "SearchSpecification<Horse>(" without new (e.g. passed to methods)
                    var regex2 = new Regex(@"\bSearchSpecification\s*<\s*" + Regex.Escape(entityName) + @"\s*>", RegexOptions.Singleline);
                    if (regex2.IsMatch(text) && !file.EndsWith(nameof(SearchSpecificationUsageTests) + ".cs", StringComparison.OrdinalIgnoreCase))
                    {
                        sourceOffenders.Add($"{Path.GetRelativePath(repoRoot, file)} references 'SearchSpecification<{entityName}>'");
                    }
                }
            }
        }

        // 3) IL-level scan of build output (fallback / double check)
        var buildDir = Path.GetDirectoryName(typeof(SearchSpecificationUsageTests).Assembly.Location) ?? Environment.CurrentDirectory;
        _output.WriteLine($"Probing build folder: {buildDir}");
        var probedAssemblies = Directory.EnumerateFiles(buildDir, "TripleDerby*.dll").ToList();
        if (!probedAssemblies.Any())
        {
            // fallback: scan loaded assemblies
            probedAssemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && a.GetName().Name != null && a.GetName().Name.StartsWith("TripleDerby", StringComparison.OrdinalIgnoreCase))
                .Select(a => a.Location)
                .Where(l => !string.IsNullOrEmpty(l)));
        }

        var ilOffenders = new List<string>();
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(buildDir);
        var readerParams = new ReaderParameters { AssemblyResolver = resolver };

        foreach (var asmPath in probedAssemblies.Distinct())
        {
            if (string.IsNullOrEmpty(asmPath) || !File.Exists(asmPath)) continue;
            _output.WriteLine($"Scanning assembly: {Path.GetFileName(asmPath)}");
            using var asmDef = AssemblyDefinition.ReadAssembly(asmPath, readerParams);
            foreach (var module in asmDef.Modules)
            {
                foreach (var typeDef in module.Types)
                {
                    if (string.IsNullOrEmpty(typeDef.Name) || typeDef.Name.StartsWith("<")) continue;

                    foreach (var method in typeDef.Methods)
                    {
                        if (!method.HasBody) continue;
                        foreach (var instr in method.Body.Instructions)
                        {
                            if (instr.OpCode != OpCodes.Newobj && instr.OpCode != OpCodes.Call && instr.OpCode != OpCodes.Callvirt)
                                continue;

                            if (!(instr.Operand is MethodReference mref)) continue;
                            if (!string.Equals(mref.Name, ".ctor", StringComparison.Ordinal)) continue;

                            var declaringType = mref.DeclaringType;
                            if (declaringType is GenericInstanceType git)
                            {
                                var elementTypeFullName = git.ElementType.FullName;
                                var searchSpecOpenFullName = typeof(SearchSpecification<>).FullName;
                                if (!string.Equals(elementTypeFullName, searchSpecOpenFullName, StringComparison.Ordinal))
                                    continue;

                                var genericArg = git.GenericArguments.FirstOrDefault();
                                if (genericArg == null) continue;
                                var argFullName = genericArg.FullName;

                                if (argFullName != null && protectedEntityTypeFullNames.Contains(argFullName))
                                {
                                    // Skip legitimate usage in the derived spec's own constructor
                                    var containingTypeFullName = typeDef.FullName.Replace("/", ".");
                                    var isInDerivedSpec = derivedSpecs.Any(d => d.FullName == containingTypeFullName);
                                    if (isInDerivedSpec) continue;

                                    ilOffenders.Add($"{Path.GetFileName(asmPath)} -> {typeDef.FullName}.{method.Name} uses SearchSpecification<{genericArg.FullName}> at IL offset {instr.Offset}");
                                }
                            }
                        }
                    }
                }
            }
        }

        // Aggregate results and fail if any offenders found
        var offenders = new List<string>();
        offenders.AddRange(sourceOffenders);
        offenders.AddRange(ilOffenders);

        if (offenders.Any())
        {
            var message = "Forbidden direct usage of closed generic SearchSpecification<T> found for entity types that have dedicated SearchSpecification implementations:\n"
                          + string.Join("\n", offenders);
            _output.WriteLine(message);
            Assert.False(true, message);
        }
    }

    // Walk up directories looking for a .sln file to identify repo root; returns null if not found.
    private static string? FindRepoRoot(string start)
    {
        if (string.IsNullOrEmpty(start)) return null;
        var dir = new DirectoryInfo(start);
        while (dir != null)
        {
            if (Directory.EnumerateFiles(dir.FullName, "*.sln").Any()) return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}