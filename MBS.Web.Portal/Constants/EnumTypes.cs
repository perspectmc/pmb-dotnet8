using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MBS.Web.Portal.Constants
{
    public enum ReportType
    {
        Both = 1,
        Paid = 2,
        Rejected = 3,
        UnitRecord = 4,
        UnitRecordWithPaidClaim = 5,
        UnitRecordWithRejectedClaim = 6
    }

    public enum SearchClaimType
    {
        All = -1,
        Unsubmitted = 0,
        Submitted = 1,
        Pending = 2,
        Paid = 3,
        Rejected = 4,
        Deleted = 5
    }

    public enum ClaimsInType
	{
		ClaimsIn = 1,
		PaidClaim = 2,
		RejectedClaim = 3
	}

    public enum ClaimType
    {
        MSB = 0,
        WCB = 1
    }

    public enum DeliverStatus
    {
        PENDING = 0,
        FAIL = 1,
        SUCCESS = 2
    }
}