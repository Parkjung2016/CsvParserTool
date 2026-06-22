using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CSVParserTool
{
    /// <summary>미리보기 RichTextBox용 간단 C# 구문 색상 (IDE 느낌).</summary>
    internal static class CSharpPreviewHighlighter
    {
        private static readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "public", "private", "protected", "internal", "static", "readonly", "class", "struct",
            "enum", "interface", "namespace", "using", "return", "new", "null", "true", "false",
            "int", "float", "double", "bool", "string", "void", "object", "var", "async", "await",
            "get", "set", "partial", "where", "sealed", "override", "virtual", "abstract"
        };

        public static void Apply(RichTextBox box, string code, bool dark)
        {
            if (box == null)
                return;

            PreviewSyntaxColors colors = dark ? PreviewSyntaxColors.Dark : PreviewSyntaxColors.Light;

            box.SuspendLayout();
            box.Clear();
            box.BackColor = colors.Background;
            box.ForeColor = colors.Default;
            box.DetectUrls = false;

            if (string.IsNullOrEmpty(code))
            {
                box.ResumeLayout();
                return;
            }

            int i = 0;
            while (i < code.Length)
            {
                char c = code[i];

                if (c == '\r')
                {
                    i++;
                    continue;
                }

                if (c == '\n')
                {
                    Append(box, "\n", colors.Default);
                    i++;
                    continue;
                }

                if (c == '/' && i + 1 < code.Length && code[i + 1] == '/')
                {
                    int start = i;
                    while (i < code.Length && code[i] != '\n')
                        i++;
                    Append(box, code.Substring(start, i - start), colors.Comment);
                    continue;
                }

                if (c == '"')
                {
                    int start = i;
                    i++;
                    while (i < code.Length)
                    {
                        if (code[i] == '\\' && i + 1 < code.Length)
                        {
                            i += 2;
                            continue;
                        }

                        if (code[i] == '"')
                        {
                            i++;
                            break;
                        }

                        i++;
                    }

                    Append(box, code.Substring(start, i - start), colors.String);
                    continue;
                }

                if (c == '\'')
                {
                    int start = i;
                    i++;
                    while (i < code.Length && code[i] != '\'')
                        i++;
                    if (i < code.Length)
                        i++;
                    Append(box, code.Substring(start, i - start), colors.String);
                    continue;
                }

                if (c == '[')
                {
                    int start = i;
                    while (i < code.Length && code[i] != ']')
                        i++;
                    if (i < code.Length)
                        i++;
                    Append(box, code.Substring(start, i - start), colors.Attribute);
                    continue;
                }

                if (char.IsDigit(c))
                {
                    int start = i;
                    while (i < code.Length && (char.IsDigit(code[i]) || code[i] == '.'))
                        i++;
                    Append(box, code.Substring(start, i - start), colors.Number);
                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    int start = i;
                    while (i < code.Length && (char.IsLetterOrDigit(code[i]) || code[i] == '_'))
                        i++;
                    string word = code.Substring(start, i - start);
                    Color color = Keywords.Contains(word) ? colors.Keyword
                        : char.IsUpper(word[0]) ? colors.Type
                        : colors.Default;
                    Append(box, word, color);
                    continue;
                }

                Append(box, c.ToString(), colors.Default);
                i++;
            }

            box.SelectionStart = 0;
            box.SelectionLength = 0;
            box.ResumeLayout();
        }

        private static void Append(RichTextBox box, string text, Color color)
        {
            box.SelectionColor = color;
            box.AppendText(text);
        }

        private sealed class PreviewSyntaxColors
        {
            public static readonly PreviewSyntaxColors Dark = new PreviewSyntaxColors(
                Color.FromArgb(30, 30, 30),
                Color.FromArgb(212, 212, 212),
                Color.FromArgb(86, 156, 214),
                Color.FromArgb(78, 201, 176),
                Color.FromArgb(206, 145, 120),
                Color.FromArgb(106, 153, 85),
                Color.FromArgb(156, 220, 254),
                Color.FromArgb(181, 206, 168));

            public static readonly PreviewSyntaxColors Light = new PreviewSyntaxColors(
                Color.FromArgb(250, 250, 250),
                Color.FromArgb(36, 41, 46),
                Color.FromArgb(0, 0, 255),
                Color.FromArgb(43, 145, 175),
                Color.FromArgb(163, 21, 21),
                Color.FromArgb(0, 128, 0),
                Color.FromArgb(128, 64, 0),
                Color.FromArgb(9, 134, 88));

            public Color Background { get; }
            public Color Default { get; }
            public Color Keyword { get; }
            public Color Type { get; }
            public Color String { get; }
            public Color Comment { get; }
            public Color Attribute { get; }
            public Color Number { get; }

            private PreviewSyntaxColors(
                Color background,
                Color @default,
                Color keyword,
                Color type,
                Color @string,
                Color comment,
                Color attribute,
                Color number)
            {
                Background = background;
                Default = @default;
                Keyword = keyword;
                Type = type;
                String = @string;
                Comment = comment;
                Attribute = attribute;
                Number = number;
            }
        }
    }
}
