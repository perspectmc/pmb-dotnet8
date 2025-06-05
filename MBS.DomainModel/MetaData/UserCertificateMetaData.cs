using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MBS.DomainModel
{
    [MetadataType(typeof(UserCertificateMetaData))]
    public partial class UserCertificates
    {
    }

    public class UserCertificateMetaData
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Certificate Passkey")]
        [DataType(DataType.Password)]
        public string CertificatePassKey { get; set; }
    }
}
