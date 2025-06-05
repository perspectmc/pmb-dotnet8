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
