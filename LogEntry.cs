using System;
using CSVParserTool;

public class LogEntry
{
    public DateTime Time { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; }

    public LogEntry(LogLevel level, string message)
    {
        Time = DateTime.Now;
        Level = level;
        Message = message;
    }
}
