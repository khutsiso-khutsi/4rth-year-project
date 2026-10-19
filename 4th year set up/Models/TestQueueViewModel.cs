using System.Collections.Generic;

namespace _4th_year_set_up.Models
{
    public class TestQueueViewModel
    {
        public int AvailableToClaim { get; set; }

        public int MyActiveTestsCount { get; set; }

        public int ReturnedForReview { get; set; }

        public int CompletedToday { get; set; }

        public List<EligibleTestViewModel> AvailableTests { get; set; }
            = new List<EligibleTestViewModel>();

        public List<ActiveTestViewModel> ActiveTests { get; set; }
            = new List<ActiveTestViewModel>();
    }
}
