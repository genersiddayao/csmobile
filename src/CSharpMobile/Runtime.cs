using System.Diagnostics;
using System.Text;
using CSharpMobile;

// These two classes are what student code is rewritten to call.
// They live in the global namespace so rewritten code can say global::__Con / global::__Rt.

/// <summary>Stand-in for System.Console that talks to the browser.</summary>
public static class __Con
{
    internal static readonly ConsoleWriter Writer = new ConsoleWriter();
    static readonly Queue<char> _readBuffer = new Queue<char>();
    static ConsoleColor _fg = ConsoleColor.Gray, _bg = ConsoleColor.Black;
    static bool _fgSet, _bgSet;

    internal static void Reset()
    {
        _readBuffer.Clear();
        _fg = ConsoleColor.Gray; _bg = ConsoleColor.Black; _fgSet = _bgSet = false;
        Writer.Discard();
    }

    internal static string FgName => _fgSet ? _fg.ToString() : null;
    internal static string BgName => _bgSet ? _bg.ToString() : null;

    public static void Flush() => Writer.Flush();

    // ---- streams ----
    public static TextWriter Out => Writer;
    public static TextWriter Error => Writer;
    public static TextReader In { get; } = new PromptReader();
    public static void SetOut(TextWriter w) { }
    public static void SetError(TextWriter w) { }
    public static void SetIn(TextReader r) { }

    // ---- output ----
    public static void Write(string value) => Writer.Write(value);
    public static void Write(object value) => Writer.Write(value);
    public static void Write(bool value) => Writer.Write(value);
    public static void Write(char value) => Writer.Write(value);
    public static void Write(char[] buffer) => Writer.Write(buffer);
    public static void Write(char[] buffer, int index, int count) => Writer.Write(buffer, index, count);
    public static void Write(decimal value) => Writer.Write(value);
    public static void Write(double value) => Writer.Write(value);
    public static void Write(float value) => Writer.Write(value);
    public static void Write(int value) => Writer.Write(value);
    public static void Write(uint value) => Writer.Write(value);
    public static void Write(long value) => Writer.Write(value);
    public static void Write(ulong value) => Writer.Write(value);
    public static void Write(string format, object arg0) => Writer.Write(format, arg0);
    public static void Write(string format, object arg0, object arg1) => Writer.Write(format, arg0, arg1);
    public static void Write(string format, object arg0, object arg1, object arg2) => Writer.Write(format, arg0, arg1, arg2);
    public static void Write(string format, params object[] arg) => Writer.Write(format, arg);

    public static void WriteLine() => Writer.WriteLine();
    public static void WriteLine(string value) => Writer.WriteLine(value);
    public static void WriteLine(object value) => Writer.WriteLine(value);
    public static void WriteLine(bool value) => Writer.WriteLine(value);
    public static void WriteLine(char value) => Writer.WriteLine(value);
    public static void WriteLine(char[] buffer) => Writer.WriteLine(buffer);
    public static void WriteLine(char[] buffer, int index, int count) => Writer.WriteLine(buffer, index, count);
    public static void WriteLine(decimal value) => Writer.WriteLine(value);
    public static void WriteLine(double value) => Writer.WriteLine(value);
    public static void WriteLine(float value) => Writer.WriteLine(value);
    public static void WriteLine(int value) => Writer.WriteLine(value);
    public static void WriteLine(uint value) => Writer.WriteLine(value);
    public static void WriteLine(long value) => Writer.WriteLine(value);
    public static void WriteLine(ulong value) => Writer.WriteLine(value);
    public static void WriteLine(string format, object arg0) => Writer.WriteLine(format, arg0);
    public static void WriteLine(string format, object arg0, object arg1) => Writer.WriteLine(format, arg0, arg1);
    public static void WriteLine(string format, object arg0, object arg1, object arg2) => Writer.WriteLine(format, arg0, arg1, arg2);
    public static void WriteLine(string format, params object[] arg) => Writer.WriteLine(format, arg);

