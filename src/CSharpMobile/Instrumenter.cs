using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpMobile;

internal enum InstrumentMode { Async, Sync }

/// <summary>
/// Rewrites student code so it can run in the browser:
///  - System.Console calls go to global::__Con
///  - (Async mode) Console.ReadLine/Read/ReadKey become awaited, and every method that
///    reaches them becomes async, so the program can pause for typing without freezing the page
///  - loops get a stop/infinite-loop check, methods get a stack-depth check
/// Line numbers are preserved: nothing adds or removes line breaks.
/// </summary>
internal sealed class Instrumenter
{
    static readonly HashSet<string> InputMethods = new HashSet<string> { "ReadLine", "Read", "ReadKey" };

    readonly SemanticModel _model;
    readonly SyntaxTree _tree;
    readonly InstrumentMode _mode;
    readonly INamedTypeSymbol _console;
    readonly HashSet<SyntaxNode> _asyncFunctions = new HashSet<SyntaxNode>();
    readonly HashSet<IMethodSymbol> _asyncSymbols = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
    readonly Dictionary<IMethodSymbol, SyntaxNode> _userMethods = new Dictionary<IMethodSymbol, SyntaxNode>(SymbolEqualityComparer.Default);

    public Instrumenter(SemanticModel model, SyntaxTree tree, InstrumentMode mode)
    {
        _model = model;
        _tree = tree;
        _mode = mode;
        _console = model.Compilation.GetTypeByMetadataName("System.Console");
    }

    public SyntaxNode Rewrite(out string reason)
    {
        reason = null;
        var root = _tree.GetRoot();

        foreach (var node in root.DescendantNodes())
        {
            if (node is MethodDeclarationSyntax || node is LocalFunctionStatementSyntax)
            {
                if (_model.GetDeclaredSymbol(node) is IMethodSymbol ms) _userMethods[ms] = node;
            }
        }

        if (_mode == InstrumentMode.Async && !PlanAsync(root, out reason)) return null;

        var targets = new List<SyntaxNode>();
        foreach (var node in root.DescendantNodes())
        {
            switch (node)
            {
                case InvocationExpressionSyntax inv when NeedsInvocationRewrite(inv):
                case MemberAccessExpressionSyntax ma when IsConsoleMemberAccess(ma):
                case IdentifierNameSyntax id when IsConsoleStaticImportName(id):
                case WhileStatementSyntax _:
                case DoStatementSyntax _:
                case ForStatementSyntax _:
                case CommonForEachStatementSyntax _:
                case MethodDeclarationSyntax _:
                case LocalFunctionStatementSyntax _:
                case ConstructorDeclarationSyntax _:
                    targets.Add(node);
                    break;
            }
        }

        return root.ReplaceNodes(targets, Transform);
    }

    // ------------------------------------------------------------------ planning

