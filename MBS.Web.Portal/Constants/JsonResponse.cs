using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MBS.Web.Portal.Constants
{
    public class JsonResponse
    {
        public int Status { get; set; }

        public string Message { get; set; }      
    }

    public class SubmitResponse
    {
        public bool MSBIsPDFContent { get; set; }

        public string MSBValidationReportPDFFileName { get; set; }

        public bool MSBSubmitted { get; set; }

        public bool MSBRejected { get; set; }

        public bool MSBServerError { get; set; }

        public string MSBMessage { get; set; }

        public bool WCBSubmitted { get; set; }

        public string WCBMessage { get; set; }

        public string WCBSubmittedIds { get; set; }
    }

    public class FaxReceiptResponse
    {
        public string CompletionTime { get; set; }

        public string PageSent { get; set; }

        public string SubmissionTime { get; set; }

        public string FaxStatus { get; set; }        

        public bool IsSuccess { get; set; }
    }
}