    // ---- input: blocking versions (browser prompt dialog) ----
    public static string ReadLine()
    {
        if (_readBuffer.Count > 0) return DrainBufferLine();
        Writer.Flush();
        var s = Bridge.PromptSync("line");
        if (s == null) { __Rt.StopRequested = true; throw new __StopException(); }
        __Rt.MarkActivity();
        return s;
    }

    public static int Read()
    {
        if (_readBuffer.Count == 0)
        {
            var line = ReadLine();
            foreach (var c in line) _readBuffer.Enqueue(c);
            _readBuffer.Enqueue('\n');
        }
        return _readBuffer.Dequeue();
    }

    public static ConsoleKeyInfo ReadKey() => ReadKey(false);
    public static ConsoleKeyInfo ReadKey(bool intercept)
    {
        Writer.Flush();
        var s = Bridge.PromptSync(intercept ? "key-hidden" : "key");
        if (s == null) { __Rt.StopRequested = true; throw new __StopException(); }
        __Rt.MarkActivity();
        return ToKeyInfo(s);
    }

    // ---- input: awaitable versions (inline input in the output panel) ----
    public static async Task<string> ReadLineAsync()
    {
        if (_readBuffer.Count > 0) return DrainBufferLine();
        Writer.Flush();
        if (__Rt.StopRequested) await __Rt.Never();
        var s = await Bridge.ReadAsync("line");
        if (s == null || __Rt.StopRequested) await __Rt.Never();
        __Rt.MarkActivity();
        return s;
    }

    public static async Task<int> ReadAsync()
    {
        if (_readBuffer.Count == 0)
        {
            var line = await ReadLineAsync();
            foreach (var c in line) _readBuffer.Enqueue(c);
            _readBuffer.Enqueue('\n');
        }
        return _readBuffer.Dequeue();
    }

    public static Task<ConsoleKeyInfo> ReadKeyAsync() => ReadKeyAsync(false);
    public static async Task<ConsoleKeyInfo> ReadKeyAsync(bool intercept)
    {
        Writer.Flush();
        if (__Rt.StopRequested) await __Rt.Never();
        var s = await Bridge.ReadAsync(intercept ? "key-hidden" : "key");
        if (s == null || __Rt.StopRequested) await __Rt.Never();
        __Rt.MarkActivity();
        return ToKeyInfo(s);
    }

    static string DrainBufferLine()
    {
        var sb = new StringBuilder();
        while (_readBuffer.Count > 0)
        {
            var c = _readBuffer.Dequeue();
            if (c == '\n') break;
            sb.Append(c);
        }
        return sb.ToString();
    }

