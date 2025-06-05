using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace MBS.Common
{
    public static class GenericHelper
    {
        public static CertificateInfo GetCertificateInfo(byte[] certificateBytes, string password)
        {
            CertificateInfo result = null;

            try
            {
                var cert = new X509Certificate2(certificateBytes, password, X509KeyStorageFlags.MachineKeySet);
                result = new CertificateInfo();
                result.Subject = cert.Subject;
                result.Issuer = cert.Issuer;
                result.ValidFrom = cert.NotBefore;
                result.ValidTo = cert.NotAfter;
            }
            catch
            {
            }

            return result;
        }
    }
}
