using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MBS.DomainModel
{
    [MetadataType(typeof(UserProfileMetaData))]
    public partial class UserProfiles
    {
    }

    public class UserProfileMetaData
    {
        [Required]
        [Display(Name = "Doctor Number")]
        [StringLength(4)]
        public string DoctorNumber { get; set; }

        [Required]
        [StringLength(80)]
        [Display(Name = "Doctor/Clinic Name")]
        public string DoctorName { get; set; }

        [Required]
        [Display(Name = "Clinic Number")]
        [StringLength(4)]
        public string ClinicNumber { get; set; }

        [Required]
        [Display(Name = "Group Number")]
        [StringLength(3)]
        public string GroupNumber { get; set; }

        [Display(Name = "Group User Key")]
        [StringLength(10)]
        public string GroupUserKey { get; set; }

        [Required]
        [StringLength(3)]
        [Display(Name = "Diagnostic Code")]
        public string DiagnosticCode{ get; set; }

        [StringLength(1)]
        [Display(Name = "Premium Code")]
        public string DefaultPremCode { get; set; }

        [Display(Name = "Service Location")]
        public string DefaultServiceLocation { get; set; }

        [Display(Name = "Corporation Indicator")]
        public string CorporationIndicator { get; set; }

        [Required]
        [Display(Name = "Phone Number (306-123-4567)")]
        [StringLength(12)]
        public string PhoneNumber { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Street Address")]
        public string Street { get; set; }       

        [Required]
        [StringLength(50)]
        [Display(Name = "Town/City")]
        public string City { get; set; }

        [Required]
        public string Province { get; set; }
        
        [Required]
        [StringLength(6)]
        [Display(Name = "Postal Code")]
        [DataType(DataType.PostalCode)]
        public string PostalCode { get; set; }

        [Display(Name = "Claim Mode")]
        public string ClaimMode { get; set; }
    }
}