    static ConsoleKeyInfo ToKeyInfo(string s)
    {
        if (string.IsNullOrEmpty(s) || s == "\n" || s == "\r") return new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);
        if (s == "Escape") return new ConsoleKeyInfo('\u001b', ConsoleKey.Escape, false, false, false);
        if (s == "Backspace") return new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false);
        if (s == "Tab") return new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false);
        if (s == "ArrowUp") return new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false);
        if (s == "ArrowDown") return new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false);
        if (s == "ArrowLeft") return new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false);
        if (s == "ArrowRight") return new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false);
        char ch = s[0];
        ConsoleKey key = ConsoleKey.NoName;
        if (char.IsLetter(ch) && ch < 128) key = (ConsoleKey)char.ToUpperInvariant(ch);
        else if (char.IsDigit(ch)) key = (ConsoleKey)('0' + (ch - '0'));
        else if (ch == ' ') key = ConsoleKey.Spacebar;
        return new ConsoleKeyInfo(ch, key, char.IsUpper(ch), false, false);
    }

    // ---- screen ----
    public static void Clear() { Writer.Discard(); Bridge.Clear(); }

    public static ConsoleColor ForegroundColor
    {
        get => _fg;
        set { Writer.Flush(); _fg = value; _fgSet = true; }
    }
    public static ConsoleColor BackgroundColor
    {
        get => _bg;
        set { Writer.Flush(); _bg = value; _bgSet = true; }
    }
    public static void ResetColor() { Writer.Flush(); _fg = ConsoleColor.Gray; _bg = ConsoleColor.Black; _fgSet = _bgSet = false; }

    public static string Title { get; set; } = "C#Mobile";
    public static bool CursorVisible { get; set; } = true;
    public static int CursorSize { get; set; } = 25;
    public static int CursorLeft { get; set; }
    public static int CursorTop { get; set; }
    public static int WindowWidth { get; set; } = 80;
    public static int WindowHeight { get; set; } = 25;
    public static int BufferWidth { get; set; } = 80;
    public static int BufferHeight { get; set; } = 300;
    public static int LargestWindowWidth => 80;
    public static int LargestWindowHeight => 25;
    public static bool KeyAvailable => false;
    public static bool IsInputRedirected => false;
    public static bool IsOutputRedirected => false;
    public static bool TreatControlCAsInput { get; set; }
    public static Encoding OutputEncoding { get; set; } = Encoding.UTF8;
    public static Encoding InputEncoding { get; set; } = Encoding.UTF8;
    public static void SetCursorPosition(int left, int top) { CursorLeft = left; CursorTop = top; }
    public static (int Left, int Top) GetCursorPosition() => (CursorLeft, CursorTop);
    public static void SetWindowSize(int width, int height) { }
    public static void SetBufferSize(int width, int height) { }
    public static void Beep() { }
    public static void Beep(int frequency, int duration) { }
}

/// <summary>Loop, recursion and stop checks injected into student code.</summary>
public static class __Rt
{
    internal static volatile bool StopRequested;
    static readonly Stopwatch _sinceYield = new Stopwatch();
    const int SyncLimitMs = 10_000;

    internal static void Reset() { StopRequested = false; _sinceYield.Restart(); }
    internal static void MarkActivity() => _sinceYield.Restart();

    /// <summary>A task that never finishes: used to freeze a stopped program without letting user catch blocks see it.</summary>
    internal static Task Never() => new TaskCompletionSource<bool>().Task;

    public static void Tick()
    {
        if (StopRequested) throw new __StopException();
        if (_sinceYield.ElapsedMilliseconds > SyncLimitMs) throw new __TimeoutException();
    }

    public static async Task TickAsync()
    {
        if (StopRequested) await Never();
        if (_sinceYield.ElapsedMilliseconds > 40)
        {
            __Con.Flush();
            await Task.Delay(1);
            _sinceYield.Restart();
            if (StopRequested) await Never();
        }
    }

    public static void Enter()
    {
        System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
    }
}

public sealed class __StopException : Exception
{
    public __StopException() : base("Program stopped.") { }
}

public sealed class __TimeoutException : Exception
{
    public __TimeoutException() : base("The program ran for more than 10 seconds without asking for input. It may be stuck in an infinite loop.") { }
}

namespace CSharpMobile
{
    internal sealed class ConsoleWriter : TextWriter
    {
        readonly StringBuilder _sb = new StringBuilder();
        public override Encoding Encoding => Encoding.UTF8;
        public ConsoleWriter() { CoreNewLine = new[] { '\n' }; }

        public override void Write(char value) { _sb.Append(value); if (_sb.Length > 8192) Flush(); }
        public override void Write(string value) { if (value == null) return; _sb.Append(value); if (_sb.Length > 8192) Flush(); }
        public override void Write(char[] buffer, int index, int count) { _sb.Append(buffer, index, count); if (_sb.Length > 8192) Flush(); }

        public override void Flush()
        {
            if (_sb.Length == 0) return;
            var s = _sb.ToString();
            _sb.Clear();
            Bridge.Write(s, __Con.FgName, __Con.BgName);
        }

        internal void Discard() => _sb.Clear();
    }

    internal sealed class PromptReader : TextReader
    {
        public override string ReadLine() => __Con.ReadLine();
        public override int Read() => __Con.Read();
        public override int Peek() => -1;
    }
}
