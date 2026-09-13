namespace _4th_year_set_up.Models
{
    public class ReceiveSampleViewModel
    {
            public string SampleId { get; set; }
            public string PatientId { get; set; }
            public string PatientName { get; set; }
            public string SampleType { get; set; }
            public string RequestedTest { get; set; }
            public DateTime CollectionDateTime { get; set; }
            public string SampleCondition { get; set; }
            public string Notes { get; set; }
            public List<ReceivedSampleRow> RecentSamples { get; set; }
        }

        public class ReceivedSampleRow
        {
            public string SampleId { get; set; }
            public string PatientName { get; set; }
            public string SampleType { get; set; }
            public DateTime ReceivedAt { get; set; }
            public string Status { get; set; } // "Received", "Pending", "Rejected"
        }
}
