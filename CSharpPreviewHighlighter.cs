using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace CSVParserTool
{
    internal static class CSharpPreviewHighlighter
    {
        const int WmSetRedraw = 0x000B;
        const int MaxSyntaxHighlightChars = 120000;
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

        static readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "public", "private", "protected", "internal", "static", "readonly", "class", "struct",
            "enum", "interface", "namespace", "using", "return", "new", "null", "true", "false",
            "int", "float", "double", "bool", "string", "void", "object", "var", "async", "await",
            "get", "set", "partial", "where", "sealed", "override", "virtual", "abstract"
        };

        public static void Apply(RichTextBox box, string code, bool dark)
        {
            if (box == null) return;
            PreviewSyntaxColors colors = dark ? PreviewSyntaxColors.Dark : PreviewSyntaxColors.Light;
            bool suspend = box.IsHandleCreated;
            if (suspend) SendMessage(box.Handle, WmSetRedraw, IntPtr.Zero, IntPtr.Zero);
            box.SuspendLayout();
            try
            {
                box.BackColor = colors.Background;
                box.ForeColor = colors.Default;
                box.DetectUrls = false;
                code = code ?? string.Empty;
                if (code.Length > MaxSyntaxHighlightChars)
                    box.Text = code;
                else
                    box.Rtf = BuildRtf(code, colors, box.Font);
                box.SelectionStart = 0;
                box.SelectionLength = 0;
            }
            finally
            {
                box.ResumeLayout();
                if (suspend) SendMessage(box.Handle, WmSetRedraw, new IntPtr(1), IntPtr.Zero);
                box.Invalidate();
            }
        }

        private static string BuildRtf(string code, PreviewSyntaxColors colors, Font font)
        {
            var output = new StringBuilder(Math.Max(256, code.Length + code.Length / 4));
            output.Append(@"{\rtf1\ansi\deff0{\fonttbl{\f0 ");
            AppendEscaped(output, font?.Name ?? "Segoe UI");
            output.Append(@";}}{\colortbl ;");
            AppendColor(output, colors.Default); AppendColor(output, colors.Keyword);
            AppendColor(output, colors.Type); AppendColor(output, colors.String);
            AppendColor(output, colors.Comment); AppendColor(output, colors.Attribute);
            AppendColor(output, colors.Number);
            int halfPoints = Math.Max(12, (int)Math.Round((font?.SizeInPoints ?? 9f) * 2));
            output.Append(@"}\viewkind4\uc1\pard\f0\fs").Append(halfPoints).Append(' ');

            int i = 0;
            while (i < code.Length)
            {
                int start = i;
                int color = 1;
                char c = code[i];
                if (c == '/' && i + 1 < code.Length && code[i + 1] == '/')
                { color = 5; i += 2; while (i < code.Length && code[i] != '\n') i++; }
                else if (c == '"')
                { color = 4; i++; while (i < code.Length) { if (code[i] == '\\' && i + 1 < code.Length) i += 2; else if (code[i++] == '"') break; } }
                else if (c == '\'')
                { color = 4; i++; while (i < code.Length) { if (code[i] == '\\' && i + 1 < code.Length) i += 2; else if (code[i++] == '\'') break; } }
                else if (c == '[')
                { color = 6; i++; while (i < code.Length && code[i] != ']') i++; if (i < code.Length) i++; }
                else if (char.IsDigit(c))
                { color = 7; i++; while (i < code.Length && (char.IsDigit(code[i]) || code[i] == '.')) i++; }
                else if (char.IsLetter(c) || c == '_')
                {
                    i++; while (i < code.Length && (char.IsLetterOrDigit(code[i]) || code[i] == '_')) i++;
                    string word = code.Substring(start, i - start);
                    color = Keywords.Contains(word) ? 2 : char.IsUpper(word[0]) ? 3 : 1;
                }
                else
                { i++; while (i < code.Length && IsPlain(code[i])) i++; }

                output.Append("\\cf").Append(color).Append(' ');
                AppendEscaped(output, code, start, i - start);
            }
            return output.Append('}').ToString();
        }

        private static bool IsPlain(char c) =>
            c != '/' && c != '"' && c != '\'' && c != '[' && !char.IsDigit(c) && !char.IsLetter(c) && c != '_';

        private static void AppendColor(StringBuilder sb, Color c) =>
            sb.Append("\\red").Append(c.R).Append("\\green").Append(c.G).Append("\\blue").Append(c.B).Append(';');
        private static void AppendEscaped(StringBuilder sb, string text) => AppendEscaped(sb, text, 0, text.Length);
        private static void AppendEscaped(StringBuilder sb, string text, int start, int length)
        {
            int end = start + length;
            for (int i = start; i < end; i++)
            {
                char c = text[i];
                switch (c)
                {
                    case '\\': sb.Append(@"\\"); break;
                    case '{': sb.Append(@"\{"); break;
                    case '}': sb.Append(@"\}"); break;
                    case '\r': break;
                    case '\n': sb.Append(@"\line "); break;
                    case '\t': sb.Append(@"\tab "); break;
                    default:
                        if (c <= 0x7f) sb.Append(c);
                        else sb.Append("\\u").Append(unchecked((short)c)).Append('?');
                        break;
                }
            }
        }

        private sealed class PreviewSyntaxColors
        {
            public static readonly PreviewSyntaxColors Dark = new PreviewSyntaxColors(Color.FromArgb(30, 30, 30), Color.FromArgb(212, 212, 212), Color.FromArgb(86, 156, 214), Color.FromArgb(78, 201, 176), Color.FromArgb(206, 145, 120), Color.FromArgb(106, 153, 85), Color.FromArgb(156, 220, 254), Color.FromArgb(181, 206, 168));
            public static readonly PreviewSyntaxColors Light = new PreviewSyntaxColors(Color.FromArgb(250, 250, 250), Color.FromArgb(36, 41, 46), Color.FromArgb(0, 0, 255), Color.FromArgb(43, 145, 175), Color.FromArgb(163, 21, 21), Color.FromArgb(0, 128, 0), Color.FromArgb(128, 64, 0), Color.FromArgb(9, 134, 88));
            public Color Background { get; }
            public Color Default { get; }
            public Color Keyword { get; }
            public Color Type { get; }
            public Color String { get; }
            public Color Comment { get; }
            public Color Attribute { get; }
            public Color Number { get; }
            PreviewSyntaxColors(Color background, Color value, Color keyword, Color type, Color text, Color comment, Color attribute, Color number)
            { Background = background; Default = value; Keyword = keyword; Type = type; String = text; Comment = comment; Attribute = attribute; Number = number; }
        }
    }
}