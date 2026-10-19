// Put this file in your Models folder.
// IMPORTANT: the reference ranges and critical limits below are generic adult placeholders so the screen works.
// Replace them with your laboratory's validated, age/sex-specific ranges and critical-value limits before real use.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace _4th_year_set_up.Models
{
    // One measurable parameter of a test (e.g. Haemoglobin).
    public class ParameterDef
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public decimal? Low { get; set; }
        public decimal? High { get; set; }
        public decimal? CritLow { get; set; }
        public decimal? CritHigh { get; set; }
        public bool IsText { get; set; }

        public string RangeText
        {
            get
            {
                if (IsText) return "—";
                if (Low.HasValue && High.HasValue) return $"{Fmt(Low.Value)} – {Fmt(High.Value)}";
                if (High.HasValue) return $"< {Fmt(High.Value)}";
                if (Low.HasValue) return $"> {Fmt(Low.Value)}";
                return "—";
            }
        }

        private static string Fmt(decimal d) => d.ToString("0.##", CultureInfo.InvariantCulture);
    }

    // Used only to move values between the capture page and the controller.
    // Saved results are stored on TestItem.Results as ResultLine (your existing class).
    public class ResultValue
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public string Value { get; set; }
        public string Flag { get; set; }      // normal | low | high | critical-low | critical-high
        public string Comment { get; set; }
    }

    public class CaptureResultsViewModel
    {
        public TestItem Item { get; set; }
        public List<ParameterDef> Parameters { get; set; } = new();
        public List<ResultValue> Existing { get; set; } = new();
        public bool IsReview { get; set; }
    }

    public static class ResultCatalog
    {
        // Parameters for a test, matched on the test name. Sex gives sex-specific ranges where they differ.
        // Unknown tests get one free-text "Result" field.
        public static List<ParameterDef> For(string testName, string sex = null)
        {
            var t = (testName ?? "").ToLowerInvariant();
            var female = string.Equals(sex, "Female", StringComparison.OrdinalIgnoreCase);

            decimal hbLow = female ? 12.0m : 13.0m, hbHigh = female ? 15.5m : 17.0m;
            decimal hctLow = female ? 0.36m : 0.40m, hctHigh = female ? 0.46m : 0.50m;
            decimal rbcLow = female ? 3.8m : 4.5m, rbcHigh = female ? 4.8m : 5.5m;

            if (t.Contains("full blood") || t.Contains("fbc") || t.Contains("cbc") || t.Contains("complete blood"))
                return new List<ParameterDef>
                {
                    P("wbc",  "WBC",         "×10³/µL",  4.0m, 11.0m, 2.0m, 30.0m),
                    P("rbc",  "RBC",         "×10¹²/L",  rbcLow, rbcHigh, null, null),
                    P("hb",   "Haemoglobin", "g/dL",     hbLow, hbHigh, 7.0m, 20.0m),
                    P("hct",  "Haematocrit", "L/L",      hctLow, hctHigh, 0.20m, 0.60m),
                    P("mcv",  "MCV",         "fL",       80m, 100m, null, null),
                    P("mch",  "MCH",         "pg",       27m, 32m, null, null),
                    P("mchc", "MCHC",        "g/dL",     32m, 36m, null, null),
                    P("plt",  "Platelets",   "×10³/µL",  150m, 400m, 20m, 1000m)
                };

            if (t.Contains("haemoglobin") || t.Contains("hemoglobin") || t == "hb")
                return new List<ParameterDef> { P("hb", "Haemoglobin", "g/dL", hbLow, hbHigh, 7.0m, 20.0m) };

            if (t.Contains("platelet"))
                return new List<ParameterDef> { P("plt", "Platelets", "×10³/µL", 150m, 400m, 20m, 1000m) };

            if (t.Contains("esr") || t.Contains("sedimentation"))
                return new List<ParameterDef> { P("esr", "ESR", "mm/hr", 0m, 20m, null, null) };

            if (t.Contains("reticulocyte") || t.Contains("retic"))
                return new List<ParameterDef> { P("retic", "Reticulocytes", "%", 0.5m, 2.5m, null, null) };

            if (t.Contains("partial thromboplastin") || t.Contains("aptt") || t.Contains("ptt"))
                return new List<ParameterDef> { P("aptt", "aPTT", "sec", 25m, 35m, null, 100m) };

            if (t.Contains("prothrombin") || t.Contains("inr") || t.Contains("pt/inr"))
                return new List<ParameterDef>
                {
                    P("pt",  "Prothrombin time", "sec", 11m, 13.5m, null, null),
                    P("inr", "INR",              "",    0.8m, 1.2m, null, 5.0m)
                };

            if (t.Contains("differential"))
                return new List<ParameterDef>
                {
                    P("neut", "Neutrophils", "×10³/µL", 1.8m, 7.7m, null, null),
                    P("lymp", "Lymphocytes", "×10³/µL", 1.0m, 4.8m, null, null),
                    P("mono", "Monocytes",   "×10³/µL", 0.2m, 1.0m, null, null),
                    P("eos",  "Eosinophils", "×10³/µL", 0.0m, 0.5m, null, null),
                    P("baso", "Basophils",   "×10³/µL", 0.0m, 0.1m, null, null)
                };

            if (t.Contains("film") || t.Contains("smear"))
                return new List<ParameterDef> { new ParameterDef { Key = "morph", Name = "Morphology findings", Unit = "", IsText = true } };

            return new List<ParameterDef> { new ParameterDef { Key = "result", Name = "Result", Unit = "", IsText = true } };
        }

        // Accepts "12,5" as well as "12.5" (South African regional settings use a comma).
        public static string Normalise(string raw) => (raw ?? "").Trim().Replace(',', '.');

        public static string Flag(ParameterDef d, string raw)
        {
            if (d.IsText) return "";
            if (!decimal.TryParse(Normalise(raw), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                                  CultureInfo.InvariantCulture, out var v))
                return "invalid";

            if (d.CritLow.HasValue && v < d.CritLow.Value) return "critical-low";
            if (d.CritHigh.HasValue && v > d.CritHigh.Value) return "critical-high";
            if (d.Low.HasValue && v < d.Low.Value) return "low";
            if (d.High.HasValue && v > d.High.Value) return "high";
            return "normal";
        }

        // ResultLine.Flag uses: Normal | High | Low | Critical
        public static string ToLineFlag(string flag) => flag switch
        {
            "low" => "Low",
            "high" => "High",
            "critical-low" or "critical-high" => "Critical",
            _ => "Normal"
        };

        private static ParameterDef P(string key, string name, string unit, decimal? low, decimal? high, decimal? critLow, decimal? critHigh)
            => new ParameterDef { Key = key, Name = name, Unit = unit, Low = low, High = high, CritLow = critLow, CritHigh = critHigh };
    }
}
