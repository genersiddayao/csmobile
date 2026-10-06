using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CSharpMobile;

public sealed class RunResult
{
    /// <summary>ok | compile | error | stopped | timeout | internal</summary>
    public string Status { get; set; }
    public string Mode { get; set; }
    public List<Diag> Diagnostics { get; set; }
    public long CompileMs { get; set; }
    public long RunMs { get; set; }
    public string ErrorType { get; set; }
    public string ErrorMessage { get; set; }
    public int ErrorLine { get; set; }
    public string Debug { get; set; }
}

internal static class Runner
{
    static TaskCompletionSource<bool> _stopTcs;
    static bool _running;

    public static void Stop()
    {
        __Rt.StopRequested = true;
        _stopTcs?.TrySetResult(true);
    }

    public static async Task<string> WarmupAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await Compiler.EnsureReferencesAsync();
            Compiler.Compile("using System;\nclass P { static void Main() { int n = int.Parse(Console.ReadLine() ?? \"0\"); for (int i = 0; i < n; i++) Console.WriteLine(i); } }");
            return "ok " + sw.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            return "warmup failed: " + ex.Message;
        }
    }

    public static async Task<RunResult> RunAsync(string code)
    {
        var res = new RunResult();
        if (_running) Stop();
        var sw = Stopwatch.StartNew();
        CompileOutput co;
        try { await Compiler.EnsureReferencesAsync(); co = Compiler.Compile(code); }
        catch (Exception ex)
        {
            res.Status = "internal";
            res.ErrorMessage = ex.ToString();
            return res;
        }
        res.CompileMs = sw.ElapsedMilliseconds;
        res.Debug = co.Debug;
        if (co.Errors.Count > 0)
        {
            res.Status = "compile";
            res.Diagnostics = co.Errors;
            return res;
        }
        res.Mode = co.Mode;

        Assembly asm;
        try { asm = Assembly.Load(co.Pe, co.Pdb); }
        catch (Exception ex)
        {
            res.Status = "internal";
            res.ErrorMessage = "Could not load the program: " + ex.Message;
            return res;
        }

        __Rt.Reset();
        __Con.Reset();
        Console.SetOut(__Con.Writer);
        Console.SetError(__Con.Writer);
        Console.SetIn(__Con.In);
        var stopTcs = new TaskCompletionSource<bool>();
        _stopTcs = stopTcs;
        _running = true;

        var runSw = Stopwatch.StartNew();
        Task program;
        try
        {
            var entry = FindEntry(asm);
            object[] args = entry.GetParameters().Length == 0 ? null : new object[] { Array.Empty<string>() };
            var ret = entry.Invoke(null, args);
            program = ret as Task ?? Task.CompletedTask;
        }
        catch (TargetInvocationException tie)
        {
            program = Task.FromException(tie.InnerException ?? tie);
        }
        catch (Exception ex)
        {
            program = Task.FromException(ex);
        }

        await Task.WhenAny(program, stopTcs.Task);
        try { __Con.Flush(); } catch { }
        res.RunMs = runSw.ElapsedMilliseconds;
        _running = false;
        if (ReferenceEquals(_stopTcs, stopTcs)) _stopTcs = null;

        if (!program.IsCompleted)
        {
            res.Status = "stopped";
            return res;
        }
        if (program.IsFaulted)
        {
            var ex = Unwrap(program.Exception);
            if (ex is __StopException) { res.Status = "stopped"; return res; }
            if (ex is __TimeoutException) { res.Status = "timeout"; res.ErrorMessage = ex.Message; return res; }
            if (ex is InsufficientExecutionStackException)
            {
                res.Status = "error";
                res.ErrorType = "System.StackOverflowException";
                res.ErrorMessage = "Too many nested method calls. Check that your recursion has a base case that stops it.";
                res.ErrorLine = FindLine(ex);
                return res;
            }
            res.Status = "error";
            res.ErrorType = ex.GetType().FullName;
            res.ErrorMessage = ex.Message;
            res.ErrorLine = FindLine(ex);
            return res;
        }
        res.Status = __Rt.StopRequested ? "stopped" : "ok";
        return res;
    }

    /// <summary>
    /// For async Main / top-level await, the compiler's real entry point blocks on the task,
    /// which can't work on the browser's single thread. Call the async method directly instead.
    /// </summary>
    static MethodInfo FindEntry(Assembly asm)
    {
        var entry = asm.EntryPoint;
        if (entry == null) throw new InvalidOperationException("No Main method found.");
        if (entry.Name == "<Main>")
        {
            var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var real = entry.DeclaringType.GetMethods(flags)
                .FirstOrDefault(m => (m.Name == "Main" || m.Name == "<Main>$") && typeof(Task).IsAssignableFrom(m.ReturnType));
            if (real != null) return real;
        }
        return entry;
    }

    static Exception Unwrap(Exception ex)
    {
        while (true)
        {
            if (ex is AggregateException ae && ae.InnerExceptions.Count == 1) { ex = ae.InnerExceptions[0]; continue; }
            if (ex is TargetInvocationException tie && tie.InnerException != null) { ex = tie.InnerException; continue; }
            return ex;
        }
    }

    static readonly Regex LineRx = new Regex(@"Program\.cs:(?:line )?(\d+)", RegexOptions.Compiled);

    static int FindLine(Exception ex)
    {
        try
        {
            var st = ex.StackTrace ?? "";
            var m = LineRx.Match(st);
            if (m.Success) return int.Parse(m.Groups[1].Value);
            var trace = new StackTrace(ex, true);
            foreach (var f in trace.GetFrames() ?? Array.Empty<StackFrame>())
            {
                var file = f.GetFileName();
                if (file != null && file.EndsWith("Program.cs") && f.GetFileLineNumber() > 0) return f.GetFileLineNumber();
            }
        }
        catch { }
        return 0;
    }
}
