using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MBS.Common
{
    public class CertificateInfo
    {
        public string Subject { get; set; }

        public string Issuer { get; set; }

        public DateTime ValidFrom { get; set; }

        public DateTime ValidTo { get; set; }
    }
}
