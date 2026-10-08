using System.Text.Json;
using System.Text.RegularExpressions;
try
{
    if (args.Length == 0) { Console.WriteLine("LogWorkbench FILE [--level LEVEL] [--contains TEXT] [--limit N]"); return 0; }
    string? level = null, contains = null; int limit = 20;
    for (int i = 1; i < args.Length; i += 2)
    {
        if (i + 1 >= args.Length) throw new ArgumentException("Missing option value");
        switch (args[i])
        {
            case "--level": level = args[i+1].ToUpperInvariant(); break;
            case "--contains": contains = args[i+1]; break;
            case "--limit": limit = int.Parse(args[i+1]); if (limit < 0 || limit > 10000) throw new ArgumentException("Limit must be 0..10000"); break;
            default: throw new ArgumentException("Unknown option: " + args[i]);
        }
    }
    string[] levels = ["TRACE", "DEBUG", "INFO", "WARN", "ERROR", "FATAL", "UNKNOWN"];
    if (level != null && !levels.Contains(level)) throw new ArgumentException("Unsupported level");
    long total = 0, matched = 0, malformed = 0;
    var counts = new SortedDictionary<string,long>(); var samples = new List<object>();
    var pattern = new Regex(@"\b(TRACE|DEBUG|INFO|WARN|WARNING|ERROR|FATAL)\b", RegexOptions.IgnoreCase);
    foreach (string line in File.ReadLines(args[0]))
    {
        total++; string severity = "UNKNOWN", message = line;
        if (line.TrimStart().StartsWith('{'))
        {
            try { using var doc = JsonDocument.Parse(line); var root = doc.RootElement;
                if (root.TryGetProperty("level", out var l) && l.ValueKind == JsonValueKind.String) severity = l.GetString()!.ToUpperInvariant();
                if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String) message = m.GetString()!;
            } catch (JsonException) { malformed++; }
        }
        else { var match = pattern.Match(line); if (match.Success) severity = match.Value.ToUpperInvariant(); }
        if (severity == "WARNING") severity = "WARN";
        if (!levels.Contains(severity)) severity = "UNKNOWN";
        counts[severity] = counts.GetValueOrDefault(severity) + 1;
        if (level != null && severity != level) continue;
        if (contains != null && !message.Contains(contains, StringComparison.OrdinalIgnoreCase)) continue;
        matched++; if (samples.Count < limit) samples.Add(new { line = total, level = severity, message });
    }
    Console.WriteLine(JsonSerializer.Serialize(new { total, matched, malformed, counts, samples }, new JsonSerializerOptions { WriteIndented = true }));
    return 0;
}
catch (Exception e) { Console.Error.WriteLine(e.Message); return 1; }
