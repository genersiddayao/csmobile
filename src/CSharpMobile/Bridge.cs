using Microsoft.JSInterop;

namespace CSharpMobile;

/// <summary>Calls between the page's JavaScript and .NET.</summary>
public static class Bridge
{
    internal static IJSInProcessRuntime JS;

    internal static void Write(string text, string fg, string bg) => JS.InvokeVoid("csm.write", text, fg, bg);
    internal static void Clear() => JS.InvokeVoid("csm.clear");

    /// <summary>Blocking input through the browser's prompt dialog (fallback mode). Null means cancelled.</summary>
    internal static string PromptSync(string kind) => JS.Invoke<string>("csm.promptSync", kind);

    /// <summary>Inline input in the output panel. Resolves to null when the program is stopped.</summary>
    internal static ValueTask<string> ReadAsync(string kind) => JS.InvokeAsync<string>("csm.readLine", kind);

    [JSInvokable]
    public static Task<RunResult> Run(string code) => Runner.RunAsync(code);

    [JSInvokable]
    public static void Stop() => Runner.Stop();

    [JSInvokable]
    public static Task<string> Warmup() => Runner.WarmupAsync();
}