    bool PlanAsync(SyntaxNode root, out string reason)
    {
        reason = null;
        var edges = new Dictionary<SyntaxNode, HashSet<SyntaxNode>>();
        var need = new HashSet<SyntaxNode>();
        var callSites = new List<(InvocationExpressionSyntax inv, SyntaxNode callee)>();
        var inputSites = new List<InvocationExpressionSyntax>();

        foreach (var inv in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var sym = GetMethod(inv);
            if (sym == null) continue;
            var caller = EnclosingFunction(inv);
            if (IsInput(sym))
            {
                need.Add(caller);
                inputSites.Add(inv);
                continue;
            }
            var callee = UserMethodNode(sym);
            if (callee == null) continue;
            if (!edges.TryGetValue(callee, out var callers)) edges[callee] = callers = new HashSet<SyntaxNode>();
            callers.Add(caller);
            callSites.Add((inv, callee));
        }

        // Every caller of an async function becomes async, transitively.
        var queue = new Queue<SyntaxNode>(need);
        while (queue.Count > 0)
        {
            var n = queue.Dequeue();
            if (!edges.TryGetValue(n, out var callers)) continue;
            foreach (var c in callers) if (need.Add(c)) queue.Enqueue(c);
        }

        foreach (var n in need)
        {
            if (n is CompilationUnitSyntax) continue;
            if (!(n is MethodDeclarationSyntax || n is LocalFunctionStatementSyntax))
            {
                reason = "input used inside " + n.Kind();
                return false;
            }
            if (!CanBecomeAsync(n, out reason)) return false;
        }

        // Make Main async too, so loops in Main can yield to the page (live output + Stop button).
        var entry = _model.Compilation.GetEntryPoint(default);
        if (entry != null && !entry.IsImplicitlyDeclared && _userMethods.TryGetValue(entry, out var mainNode) && !need.Contains(mainNode))
        {
            if (CanBecomeAsync(mainNode, out _) && !IsReferencedOutsideInvocation(entry, root)) need.Add(mainNode);
        }

        foreach (var n in need)
        {
            _asyncFunctions.Add(n);
            if (!(n is CompilationUnitSyntax) && _model.GetDeclaredSymbol(n) is IMethodSymbol ms) _asyncSymbols.Add(ms);
        }

        foreach (var inv in inputSites)
        {
            if (!CanAwaitAt(inv)) { reason = "input call in a spot that cannot await"; return false; }
        }
        foreach (var (inv, callee) in callSites)
        {
            if (!_asyncFunctions.Contains(callee)) continue;
            if (!CanAwaitAt(inv)) { reason = "call to an input method in a spot that cannot await"; return false; }
        }
        foreach (var ms in _asyncSymbols)
        {
            if (IsReferencedOutsideInvocation(ms, root)) { reason = ms.Name + " is used as a delegate"; return false; }
        }
        return true;
    }

    bool CanBecomeAsync(SyntaxNode n, out string reason)
    {
        reason = null;
        if (!(_model.GetDeclaredSymbol(n) is IMethodSymbol ms)) { reason = "no symbol"; return false; }
        if (ms.IsOverride || ms.IsVirtual || ms.IsAbstract || ms.IsExtern) { reason = ms.Name + " is virtual/override"; return false; }
        if (ms.ReturnsByRef || ms.ReturnsByRefReadonly) { reason = ms.Name + " returns by ref"; return false; }
        if (ms.Parameters.Any(p => p.RefKind != RefKind.None)) { reason = ms.Name + " has ref/out parameters"; return false; }
        if (n.DescendantNodes().Any(d => d is YieldStatementSyntax && EnclosingFunction(d) == n)) { reason = ms.Name + " is an iterator"; return false; }
        if (ImplementsInterface(ms)) { reason = ms.Name + " implements an interface"; return false; }
        if (ms.IsAsync)
        {
            if (ms.ReturnsVoid) { reason = ms.Name + " is async void"; return false; }
            return true;
        }
        if (IsTaskLike(ms.ReturnType)) { reason = ms.Name + " already returns a Task"; return false; }
        if (ms.ReturnType.IsRefLikeType) { reason = ms.Name + " returns a ref struct"; return false; }
        return true;
    }

    static bool ImplementsInterface(IMethodSymbol ms)
    {
        if (ms.ExplicitInterfaceImplementations.Length > 0) return true;
        var type = ms.ContainingType;
        if (type == null) return false;
        foreach (var iface in type.AllInterfaces)
            foreach (var member in iface.GetMembers().OfType<IMethodSymbol>())
                if (SymbolEqualityComparer.Default.Equals(type.FindImplementationForInterfaceMember(member), ms)) return true;
        return false;
    }

    static bool IsTaskLike(ITypeSymbol t)
    {
        var ns = t.ContainingNamespace?.ToDisplayString();
        return ns == "System.Threading.Tasks" && (t.Name == "Task" || t.Name == "ValueTask");
    }

    bool IsReferencedOutsideInvocation(IMethodSymbol target, SyntaxNode root)
    {
        foreach (var id in root.DescendantNodes().OfType<SimpleNameSyntax>())
        {
            if (id.Identifier.ValueText != target.Name) continue;
            var sym = _model.GetSymbolInfo(id).Symbol as IMethodSymbol;
            if (sym == null || !SymbolEqualityComparer.Default.Equals(Normalize(sym), target)) continue;
            SyntaxNode expr = id;
            if (id.Parent is MemberAccessExpressionSyntax ma && ma.Name == id) expr = ma;
            if (id.Parent is MemberBindingExpressionSyntax mb && mb.Name == id) expr = mb;
            if (expr.Parent is InvocationExpressionSyntax inv && inv.Expression == expr) continue;
            return true;
        }
        return false;
    }

