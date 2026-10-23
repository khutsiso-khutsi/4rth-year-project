using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;

namespace _4th_year_set_up.Models
{
    // ------------------------------------------------------------------ consumables check (sample verification)

    // One consumable a tube needs, against what the Lab Manager has in stock.
    public class ConsumableNeed
    {
        public string Name { get; set; }
        public int Needed { get; set; }
        public int OnHand { get; set; }
        public bool IsShort => OnHand < Needed;
    }

    // Result of checking a tube. If no test on the tube has consumables linked yet (dbo.TestTypeConsumables is empty
    // for it), HasMapping is false and the tube is allowed through; the screen says so.
    public class ConsumableCheck
    {
        public List<ConsumableNeed> Needs { get; set; } = new();
        public List<string> MissingTests { get; set; } = new();       // tests on the tube with no consumable linked
        public bool HasMapping => Needs.Count > 0;
        public bool IsSufficient => Needs.All(n => !n.IsShort);
        public bool IsBlocked => MissingTests.Count > 0 || !IsSufficient;
        public IEnumerable<ConsumableNeed> Short => Needs.Where(n => n.IsShort);
        public string ShortText => string.Join(", ", Short.Select(n => $"{n.Name} (need {n.Needed}, have {n.OnHand})"));

        // The reason a tube is blocked, in words for the screen.
        public string BlockText =>
            MissingTests.Count > 0
                ? $"No consumables are linked to {string.Join(", ", MissingTests)}. Ask the lab manager to link them first."
                : $"Not enough stock: {ShortText}. Ask the lab manager to restock.";
    }

    // ------------------------------------------------------------------ reports

    public enum ReportKind { Turnaround, Outstanding, Workload, Rejections }

    // What the technician chose on the Reporting screen.
    public class ReportRequest
    {
        public ReportKind Kind { get; set; } = ReportKind.Turnaround;
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int? TestTypeId { get; set; }
        public string Priority { get; set; }          // null = all
        public int? TechnicianId { get; set; }        // null = all
        public bool OnlyMine { get; set; }
        public string Graph { get; set; } = "bar";    // bar | line

        public static string TitleOf(ReportKind k) => k switch
        {
            ReportKind.Turnaround => "Test Turnaround Report",
            ReportKind.Outstanding => "Outstanding Tests Report",
            ReportKind.Workload => "Technician Workload Report",
            _ => "Rejections and Returns Report"
        };
    }

    public class ReportsPageViewModel
    {
        public List<KeyValuePair<int, string>> TestTypes { get; set; } = new();
        public List<KeyValuePair<int, string>> Technicians { get; set; } = new();
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    public class ChartPoint
    {
        public string Label { get; set; }
        public double Value { get; set; }
        public double? Target { get; set; }
        public string Note { get; set; }               // small text under the label (for example "31 tests")
    }

    public class ReportChart
    {
        public string Type { get; set; } = "bar";      // bar | line
        public string Title { get; set; }
        public string Caption { get; set; }
        public string Note { get; set; }
        public string YLabel { get; set; }
        public string SeriesName { get; set; }
        public string TargetName { get; set; }
        public string ValueFormat { get; set; } = "0.0";
        public List<ChartPoint> Points { get; set; } = new();
    }

    public class ReportTable
    {
        public string Heading { get; set; }
        public string Note { get; set; }
        public List<string> Columns { get; set; } = new();
        public List<bool> Numeric { get; set; } = new();          // right-align this column
        public List<string[]> Rows { get; set; } = new();
        public string[] Total { get; set; }                       // optional totals row
    }

