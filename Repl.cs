using System.Text;

namespace Rechner
{
    internal class Repl
    {
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
                    Console.CursorLeft = _currentCharacter + 3;
                }
            }
        }

        public string Read()
        {
            doc = new();
            CurrentCharacter = 0;
            Console.CursorLeft = 0;
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write(">> ");
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
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write(">> ");
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
                        _ => ConsoleColor.Gray,
                    };
                    Console.Write(t.Lexeme);
                    Console.ResetColor();
                }
            }

            Console.CursorLeft = CurrentCharacter + 3;
        }
    }
}
