using System.Text;

namespace Rechner
{
    internal class Repl
    {
        private string inputStart = "";
        private bool graph = false;
        private StringBuilder doc = new();
        private int _currentCharacter = 0;
        public int CurrentCharacter
        {
            get => _currentCharacter;
            set
            {
                if (_currentCharacter != value)
                {
                    _currentCharacter = value;
                    Console.CursorLeft = _currentCharacter + inputStart.Length;
                }
            }
        }

        public string Read(bool _graph = false)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            graph = _graph;
            inputStart = graph ? "f(x) = " : ">> ";
            Console.Write(inputStart);
            Console.ResetColor();

            while (true)
            {
                ConsoleKeyInfo ck = Console.ReadKey(true);
                if (ck.Key == ConsoleKey.Backspace)
                {
                    if (doc.Length == 0 || CurrentCharacter <= 0)
                        continue;

                    doc = doc.Remove(CurrentCharacter - 1, 1);
                    --CurrentCharacter;
                }
                else if (ck.Key == ConsoleKey.LeftArrow)
                {
                    if (CurrentCharacter > 0)
                        --CurrentCharacter;
                    continue;
                }
                else if (ck.Key == ConsoleKey.RightArrow)
                {
                    if (CurrentCharacter <= doc.Length - 1)
                        ++CurrentCharacter;
                    continue;
                }
                else if (ck.Key == ConsoleKey.Escape)
                {
                    doc = new();
                    CurrentCharacter = 0;
                }
                else if (ck.Key == ConsoleKey.Enter)
                    break;
                else if (ck.KeyChar >= ' ' || char.IsLetter(ck.KeyChar))
                {
                    if (CurrentCharacter == doc.Length)
                        doc.Append(ck.KeyChar);
                    else
                        doc.Insert(CurrentCharacter, ck.KeyChar);
                    ++CurrentCharacter;
                }

                Print();
            }

            return doc.ToString();
        }

        public void Print()
        {
            if (graph) Console.Clear();
            else
            {
                int currentLineCursor = Console.CursorTop;
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, currentLineCursor);
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write(inputStart);
            Console.ResetColor();

            doc = doc.Replace("\r", "");
            if (doc.ToString().TrimEnd().Length > 0)
            {
                Lexer lexer = new(doc.ToString());
                List<Token> tokens = lexer.Lex();
                foreach (Token t in tokens.SkipLast(1))
                {
                    Console.ForegroundColor = t.Kind switch
                    {
                        TokenKind.Number => ConsoleColor.Cyan,
                        TokenKind.Identifier => ConsoleColor.DarkYellow,
                        TokenKind.Comma => ConsoleColor.Gray,
                        TokenKind.LParen or TokenKind.RParen => ConsoleColor.DarkGray,
                        TokenKind.Bad => ConsoleColor.Red,
                        TokenKind.Del => ConsoleColor.DarkBlue,
                        _ => ConsoleColor.Gray,
                    };
                    Console.Write(t.Lexeme);
                    Console.ResetColor();
                }
            }

            Console.CursorLeft = CurrentCharacter + inputStart.Length;
        }
    }
}
