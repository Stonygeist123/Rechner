namespace Rechner
{
    internal class Parser
    {
        public Dictionary<TextSpan, string> diagnostics = new();
        private readonly bool graph;
        private bool inFn = false;
        private int current = 0;
        private readonly List<Token> tokens;
        public Dictionary<string, double> Vars { get; }
        public Dictionary<string, Expr> Fns { get; }
        public Parser(List<Token> _tokens, Dictionary<TextSpan, string> _diagnostics, Dictionary<string, double> _vars, Dictionary<string, Expr> _fns, bool _graph = false)
        {
            tokens = _tokens.FindAll(t => t.Kind != TokenKind.Space);
            Tokens = _tokens;
            diagnostics = _diagnostics;
            Vars = _vars;
            Fns = _fns;
            graph = _graph;
        }

        public Expr ParseExpr(ushort pred = 0)
        {
            Token t = Advance();

            if (graph)
                try
                {
                    return t.Kind switch
                    {
                        TokenKind.Number => CheckExtension(new LiteralExpr(t.Literal ?? 0), pred),
                        TokenKind.Identifier => CheckExtension(GetName(t), pred),
                        TokenKind.Minus => CheckExtension(new UnaryExpr(ParseExpr(4), TokenKind.Minus), pred),
                        TokenKind.LParen => CheckExtension(GetGrouping(), pred),
                        _ => Err(t.Span)
                    };
                }
                catch { return new ErrorExpr(); }
            else
                try
                {
                    return t.Kind switch
                    {
                        TokenKind.Number => CheckExtension(new LiteralExpr(t.Literal ?? 0), pred),
                        TokenKind.Identifier => CheckExtension(GetName(t), pred),
                        TokenKind.Minus => CheckExtension(new UnaryExpr(ParseExpr(4), TokenKind.Minus), pred),
                        TokenKind.LParen => CheckExtension(GetGrouping(), pred),
                        TokenKind.Del => GetDel(),
                        _ => Err(t.Span)
                    };
                }
                catch { return new ErrorExpr(); }
        }

        private Expr CheckExtension(Expr left, ushort pred)
        {
            if (IsAtEnd)
                return left;

            Token t = Current;
            if (BinPrecedence(t.Kind) > 0)
            {
                ushort precedence;
                Token op = t;
                do
                {
                    precedence = BinPrecedence(op.Kind);
                    if (precedence <= pred)
                        break;

                    op = Advance();
                    Expr right = ParseExpr(precedence);
                    left = CheckExtension(new BinaryExpr(left, op.Kind, right), precedence);
                } while (!IsAtEnd && BinPrecedence(Current.Kind) > 0);
                return left;
            }
            else if (t.Kind == TokenKind.LParen)
            {
                Advance();
                if (left is NameExpr n)
                {
                    Expr arg = ParseExpr();
                    if (Current.Kind != TokenKind.RParen)
                        return Err(t.Span, "Erwartete ')'.");

                    Advance();
                    if (!BuiltIn.Functions.Any(f => f.Name == n.Name) && !Fns.TryGetValue(n.Name, out _))
                        return Err(t.Span, $"Konnte die Funktion \"{n.Name}\" nicht finden.");
                    left = new CallExpr(n.Name, arg, BuiltIn.Functions.Any(f => f.Name == n.Name) ? null : Fns[n.Name]);
                }
                else
                    left = new BinaryExpr(left, TokenKind.Star, GetGrouping());
            }
            else if (t.Kind == TokenKind.Bang)
            {
                double l = left.Calc();
                if (l == Math.Round(l) && l >= 0)
                    return new UnaryExpr(left, TokenKind.Bang);
                else return Err(t.Span, "Die Fakultät kann nur auf natürliche Zahlen angewendet werden.");
            }
            else if (t.Kind == TokenKind.Eq)
            {
                if (graph)
                    return Err(t.Span);

                Advance();
                if (left is NameExpr n)
                {
                    Expr value = ParseExpr();
                    if (BuiltIn.Vars.ContainsKey(n.Name)) return Err(t.Span, $"\"{n.Name}\" existiert bereits.");
                    else if (Vars.ContainsKey(n.Name))
                        Vars[n.Name] = value.Calc();
                    else
                        Vars.Add(n.Name, value.Calc());
                    return new VarDecl(n.Name, value);
                }
                else
                    return Err(t.Span, "Variablen brauchen einen Namen.");
            }
            else if (t.Kind == TokenKind.Colon && left is NameExpr n)
            {
                if (Fns.ContainsKey(n.Name) || BuiltIn.Functions.Any(f => f.Name == n.Name))
                    return Err(t.Span, $"Die Funktion \"{n.Name}\" existiert bereits.");

                Advance();
                inFn = true;
                Expr term = ParseExpr();
                inFn = false;

                if (!diagnostics.Any())
                    Fns.Add(n.Name, term);
                return new FnDecl(n.Name, term);
            }
            else return left;

            return CheckExtension(left, 0);
        }

        private ErrorExpr Err(TextSpan span, string msg = "Ungültiger Ausdruck.")
        {
            Error(span, msg);
            return new ErrorExpr();
        }

        public Expr GetGrouping()
        {
            Expr expr = ParseExpr();
            if (Current.Kind != TokenKind.RParen)
                return Err(Current.Span, "Erwartete ')'.");

            expr = new GroupingExpr(expr);
            Advance();
            return expr;
        }

        public Expr GetName(Token t)
        {
            if (Current.Kind == TokenKind.Eq)
                return new NameExpr(t.Lexeme);

            if (!Vars.ContainsKey(t.Lexeme) && !BuiltIn.Vars.ContainsKey(t.Lexeme) && !BuiltIn.Functions.Any(fn => fn.Name == t.Lexeme) && !Fns.ContainsKey(t.Lexeme)
                && ((!inFn && !graph) || t.Lexeme != "x")
                && Current.Kind != TokenKind.Colon)
                return Err(t.Span, $"Konnte \"{t.Lexeme}\" nicht finden.");

            if (Current.Kind != TokenKind.LParen)
            {
                if (BuiltIn.Functions.Any(fn => fn.Name == t.Lexeme))
                    return Err(t.Span, $"\"{t.Lexeme}\" ist eine Funktion - du musst sie aufrufen.");

                if ((graph || inFn) && t.Lexeme == "x") return new NameExpr(t.Lexeme);
                else if (BuiltIn.Vars.TryGetValue(t.Lexeme, out double v)) return new LiteralExpr(v);
                else if (Vars.TryGetValue(t.Lexeme, out double v1)) return new LiteralExpr(v1);
            }


            return new NameExpr(t.Lexeme);
        }

        public Expr GetDel()
        {
            Token id = Advance();
            if (id.Kind != TokenKind.Identifier)
                return Err(id.Span, "Erwartete einen Namen.");
            else if (BuiltIn.Vars.ContainsKey(id.Lexeme))
                return Err(id.Span, $"Kann keine Konstante löschen.");
            else if (BuiltIn.Functions.Any(fn => fn.Name == id.Lexeme))
                return Err(id.Span, $"Kann keine Funktion löschen.");
            else if (!Vars.ContainsKey(id.Lexeme))
                return Err(id.Span, $"Konnte \"{id.Lexeme}\" nicht finden.");

            Vars.Remove(id.Lexeme);
            return new DelExpr(id.Lexeme);
        }

        public bool IsAtEnd => current >= tokens.Count;
        public Token Current => tokens[current];
        public Token Peek(int offset) => tokens[current + offset];

        public List<Token> Tokens { get; }

        public Token Advance()
        {
            Token t = Current;
            ++current;
            return t;
        }

        public void Error(TextSpan span, string msg)
        {
            if (!diagnostics.ContainsKey(span)) diagnostics.Add(span, msg);
            throw new Exception();
        }

        public static ushort BinPrecedence(TokenKind k) => k switch
        {
            TokenKind.Power => 3,
            TokenKind.Star or TokenKind.Slash or TokenKind.Mod => 2,
            TokenKind.Plus or TokenKind.Minus => 1,
            _ => 0,
        };
    }
}