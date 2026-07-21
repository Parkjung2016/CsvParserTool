using System;
using System.Collections.Generic;
using System.Globalization;

namespace CSVParserTool
{
    /// <summary>CSV/엑셀 2행(타입 행) 및 자동 추론 공통 타입 규칙.</summary>
    public static class CsvColumnTypes
    {
        private static readonly HashSet<string> PrimitiveNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "bool", "uint", "int", "float", "double", "string", "str"
        };

        /// <summary>헤더 바로 아래 행이 컬럼 버전 행인지 판별 (<c>1.0.0</c> 형식).</summary>
        public static bool LooksLikeVersionRow(IReadOnlyList<string> cells, IReadOnlyList<string> headers)
        {
            if (cells == null || headers == null || headers.Count == 0)
                return false;

            if (LooksLikeTypeRow(cells, headers))
                return false;

            int nonEmpty = 0;
            int versionLike = 0;
            for (int i = 0; i < headers.Count; i++)
            {
                string cell = i < cells.Count ? cells[i]?.Trim() ?? string.Empty : string.Empty;
                if (string.IsNullOrEmpty(cell))
                    continue;

                nonEmpty++;
                if (DataVersion.TryParse(cell, out _))
                    versionLike++;
            }

            return nonEmpty > 0 && versionLike == nonEmpty;
        }

        /// <summary>헤더 바로 아래 행이 타입 지정 행인지 판별.</summary>
        public static bool LooksLikeTypeRow(IReadOnlyList<string> cells, IReadOnlyList<string> headers)
        {
            if (cells == null || headers == null || headers.Count == 0)
                return false;

            int nonEmpty = 0;
            int typeLike = 0;
            for (int i = 0; i < headers.Count; i++)
            {
                string cell = i < cells.Count ? cells[i]?.Trim() ?? string.Empty : string.Empty;
                if (string.IsNullOrEmpty(cell))
                    continue;

                nonEmpty++;
                if (IsExplicitTypeToken(cell, headers[i]))
                    typeLike++;
            }

            if (nonEmpty == 0)
                return false;

            return typeLike == nonEmpty;
        }

        public static bool IsExplicitTypeToken(string raw, string headerName)
        {
            return TryNormalizeExplicit(raw, headerName, out _);
        }

        /// <summary>타입 행 셀 → C# 컬럼 타입 (<c>int</c>, enum 이름 등).</summary>
        public static bool TryNormalizeExplicit(string raw, string headerName, out string columnType)
        {
            columnType = null;
            raw = raw?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(raw))
                return false;

            if (TryUnwrapArrayToken(raw, out string arrayElementToken))
            {
                if (TryUnwrapArrayToken(arrayElementToken, out _))
                    return false;

                if (IsReferenceTypeToken(arrayElementToken))
                {
                    columnType = "string[]";
                    return true;
                }

                if (!TryNormalizeScalar(arrayElementToken, headerName, out string arrayElementType))
                    return false;

                columnType = arrayElementType + "[]";
                return true;
            }

            if (IsReferenceTypeToken(raw))
            {
                // Reference types are resolved after every table has been parsed.
                columnType = "string";
                return true;
            }

            return TryNormalizeScalar(raw, headerName, out columnType);
        }

        private static bool TryNormalizeScalar(string raw, string headerName, out string columnType)
        {
            columnType = null;
            if (TryNormalizeEnum(raw, headerName, out string enumName))
            {
                columnType = enumName;
                return true;
            }

            return TryNormalizePrimitive(raw, out columnType);
        }

        private static bool TryNormalizeEnum(string raw, string headerName, out string enumName)
        {
            enumName = null;
            raw = raw?.Trim() ?? string.Empty;
            if (!raw.StartsWith("enum:", StringComparison.OrdinalIgnoreCase))
                return false;

            enumName = raw.Substring(5).Trim();
            if (!IsValidTypeIdentifier(enumName))
            {
                enumName = null;
                return false;
            }

            return true;
        }
        public static bool TryGetArrayElementType(string columnType, out string elementType)
        {
            elementType = null;
            if (!TryUnwrapArrayToken(columnType, out string rawElement))
                return false;

            elementType = rawElement.Trim();
            return !string.IsNullOrEmpty(elementType);
        }

        internal static string[] SplitArrayCell(string value)
        {
            value = value?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(value))
                return Array.Empty<string>();

            string[] rawItems = value.Split('|');
            var items = new List<string>(rawItems.Length);
            foreach (string rawItem in rawItems)
            {
                string item = rawItem?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(item))
                    items.Add(item);
            }
            return items.ToArray();
        }

        private static bool TryUnwrapArrayToken(string raw, out string elementToken)
        {
            elementToken = null;
            raw = raw?.Trim() ?? string.Empty;
            if (!raw.EndsWith("[]", StringComparison.Ordinal))
                return false;

            elementToken = raw.Substring(0, raw.Length - 2).Trim();
            return !string.IsNullOrEmpty(elementToken);
        }

        public static bool IsValidTypeIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim();
            if (!char.IsLetter(value[0]) && value[0] != '_')
                return false;

            for (int i = 1; i < value.Length; i++)
            {
                if (!char.IsLetterOrDigit(value[i]) && value[i] != '_')
                    return false;
            }

            return true;
        }

        public static bool IsReferenceTypeToken(string raw) =>
            IsValueReferenceTypeToken(raw) || IsValidationReferenceTypeToken(raw);

        public static bool IsValueReferenceTypeToken(string raw) =>
            !string.IsNullOrWhiteSpace(raw)
            && raw.TrimStart().StartsWith("ref ", StringComparison.OrdinalIgnoreCase);

        public static bool IsValidationReferenceTypeToken(string raw) =>
            !string.IsNullOrWhiteSpace(raw)
            && raw.TrimStart().StartsWith("keyref ", StringComparison.OrdinalIgnoreCase);

        public static bool IsPrimitiveType(string columnType) =>
            !string.IsNullOrWhiteSpace(columnType) && PrimitiveNames.Contains(columnType.Trim());

        internal static string InferPrimitiveFromValue(string value)
        {
            value = value?.Trim() ?? string.Empty;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                return "int";
            if (uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                return "uint";
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                return "float";
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                return "double";
            if (bool.TryParse(value, out _))
                return "bool";
            return "string";
        }

        private static bool TryNormalizePrimitive(string raw, out string primitive)
        {
            primitive = null;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            if (raw.Equals("str", StringComparison.OrdinalIgnoreCase))
            {
                primitive = "string";
                return true;
            }

            if (!PrimitiveNames.Contains(raw))
                return false;

            primitive = raw.Equals("string", StringComparison.OrdinalIgnoreCase) ? "string" : raw.ToLowerInvariant();
            return true;
        }
    }
}
