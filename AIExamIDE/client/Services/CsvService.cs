using System;
using System.Collections.Generic;
using System.Linq;

namespace AIExamIDE.Services
{
    public class CsvService
    {
        public string ConvertTableToCsv(List<CsvColumn> columns, List<CsvRow> rows)
        {
            var lines = new List<string>();
            
            // Add header line
            if (columns.Any())
            {
                var headers = columns.Select(c => QuoteIfNeeded(c.Name));
                lines.Add(string.Join(",", headers));
            }
            
            // Add data rows
            foreach (var row in rows)
            {
                var values = new List<string>();
                for (int i = 0; i < columns.Count; i++)
                {
                    var value = i < row.Values.Count ? row.Values[i] : "";
                    values.Add(QuoteIfNeeded(value));
                }
                lines.Add(string.Join(",", values));
            }
            
            return string.Join(Environment.NewLine, lines);
        }

        public (List<CsvColumn> columns, List<CsvRow> rows) ParseCsvText(string csvText)
        {
            var columns = new List<CsvColumn>();
            var rows = new List<CsvRow>();

            if (string.IsNullOrWhiteSpace(csvText))
            {
                Console.WriteLine("ParseCsvText: CSV text is null or empty");
                return (columns, rows);
            }
            
            Console.WriteLine($"ParseCsvText: Input length = {csvText.Length}");
            Console.WriteLine($"ParseCsvText: First 200 chars = '{csvText.Substring(0, Math.Min(200, csvText.Length))}'");
            
            var lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                              .Where(line => !string.IsNullOrWhiteSpace(line))
                              .ToList();
            
            Console.WriteLine($"ParseCsvText: Found {lines.Count} non-empty lines");
            
            if (!lines.Any()) 
                return (columns, rows);
            
            // Parse header
            Console.WriteLine($"ParseCsvText: Parsing header line: '{lines[0]}'");
            var headers = SplitCsvLine(lines[0]);
            Console.WriteLine($"ParseCsvText: Split header into {headers.Count} parts: [{string.Join(", ", headers)}]");
            
            foreach (var header in headers)
            {
                columns.Add(new CsvColumn { Name = header.Trim() });
            }
            
            // Parse data rows
            for (int i = 1; i < lines.Count; i++)
            {
                var fields = SplitCsvLine(lines[i]);
                var row = new CsvRow();
                
                for (int j = 0; j < columns.Count; j++)
                {
                    var value = j < fields.Count ? fields[j].Trim() : "";
                    row.Values.Add(value);
                }
                
                rows.Add(row);
            }

            Console.WriteLine($"ParseCsvText: Final result = {columns.Count} columns, {rows.Count} rows");
            return (columns, rows);
        }

        private List<string> SplitCsvLine(string line)
        {
            var result = new List<string>();
            var current = "";
            var inQuotes = false;
            
            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];
                
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current += '"';
                        i++; // Skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current);
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            
            result.Add(current);
            return result;
        }

        private string QuoteIfNeeded(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                var escaped = value.Replace("\"", "\"\"");
                return "\"" + escaped + "\"";
            }
            
            return value;
        }
    }

    // Data classes for the CSV service
    public class CsvColumn
    {
        public string Name { get; set; } = "";
    }

    public class CsvRow
    {
        public List<string> Values { get; set; } = new();
    }
}