using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MBS.DomainModel;

namespace MBS.Web.Portal.Models
{
    public class NotSubmittedViewModel
    {
        public bool AllowToSubmit { get; set; }

        public bool ContainUserProfile { get; set; }

        public bool ContainRequiredSubmissionData { get; set; }

        public double TotalAmount { get; set; }

        public DateTime CurrentDate { get; set; }

        public IEnumerable<SimpleRecord> UnSubmittedList { get; set; }
    }
}