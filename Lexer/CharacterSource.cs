using System;
using System.IO;

namespace BaD
{
    public struct StreamPosition
    {
        public int Line;
        public int Column;

        public StreamPosition(int line, int column)
        {
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            return $"line {Line}, column {Column}";
        }
    }

    public interface ICharacterSource
    {
        StreamPosition GetStreamPosition();
        char Peek();
        char Get();
        bool IsEof();
        void Unget(char s);
        char Peek(int i);
        bool IsValidPeek(int i);
    }

    public class CharacterSourceException : Exception
    {
        public CharacterSourceException(string message) : base(message) { }
    }

    public class StringCharacterSource : ICharacterSource
    {
        private string source;
        private int position;
        private int line;
        private int column;
        private string ungetBuffer = "";

        public StringCharacterSource(string source)
        {
            this.source = source;
            position = 0;
            line = 1;
            column = 1;
        }

        public void Unget(char c)
        {
            ungetBuffer = c + ungetBuffer;
            if (c == '\n' || c == '\r' || c == '\u2028' || c == '\u2029')
            {
                line--;
                column = 1;
            }
            else
            {
                column--;
            }
        }

        public StreamPosition GetStreamPosition()
        {
            return new StreamPosition(line, column);
        }

        public char Peek()
        {
            if (ungetBuffer.Length > 0)
            {
                return ungetBuffer[0];
            }
            if (position >= source.Length)
            {
                throw new CharacterSourceException("Attempted to peek past the end of the source");
            }
            return source[position];
        }

        public char Get()
        {
            if (ungetBuffer.Length > 0)
            {
                char c = ungetBuffer[0];
                ungetBuffer = ungetBuffer.Substring(1);
                if (c == '\n' || c == '\r' || c == '\u2028' || c == '\u2029')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
                return c;
            }
            if (position >= source.Length)
            {
                throw new CharacterSourceException("Attempted to get past the end of the source");
            }
            if (source[position] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
            return source[position++];
        }

        public bool IsEof()
        {
            return position >= source.Length && ungetBuffer.Length == 0;
        }

        public char Peek(int i)
        {
            if (ungetBuffer.Length > i)
            {
                return ungetBuffer[i];
            }
            if (position + i >= source.Length)
            {
                throw new CharacterSourceException("Attempted to peek past the end of the source");
            }
            return source[position + i];
        }

        public bool IsValidPeek(int i)
        {
            return position + i < source.Length;
        }
    }

    public class FileCharacterSource : ICharacterSource
    {
        private readonly StreamReader reader;
        private string ungetBuffer = "";
        private int line = 1;
        private int column = 1;

        public FileCharacterSource(string filename)
        {
            reader = new StreamReader(filename);
        }

        public void Unget(char s)
        {
            if( s == '\n' || s == '\r' || s == '\u2028' || s == '\u2029')
            {
                line--;
                column = 1;
            }
            else
            {
                column--;
            }
            ungetBuffer = s + ungetBuffer;
        }

        public StreamPosition GetStreamPosition()
        {
            return new StreamPosition(line, column);
        }

        public char Peek()
        {
            if (ungetBuffer.Length > 0)
            {
                return ungetBuffer[0];
            }
            int value = reader.Peek();
            if (value == -1)
            {
                throw new CharacterSourceException("Attempted to peek past the end of the source");
            }
            return (char)value;
        }

        public char Peek(int i)
        {
            if (ungetBuffer.Length > i)
            {
                return ungetBuffer[i]; // we can peek into the unget buffer
            }

            int remaining = i - ungetBuffer.Length;

            if (remaining > 0)
            {
                char[] buffer = new char[remaining];
                int read = reader.Read(buffer, 0, remaining);
                if (read < remaining)
                {
                    throw new CharacterSourceException("Attempted to peek past the end of the source");
                }
                // now put the character into the unget buffer
                ungetBuffer = ungetBuffer + new string(buffer);
            }

            int value = reader.Peek();
            if (value == -1)
            {
                throw new CharacterSourceException("Attempted to peek past the end of the source");
            }
            return (char)value;
        }

        public char Get()
        {
            if (ungetBuffer.Length > 0)
            {
                char c = ungetBuffer[0];
                ungetBuffer = ungetBuffer.Substring(1);
                if (c == '\n' || c == '\r' || c == '\u2028' || c == '\u2029')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
                return c;
            }
            int value = reader.Read();
            if (value == -1)
            {
                throw new CharacterSourceException("Attempted to get past the end of the source");
            }
            if (value == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
            return (char)value;
        }

        public bool IsEof()
        {
            return reader.EndOfStream;
        }

        public bool IsValidPeek(int i)
        {
            return true;
        }
    }
}