using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Profile;

namespace MBS.Web.Portal.Models
{
    public class UserManagementViewModel    
    {
        public IEnumerable<UserModel> Users { get; set; }
    }

    public class UserModel
    {
        public Guid UserId { get; set; }

        [Display(Name = "Name")]
        public string Name { get; set; }

        [Display(Name = "Username")]
        public string UserName { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Last Login Date")]
        public DateTime LastLoginDate { get; set; }

        [Display(Name = "Locked")]
        public bool IsLockOut { get; set; }
    }

    public class RegisterModel
    {
        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [Display(Name = "Email")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required]
        [StringLength(15, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class ResetPasswordModel
    {
        public Guid UserId { get; set; }

        public string Username { get; set; }

        [Required]
        [StringLength(15, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 7)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmNewPassword { get; set; }
    }

    public class SendBulkEmailModel
    {
        public string UserEmail { get; set; }

        public string MessageSubject { get; set; }

        public string MessageBody { get; set; }
    }
}
