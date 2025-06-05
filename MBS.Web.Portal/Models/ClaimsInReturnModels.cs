using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MBS.DomainModel;

namespace MBS.Web.Portal.Models
{
    public class ClaimsInReturnViewModel
    {
        public int ComeBackHour { get; set; }

        public bool HasCertificateUploaded { get; set; }

        public bool IsSiteDown { get; set; }

		public HttpPostedFileBase MyFile { get; set; }

        public IEnumerable<ClaimsInReturn> ClaimsInReturnList { get; set; }
    }
}