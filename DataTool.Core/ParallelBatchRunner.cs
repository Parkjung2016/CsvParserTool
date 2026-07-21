using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CSVParserTool
{
    /// <summary>항목을 배치로 나눠 배치마다 병렬 처리합니다. (동시 메모리·파일 핸들 제한)</summary>
    internal static class ParallelBatchRunner
    {
        /// <summary>동시 처리 개수. 코어 수 기준, 최대 8로 제한 (XLSX/대용량 CSV 메모리 부담 완화).</summary>
        public static int DefaultBatchSize =>
            Math.Max(1, Math.Min(Math.Max(1, Environment.ProcessorCount - 1), 8));

        public static void ForEach<T>(
            IReadOnlyList<T> items,
            Action<T> body,
            int batchSize = 0,
            Action<string> batchLog = null)
        {
            if (items == null || items.Count == 0)
                return;

            if (body == null)
                throw new ArgumentNullException(nameof(body));

            if (batchSize <= 0)
                batchSize = DefaultBatchSize;

            int batchCount = (items.Count + batchSize - 1) / batchSize;
            for (int batchIndex = 0; batchIndex < batchCount; batchIndex++)
            {
                int start = batchIndex * batchSize;
                int count = Math.Min(batchSize, items.Count - start);

                batchLog?.Invoke($"배치 {batchIndex + 1}/{batchCount} ({count}개) 처리 중…");

                Parallel.ForEach(
                    System.Linq.Enumerable.Range(start, count),
                    new ParallelOptions { MaxDegreeOfParallelism = count },
                    i => body(items[i]));
            }
        }
    }
}
