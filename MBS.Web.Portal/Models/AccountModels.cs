using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Profile;

namespace MBS.Web.Portal.Models
{    
    public class LocalPasswordModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 7)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class ChangeEmailModel
    {
        [Display(Name = "Current Email Address")]
        [DataType(DataType.EmailAddress)]
        public string CurrentEmail { get; set; }

        [Required]
        [Display(Name = "New Email Address")]
        [EmailAddress(ErrorMessage = "The email address must be in valid format.")]
        [DataType(DataType.EmailAddress)]
        public string NewEmail { get; set; }
    }

    public class ChangeEmailRequestModel
    {
        [Display(Name = "New Email Address")]
        [DataType(DataType.EmailAddress)]
        public string NewEmail { get; set; }

        [Required]
        [Display(Name = "Current Email Address")]
        [EmailAddress(ErrorMessage = "The current email address must be in valid format.")]
        [DataType(DataType.EmailAddress)]
        public string CurrentEmail { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "PIN Code (max 5 attempts)")]
        public string PinCode { get; set; }

        public string Token { get; set; }
    }

    public class LoginModel
    {
        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        public List<string> NotificationDetails { get; set; }
    }

    public class ForgotPasswordModel
    {
        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; }

        [Display(Name = "Registered Email")]
        [EmailAddress(ErrorMessage="The email address must be in valid format.")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }   
        
        public int ErrorCount { get; set; } 
    }

    public class ResetForgotPasswordModel
    {        
        public string ResetForgotPasswordToken { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 7)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class ChangeSecretModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string CurrentPassword { get; set; }

        [Required]
        [StringLength(255, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [Display(Name = "New Secret Question")]
        public string SecretQuestion { get; set; }

        [Required]
        [StringLength(20, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 4)]
        [Display(Name = "New Secret Answer")]
        public string SecretAnswer { get; set; }
    }
}
