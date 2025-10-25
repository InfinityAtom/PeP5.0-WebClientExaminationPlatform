using System.Text;
using System.Text.Encodings.Web;
using AIExamIDE.Models;

namespace AIExamIDE.Backend;

public static class SeatmapPrintTemplate
{
    public static string Render(string roomName, IEnumerable<Desk> desks)
    {
        var encoder = HtmlEncoder.Default;
        var safeName = encoder.Encode(roomName);
        var sb = new StringBuilder();
        sb.Append("""
<!doctype html><html><head><meta charset="utf-8"><title>Seat Map - 
""");
        sb.Append(safeName);
        sb.Append("""
</title><style>body{font-family:Arial;margin:0;padding:0;background:#f5f5f5;color:#333}
.container{max-width:960px;margin:20px auto;padding:20px;background:#fff;border-radius:8px;box-shadow:0 2px 8px rgba(0,0,0,0.1)}
.title{text-align:center;font-size:24px;margin-bottom:20px}
.grid{position:relative;width:900px;height:600px;border:1px solid #ccc;margin:0 auto;background:#fafafa}
.desk{position:absolute;border:1px solid #333;border-radius:4px;padding:6px;min-width:60px;text-align:center;background:#f7f7f7;box-shadow:0 2px 4px rgba(0,0,0,0.08)}
.meta{margin-top:12px;text-align:center;font-size:12px;color:#666}</style></head><body>
<div class="container">
<div class="title">Seat Map: 
""");
        sb.Append(safeName);
        sb.Append("</div><div class=\"grid\">");

        foreach (var desk in desks)
        {
            var left = desk.X;
            var top = desk.Y;
            var name = encoder.Encode(string.IsNullOrWhiteSpace(desk.Name) ? (desk.Id ?? "Desk") : desk.Name);
            sb.Append("<div class=\"desk\" style=\"left:");
            sb.Append(left);
            sb.Append("px;top:");
            sb.Append(top);
            sb.Append("px;\">");
            sb.Append(name);
            sb.Append("</div>");
        }

        sb.Append("</div><div class=\"meta\">Generated ");
        sb.Append(DateTime.UtcNow.ToString("u"));
        sb.Append("</div></div><script>window.print && window.print();</script></body></html>");
        return sb.ToString();
    }
}
