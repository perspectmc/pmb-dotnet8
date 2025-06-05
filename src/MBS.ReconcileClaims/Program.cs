using MBS.DomainModel;
using MBS.ReconcileClaims;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MBS.ReconcileClaims
{
    class Program
    {
        private MedicalBillingSystemEntities _context;

        static void Main(string[] args)
        {
            var program = new Program();
            program.ProcessPaidClaimsFromReturnFile();
        }

        private void ProcessPaidClaimsFromReturnFile()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return;
            }

            var userId = new Guid("D2075A51-EDB4-49AA-8216-454246F8E73C");

            var profile = GetUserProfile(userId);
            var clinicNumber = profile.ClinicNumber.ToString().PadLeft(3, '0');

            var returnFiles = GetReturnFilesByUserId(userId);

            var returnClaimList = new List<ClaimsInReturn>();

            var paidClaims = new List<PaidItem>();
            foreach(var item in returnFiles)
            {
                var content = SecurityHelper.DecryptString(item.Content);
                var parser = new ReturnParser(content, profile.DoctorNumber.ToString(), profile.ClinicNumber.ToString(), userId, _context);

                foreach (var paidLine in parser.PaidLines)
                {
                    paidClaims.Add(parser.GetPaidAmount(paidLine));
                }

                var returnClaims = parser.GenerateReturnClaims();
                returnClaimList.AddRange(returnClaims);
            }

            //var returnSubmitted = returnClaimList.Sum(x => x.TotalSubmitted);
            //var returnApproved = returnClaimList.Sum(x => x.TotalApproved);
            
            //var claims2016 = returnClaimList.SelectMany(x => x.PaidClaim.SelectMany(y => y.ServiceRecord)).Where(x => x.ServiceDate < new DateTime(2017, 1, 1));
            //var paidRecordCount = claims2016.SelectMany(x => x.UnitRecord).Count();
            //var total2016 = claims2016.Sum(x => (decimal)x.PaidAmount);

            //var claims2017 = returnClaimList.SelectMany(x => x.PaidClaim.SelectMany(y => y.ServiceRecord)).Where(x => x.ServiceDate >= new DateTime(2017, 1, 1));
            //var paidRecordCount2017 = claims2017.SelectMany(x => x.UnitRecord).Count();
            //var total2017 = claims2017.Sum(x => (decimal)x.PaidAmount);

            var paidReturnList2016 = paidClaims.Where(x => x.ServiceDate < new DateTime(2017, 1, 1));
            var paid220For2016 = paidReturnList2016.Where(x => x.ClinicNumber == "220");
            var paid220Total2016 = string.Format("{0:C}", paid220For2016.Sum(x => x.PaidAmount));
            var paid520For2016 = paidReturnList2016.Where(x => x.ClinicNumber == "520");
            var paid520Total2016 = string.Format("{0:C}", paid520For2016.Sum(x => x.PaidAmount));
            
            //var paidReturnList2017 = paidClaims.Where(x => x.ServiceDate >= new DateTime(2017, 1, 1) && x.ClinicNumber == clinicNumber);
            //var notTheSameCountList2017 = paidReturnList2017.Where(x => !x.IsTheSame).ToList();
            //var paidReturnRecordCount2017 = paidReturnList2017.Count();
            //var paidTotal2017 = string.Format("{0:C}", paidReturnList2017.Sum(x => x.PaidAmount));

            Console.WriteLine("done");
        }

        private IEnumerable<ClaimsInReturn> GetReturnFilesByUserId(Guid userId)
        {
            return _context.ClaimsInReturn.Where(x => x.UserId == userId && x.Content != null).OrderBy(x => x.UploadDate).ToList();
        }

        private UserProfiles GetUserProfile(Guid userId)
        {
            return _context.UserProfiles.FirstOrDefault(x => x.UserId == userId);
        }

        private void WriteToFile(string fileName, string content)
        {
            var file = new StreamWriter(fileName);
            file.Write(content);
            file.Close();
        }
    }
}
