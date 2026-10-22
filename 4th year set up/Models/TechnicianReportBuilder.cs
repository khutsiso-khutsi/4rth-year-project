using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace _4th_year_set_up.Models
{
    // Turns the loaded requests into a ReportDoc for the four reports.
    // Confidentiality: reports show request numbers, test names and technician names only. Never a patient name or ID.
    public static class TechnicianReportBuilder
    {
        public static ReportDoc Build(IEnumerable<LabRequest> requests, ReportRequest r, string generatedBy,
                                      string currentTechId, Func<string, string> nameOf)
        {
            var from = r.From.Date;
            var to = r.To.Date.AddDays(1).AddTicks(-1);

            var doc = new ReportDoc
            {
                Title = ReportRequest.TitleOf(r.Kind),
                GeneratedBy = generatedBy,
                From = from,
                To = r.To.Date
            };

            var tests = requests.SelectMany(q => q.Tests).ToList();
            var filters = new List<string>();

            if (r.TestTypeId.HasValue)
            {
                tests = tests.Where(t => t.TestTypeId == r.TestTypeId.Value).ToList();
                filters.Add("Test type: " + (tests.FirstOrDefault()?.Test ?? "selected type"));
            }
            else filters.Add("Test type: All");

            if (!string.IsNullOrEmpty(r.Priority))
            {
                tests = tests.Where(t => t.Request.Priority == r.Priority).ToList();
                filters.Add("Priority: " + r.Priority);
            }
            else filters.Add("Priority: All");

            if (r.OnlyMine)
            {
                tests = tests.Where(t => t.AssignedTo == currentTechId || t.CapturedBy == currentTechId || t.VerifiedBy == currentTechId).ToList();
                filters.Add("Technician: " + nameOf(currentTechId) + " (own work only)");
            }
            else if (r.TechnicianId.HasValue)
            {
                var id = r.TechnicianId.Value.ToString();
                tests = tests.Where(t => t.AssignedTo == id || t.CapturedBy == id || t.VerifiedBy == id).ToList();
                filters.Add("Technician: " + nameOf(id));
            }
            else filters.Add("Technician: All");

            doc.FilterText = string.Join("  |  ", filters);

            switch (r.Kind)
            {
                case ReportKind.Turnaround: Turnaround(doc, tests, from, to, r.Graph); break;
                case ReportKind.Outstanding: Outstanding(doc, tests, from, to, r.Graph); break;
                case ReportKind.Workload: Workload(doc, tests, from, to, r.Graph, nameOf); break;
                default: Rejections(doc, tests, from, to, r.Graph, nameOf); break;
            }

            if (doc.Tables.Count == 0 || doc.Tables.All(t => t.Rows.Count == 0))
            {
                doc.IsEmpty = true;
                doc.Chart = null;
                doc.Summary.Clear();
            }
            return doc;
        }

        // ---------------------------------------------------------------- helpers

        private static bool In(DateTime? d, DateTime from, DateTime to) => d.HasValue && d.Value >= from && d.Value <= to;

        private static string Dur(double minutes)
        {
            if (minutes < 60) return $"{minutes:0} min";
            var h = (int)(minutes / 60);
            var m = (int)Math.Round(minutes - h * 60);
            return m == 0 ? $"{h} h" : $"{h} h {m} min";
        }

        private static string Pct(int part, int whole) => whole == 0 ? "—" : $"{100.0 * part / whole:0}%";

        // Week buckets (Monday start) that cover the period, for the line graph.
        private static List<(DateTime start, string label)> Weeks(DateTime from, DateTime to)
        {
            var list = new List<(DateTime, string)>();
            var d = from.Date;
            d = d.AddDays(-(((int)d.DayOfWeek + 6) % 7));
            while (d <= to)
            {
                list.Add((d, d.ToString("d MMM", CultureInfo.InvariantCulture)));
                d = d.AddDays(7);
            }
            return list;
        }

        private static int WeekIndex(List<(DateTime start, string label)> weeks, DateTime d)
        {
            for (var i = weeks.Count - 1; i >= 0; i--)
                if (d.Date >= weeks[i].start) return i;
            return 0;
        }

        private static ReportChart NewChart(string graph, string title, string caption, string y, string series, string format = "0")
            => new ReportChart
            {
                Type = graph == "line" ? "line" : "bar",
                Title = title,
                Caption = caption,
                YLabel = y,
                SeriesName = series,
                ValueFormat = format
            };

        // ---------------------------------------------------------------- 1. turnaround

        private static void Turnaround(ReportDoc doc, List<TestItem> all, DateTime from, DateTime to, string graph)
        {
            // A test counts when its result was verified inside the period. Clock = sample received until verified.
            var done = all.Where(t => t.Stage == TestStage.Verified && In(t.VerifiedAt, from, to) && t.Sample != null && t.Sample.ReceivedAt > DateTime.MinValue)
                          .Select(t => (t, mins: (t.VerifiedAt.Value - t.Sample.ReceivedAt).TotalMinutes))
                          .ToList();

            var within = done.Count(x => x.t.TargetMinutes > 0 && x.mins <= x.t.TargetMinutes);
            var withTarget = done.Count(x => x.t.TargetMinutes > 0);

            doc.Summary.Add(new("Tests completed", done.Count.ToString()));
            doc.Summary.Add(new("Average turnaround", done.Count == 0 ? "—" : Dur(done.Average(x => x.mins))));
            doc.Summary.Add(new("Within target", Pct(within, withTarget)));
            doc.Summary.Add(new("Longest turnaround", done.Count == 0 ? "—" : Dur(done.Max(x => x.mins))));

            var byType = done.GroupBy(x => x.t.Test).OrderBy(g => g.Key).ToList();
            var table = new ReportTable
            {
                Heading = "Turnaround by test type",
                Note = "Turnaround runs from the moment the sample is received until the result is verified.",
                Columns = new() { "Test type", "Completed", "Average", "Target", "Within target", "Longest" },
                Numeric = new() { false, true, true, true, true, true }
            };
            foreach (var g in byType)
            {
                var target = g.First().t.TargetMinutes;
                var ok = g.Count(x => target > 0 && x.mins <= target);
                table.Rows.Add(new[]
                {
                    g.Key, g.Count().ToString(), Dur(g.Average(x => x.mins)),
                    target > 0 ? Dur(target) : "—", target > 0 ? $"{ok} of {g.Count()} ({Pct(ok, g.Count())})" : "—",
                    Dur(g.Max(x => x.mins))
                });
            }
            doc.Tables.Add(table);

            if (graph == "line")
            {
                var weeks = Weeks(from, to);
                var chart = NewChart("line", "Average turnaround per week", "Weekly average, from sample received to result verified.", "Minutes", "Average turnaround (min)");
                for (var i = 0; i < weeks.Count; i++)
                {
                    var wk = done.Where(x => WeekIndex(weeks, x.t.VerifiedAt.Value) == i).ToList();
                    chart.Points.Add(new ChartPoint { Label = weeks[i].label, Value = wk.Count == 0 ? 0 : wk.Average(x => x.mins), Note = wk.Count + " tests" });
                }
                doc.Chart = chart;
            }
            else
            {
                var chart = NewChart("bar", "Average turnaround by test type", "Bars show the average; the orange mark is the target for that test.", "Minutes", "Average turnaround (min)");
                chart.TargetName = "Target";
                foreach (var g in byType)
                    chart.Points.Add(new ChartPoint { Label = g.Key, Value = g.Average(x => x.mins), Target = g.First().t.TargetMinutes > 0 ? g.First().t.TargetMinutes : null, Note = g.Count() + " tests" });
                doc.Chart = chart;
            }
        }

        // ---------------------------------------------------------------- 2. outstanding

        private static void Outstanding(ReportDoc doc, List<TestItem> all, DateTime from, DateTime to, string graph)
        {
            // Tests still open that belong to requests made inside the period.
            var open = all.Where(t => t.Stage != TestStage.Verified && t.Request.RequestedAt >= from && t.Request.RequestedAt <= to)
                          .OrderByDescending(t => t.Tat == TatStatus.Overdue).ThenBy(t => t.Request.RequestedAt).ToList();

            doc.Summary.Add(new("Outstanding tests", open.Count.ToString()));
            doc.Summary.Add(new("Overdue", open.Count(t => t.Tat == TatStatus.Overdue).ToString()));
            doc.Summary.Add(new("Urgent or STAT", open.Count(t => t.Request.Priority != "Routine").ToString()));
            doc.Summary.Add(new("Oldest request", open.Count == 0 ? "—" : open.Min(t => t.Request.RequestedAt).ToString("d MMM yyyy", CultureInfo.InvariantCulture)));

            var table = new ReportTable
            {
                Heading = "Tests still to be completed",
                Note = "Overdue tests are listed first, then oldest request first.",
                Columns = new() { "Request", "Test", "Priority", "Stage", "With", "Requested", "Due" },
                Numeric = new() { false, false, false, false, false, false, false }
            };
            foreach (var t in open)
                table.Rows.Add(new[]
                {
                    t.ReqNum, t.Test, t.Request.Priority, t.StageLabel,
                    string.IsNullOrEmpty(t.AssignedTo) ? "Unassigned" : TechnicianNames.Of(t.AssignedTo),
                    t.Request.RequestedAt.ToString("d MMM HH:mm", CultureInfo.InvariantCulture), t.DueText ?? "—"
                });
            doc.Tables.Add(table);

            if (graph == "line")
            {
                var weeks = Weeks(from, to);
                var chart = NewChart("line", "Outstanding tests by week requested", "How many tests from each week's requests are still open.", "Tests", "Outstanding tests");
                for (var i = 0; i < weeks.Count; i++)
                    chart.Points.Add(new ChartPoint { Label = weeks[i].label, Value = open.Count(t => WeekIndex(weeks, t.Request.RequestedAt) == i) });
                doc.Chart = chart;
            }
            else
            {
                var chart = NewChart("bar", "Outstanding tests by test type", "Open tests per test type; this shows where the backlog sits.", "Tests", "Outstanding tests");
                foreach (var g in open.GroupBy(t => t.Test).OrderByDescending(g => g.Count()).ThenBy(g => g.Key))
                    chart.Points.Add(new ChartPoint { Label = g.Key, Value = g.Count(), Note = g.Count(t => t.Tat == TatStatus.Overdue) + " overdue" });
                doc.Chart = chart;
            }
        }

        // ---------------------------------------------------------------- 3. workload

        private static void Workload(ReportDoc doc, List<TestItem> all, DateTime from, DateTime to, string graph, Func<string, string> nameOf)
        {
            var captured = all.Where(t => !string.IsNullOrEmpty(t.CapturedBy) && In(t.CapturedAt, from, to)).ToList();
            var verified = all.Where(t => !string.IsNullOrEmpty(t.VerifiedBy) && In(t.VerifiedAt, from, to)).ToList();
            var samples = all.Select(t => t.Sample).Where(s => s != null && !string.IsNullOrEmpty(s.VerifiedBy) && In(s.VerifiedAt, from, to)).Distinct().ToList();
            var returned = all.SelectMany(t => t.History.Where(h => !string.IsNullOrEmpty(h.CapturedBy) && In(h.CapturedAt, from, to))).ToList();

            var ids = captured.Select(t => t.CapturedBy).Concat(verified.Select(t => t.VerifiedBy))
                              .Concat(samples.Select(s => s.VerifiedBy)).Distinct().OrderBy(nameOf).ToList();

            doc.Summary.Add(new("Results captured", captured.Count.ToString()));
            doc.Summary.Add(new("Results verified", verified.Count.ToString()));
            doc.Summary.Add(new("Samples verified", samples.Count.ToString()));
            doc.Summary.Add(new("Technicians active", ids.Count.ToString()));

            var table = new ReportTable
            {
                Heading = "Work done by each technician",
                Note = "Counts are for actions completed inside the selected period.",
                Columns = new() { "Technician", "Samples verified", "Results captured", "Results verified", "Returned to them" },
                Numeric = new() { false, true, true, true, true }
            };
            foreach (var id in ids)
                table.Rows.Add(new[]
                {
                    nameOf(id),
                    samples.Count(s => s.VerifiedBy == id).ToString(),
                    captured.Count(t => t.CapturedBy == id).ToString(),
                    verified.Count(t => t.VerifiedBy == id).ToString(),
                    returned.Count(h => h.CapturedBy == id).ToString()
                });
            if (ids.Count > 0)
                table.Total = new[] { "Total", samples.Count.ToString(), captured.Count.ToString(), verified.Count.ToString(), returned.Count.ToString() };
            doc.Tables.Add(table);

            if (graph == "line")
            {
                var weeks = Weeks(from, to);
                var chart = NewChart("line", "Results captured per week", "All technicians together, by week.", "Results", "Results captured");
                for (var i = 0; i < weeks.Count; i++)
                    chart.Points.Add(new ChartPoint { Label = weeks[i].label, Value = captured.Count(t => WeekIndex(weeks, t.CapturedAt.Value) == i) });
                doc.Chart = chart;
            }
            else
            {
                var chart = NewChart("bar", "Results captured by technician", "Number of results each technician captured in the period.", "Results", "Results captured");
                foreach (var id in ids)
                    chart.Points.Add(new ChartPoint { Label = nameOf(id).Split(' ')[0], Value = captured.Count(t => t.CapturedBy == id) });
                doc.Chart = chart;
            }
        }

        // ---------------------------------------------------------------- 4. rejections and returns

        private static void Rejections(ReportDoc doc, List<TestItem> all, DateTime from, DateTime to, string graph, Func<string, string> nameOf)
        {
            var rejected = all.Select(t => t.Sample).Where(s => s != null && s.Status == TubeStatus.Rejected && In(s.VerifiedAt, from, to)).Distinct().ToList();
            var returns = all.Where(t => t.History.Count > 0)
                             .SelectMany(t => t.History.Select(h => (t, h)))
                             .Where(x => In(x.h.CapturedAt, from, to)).ToList();

            doc.Summary.Add(new("Samples rejected", rejected.Count.ToString()));
            doc.Summary.Add(new("Results returned", returns.Count.ToString()));
            var top = rejected.GroupBy(s => ReasonOf(s)).OrderByDescending(g => g.Count()).FirstOrDefault();
            doc.Summary.Add(new("Most common rejection", top == null ? "—" : top.Key));
            doc.Summary.Add(new("Tests affected", (rejected.Sum(s => s.Tests.Count) + returns.Select(x => x.t).Distinct().Count()).ToString()));

            var t1 = new ReportTable
            {
                Heading = "Rejected samples",
                Note = "A rejected sample means a new sample has to be collected.",
                Columns = new() { "Request", "Tube", "Tests", "Reason", "Rejected by", "Date" },
                Numeric = new() { false, false, false, false, false, false }
            };
            foreach (var s in rejected.OrderBy(s => s.VerifiedAt))
                t1.Rows.Add(new[]
                {
                    s.Request.ReqNum, s.TubeId, s.TestsLabel, ReasonOf(s), nameOf(s.VerifiedBy),
                    s.VerifiedAt.Value.ToString("d MMM HH:mm", CultureInfo.InvariantCulture)
                });
            doc.Tables.Add(t1);

            var t2 = new ReportTable
            {
                Heading = "Results returned for review",
                Note = "Each row is a result that a verifier sent back to the technician who captured it.",
                Columns = new() { "Request", "Test", "Captured by", "Captured", "Now" },
                Numeric = new() { false, false, false, false, false }
            };
            foreach (var x in returns.OrderBy(x => x.h.CapturedAt))
                t2.Rows.Add(new[]
                {
                    x.t.ReqNum, x.t.Test, nameOf(x.h.CapturedBy),
                    x.h.CapturedAt.Value.ToString("d MMM HH:mm", CultureInfo.InvariantCulture), x.t.StageLabel
                });
            doc.Tables.Add(t2);

            if (graph == "line")
            {
                var weeks = Weeks(from, to);
                var chart = NewChart("line", "Rejections and returns per week", "Rejected samples plus results returned, by week.", "Count", "Rejections and returns");
                for (var i = 0; i < weeks.Count; i++)
                    chart.Points.Add(new ChartPoint
                    {
                        Label = weeks[i].label,
                        Value = rejected.Count(s => WeekIndex(weeks, s.VerifiedAt.Value) == i) + returns.Count(x => WeekIndex(weeks, x.h.CapturedAt.Value) == i)
                    });
                doc.Chart = chart;
            }
            else
            {
                var chart = NewChart("bar", "Rejected samples by reason", "Why samples were rejected in the period.", "Samples", "Samples rejected");
                foreach (var g in rejected.GroupBy(s => ReasonOf(s)).OrderByDescending(g => g.Count()).ThenBy(g => g.Key))
                    chart.Points.Add(new ChartPoint { Label = g.Key, Value = g.Count() });
                doc.Chart = chart;
            }
        }

        private static string ReasonOf(SampleTube s) => string.IsNullOrWhiteSpace(s.Condition) || s.Condition == "Not yet verified" ? "Other" : s.Condition;
    }
}