    bool CanAwaitAt(InvocationExpressionSyntax inv)
    {
        if (inv.Expression is MemberBindingExpressionSyntax) return false; // x?.M()
        return IsAsyncContext(inv);
    }

    /// <summary>True if an await can be placed at this node.</summary>
    bool IsAsyncContext(SyntaxNode node)
    {
        if (_mode != InstrumentMode.Async) return false;
        for (var p = node.Parent; p != null; p = p.Parent)
        {
            if (p is LockStatementSyntax || p is UnsafeStatementSyntax || p is FixedStatementSyntax || p is QueryExpressionSyntax) return false;
            if (p is CatchFilterClauseSyntax) return false;
            if (IsFunctionBoundary(p)) return _asyncFunctions.Contains(p);
            if (p is GlobalStatementSyntax) return true;
        }
        return false;
    }

    static bool IsFunctionBoundary(SyntaxNode p) =>
        p is MethodDeclarationSyntax || p is LocalFunctionStatementSyntax || p is ConstructorDeclarationSyntax ||
        p is DestructorDeclarationSyntax || p is AccessorDeclarationSyntax || p is OperatorDeclarationSyntax ||
        p is ConversionOperatorDeclarationSyntax || p is AnonymousFunctionExpressionSyntax ||
        p is PropertyDeclarationSyntax || p is IndexerDeclarationSyntax || p is FieldDeclarationSyntax ||
        p is EventFieldDeclarationSyntax || p is BaseTypeDeclarationSyntax;

    /// <summary>The function a node runs in; top-level statements map to the compilation unit.</summary>
    static SyntaxNode EnclosingFunction(SyntaxNode node)
    {
        for (var p = node.Parent; p != null; p = p.Parent)
        {
            if (IsFunctionBoundary(p)) return p;
            if (p is GlobalStatementSyntax) return p.Parent;
        }
        return node.SyntaxTree.GetRoot();
    }

    // ------------------------------------------------------------------ helpers

    IMethodSymbol GetMethod(InvocationExpressionSyntax inv)
    {
        var info = _model.GetSymbolInfo(inv);
        return info.Symbol as IMethodSymbol ?? info.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
    }

    static IMethodSymbol Normalize(IMethodSymbol m) => (m.ReducedFrom ?? m).OriginalDefinition;

    SyntaxNode UserMethodNode(IMethodSymbol sym) =>
        _userMethods.TryGetValue(Normalize(sym), out var n) ? n : null;

    bool IsConsole(ISymbol s) => s != null && SymbolEqualityComparer.Default.Equals(s.ContainingType, _console);
    bool IsInput(IMethodSymbol m) => IsConsole(m) && InputMethods.Contains(m.Name);

    bool IsConsoleMemberAccess(MemberAccessExpressionSyntax ma)
    {
        var info = _model.GetSymbolInfo(ma);
        var s = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
        return s != null && !(s is ITypeSymbol) && IsConsole(s);
    }

    bool IsConsoleStaticImportName(IdentifierNameSyntax id)
    {
        if (id.Parent is MemberAccessExpressionSyntax ma && ma.Name == id) return false;
        if (id.Parent is QualifiedNameSyntax || id.Parent is MemberBindingExpressionSyntax) return false;
        if (id.Ancestors().Any(a => a is UsingDirectiveSyntax)) return false;
        var info = _model.GetSymbolInfo(id);
        var s = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
        return s != null && !(s is ITypeSymbol) && IsConsole(s);
    }

    bool NeedsInvocationRewrite(InvocationExpressionSyntax inv)
    {
        if (_mode != InstrumentMode.Async) return false;
        var sym = GetMethod(inv);
        if (sym == null) return false;
        if (IsInput(sym)) return true;
        var callee = UserMethodNode(sym);
        return callee != null && _asyncFunctions.Contains(callee);
    }

