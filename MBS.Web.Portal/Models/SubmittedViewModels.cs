using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MBS.DomainModel;

namespace MBS.Web.Portal.Models
{
    public class SubmittedViewModel
    {
        public bool AllowToDownload { get; set; }

        public IEnumerable<ClaimsIn> ClaimsInList { get; set; }
    }
}