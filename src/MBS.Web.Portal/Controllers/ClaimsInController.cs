using MBS.DataCache;
using MBS.Web.Portal.Constants;
using MBS.Web.Portal.Models;
using MBS.Web.Portal.RazorPDF;
using MBS.Web.Portal.Repositories;
using MBS.Web.Portal.Services;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI;

namespace MBS.Web.Portal.Controllers
{
    [OutputCache(NoStore = true, Duration = 0)]
    [Authorize(Roles="Member")]
    public class ClaimsInController : Controller
    {
        private readonly IClaimsInRepository _repository;
		private readonly Guid _userId = Guid.Empty;
        private int _timeZoneOffset = -6;
		// If you are using Dependency Injection, you can delete the following constructor
        public ClaimsInController() : this(new ClaimsInRepository())
        {
        }

        public ClaimsInController(IClaimsInRepository repository)
        {
            _repository = repository;
            var user = Membership.GetUser();
            if (user != null)
            {
                _userId = new Guid(user.ProviderUserKey.ToString());
            }

            _timeZoneOffset = ConfigHelper.GetTimeZoneOffset();
        }

        public ActionResult ViewClaimsInFiles()
        {
            ViewBag.TimeZoneOffset = _timeZoneOffset;
            var model = _repository.GetClaimsInWithContentByUser(_userId).ToList();
            return View(model);
        }

        #region Download

        public ActionResult DownloadFile(string id)
        {
            byte[] fileContent = new byte[1];
            var createdDate = DateTime.UtcNow.AddHours(_timeZoneOffset);

            var claimsInId = Guid.Empty;
            Guid.TryParse(id, out claimsInId);

            if (_repository.IsAccessValidClaimsIn(_userId, claimsInId))
            {
                var claimIn = _repository.GetClaimsIn(claimsInId);
                if (claimIn != null)
                {
                    fileContent = System.Text.ASCIIEncoding.ASCII.GetBytes(claimIn.Content);
                    createdDate = claimIn.CreatedDate.AddHours(_timeZoneOffset);
                }
                else
                {
                    fileContent = System.Text.ASCIIEncoding.ASCII.GetBytes("Unable to retrieve report content or invalid access! Please check the parameters!");
                }
            }
            else
            {
                fileContent = System.Text.ASCIIEncoding.ASCII.GetBytes("Unable to retrieve report content or invalid access! Please check the parameters!");
            }

            return File(fileContent, "text", "ClaimsIn-" + createdDate.ToString("yyyyMMddHHmmss") + ".txt");
        }

        public ActionResult DownloadValidation(string id)
        {
            var createdDate = DateTime.UtcNow.AddHours(_timeZoneOffset);
            byte[] fileContent = new byte[1];
            var fileType = "text";
            var fileExtension = ".txt";

            var claimsInId = Guid.Empty;
            Guid.TryParse(id, out claimsInId);

            if (_repository.IsAccessValidClaimsIn(_userId, claimsInId))
            {
                var claimIn = _repository.GetClaimsIn(claimsInId);
                if (claimIn != null)
                {
                    createdDate = claimIn.CreatedDate.AddHours(_timeZoneOffset);
                    if (string.IsNullOrEmpty(claimIn.ValidationContent))
                    {
                        fileContent = System.Text.ASCIIEncoding.ASCII.GetBytes("No Content");
                    }
                    else if (claimIn.ValidationContent.StartsWith("<div style"))
                    {
                        fileContent = System.Text.ASCIIEncoding.ASCII.GetBytes(claimIn.ValidationContent);
                        fileExtension = ".html";
                        fileType = "html";
                    }
                    else
                    {
                        fileContent = Convert.FromBase64String(claimIn.ValidationContent);
                        fileExtension = ".pdf";
                        fileType = "pdf";
                    }
                }
                else
                {
                    fileContent = System.Text.ASCIIEncoding.ASCII.GetBytes("Unable to retrieve validation content or invalid access! Please check the parameters!");
                }
            }
            else
            {
                fileContent = System.Text.ASCIIEncoding.ASCII.GetBytes("Unable to retrieve validation content or invalid access! Please check the parameters!");
            }

            return File(fileContent, fileType, "Submission_Validation_Report_" + createdDate.ToString("yyyyMMddHHmmss") + fileExtension);
        }


        #endregion
    }
}
