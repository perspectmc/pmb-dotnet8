using System;
using System.Web;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using MBS.DomainModel;

namespace MBS.Web.Portal.Models
{
    public class UserCertificateViewModel
    {
        public UserCertificates UserCertificate { get; set; }

        [Display(Name = "Certificate Subject")]
        public string CertificateSubject { get; set; }

        [Display(Name = "Certificate Issuer")]
        public string CertificatePublisher { get; set; }

        [Display(Name = "Certificate Valid From")]
        public DateTime CertificateValidFrom { get; set; }

        [Display(Name = "Certificate Valid To")]
        public DateTime CertificateValidTo { get; set; }

        public bool CertificateExsited { get; set; }

        public HttpPostedFileBase CertificateFile { get; set; }
    }
}