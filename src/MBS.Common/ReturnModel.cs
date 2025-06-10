/*
=============================================================================
FILE: ReturnModel.cs (Foundation File - Used by entire system)
BUSINESS PURPOSE: 
Defines the "language" for communicating with MSB (Medical Services Branch).
When MSB sends back responses to claims, this file tells our system how to 
interpret and categorize those responses.

CORE BUSINESS WORKFLOW:
1. Physicians submit claims → MSB reviews → MSB sends return files
2. DAILY returns = Rejections only (allows quick fixes before 2-week deadline)
3. BIWEEKLY returns = Complete results (paid + rejected claims)
4. WCB returns = Workers compensation (separate fax workflow)
5. System automatically processes these using API keys embedded per practice

KEY COMPONENTS:
- ReturnFileType enum: Categorizes MSB response files (DAILY/BIWEEKLY/WCB/MANUAL)
- ErrorType enum: Defines 9 types of processing errors that can occur
- MSBAccessToken class: Handles API authentication with MSB systems
- ReturnModel classes: Data structures for file processing

UPSTREAM DEPENDENCIES (Files that use this):
- ServiceRecordController.cs (claim submission & status display)
- ReturnRetrivalJob.cs (automated daily/biweekly file pickup)
- ClaimService.cs (MSB API integration)
- ReturnParser.cs (converts MSB format to database format)
- SubmitPendingClaims.cs (processes submitted claim responses)

DOWNSTREAM DEPENDENCIES (What this file uses):
- Only basic .NET System libraries (very stable)
- No dependencies on other MBS files

BUSINESS IMPACT: HIGH - OPERATIONAL CRITICAL
- If broken: Automated claim processing stops, manual intervention required
- Revenue impact: Delayed processing, missed 2-week correction deadlines
- Fallback: Manual download from MSB portal + manual upload (labor intensive)
- Affects: All practices using the automated billing platform

MIGRATION ACTIONS NEEDED (.NET Framework → .NET 8):
- [ ] Verify enum serialization patterns work in .NET 8
- [ ] Test JSON serialization for API communication
- [ ] Validate MSBAccessToken OAuth flow compatibility
- [ ] Test ReturnFileType enum database storage/retrieval
- [ ] Verify ErrorType enum handling in error scenarios

FUTURE ENHANCEMENTS RECOMMENDED:
- [ ] Add manual file upload capability as backup to API processing
- [ ] Consider adding more granular error types for better troubleshooting
- [ ] Evaluate adding retry logic definitions for failed API calls

MIGRATION PRIORITY: High (Foundation file - many others depend on this)
COMPLEXITY LEVEL: Low (Simple data structures, minimal dependencies)
=============================================================================
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MBS.Common
{
    public enum ErrorType
    {
        CERTIFICATE_ERROR,
        EMPTY_CONTENT,
        REJECTED_CLAIM,
        UNAVAILABLE,
        VALIDATION_FAILED,
        MSB_SERVER_ERROR,
        SERVER_ERROR,
        DUPLICATE_FILENAME,
        UNAUTHORIZED
    }

    public enum ReturnFileType
    {
        DAILY = 0,
        BIWEEKLY = 1,
        WCB = 2,
        MANUAL = 3     
    }

    public class ReturnModel
    {
        public bool IsSuccess { get; set; }

        public ErrorType ErrorType { get; set; }

        public string ISCContent { get; set; }

        public ReturnFileType ReturnFileType { get; set; }

        public string FileName { get; set; }
    }

    public class ReturnFileNameListModel
    {
        public bool IsSuccess { get; set; }

        public string ErrorMessage { get; set; }

        public IEnumerable<string> FileNames { get; set; }
    }


    public class ReturnFileModel
    {
        public bool IsSuccess { get; set; }

        public string ErrorMessage { get; set; }

        public string FileContent { get; set; }

        public string FileName { get; set; }

        public ReturnFileType ReturnFileType { get; set; }

        public DateTime FileDateTime { get; set; }
    }

    public class SpecicalCode
    {
        public string Code { get; set; }

        public float Rate { get; set; }
    }

    [DataContract]
    public class MSBAccessToken
    {
        [DataMember]
        public string access_token { get; set; }
        [DataMember]
        public string scope { get; set; }
        [DataMember]
        public int expires_in { get; set; }
        [DataMember]
        public string token_type { get; set; }

        public DateTime token_expired_in { get; set; }
    }
}