    // ------------------------------------------------------------------ transforms

    SyntaxNode Transform(SyntaxNode original, SyntaxNode current)
    {
        switch (original)
        {
            case MemberAccessExpressionSyntax ma:
                return SyntaxFactory.ParseExpression("global::__Con." + ma.Name.Identifier.ValueText).WithTriviaFrom(current);
            case IdentifierNameSyntax id:
                return SyntaxFactory.ParseExpression("global::__Con." + id.Identifier.ValueText).WithTriviaFrom(current);
            case InvocationExpressionSyntax inv:
                return TransformInvocation(inv, (InvocationExpressionSyntax)current);
            case WhileStatementSyntax _:
            case DoStatementSyntax _:
            case ForStatementSyntax _:
            case CommonForEachStatementSyntax _:
                return TransformLoop(original, current);
            case MethodDeclarationSyntax md:
                return TransformMethod(md, (MethodDeclarationSyntax)current);
            case LocalFunctionStatementSyntax lf:
                return TransformLocalFunction(lf, (LocalFunctionStatementSyntax)current);
            case ConstructorDeclarationSyntax _:
                var ctor = (ConstructorDeclarationSyntax)current;
                return ctor.Body != null ? ctor.WithBody(InsertEnter(ctor.Body)) : ctor;
        }
        return current;
    }

    SyntaxNode TransformInvocation(InvocationExpressionSyntax original, InvocationExpressionSyntax current)
    {
        var sym = GetMethod(original);
        ExpressionSyntax call = current;
        if (sym != null && IsInput(sym))
        {
            var name = "global::__Con." + sym.Name + "Async";
            call = current.WithExpression(SyntaxFactory.ParseExpression(name).WithTriviaFrom(current.Expression));
        }
        return Await(call, original);
    }

    static ExpressionSyntax Await(ExpressionSyntax expr, SyntaxNode original)
    {
        var inner = expr.WithoutTrivia();
        var awaitExpr = SyntaxFactory.AwaitExpression(
            SyntaxFactory.Token(SyntaxKind.AwaitKeyword).WithTrailingTrivia(SyntaxFactory.Space), inner);
        bool needsParens =
            (original.Parent is MemberAccessExpressionSyntax ma && ma.Expression == original) ||
            (original.Parent is ElementAccessExpressionSyntax ea && ea.Expression == original) ||
            (original.Parent is ConditionalAccessExpressionSyntax ca && ca.Expression == original) ||
            (original.Parent is InvocationExpressionSyntax iv && iv.Expression == original) ||
            original.Parent is PostfixUnaryExpressionSyntax;
        ExpressionSyntax result = needsParens ? SyntaxFactory.ParenthesizedExpression(awaitExpr) : awaitExpr;
        return result.WithTriviaFrom(expr);
    }

    SyntaxNode TransformLoop(SyntaxNode original, SyntaxNode current)
    {
        var tick = IsAsyncContext(original)
            ? SyntaxFactory.ParseStatement("await global::__Rt.TickAsync();")
            : SyntaxFactory.ParseStatement("global::__Rt.Tick();");
        StatementSyntax body = current switch
        {
            WhileStatementSyntax w => w.Statement,
            DoStatementSyntax d => d.Statement,
            ForStatementSyntax f => f.Statement,
            CommonForEachStatementSyntax fe => fe.Statement,
            _ => null,
        };
        StatementSyntax newBody = body is BlockSyntax b
            ? b.WithStatements(b.Statements.Insert(0, tick))
            : SyntaxFactory.Block(tick, body);
        return current switch
        {
            WhileStatementSyntax w => w.WithStatement(newBody),
            DoStatementSyntax d => d.WithStatement(newBody),
            ForStatementSyntax f => f.WithStatement(newBody),
            ForEachStatementSyntax fe => fe.WithStatement(newBody),
            ForEachVariableStatementSyntax fv => fv.WithStatement(newBody),
            _ => current,
        };
    }

    static BlockSyntax InsertEnter(BlockSyntax body) =>
        body.WithStatements(body.Statements.Insert(0, SyntaxFactory.ParseStatement("global::__Rt.Enter();")));

