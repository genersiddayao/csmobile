# C#Mobile

**A free C# console editor and runner for your phone.** No install, no fee, no sign-up.

### ▶ Website: **https://genersiddayao.github.io/csmobile/**
### ▶ Go straight to the app: **https://genersiddayao.github.io/csmobile/app/**

Built for students and faculty of Cagayan State University, and free for anyone learning C#.

---

## Why this exists

Programming courses like Data Structures and Algorithms use C# console apps, but many students don't own a laptop. C#Mobile runs Microsoft's real C# compiler on the phone itself, so students can write, run and debug console programs anywhere.

## Features

- Real C# (latest language version) compiled by Roslyn, running on .NET 8 in the browser
- **Interactive `Console.ReadLine()`**: the program pauses and you type right in the console, so menu-driven programs work just like in Visual Studio
- `Console.ReadKey()`, `Console.Clear()`, `ForegroundColor` / `BackgroundColor`
- Build errors with line numbers and a tap-to-jump button
- Runtime exceptions show the type, message and line number
- **Stop** button for runaway loops, plus automatic detection of infinite loops and infinite recursion
- Classic `class Program { static void Main() }` style or top-level statements
- Phone key bar for `; { } ( ) [ ] " < > =` and more
- Multiple files saved on your device; open `.cs` files from your phone or copy code to submit
- 8 built-in examples: Stack menu, circular queue, linked list, sorting, binary search, recursion, OOP
- Light and dark mode

## Quick start

1. Open **https://genersiddayao.github.io/csmobile/** on your phone and tap **Launch the app**. The first visit downloads the compiler (about 6 MB). After that it opens from your phone's cache.
2. Type your program, or open the menu (☰) and pick an example.
3. Tap **Run**. When your program asks for input, type in the console and press Enter.

## What's supported

| Works | Not available |
| --- | --- |
| Classes, structs, records, interfaces, inheritance, generics | Reading/writing files |
| `List`, `Stack`, `Queue`, `Dictionary`, `HashSet`, `LinkedList`, arrays | NuGet packages |
| LINQ, lambdas, exceptions, string formatting | Threads, `Thread.Sleep`, networking |
| `Console.ReadLine`, `Read`, `ReadKey`, `Clear`, colors | Windows Forms / WPF |

Input normally appears inline in the console. In rare cases (for example, reading input inside a property getter or constructor), the app falls back to a pop-up box for input instead.

## How it works

- **Blazor WebAssembly** runs .NET 8 in the browser.
- **Roslyn** (Microsoft.CodeAnalysis.CSharp) compiles the student's code on the device.
- Before compiling, the code is rewritten so `Console.ReadLine()` can wait for typing without freezing the page: methods that read input become `async`, and their calls are awaited. Line numbers are preserved, so errors point to the student's original line.
- GitHub Actions builds the app and publishes it to the `gh-pages` branch, which GitHub Pages serves.

Source: `src/CSharpMobile/` (the app, served at `/app/`) · `landing/` (the welcome page) · Build: `.github/workflows/deploy.yml`

## About

Developed by **Generino P. Siddayao** (Sir Gener), faculty member of the College of Information and Computing Sciences, Cagayan State University – Carig Campus, Tuguegarao City, Philippines.

Sister project: [PyMobile IDE](https://genersiddayao.github.io/pymobile/), a free Python runner for phones.

Found a bug or have a feature idea? Open an [issue](https://github.com/genersiddayao/csmobile/issues), or tell your instructor.
