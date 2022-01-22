using System;
using System.Text;

namespace CoolPaster;

public class UnescapeCsharp
{
    public enum LiteralType
    {
        Regular,
        Verbatim,
        Interpolated,
        InterpolatedVerbatim,
    }

    //
    // Implementation
    //

    // Very simple input string stream with put back functionality.
    private class Stream
    {
        public Stream(String s)
        {
            _s = s;
            _position = 0;
        }

        public bool HasNext()
        {
            return _position < _s.Length;
        }

        public char Next()
        {
            return _s[_position++];
        }

        public void PutBack()
        {
            _position--;
        }

        private String _s;
        private int _position;
    }

    public static String Unescape(String s, LiteralType type)
    {
        StringBuilder sb = new StringBuilder(s.Length);
        Stream input = new Stream(s);

        while (input.HasNext())
        {
            char c = input.Next();
            bool unescaped = false;

            switch (type)
            {
                case LiteralType.Regular:
                    unescaped = UnescapeRegular(c, input, sb);
                    break;
                case LiteralType.Verbatim:
                    unescaped = UnescapeVerbatim(c, input, sb);
                    break;
                case LiteralType.Interpolated:
                    unescaped = UnescapeInterpolated(c, input, sb) || UnescapeRegular(c, input, sb);
                    break;
                case LiteralType.InterpolatedVerbatim:
                    unescaped = UnescapeInterpolated(c, input, sb) || UnescapeVerbatim(c, input, sb);
                    break;
            }

            if (!unescaped)
                sb.Append(c);
        }

        return sb.ToString();
    }

    private static bool UnescapeRegular(char c, Stream input, StringBuilder sb)
    {
        if (c == '\\' && input.HasNext())
        {
            char cc = input.Next();
            switch (cc)
            {
                case '0':
                    sb.Append('\0');
                    return true;
                case 'a':
                    sb.Append('\u0007');
                    return true;
                case 'b':
                    sb.Append('\u0008');
                    return true;
                case 'f':
                    sb.Append('\u000C');
                    return true;
                case 'n':
                    sb.Append('\n');
                    return true;
                case 'r':
                    sb.Append('\r');
                    return true;
                case 't':
                    sb.Append('\t');
                    return true;
                case 'v':
                    sb.Append('\u000B');
                    return true;
                case '\'':
                    sb.Append('\'');
                    return true;
                case '"':
                    sb.Append('\"');
                    return true;
                case 'u':
                    if (input.HasNext())
                    {
                        char u1 = input.Next();
                        if (IsHexDigit(u1) && input.HasNext())
                        {
                            char u2 = input.Next();
                            if (IsHexDigit(u2) && input.HasNext())
                            {
                                char u3 = input.Next();
                                if (IsHexDigit(u3) && input.HasNext())
                                {
                                    char u4 = input.Next();
                                    if (IsHexDigit(u4))
                                    {
                                        char ccc = (char)(
                                            (HexToInt(u1) << 12) |
                                            (HexToInt(u2) << 8) |
                                            (HexToInt(u3) << 4) |
                                            HexToInt(u4));
                                        sb.Append(ccc);
                                        return true;
                                    }

                                    input.PutBack(); // hex nibble 4
                                }

                                input.PutBack(); // hex nibble 3
                            }

                            input.PutBack(); // hex nibble 2
                        }

                        input.PutBack(); // hex nibble 1
                    }

                    input.PutBack(); // 'u'
                    return false;
                default:
                    input.PutBack();
                    return false;
            }
        }

        return false;
    }

    private static bool UnescapeVerbatim(char c, Stream input, StringBuilder sb)
    {
        if (c == '"' && input.HasNext())
        {
            char cc = input.Next();
            if (cc == '"')
            {
                sb.Append('"');
                return true;
            }

            input.PutBack();
        }

        return false;
    }

    private static bool UnescapeInterpolated(char c, Stream input, StringBuilder sb)
    {
        if ((c == '{' || c == '}') && input.HasNext())
        {
            char cc = input.Next();
            if (cc == c)
            {
                sb.Append(c);
                return true;
            }

            input.PutBack();
        }

        return false;
    }

    private static bool IsHexDigit(char c)
    {
        c = char.ToLower(c);
        return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f');
    }

    private static int HexToInt(char c)
    {
        c = char.ToLower(c);
        return c >= '0' && c <= '9' ? c - '0' : c - 'a' + 10;
    }
}
