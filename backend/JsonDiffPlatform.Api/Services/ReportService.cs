using System.Text;
using System.Text.Json;
using JsonDiffPlatform.Api.Models;

namespace JsonDiffPlatform.Api.Services;

public sealed class ReportService
{
    public ReportFile Build(HistoryRecord record, string format)
    {
        var normalized = (format ?? "html").Trim().ToLowerInvariant();
        return normalized switch
        {
            "json" => new ReportFile("application/json; charset=utf-8", $"{record.Id}.json", JsonBytes(record)),
            "csv" => new ReportFile("text/csv; charset=utf-8", $"{record.Id}.csv", CsvBytes(record)),
            "markdown" or "md" => new ReportFile("text/markdown; charset=utf-8", $"{record.Id}.md", MarkdownBytes(record)),
            "excel" or "xlsx" or "xls" => new ReportFile("application/vnd.ms-excel", $"{record.Id}.xls", ExcelBytes(record)),
            "pdf" => new ReportFile("application/pdf", $"{record.Id}.pdf", PdfBytes(record)),
            _ => new ReportFile("text/html; charset=utf-8", $"{record.Id}.html", HtmlBytes(record))
        };
    }

    private static byte[] JsonBytes(HistoryRecord record) => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(record, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));

    private static byte[] CsvBytes(HistoryRecord record)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Path,Kind,OldType,NewType,OldValue,NewValue,Message");
        foreach (var diff in record.Result.Differences)
        {
            builder.AppendLine(string.Join(",", new[]
            {
                EscapeCsv(diff.Path), EscapeCsv(diff.Kind), EscapeCsv(diff.OldType), EscapeCsv(diff.NewType),
                EscapeCsv(diff.OldValue), EscapeCsv(diff.NewValue), EscapeCsv(diff.Message)
            }));
        }

        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(builder.ToString());
    }

    private static byte[] MarkdownBytes(HistoryRecord record)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# JSON 比较报告 - {record.Name}");
        builder.AppendLine();
        builder.AppendLine($"- 生成时间：{record.CreatedAt:yyyy-MM-dd HH:mm:ss zzz}");
        builder.AppendLine($"- 比较结果：{(record.Result.IsEqual ? "一致" : "存在差异")}");
        builder.AppendLine($"- 差异数量：{record.Result.Summary.Total}");
        builder.AppendLine($"- 耗时：{record.Result.DurationMs} ms");
        builder.AppendLine();
        builder.AppendLine("| 路径 | 类型 | 基准值 | 目标值 | 说明 |");
        builder.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (var diff in record.Result.Differences)
        {
            builder.AppendLine($"| {EscapeMarkdown(diff.Path)} | {diff.Kind} | {EscapeMarkdown(diff.OldValue)} | {EscapeMarkdown(diff.NewValue)} | {EscapeMarkdown(diff.Message)} |");
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static byte[] HtmlBytes(HistoryRecord record)
    {
        var rows = string.Join(Environment.NewLine, record.Result.Differences.Select(diff => $"<tr><td>{Html(diff.Path)}</td><td><span class=\"kind\">{Html(diff.Kind)}</span></td><td><pre>{Html(diff.OldValue)}</pre></td><td><pre>{Html(diff.NewValue)}</pre></td><td>{Html(diff.Message)}</td></tr>"));
        var html = "<!doctype html><html lang=\"zh-CN\"><head><meta charset=\"utf-8\"><title>JSON 比较报告</title>"
            + "<style>body{font-family:Arial,\"Microsoft YaHei\",sans-serif;color:#172033;margin:32px}h1{margin-bottom:4px}.meta{color:#667085;margin-bottom:24px}table{border-collapse:collapse;width:100%}th,td{border:1px solid #dbe1ea;padding:10px;text-align:left;vertical-align:top}th{background:#f4f7fb}pre{white-space:pre-wrap;margin:0;max-width:360px}.kind{color:#0e756c;font-weight:700}</style></head>"
            + $"<body><h1>JSON 比较报告</h1><div class=\"meta\">{Html(record.Name)} · {record.CreatedAt:yyyy-MM-dd HH:mm:ss} · 差异 {record.Result.Summary.Total} 条</div>"
            + $"<table><thead><tr><th>路径</th><th>类型</th><th>基准值</th><th>目标值</th><th>说明</th></tr></thead><tbody>{rows}</tbody></table></body></html>";
        return Encoding.UTF8.GetBytes(html);
    }

    private static byte[] ExcelBytes(HistoryRecord record)
    {
        var builder = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\"><Worksheet ss:Name=\"Differences\"><Table>");
        builder.Append("<Row>");
        foreach (var heading in new[] { "Path", "Kind", "OldType", "NewType", "OldValue", "NewValue", "Message" })
        {
            builder.Append($"<Cell><Data ss:Type=\"String\">{Xml(heading)}</Data></Cell>");
        }
        builder.Append("</Row>");
        foreach (var diff in record.Result.Differences)
        {
            builder.Append("<Row>");
            foreach (var value in new[] { diff.Path, diff.Kind, diff.OldType, diff.NewType, diff.OldValue ?? string.Empty, diff.NewValue ?? string.Empty, diff.Message })
            {
                builder.Append($"<Cell><Data ss:Type=\"String\">{Xml(value)}</Data></Cell>");
            }
            builder.Append("</Row>");
        }
        builder.Append("</Table></Worksheet></Workbook>");
        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static byte[] PdfBytes(HistoryRecord record)
    {
        var lines = new List<string>
        {
            "Interface JSON Compare Platform",
            $"Report: {record.Name}",
            $"Result: {(record.Result.IsEqual ? "EQUAL" : "DIFFERENT")}",
            $"Differences: {record.Result.Summary.Total}",
            $"Duration: {record.Result.DurationMs} ms"
        };
        lines.AddRange(record.Result.Differences.Take(38).Select(diff => $"{diff.Kind} {diff.Path}"));
        return MinimalPdf(lines);
    }

    private static byte[] MinimalPdf(IEnumerable<string> lines)
    {
        var content = new StringBuilder("BT /F1 9 Tf 40 760 Td ");
        var first = true;
        foreach (var line in lines)
        {
            if (!first)
            {
                content.Append(" 0 -18 Td ");
            }
            first = false;
            content.Append('(').Append(EscapePdf(line)).Append(") Tj ");
        }
        content.Append("ET");
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content.ToString())} >>\nstream\n{content}\nendstream"
        };
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.WriteLine("%PDF-1.4");
        writer.Flush();
        var offsets = new List<long> { 0 };
        foreach (var (value, index) in objects.Select((value, index) => (value, index)))
        {
            offsets.Add(stream.Position);
            writer.WriteLine($"{index + 1} 0 obj");
            writer.WriteLine(value);
            writer.WriteLine("endobj");
            writer.Flush();
        }
        var xref = stream.Position;
        writer.WriteLine($"xref\n0 {objects.Length + 1}");
        writer.WriteLine("0000000000 65535 f ");
        foreach (var offset in offsets.Skip(1))
        {
            writer.WriteLine($"{offset:0000000000} 00000 n ");
        }
        writer.WriteLine($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        writer.Flush();
        return stream.ToArray();
    }

    private static string EscapeCsv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
    private static string EscapeMarkdown(string? value) => (value ?? string.Empty).Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    private static string EscapePdf(string value) => new(value.Where(character => character is >= ' ' and <= '~').Select(character => character is '(' or ')' or '\\' ? '\\' : character).ToArray());
    private static string Html(string? value) => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
    private static string Xml(string? value) => System.Security.SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;
}

public sealed record ReportFile(string ContentType, string FileName, byte[] Content);
