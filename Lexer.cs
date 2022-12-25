namespace Rechner
{
    internal class Lexer
    {
        public Dictionary<TextSpan, string> diagnostics = new();

        private readonly string src;
        private int start = 0, current = 0;
        public Lexer(string _src) => src = _src;
        public List<Token> Lex()
        {
            List<Token> tokens = new();
            while (!IsAtEnd)
            {
                start = current;
                Token t = Tokenize();
                tokens.Add(t);
            }

            tokens.Add(new Token(TokenKind.Eof, new TextSpan(current, current), ""));
            return tokens;
        }

        public Token Tokenize()
        {
            TokenKind kind = TokenKind.Bad;
            char c = Peek();
            switch (c)
            {
                case '+':
                    kind = TokenKind.Plus;
                    Advance(); break;
                case '-':
                    kind = TokenKind.Minus;
                    Advance(); break;
                case '*':
                    kind = TokenKind.Star;
                    Advance(); break;
                case '/':
                    kind = TokenKind.Slash;
                    Advance(); break;
                case '%':
                    kind = TokenKind.Mod;
                    Advance(); break;
                case '^':
                    kind = TokenKind.Power;
                    Advance(); break;
                case '!':
                    kind = TokenKind.Bang;
                    Advance(); break;
                case '=':
                    kind = TokenKind.Eq;
                    Advance(); break;
                case '(':
                    kind = TokenKind.LParen;
                    Advance(); break;
                case ')':
                    kind = TokenKind.RParen;
                    Advance(); break;
                case ',':
                    kind = TokenKind.Comma;
                    Advance(); break;
                case ':':
                    kind = TokenKind.Colon;
                    Advance(); break;
                case ' ':
                case '\r':
                case '\n':
                case '\t':
                    kind = TokenKind.Space;
                    Advance(); break;
                default:
                    if (c == '.' && char.IsDigit(Peek(1)))
                        c = '0';

                    if (char.IsDigit(c))
                    {
                        kind = TokenKind.Number;
                        bool hasDot = false, isPower = false;
                        double val = 0, fractionSize = 1;
                        while (true)
                        {
                            int n;
                            char cur = Peek();
                            if (!char.IsDigit(cur) && cur != '.' && cur != 'e' && cur != '_' && cur != '-')
                                break;

                            Advance();
                            if (char.IsDigit(cur))
                                n = int.Parse(cur.ToString());
                            else if (cur == '_')
                                continue;
                            else if (cur == '.')
                            {
                                if (hasDot)
                                    Error(Span, "Ungültige Fließkommazahl.");

                                hasDot = true;
                                continue;
                            }
                            else if (cur == 'e')
                            {
                                isPower = true;
                                break;
                            }
                            else
                                break;

                            if (hasDot)
                                val += n * (fractionSize /= 10);
                            else
                            {
                                val *= 10;
                                val += n;
                            }
                        }

                        bool negPower = false;
                        if (isPower)
                        {
                            double power = 0;
                            while (true)
                            {
                                char cur = Peek();
                                if (cur == '\0')
                                    break;

                                if (!char.IsDigit(cur) && cur != '-' && cur != '_')
                                    break;

                                Advance();
                                if (cur == '_')
                                    continue;

                                if (cur == '-')
                                {
                                    if (negPower)
                                        Error(Span, "Invalid number literal with multiple negatives.");

                                    negPower = true;
                                    continue;
                                }

                                power *= 10;
                                power += int.Parse(cur.ToString());
                            }


                            if (negPower)
                                power = -power;

                            val *= Math.Pow(10, power);
                        }

                        return new Token(kind, Span, src[start..current], val);
                    }
                    else if (char.IsLetter(c) || c == '_')
                    {
                        while (char.IsLetter(Peek()) || Peek() == '_') Advance();
                        kind = src[start..current] == "del" ? TokenKind.Del : TokenKind.Identifier;
                    }
                    else
                    {
                        Advance();
                        Error(Span, $"Unbekanntes Zeichen: '{c}'.");
                    }

                    break;
            }

            return new Token(kind, Span, src[start..current]);
        }

        public TextSpan Span => new(start, current);
        public bool IsAtEnd => current >= src.Length;
        public char Peek(int offset = 0) => current + offset >= src.Length ? '\0' : src[current + offset];
        public char Advance() => IsAtEnd ? '\0' : src[current++];
        public void Error(TextSpan span, string msg) => diagnostics.Add(span, msg);
    }
}