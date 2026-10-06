using Microsoft.JSInterop;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace CSharpMobile;

public sealed class Diag
{
    public int Line { get; set; }
    public int Col { get; set; }
    public string Id { get; set; }
    public string Message { get; set; }
}

internal sealed class CompileOutput
{
    public List<Diag> Errors = new List<Diag>();
    public byte[] Pe, Pdb;
    public string Mode;
    public string Debug;
}

internal static class Compiler
{
    static readonly string[] ReferenceNames =
    {
        "System.Private.CoreLib", "System.Runtime", "System.Console", "System.Collections",
        "System.Collections.NonGeneric", "System.Collections.Specialized", "System.Collections.Concurrent",
        "System.Linq", "System.Text.RegularExpressions", "System.Runtime.Numerics", "System.Runtime.Extensions",
        "System.Memory", "System.ObjectModel", "System.Threading", "System.Text.Encoding.Extensions",
    };

    static readonly CSharpParseOptions ParseOptions = new CSharpParseOptions(LanguageVersion.Latest);

    static readonly SyntaxTree GlobalUsings = CSharpSyntaxTree.ParseText(
        "global using System;\nglobal using System.Collections.Generic;\nglobal using System.IO;\n" +
        "global using System.Linq;\nglobal using System.Text;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n",
        ParseOptions, "GlobalUsings.cs");

    static readonly CSharpCompilationOptions CompileOptions = new CSharpCompilationOptions(
            OutputKind.ConsoleApplication,
            optimizationLevel: OptimizationLevel.Debug,
            concurrentBuild: false,
            nullableContextOptions: NullableContextOptions.Disable,
            allowUnsafe: true);

    static List<MetadataReference> _refs;
    static int _counter;

    static List<MetadataReference> References => _refs ?? throw new InvalidOperationException("Compiler references not loaded yet.");

    /// <summary>Downloads (from the browser cache, normally) the assemblies student code compiles against.</summary>
    public static async Task EnsureReferencesAsync()
    {
        if (_refs != null) return;
        var list = new List<MetadataReference>();
        foreach (var name in ReferenceNames.Append("CSharpMobile"))
        {
            try
            {
                var bytes = await Bridge.JS.InvokeAsync<byte[]>("csm.fetchAssembly", name + ".dll");
                if (bytes != null && bytes.Length > 0) list.Add(MetadataReference.CreateFromImage(bytes, filePath: name + ".dll"));
            }
            catch { /* not shipped; skip */ }
        }
        if (list.Count < 3) throw new InvalidOperationException("Could not download the C# reference assemblies.");
        _refs = list;
    }

    public static CompileOutput Compile(string code)
    {
        var result = new CompileOutput();
        var tree0 = CSharpSyntaxTree.ParseText(code, ParseOptions, "Program.cs");
        var comp0 = CSharpCompilation.Create("UserProgram" + (++_counter), new[] { tree0, GlobalUsings }, References, CompileOptions);

        foreach (var d in comp0.GetDiagnostics())
        {
            if (d.Severity != DiagnosticSeverity.Error) continue;
            result.Errors.Add(ToDiag(d));
        }
        if (result.Errors.Count > 0)
        {
            result.Errors = result.Errors.OrderBy(e => e.Line).ThenBy(e => e.Col).Take(20).ToList();
            return result;
        }

        var model = comp0.GetSemanticModel(tree0);
        var log = new System.Text.StringBuilder();

        foreach (var mode in new[] { InstrumentMode.Async, InstrumentMode.Sync })
        {
            try
            {
                var newRoot = new Instrumenter(model, tree0, mode).Rewrite(out var reason);
                if (newRoot == null) { log.Append(mode).Append(": ").Append(reason).Append('\n'); continue; }
                var tree1 = tree0.WithRootAndOptions(newRoot, ParseOptions);
                var comp1 = comp0.ReplaceSyntaxTree(tree0, tree1);
                if (TryEmit(comp1, result, out var firstError))
                {
                    result.Mode = mode == InstrumentMode.Async ? "interactive" : "prompt";
                    result.Debug = log.ToString();
                    return result;
                }
                log.Append(mode).Append(" emit failed: ").Append(firstError).Append('\n');
            }
            catch (Exception ex)
            {
                log.Append(mode).Append(" crashed: ").Append(ex.GetType().Name).Append(' ').Append(ex.Message).Append('\n');
            }
        }

        if (TryEmit(comp0, result, out var err0))
        {
            result.Mode = "plain";
            result.Debug = log.ToString();
            return result;
        }
        result.Errors.Add(new Diag { Line = 1, Col = 1, Id = "CSM0001", Message = "Could not build the program: " + err0 });
        result.Debug = log.ToString();
        return result;
    }

    static bool TryEmit(CSharpCompilation comp, CompileOutput result, out string firstError)
    {
        using var pe = new MemoryStream();
        using var pdb = new MemoryStream();
        var emit = comp.Emit(pe, pdb, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
        if (!emit.Success)
        {
            var e = emit.Diagnostics.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
            firstError = e == null ? "unknown error" : e.ToString();
            return false;
        }
        result.Pe = pe.ToArray();
        result.Pdb = pdb.ToArray();
        firstError = null;
        return true;
    }

    static Diag ToDiag(Diagnostic d)
    {
        var span = d.Location.GetLineSpan();
        bool inUser = d.Location.IsInSource && span.Path == "Program.cs";
        return new Diag
        {
            Line = inUser ? span.StartLinePosition.Line + 1 : 0,
            Col = inUser ? span.StartLinePosition.Character + 1 : 0,
            Id = d.Id,
            Message = d.GetMessage(),
        };
    }
}
