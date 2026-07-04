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

            if (raw.StartsWith("List<", StringComparison.OrdinalIgnoreCase))
                return false;

            if (IsEnumHeader(headerName))
            {
                if (raw.Equals("enum", StringComparison.OrdinalIgnoreCase)
                    || raw.Equals(headerName, StringComparison.OrdinalIgnoreCase))
                {
                    columnType = headerName;
                    return true;
                }
            }

            if (TryNormalizePrimitive(raw, out string primitive))
            {
                columnType = primitive;
                return true;
            }

            return false;
        }

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

        internal static bool IsEnumHeader(string headerName) =>
            !string.IsNullOrEmpty(headerName) && headerName.StartsWith("E", StringComparison.Ordinal);

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
