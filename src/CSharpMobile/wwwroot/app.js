(function () {
'use strict';
const $ = id => document.getElementById(id);

const EXAMPLES = [
 {name:"Program.cs", title:"Hello + ReadLine", desc:"Input, parsing, interpolation", code:
`using System;

class Program
{
    static void Main(string[] args)
    {
        Console.Write("Enter your name: ");
        string name = Console.ReadLine();

        Console.Write("Enter your age: ");
        int age = int.Parse(Console.ReadLine());

        Console.WriteLine($"Hello, {name}! Next year you will be {age + 1}.");
    }
}
`},
 {name:"StackMenu.cs", title:"Stack menu", desc:"Menu-driven Push / Pop / Peek", code:
`using System;
using System.Collections.Generic;

class Program
{
    static Stack<int> stack = new Stack<int>();

    static void Main(string[] args)
    {
        int choice;
        do
        {
            Console.WriteLine("===== STACK MENU =====");
            Console.WriteLine("[1] Push");
            Console.WriteLine("[2] Pop");
            Console.WriteLine("[3] Peek");
            Console.WriteLine("[4] Display");
            Console.WriteLine("[0] Exit");
            Console.Write("Enter choice: ");
            choice = int.Parse(Console.ReadLine());

            switch (choice)
            {
                case 1: Push(); break;
                case 2: Pop(); break;
                case 3: Peek(); break;
                case 4: Display(); break;
                case 0: Console.WriteLine("Goodbye!"); break;
                default: Console.WriteLine("Invalid choice."); break;
            }
            Console.WriteLine();
        } while (choice != 0);
    }

    static void Push()
    {
        Console.Write("Enter a number: ");
        int value = int.Parse(Console.ReadLine());
        stack.Push(value);
        Console.WriteLine($"{value} pushed.");
    }

    static void Pop()
    {
        if (stack.Count == 0) { Console.WriteLine("Stack is empty."); return; }
        Console.WriteLine($"{stack.Pop()} popped.");
    }

    static void Peek()
    {
        if (stack.Count == 0) { Console.WriteLine("Stack is empty."); return; }
        Console.WriteLine($"Top: {stack.Peek()}");
    }

    static void Display()
    {
        if (stack.Count == 0) { Console.WriteLine("Stack is empty."); return; }
        Console.WriteLine("Top -> " + string.Join(" | ", stack));
    }
}
`},
 {name:"CircularQueue.cs", title:"Circular queue", desc:"Array-based queue class + menu", code:
`using System;

class CircularQueue
{
    private int[] items;
    private int front = 0, rear = -1, count = 0;

    public CircularQueue(int size) { items = new int[size]; }

    public bool IsEmpty() => count == 0;
    public bool IsFull() => count == items.Length;

    public void Enqueue(int value)
    {
        if (IsFull()) { Console.WriteLine("Queue overflow!"); return; }
        rear = (rear + 1) % items.Length;
        items[rear] = value;
        count++;
    }

    public int Dequeue()
    {
        if (IsEmpty()) throw new InvalidOperationException("Queue underflow!");
        int value = items[front];
        front = (front + 1) % items.Length;
        count--;
        return value;
    }

    public void Display()
    {
        if (IsEmpty()) { Console.WriteLine("Queue is empty."); return; }
        Console.Write("Front -> ");
        for (int i = 0; i < count; i++)
            Console.Write(items[(front + i) % items.Length] + " ");
        Console.WriteLine("<- Rear");
    }
}

class Program
{
    static void Main()
    {
        CircularQueue q = new CircularQueue(5);
        while (true)
        {
            Console.Write("[1] Enqueue [2] Dequeue [3] Display [0] Exit: ");
            string input = Console.ReadLine();
            if (input == "0") break;
            try
            {
                if (input == "1")
                {
                    Console.Write("Value: ");
                    q.Enqueue(int.Parse(Console.ReadLine()));
                }
                else if (input == "2") Console.WriteLine($"Dequeued {q.Dequeue()}");
                else if (input == "3") q.Display();
                else Console.WriteLine("Invalid choice.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
        Console.WriteLine("Done.");
    }
}
`},
 {name:"LinkedList.cs", title:"Singly linked list", desc:"Node class, add, remove, print", code:
`using System;

class Node
{
    public int Data;
    public Node Next;
    public Node(int data) { Data = data; }
}

class SinglyLinkedList
{
    private Node head;

    public void AddLast(int data)
    {
        Node node = new Node(data);
        if (head == null) { head = node; return; }
        Node current = head;
        while (current.Next != null) current = current.Next;
        current.Next = node;
    }

    public bool Remove(int data)
    {
        if (head == null) return false;
        if (head.Data == data) { head = head.Next; return true; }
        Node current = head;
        while (current.Next != null && current.Next.Data != data) current = current.Next;
        if (current.Next == null) return false;
        current.Next = current.Next.Next;
        return true;
    }

    public void Print()
    {
        Node current = head;
        while (current != null)
        {
            Console.Write(current.Data + " -> ");
            current = current.Next;
        }
        Console.WriteLine("null");
    }
}

class Program
{
    static void Main()
    {
        SinglyLinkedList list = new SinglyLinkedList();
        foreach (int n in new[] { 10, 20, 30, 40 }) list.AddLast(n);
        list.Print();

        list.Remove(20);
        Console.Write("After removing 20: ");
        list.Print();
    }
}
`},
 {name:"Sorting.cs", title:"Bubble & selection sort", desc:"Nested loops, swapping", code:
`using System;

class Program
{
    static void BubbleSort(int[] a)
    {
        for (int i = 0; i < a.Length - 1; i++)
            for (int j = 0; j < a.Length - 1 - i; j++)
                if (a[j] > a[j + 1])
                {
                    int temp = a[j];
                    a[j] = a[j + 1];
                    a[j + 1] = temp;
                }
    }

    static void SelectionSort(int[] a)
    {
        for (int i = 0; i < a.Length - 1; i++)
        {
            int min = i;
            for (int j = i + 1; j < a.Length; j++)
                if (a[j] < a[min]) min = j;
            (a[i], a[min]) = (a[min], a[i]);
        }
    }

    static void Main()
    {
        int[] data = { 64, 25, 12, 22, 11, 90, 3 };
        Console.WriteLine("Original:  " + string.Join(", ", data));

        int[] b = (int[])data.Clone();
        BubbleSort(b);
        Console.WriteLine("Bubble:    " + string.Join(", ", b));

        int[] s = (int[])data.Clone();
        SelectionSort(s);
        Console.WriteLine("Selection: " + string.Join(", ", s));
    }
}
`},
 {name:"BinarySearch.cs", title:"Binary search", desc:"Search a sorted array step by step", code:
`using System;

class Program
{
    static int BinarySearch(int[] arr, int target)
    {
        int low = 0, high = arr.Length - 1;
        while (low <= high)
        {
            int mid = (low + high) / 2;
            Console.WriteLine($"  checking index {mid} (value {arr[mid]})");
            if (arr[mid] == target) return mid;
            if (arr[mid] < target) low = mid + 1;
            else high = mid - 1;
        }
        return -1;
    }

    static void Main()
    {
        int[] numbers = { 3, 8, 15, 23, 42, 56, 71, 89, 94 };
        Console.WriteLine("Array: " + string.Join(" ", numbers));
        Console.Write("Search for: ");
        int target = int.Parse(Console.ReadLine());

        int index = BinarySearch(numbers, target);
        Console.WriteLine(index >= 0 ? $"Found at index {index}." : "Not found.");
    }
}
`},
 {name:"Recursion.cs", title:"Recursion", desc:"Factorial, Fibonacci, Tower of Hanoi", code:
`using System;

class Program
{
    static long Factorial(int n) => n <= 1 ? 1 : n * Factorial(n - 1);

    static int Fibonacci(int n) => n < 2 ? n : Fibonacci(n - 1) + Fibonacci(n - 2);

    static void Hanoi(int disks, char from, char to, char via)
    {
        if (disks == 0) return;
        Hanoi(disks - 1, from, via, to);
        Console.WriteLine($"Move disk {disks} from {from} to {to}");
        Hanoi(disks - 1, via, to, from);
    }

    static void Main()
    {
        for (int i = 1; i <= 10; i++)
            Console.WriteLine($"{i}! = {Factorial(i)}");

        Console.Write("Fibonacci: ");
        for (int i = 0; i < 12; i++) Console.Write(Fibonacci(i) + " ");
        Console.WriteLine();

        Console.WriteLine("Tower of Hanoi (3 disks):");
        Hanoi(3, 'A', 'C', 'B');
    }
}
`},
 {name:"BankAccount.cs", title:"Classes & exceptions", desc:"Properties, methods, try/catch", code:
`using System;

class BankAccount
{
    public string Owner { get; }
    public decimal Balance { get; private set; }

    public BankAccount(string owner, decimal balance)
    {
        Owner = owner;
        Balance = balance;
    }

    public void Deposit(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Deposit must be positive.");
        Balance += amount;
    }

    public void Withdraw(decimal amount)
    {
        if (amount > Balance) throw new InvalidOperationException("Insufficient funds.");
        Balance -= amount;
    }

    public override string ToString() => $"{Owner}: PHP {Balance:N2}";
}

class Program
{
    static void Main()
    {
        var account = new BankAccount("Juan", 1500m);
        account.Deposit(2500m);
        account.Withdraw(800m);
        Console.WriteLine(account);

        try
        {
            account.Withdraw(10000m);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
`}
];

/* ---------- storage ---------- */
const KEY = "csmobile.v1";
function uid() { return Math.random().toString(36).slice(2, 9); }
function load() {
  try { const s = JSON.parse(localStorage.getItem(KEY)); if (s && Array.isArray(s.files) && s.files.length) return s; } catch (e) {}
  return { files: [{ id: uid(), name: "Program.cs", code: EXAMPLES[0].code }], current: null, fs: 15, theme: null };
}
function save() { try { localStorage.setItem(KEY, JSON.stringify(state)); } catch (e) {} }
const state = load();
if (!state.current || !state.files.find(f => f.id === state.current)) state.current = state.files[0].id;
const cur = () => state.files.find(f => f.id === state.current);

/* ---------- theme & font ---------- */
function applyTheme() { if (state.theme) document.documentElement.setAttribute("data-theme", state.theme); else document.documentElement.removeAttribute("data-theme"); }
applyTheme();
$("themeBtn").onclick = () => { const dark = state.theme ? state.theme === "dark" : matchMedia("(prefers-color-scheme: dark)").matches; state.theme = dark ? "light" : "dark"; applyTheme(); save(); };
function applyFs() { document.documentElement.style.setProperty("--fs", state.fs + "px"); if (ed) ed.refresh(); }
$("fontUp").onclick = () => { state.fs = Math.min(24, (state.fs || 15) + 1); applyFs(); save(); };
$("fontDown").onclick = () => { state.fs = Math.max(11, (state.fs || 15) - 1); applyFs(); save(); };

let tt; function toast(m) { const t = $("toast"); t.textContent = m; t.classList.add("on"); clearTimeout(tt); tt = setTimeout(() => t.classList.remove("on"), 2000); }

/* ---------- editor ---------- */
let ed = null;
function initEditor() {
  if (!window.CodeMirror) { $("loadTitle").textContent = "Couldn't load the editor. Check your connection and reload."; return; }
  ed = CodeMirror($("edwrap"), {
    value: cur().code, mode: "text/x-csharp", lineNumbers: true, indentUnit: 4, tabSize: 4,
    matchBrackets: true, autoCloseBrackets: true, styleActiveLine: true, viewportMargin: 20,
    extraKeys: {
      "Tab": cm => cm.somethingSelected() ? cm.indentSelection("add") : cm.replaceSelection("    "),
      "Shift-Tab": cm => cm.indentSelection("subtract"),
      "Ctrl-Enter": () => run(), "Cmd-Enter": () => run(), "F5": () => run()
    }
  });
  let st; ed.on("change", () => { clearErr(); clearTimeout(st); st = setTimeout(() => { cur().code = ed.getValue(); save(); }, 300); });
  applyFs();
}
let errLine = null;
function clearErr() { if (errLine !== null && ed) { ed.removeLineClass(errLine, "background", "cm-errline"); errLine = null; } }
function markErr(n) { if (!ed || !n) return; clearErr(); errLine = n - 1; ed.addLineClass(errLine, "background", "cm-errline"); }
function gotoLine(n) { setView("code"); if (!ed) return; ed.focus(); ed.setCursor({ line: n - 1, ch: 0 }); ed.scrollIntoView({ line: n - 1, ch: 0 }, 80); }

/* ---------- key bar ---------- */
const KEYS = [["⇥","tab","Indent"],["⇤","dedent","Outdent"],[";"],["{","}"],["(",")"],["[","]"],['"','"'],["'","'"],["="],["<"],[">"],["+"],["-"],["*"],["/"],["%"],["!"],["&"],["|"],["."],[","],[":"],["?"],["$"],["_"],["undo","undo","Undo"],["redo","redo","Redo"]];
KEYS.forEach(k => {
  const [a, c, label] = k; const b = document.createElement("button"); b.type = "button";
  b.textContent = (a === "undo" || a === "redo") ? label : (c && c.length === 1 && c !== a ? a + c : a);
  if (label) b.setAttribute("aria-label", label); if (a === "undo" || a === "redo") b.className = "wide";
  b.addEventListener("mousedown", e => e.preventDefault());
  b.onclick = () => {
    if (!ed) return; ed.focus();
    if (c === "tab") { if (ed.somethingSelected()) ed.execCommand("indentMore"); else ed.replaceSelection("    "); }
    else if (c === "dedent") ed.execCommand("indentLess");
    else if (c === "undo") ed.undo(); else if (c === "redo") ed.redo();
    else if (c && c.length === 1) { const sel = ed.getSelection(); ed.replaceSelection(a + sel + c); if (!sel) { const p = ed.getCursor(); ed.setCursor({ line: p.line, ch: p.ch - 1 }); } }
    else ed.replaceSelection(a);
  };
  $("keys").appendChild(b);
});

/* ---------- views ---------- */
function setView(v) { $("work").dataset.view = v; $("tabCode").setAttribute("aria-selected", v === "code"); $("tabOut").setAttribute("aria-selected", v === "out"); if (v === "out") $("outDot").hidden = true; if (v === "code" && ed) setTimeout(() => ed.refresh(), 0); }
$("tabCode").onclick = () => setView("code"); $("tabOut").onclick = () => setView("out");
const narrow = () => matchMedia("(max-width:760px)").matches;

/* ---------- console ---------- */
const con = $("console");
const COLORS = { Black:"#000000", DarkBlue:"#3b5bdb", DarkGreen:"#2f9e44", DarkCyan:"#1098ad", DarkRed:"#e03131", DarkMagenta:"#ae3ec9", DarkYellow:"#f08c00", Gray:null, DarkGray:"#868e96", Blue:"#74a7ff", Green:"#69db7c", Cyan:"#66d9e8", Red:"#ff8787", Magenta:"#e599f7", Yellow:"#ffe066", White:"#ffffff" };
const BGS = { Black:null, DarkBlue:"#1c2a6b", DarkGreen:"#173d22", DarkCyan:"#0b3c45", DarkRed:"#5c1414", DarkMagenta:"#4b1a59", DarkYellow:"#5a3d00", Gray:"#868e96", DarkGray:"#495057", Blue:"#3b5bdb", Green:"#2f9e44", Cyan:"#1098ad", Red:"#e03131", Magenta:"#ae3ec9", Yellow:"#f08c00", White:"#f1f3f5" };
function showOutput() { $("empty").hidden = true; if (narrow() && $("work").dataset.view !== "out") $("outDot").hidden = false; }
function append(text, cls, fg, bg) {
  showOutput();
  const last = con.lastChild;
  if (!cls && !fg && !bg && last && last.nodeType === 3) last.appendData(text);
  else if (cls || fg || bg) {
    const s = document.createElement("span"); if (cls) s.className = cls;
    if (fg && COLORS[fg]) s.style.color = COLORS[fg];
    if (bg && BGS[bg]) s.style.background = BGS[bg];
    s.textContent = text; con.appendChild(s);
  } else con.appendChild(document.createTextNode(text));
  trim(); scrollOut();
}
function trim() { while (con.childNodes.length > 4000) con.removeChild(con.firstChild); }
function scrollOut() { const o = $("outscroll"); o.scrollTop = o.scrollHeight; }
function lastLine() { const t = con.textContent; const i = t.lastIndexOf("\n"); return t.slice(i + 1); }
function setStatus(t, cls) { const s = $("status"); s.textContent = t; s.className = "pill" + (cls ? " " + cls : ""); }
$("clearOut").onclick = () => { if (running) return; con.textContent = ""; $("empty").hidden = false; setStatus(ready ? "Ready" : "Loading…"); };
function copyText(txt, msg) {
  const done = () => toast(msg);
  const fallback = () => { const ta = document.createElement("textarea"); ta.value = txt; document.body.appendChild(ta); ta.select(); try { document.execCommand("copy"); done(); } catch (e) { toast("Select the text and copy it manually"); } ta.remove(); };
  try { navigator.clipboard.writeText(txt).then(done, fallback); } catch (e) { fallback(); }
}
$("copyOut").onclick = () => copyText(con.innerText, "Output copied");

/* ---------- bridge used by .NET ---------- */
let ready = false, running = false, pendingInput = null;
window.csm = {
  write(text, fg, bg) { append(text, null, fg, bg); },
  clear() { con.textContent = ""; },
  readLine(kind) {
    return new Promise(resolve => {
      setStatus("Waiting for input", "waiting");
      if (narrow()) setView("out");
      showOutput();
      const isKey = kind.startsWith("key"), hidden = kind === "key-hidden";
      const inp = document.createElement("input");
      inp.className = "cin" + (isKey ? " key" : ""); inp.id = "cin";
      inp.autocomplete = "off"; inp.autocapitalize = "off"; inp.spellcheck = false;
      inp.setAttribute("aria-label", isKey ? "Press a key" : "Program input");
      if (isKey) inp.placeholder = "key";
      con.appendChild(inp); scrollOut(); setTimeout(() => inp.focus(), 30);
      const finish = (value, echo) => {
        if (!pendingInput) return;
        pendingInput = null; inp.remove();
        if (echo !== null) append(echo, "in");
        append("\n");
        setStatus("Running…", "running");
        resolve(value);
      };
      pendingInput = { cancel: () => { pendingInput = null; inp.remove(); resolve(null); } };
      if (isKey) {
        inp.addEventListener("keydown", e => {
          const named = ["Enter","Escape","Backspace","Tab","ArrowUp","ArrowDown","ArrowLeft","ArrowRight"];
          if (named.includes(e.key)) { e.preventDefault(); finish(e.key === "Enter" ? "\n" : e.key, hidden ? null : ""); }
          else if (e.key && e.key.length === 1) { e.preventDefault(); finish(e.key, hidden ? null : e.key); }
        });
        inp.addEventListener("input", () => { const v = inp.value; if (v) finish(v[0], hidden ? null : v[0]); });
      } else {
        inp.addEventListener("keydown", e => { if (e.key === "Enter") { e.preventDefault(); finish(inp.value, inp.value); } });
      }
    });
  },
  promptSync(kind) {
    const isKey = kind.startsWith("key");
    const msg = lastLine().trim() || (isKey ? "Press a key, then OK" : "Input:");
    const r = window.prompt(msg, "");
    if (r === null) return null;
    if (isKey) { const ch = r.length ? r[0] : "\n"; if (kind !== "key-hidden" && r.length) append(ch, "in"); append("\n"); return ch; }
    append(r, "in"); append("\n");
    return r;
  },
  dotnetReady() {
    ready = true;
    $("loadTitle").textContent = "Starting the compiler…";
    setTimeout(async () => {
      setStatus("Preparing compiler…");
      try { await DotNet.invokeMethodAsync("CSharpMobile", "Warmup"); } catch (e) { console.warn(e); }
      hideLoader();
      $("runBtn").disabled = false;
      if (!running) setStatus("Ready");
    }, 50);
  },
  loadFailed(err) {
    $("loadTitle").textContent = "C# couldn't start";
    $("loadNote").textContent = "Check your internet connection and reload the page. (" + (err && err.message ? err.message : err) + ")";
    setStatus("Failed to load", "error");
  }
};
function hideLoader() { const l = $("loader"); if (l) l.remove(); }
$("skipLoad").onclick = hideLoader;

/* ---------- run ---------- */
function setRunBtn() {
  const b = $("runBtn"); b.classList.toggle("stop", running);
  b.querySelector("span").textContent = running ? "Stop" : "Run";
  b.querySelector("svg").innerHTML = running ? '<rect x="6" y="6" width="12" height="12" rx="1"/>' : '<path d="M7 4l13 8-13 8z"/>';
}
const frame = () => new Promise(r => requestAnimationFrame(() => setTimeout(r, 0)));

async function run() {
  if (running) { stop(); return; }
  if (!ready) { toast("C# is still loading"); return; }
  const code = ed ? ed.getValue() : cur().code; cur().code = code; save();
  clearErr(); con.textContent = ""; showOutput();
  running = true; setRunBtn(); setStatus("Compiling…", "running");
  if (narrow()) setView("out");
  append("Compiling…\n", "sys");
  await frame();
  let res;
  try { res = await DotNet.invokeMethodAsync("CSharpMobile", "Run", code); }
  catch (e) { res = { status: "internal", errorMessage: String(e && e.message || e) }; }
  running = false; setRunBtn();
  if (pendingInput) pendingInput.cancel();
  if (con.firstChild && con.firstChild.textContent === "Compiling…\n" && res.status !== "compile") con.removeChild(con.firstChild);
  const secs = ms => ms < 1000 ? ms + " ms" : (ms / 1000).toFixed(1) + " s";
  if (lastLine() !== "" && res.status !== "compile") append("\n");
  switch (res.status) {
    case "ok":
      setStatus("Finished · " + secs(res.runMs));
      if (res.mode === "prompt") append("(This program used pop-up boxes for input.)\n", "sys");
      break;
    case "compile": {
      con.textContent = "";
      append("Build failed: " + res.diagnostics.length + " error" + (res.diagnostics.length > 1 ? "s" : "") + "\n\n", "err");
      res.diagnostics.forEach(d => {
        if (d.line) { const b = document.createElement("button"); b.className = "jump"; b.textContent = "Line " + d.line; b.onclick = () => gotoLine(d.line); con.appendChild(b); append(": ", "err"); }
        append(d.message + " (" + d.id + ")\n", "err");
      });
      const first = res.diagnostics.find(d => d.line);
      if (first) { markErr(first.line); setStatus("Build failed · line " + first.line, "error"); } else setStatus("Build failed", "error");
      break;
    }
    case "error":
      append("Unhandled exception. " + res.errorType + ": " + res.errorMessage + "\n", "err");
      if (res.errorLine) {
        const b = document.createElement("button"); b.className = "jump"; b.textContent = "Go to line " + res.errorLine; b.onclick = () => gotoLine(res.errorLine);
        con.appendChild(b); append("\n"); markErr(res.errorLine); setStatus("Error · line " + res.errorLine, "error");
      } else setStatus("Error", "error");
      break;
    case "stopped": append("[Program stopped]\n", "sys"); setStatus("Stopped"); break;
    case "timeout": append(res.errorMessage + "\n", "err"); setStatus("Stopped · too long", "error"); break;
    default: append("Something went wrong inside C#Mobile: " + (res.errorMessage || "unknown") + "\n", "err"); setStatus("Error", "error");
  }
  if (res.debug) console.info("[csmobile]", res.mode, res.debug);
  scrollOut();
}
function stop() {
  try { DotNet.invokeMethodAsync("CSharpMobile", "Stop"); } catch (e) {}
  if (pendingInput) pendingInput.cancel();
}
$("runBtn").onclick = run;

/* ---------- files drawer ---------- */
function openDrawer(on) { $("drawer").classList.toggle("on", on); $("scrim").classList.toggle("on", on); if (on) renderFiles(); }
$("menuBtn").onclick = () => openDrawer(true); $("closeDrawer").onclick = () => openDrawer(false); $("scrim").onclick = () => openDrawer(false);
function uniqueName(n) { const base = n.replace(/\.cs$/, ""); let name = base + ".cs", i = 2; while (state.files.some(f => f.name === name)) name = base + "_" + (i++) + ".cs"; return name; }
function switchTo(id) { if (ed) cur().code = ed.getValue(); state.current = id; save(); if (ed) { ed.setValue(cur().code); ed.clearHistory(); } $("fnameBtn").textContent = cur().name; clearErr(); }
let renaming = null, confirmDel = null;
const I = { edit: '<svg viewBox="0 0 24 24"><path d="M4 20h4L19 9l-4-4L4 16z"/></svg>', del: '<svg viewBox="0 0 24 24"><path d="M4 7h16M9 7V4h6v3M6 7l1 13h10l1-13"/></svg>' };
function renderFiles() {
  const L = $("fileList"); L.innerHTML = "";
  state.files.forEach(f => {
    if (renaming === f.id) {
      const r = document.createElement("div"); r.className = "rename"; r.innerHTML = '<input id="renameInput"><button class="textbtn">Save</button>';
      const inp = r.querySelector("input"); inp.value = f.name;
      const commit = () => { let v = inp.value.trim(); if (v) { if (!/\.\w+$/.test(v)) v += ".cs"; if (v !== f.name && state.files.some(x => x.name === v)) { toast("A file with that name exists"); return; } f.name = v; save(); $("fnameBtn").textContent = cur().name; } renaming = null; renderFiles(); };
      r.querySelector("button").onclick = commit; inp.onkeydown = e => { if (e.key === "Enter") commit(); if (e.key === "Escape") { renaming = null; renderFiles(); } };
      L.appendChild(r); setTimeout(() => { inp.focus(); inp.select(); }, 20); return;
    }
    const r = document.createElement("div"); r.className = "row" + (f.id === state.current ? " cur" : "");
    const o = document.createElement("button"); o.className = "open"; o.textContent = f.name; o.onclick = () => { switchTo(f.id); openDrawer(false); setView("code"); };
    const e = document.createElement("button"); e.className = "mini"; e.innerHTML = I.edit; e.setAttribute("aria-label", "Rename " + f.name); e.onclick = () => { renaming = f.id; confirmDel = null; renderFiles(); };
    const d = document.createElement("button"); d.className = "mini" + (confirmDel === f.id ? " danger" : ""); d.innerHTML = confirmDel === f.id ? "<small>Sure?</small>" : I.del; d.setAttribute("aria-label", "Delete " + f.name);
    d.onclick = () => {
      if (confirmDel !== f.id) { confirmDel = f.id; renderFiles(); return; }
      if (state.files.length === 1) { f.code = ""; f.name = "Program.cs"; if (ed) ed.setValue(""); }
      else { state.files = state.files.filter(x => x.id !== f.id); if (state.current === f.id) switchTo(state.files[0].id); }
      confirmDel = null; save(); $("fnameBtn").textContent = cur().name; renderFiles(); toast("File deleted");
    };
    r.append(o, e, d); L.appendChild(r);
  });
}
function addFile(name, code) { if (ed) cur().code = ed.getValue(); const f = { id: uid(), name: uniqueName(name), code }; state.files.push(f); switchTo(f.id); renderFiles(); return f; }
$("newFile").onclick = () => { addFile("Untitled.cs", "using System;\n\nclass Program\n{\n    static void Main()\n    {\n        \n    }\n}\n"); openDrawer(false); setView("code"); if (ed) { ed.focus(); ed.setCursor({ line: 6, ch: 8 }); } };
$("fnameBtn").onclick = () => { openDrawer(true); renaming = state.current; renderFiles(); };
$("importBtn").onclick = () => $("importInput").click();
$("importInput").onchange = e => { const file = e.target.files[0]; if (!file) return; const rd = new FileReader(); rd.onload = () => { addFile(file.name.replace(/\.txt$/, ".cs"), String(rd.result)); openDrawer(false); setView("code"); toast("Opened " + file.name); }; rd.readAsText(file); e.target.value = ""; };
$("copyCode").onclick = () => copyText(ed ? ed.getValue() : cur().code, "Code copied");
EXAMPLES.forEach(x => {
  const b = document.createElement("button"); b.className = "ex"; b.innerHTML = "<b></b><span></span>";
  b.querySelector("b").textContent = x.title; b.querySelector("span").textContent = x.desc;
  b.onclick = () => { addFile(x.name, x.code); openDrawer(false); setView("code"); toast("Opened " + x.title); };
  $("exList").appendChild(b);
});

$("fnameBtn").textContent = cur().name;
initEditor();
window.__csmTest = { run, setCode: c => ed && ed.setValue(c), isReady: () => ready && !$("runBtn").disabled, isRunning: () => running, output: () => con.textContent };
})();