    SyntaxNode TransformMethod(MethodDeclarationSyntax original, MethodDeclarationSyntax current)
    {
        var sym = _model.GetDeclaredSymbol(original);
        bool makeAsync = _asyncFunctions.Contains(original) && sym != null && !sym.IsAsync;
        bool voidLike = sym != null && (sym.ReturnsVoid || (sym.IsAsync && sym.ReturnType is INamedTypeSymbol nt && !nt.IsGenericType && IsTaskLike(nt)));

        if (current.Body != null) current = current.WithBody(InsertEnter(current.Body));
        else if (current.ExpressionBody != null)
        {
            var block = ExpressionToBlock(current.ExpressionBody, voidLike, current.SemicolonToken);
            current = current.WithExpressionBody(null).WithSemicolonToken(default).WithBody(block);
        }

        if (makeAsync)
        {
            var (mods, ret) = MakeAsyncSignature(current.Modifiers, current.ReturnType, sym.ReturnsVoid);
            current = current.WithModifiers(mods).WithReturnType(ret);
        }
        return current;
    }

    SyntaxNode TransformLocalFunction(LocalFunctionStatementSyntax original, LocalFunctionStatementSyntax current)
    {
        var sym = _model.GetDeclaredSymbol(original) as IMethodSymbol;
        bool makeAsync = _asyncFunctions.Contains(original) && sym != null && !sym.IsAsync;
        bool voidLike = sym != null && (sym.ReturnsVoid || (sym.IsAsync && sym.ReturnType is INamedTypeSymbol nt && !nt.IsGenericType && IsTaskLike(nt)));

        if (current.Body != null) current = current.WithBody(InsertEnter(current.Body));
        else if (current.ExpressionBody != null)
        {
            var block = ExpressionToBlock(current.ExpressionBody, voidLike, current.SemicolonToken);
            current = current.WithExpressionBody(null).WithSemicolonToken(default).WithBody(block);
        }

        if (makeAsync)
        {
            var (mods, ret) = MakeAsyncSignature(current.Modifiers, current.ReturnType, sym.ReturnsVoid);
            current = current.WithModifiers(mods).WithReturnType(ret);
        }
        return current;
    }

    static BlockSyntax ExpressionToBlock(ArrowExpressionClauseSyntax arrow, bool voidLike, SyntaxToken semicolon)
    {
        var expr = arrow.Expression;
        var enter = SyntaxFactory.ParseStatement("global::__Rt.Enter();");
        StatementSyntax stmt = voidLike
            ? SyntaxFactory.ExpressionStatement(expr)
            : SyntaxFactory.ReturnStatement(SyntaxFactory.Token(SyntaxKind.ReturnKeyword).WithTrailingTrivia(SyntaxFactory.Space), expr.WithoutLeadingTrivia(), SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        var open = SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithLeadingTrivia(arrow.ArrowToken.LeadingTrivia).WithTrailingTrivia(arrow.ArrowToken.TrailingTrivia);
        var close = SyntaxFactory.Token(SyntaxKind.CloseBraceToken).WithTrailingTrivia(semicolon.TrailingTrivia);
        return SyntaxFactory.Block(open, SyntaxFactory.List(new[] { enter, stmt }), close);
    }

    static (SyntaxTokenList, TypeSyntax) MakeAsyncSignature(SyntaxTokenList mods, TypeSyntax returnType, bool returnsVoid)
    {
        var typeText = returnsVoid
            ? "global::System.Threading.Tasks.Task"
            : "global::System.Threading.Tasks.Task<" + returnType.WithoutTrivia().ToString() + ">";
        var newType = SyntaxFactory.ParseTypeName(typeText);
        var asyncTok = SyntaxFactory.Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(SyntaxFactory.Space);
        if (mods.Count > 0)
        {
            return (mods.Add(asyncTok), newType.WithTriviaFrom(returnType));
        }
        asyncTok = asyncTok.WithLeadingTrivia(returnType.GetLeadingTrivia());
        return (SyntaxFactory.TokenList(asyncTok), newType.WithTrailingTrivia(returnType.GetTrailingTrivia()));
    }
}
