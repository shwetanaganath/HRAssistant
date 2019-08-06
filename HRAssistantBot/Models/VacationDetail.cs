using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HRAssistantBot.Models
{
    // Defines a state property used to track information about the user.
    public class VacationDetail
    {
        public string Name { get; set; }
        public DateTime LeaveStartDate { get; set; }

        public int NoOfDays { get; set; }

        public string SetOutlookMessage { get; set; }
        public string OutlookMessage { get; set; }

        public string OutlookMessageVisibility { set; get; }

    }
}