    // Everything the PDF template needs. The template contains no logic of its own.
    public class ReportDoc
    {
        public string Title { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public string GeneratedBy { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string FilterText { get; set; }
        public List<KeyValuePair<string, string>> Summary { get; set; } = new();
        public ReportChart Chart { get; set; }
        public List<ReportTable> Tables { get; set; } = new();
        public bool IsEmpty { get; set; }
        public string EmptyMessage { get; set; } = "No records found for the selected date range.";

        public string Period => $"{From:d MMMM yyyy} – {To:d MMMM yyyy}";
    }

    // ------------------------------------------------------------------ the graph: plain SVG so it prints and exports to PDF as-is

    public static class ReportSvg
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private const string Navy = "#1f3a5f";
        private const string Accent = "#c2410c";

        public static string Render(ReportChart c)
            => c == null || c.Points.Count == 0 ? "" : (c.Type == "line" ? Line(c) : Bar(c));

        private static string E(string s) => WebUtility.HtmlEncode(s ?? "");
        private static string N(double v) => v.ToString("0.#", Inv);

        // A "nice" top for the y axis and the step between gridlines.
        private static (double max, double step) Scale(double top)
        {
            if (top <= 0) return (1, 0.2);
            var rough = top / 5;
            var mag = Math.Pow(10, Math.Floor(Math.Log10(rough)));
            var norm = rough / mag;
            var step = (norm <= 1 ? 1 : norm <= 2 ? 2 : norm <= 5 ? 5 : 10) * mag;
            return (Math.Ceiling(top / step) * step, step);
        }

        private static string Frame(ReportChart c, double max, double step, double x0, double x1, double yb, double ph, StringBuilder sb)
        {
            for (var v = 0.0; v <= max + step / 2; v += step)
            {
                var y = yb - v / max * ph;
                sb.Append($"<line x1=\"{N(x0)}\" y1=\"{N(y)}\" x2=\"{N(x1)}\" y2=\"{N(y)}\" stroke=\"#d4d4d4\" stroke-width=\"1\"/>");
                sb.Append($"<text x=\"{N(x0 - 8)}\" y=\"{N(y + 3.5)}\" font-size=\"10\" text-anchor=\"end\" fill=\"#444\">{N(v)}</text>");
            }
            if (!string.IsNullOrEmpty(c.YLabel))
                sb.Append($"<text x=\"14\" y=\"{N(yb - ph / 2)}\" font-size=\"10\" fill=\"#444\" text-anchor=\"middle\" transform=\"rotate(-90 14 {N(yb - ph / 2)})\">{E(c.YLabel)}</text>");
            return "";
        }

        private static string Bar(ReportChart c)
        {
            const double x0 = 56, x1 = 676, yb = 210, ph = 170;
            var top = Math.Max(c.Points.Max(p => p.Value), c.Points.Max(p => p.Target ?? 0));
            var (max, step) = Scale(top);
            var sb = new StringBuilder();
            sb.Append($"<svg viewBox=\"0 0 700 262\" width=\"682\" role=\"img\" aria-label=\"{E(c.Title)}\" style=\"display:block\">");
            Frame(c, max, step, x0, x1, yb, ph, sb);

            var slot = (x1 - x0) / c.Points.Count;
            var bw = Math.Min(34, slot * 0.6);
            for (var i = 0; i < c.Points.Count; i++)
            {
                var p = c.Points[i];
                var cx = x0 + slot * i + slot / 2;
                var h = p.Value / max * ph;
                sb.Append($"<rect x=\"{N(cx - bw / 2)}\" y=\"{N(yb - h)}\" width=\"{N(bw)}\" height=\"{N(h)}\" fill=\"{Navy}\"/>");
                sb.Append($"<text x=\"{N(cx)}\" y=\"{N(yb - h - 5)}\" font-size=\"10\" text-anchor=\"middle\" fill=\"#111\">{E(p.Value.ToString(c.ValueFormat, Inv))}</text>");
                if (p.Target.HasValue)
                {
                    var ty = yb - p.Target.Value / max * ph;
                    sb.Append($"<line x1=\"{N(cx - bw / 2 - 5)}\" y1=\"{N(ty)}\" x2=\"{N(cx + bw / 2 + 5)}\" y2=\"{N(ty)}\" stroke=\"{Accent}\" stroke-width=\"2.5\"/>");
                }
                sb.Append($"<text x=\"{N(cx)}\" y=\"{N(yb + 16)}\" font-size=\"10.5\" text-anchor=\"middle\" fill=\"#111\">{E(Shorten(p.Label, slot))}</text>");
                if (!string.IsNullOrEmpty(p.Note))
                    sb.Append($"<text x=\"{N(cx)}\" y=\"{N(yb + 29)}\" font-size=\"9.5\" text-anchor=\"middle\" fill=\"#444\">{E(p.Note)}</text>");
            }
            sb.Append($"<line x1=\"{N(x0)}\" y1=\"{N(yb)}\" x2=\"{N(x1)}\" y2=\"{N(yb)}\" stroke=\"#111\" stroke-width=\"1.2\"/>");

            sb.Append($"<rect x=\"{N(x0)}\" y=\"244\" width=\"12\" height=\"10\" fill=\"{Navy}\"/><text x=\"{N(x0 + 17)}\" y=\"253\" font-size=\"10.5\" fill=\"#111\">{E(c.SeriesName)}</text>");
            if (c.Points.Any(p => p.Target.HasValue) && !string.IsNullOrEmpty(c.TargetName))
                sb.Append($"<line x1=\"{N(x0 + 190)}\" y1=\"249\" x2=\"{N(x0 + 208)}\" y2=\"249\" stroke=\"{Accent}\" stroke-width=\"2.5\"/><text x=\"{N(x0 + 213)}\" y=\"253\" font-size=\"10.5\" fill=\"#111\">{E(c.TargetName)}</text>");
            sb.Append("</svg>");
            return sb.ToString();
        }

        private static string Line(ReportChart c)
        {
            const double x0 = 56, x1 = 676, yb = 210, ph = 170;
            var top = c.Points.Max(p => p.Value);
            var (max, step) = Scale(top);
            var sb = new StringBuilder();
            sb.Append($"<svg viewBox=\"0 0 700 262\" width=\"682\" role=\"img\" aria-label=\"{E(c.Title)}\" style=\"display:block\">");
            Frame(c, max, step, x0, x1, yb, ph, sb);

            var n = c.Points.Count;
            double X(int i) => n == 1 ? (x0 + x1) / 2 : x0 + 40 + (x1 - x0 - 80) * i / (n - 1);
            double Y(double v) => yb - v / max * ph;

            // dashed reference line when the points carry a common target
            var targets = c.Points.Where(p => p.Target.HasValue).Select(p => p.Target.Value).ToList();
            if (targets.Count > 0)
            {
                var t = targets.Average();
                sb.Append($"<line x1=\"{N(x0)}\" y1=\"{N(Y(t))}\" x2=\"{N(x1)}\" y2=\"{N(Y(t))}\" stroke=\"{Accent}\" stroke-width=\"1.8\" stroke-dasharray=\"6 4\"/>");
            }

            if (n > 1)
                sb.Append($"<polyline fill=\"none\" stroke=\"{Navy}\" stroke-width=\"2.5\" points=\"" +
                          string.Join(" ", c.Points.Select((p, i) => $"{N(X(i))},{N(Y(p.Value))}")) + "\"/>");
            for (var i = 0; i < n; i++)
            {
                var p = c.Points[i];
                sb.Append($"<circle cx=\"{N(X(i))}\" cy=\"{N(Y(p.Value))}\" r=\"4.5\" fill=\"{Navy}\"/>");
                sb.Append($"<text x=\"{N(X(i))}\" y=\"{N(Y(p.Value) - 10)}\" font-size=\"10.5\" text-anchor=\"middle\" fill=\"#111\">{E(p.Value.ToString(c.ValueFormat, Inv))}</text>");
                sb.Append($"<text x=\"{N(X(i))}\" y=\"{N(yb + 16)}\" font-size=\"10.5\" text-anchor=\"middle\" fill=\"#111\">{E(p.Label)}</text>");
                if (!string.IsNullOrEmpty(p.Note))
                    sb.Append($"<text x=\"{N(X(i))}\" y=\"{N(yb + 29)}\" font-size=\"9.5\" text-anchor=\"middle\" fill=\"#444\">{E(p.Note)}</text>");
            }
            sb.Append($"<line x1=\"{N(x0)}\" y1=\"{N(yb)}\" x2=\"{N(x1)}\" y2=\"{N(yb)}\" stroke=\"#111\" stroke-width=\"1.2\"/>");

            sb.Append($"<line x1=\"{N(x0)}\" y1=\"249\" x2=\"{N(x0 + 18)}\" y2=\"249\" stroke=\"{Navy}\" stroke-width=\"2.5\"/><text x=\"{N(x0 + 23)}\" y=\"253\" font-size=\"10.5\" fill=\"#111\">{E(c.SeriesName)}</text>");
            if (targets.Count > 0 && !string.IsNullOrEmpty(c.TargetName))
                sb.Append($"<line x1=\"{N(x0 + 190)}\" y1=\"249\" x2=\"{N(x0 + 208)}\" y2=\"249\" stroke=\"{Accent}\" stroke-width=\"1.8\" stroke-dasharray=\"6 4\"/><text x=\"{N(x0 + 213)}\" y=\"253\" font-size=\"10.5\" fill=\"#111\">{E(c.TargetName)}</text>");
            sb.Append("</svg>");
            return sb.ToString();
        }

        // Keep long labels inside their slot (the full name is in the table below the chart).
        private static string Shorten(string s, double slot)
        {
            var max = Math.Max(4, (int)(slot / 6.2));
            return string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max - 1) + ".";
        }
    }
}
