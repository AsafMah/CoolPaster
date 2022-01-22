using System;
using System.Text;

namespace CoolPaster;

public static class EscapeCsharp
{
    public enum LiteralType
    {
        Regular,
        Verbatim,
        Interpolated,
        InterpolatedVerbatim,
    }

    public static String Escape(String s, LiteralType type)
    {
        var sb = new StringBuilder(s.Length * 2);
        foreach (var c in s)
        {
            var escaped = type switch {
                LiteralType.Regular => EscapeRegular(c, sb),
                LiteralType.Verbatim => EscapeVerbatim(c, sb),
                LiteralType.Interpolated => EscapeInterpolated(c, sb) || EscapeRegular(c, sb),
                LiteralType.InterpolatedVerbatim => EscapeInterpolated(c, sb) || EscapeVerbatim(c, sb),
                _ => false,
            };

            if (!escaped)
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    // We don't use temporary strings (only chars) in the escape* methods to reduce GC pressure.

    // See: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/lexical-structure#string-literals
    private static bool EscapeRegular(char c, StringBuilder sb)
    {
        switch ((int)c)
        {
            case 0x0000:
                sb.Append('\\');
                sb.Append('0');
                break;
            case 0x0007:
                sb.Append('\\');
                sb.Append('a');
                break;
            case 0x0008:
                sb.Append('\\');
                sb.Append('b');
                break;
            case 0x000C:
                sb.Append('\\');
                sb.Append('f');
                break;
            case 0x000A:
                sb.Append('\\');
                sb.Append('n');
                break;
            case 0x000D:
                sb.Append('\\');
                sb.Append('r');
                break;
            case 0x0009:
                sb.Append('\\');
                sb.Append('t');
                break;
            case 0x000B:
                sb.Append('\\');
                sb.Append('v');
                break;
            case '"':
                sb.Append('\\');
                sb.Append('"');
                break;
            default:
                if (c < 128)
                {
                    return false;
                }

                sb.Append('\\');
                sb.Append('u');
                sb.Append(HexDigit(c >> 12));
                sb.Append(HexDigit(c >> 8));
                sb.Append(HexDigit(c >> 4));
                sb.Append(HexDigit(c));

                break;
        }

        return true;
    }

    private static bool EscapeVerbatim(char c, StringBuilder sb)
    {
        switch (c)
        {
            case '"':
                sb.Append(c);
                sb.Append(c);
                break;
            default:
                return false;
        }

        return true;
    }

    private static bool EscapeInterpolated(char c, StringBuilder sb)
    {
        switch (c)
        {
            case '{':
            case '}':
                sb.Append(c);
                sb.Append(c);
                break;
            default:
                return false;
        }

        return true;
    }

    private static char HexDigit(int d)
    {
        d &= 0x0F;
        return (char)(d <= 9 ? '0' + d : 'A' + d - 10);
    }
}
