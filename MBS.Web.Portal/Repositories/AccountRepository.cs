using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using MBS.DomainModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using MBS.Web.Portal.Constants;
using System.Data.Entity.Validation;

namespace MBS.Web.Portal.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        MedicalBillingSystemEntities context = new MedicalBillingSystemEntities();

        public AccountRepository()
        {
            context.Configuration.AutoDetectChangesEnabled = false;
        }

		public UserProfiles GetUserProfile(Guid userId)
		{
			return context.UserProfiles.FirstOrDefault(x => x.UserId == userId);
		}

        public UserCertificates GetUserCertificate(Guid userId)
        {
            return context.UserCertificates.FirstOrDefault(x => x.UserId == userId);
        }

        public Memberships GetUserMemberShip(Guid userId)
        {
            return context.Memberships.FirstOrDefault(x => x.UserId == userId);
        }

        public Memberships GetUserMemberShip(string token)
        {
            return context.Memberships.FirstOrDefault(x => x.PasswordToken == token);
        }

        public void InsertOrUpdate(UserProfiles userProfile, Guid userId)
        {
            if (userProfile.UserId == Guid.Empty)
            {
                userProfile.UserId = userId;
                context.UserProfiles.Add(userProfile);
            }
            else
            {
                // Existing entity
                context.Entry(userProfile).State = System.Data.Entity.EntityState.Modified;
            }
        }

        public void InsertOrUpdate(UserCertificates userCertificate, Guid userId)
        {
            if (userCertificate.UserId == Guid.Empty)
            {
                userCertificate.UserId = userId;
                context.UserCertificates.Add(userCertificate);
            }
            else
            {
                // Existing entity
                context.Entry(userCertificate).State = System.Data.Entity.EntityState.Modified;
            }
        }

        public void UpdateMembership(Memberships membership)
        {
            context.Entry(membership).State = System.Data.Entity.EntityState.Modified;
        }

        public string GetUserDoctorName(Guid userId)
        {
            var result = string.Empty;

            var userProfile = context.UserProfiles.FirstOrDefault(x => x.UserId == userId);
            if (userProfile != null)
            {
                result = userProfile.DoctorName;
            }

            return result;
        }

        public void WakeUp()
        {
            context.Users.FirstOrDefault(x => x.UserId == Guid.NewGuid());
        }

        public void Save()
        {
            context.SaveChanges();            
        }

        public void Dispose()
        {
            context.Dispose();
        }
    }

    public interface IAccountRepository : IDisposable
    {		
		UserProfiles GetUserProfile(Guid userId);
        UserCertificates GetUserCertificate(Guid userId);
        Memberships GetUserMemberShip(Guid userId);

        Memberships GetUserMemberShip(string token);

        string GetUserDoctorName(Guid userId);
        void WakeUp();
        void InsertOrUpdate(UserProfiles userProfile, Guid userId);
        void InsertOrUpdate(UserCertificates userCertificate, Guid userId);
        void UpdateMembership(Memberships membership);
        void Save();
    }
}