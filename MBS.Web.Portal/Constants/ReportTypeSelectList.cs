using System.Collections.Generic;
using System.Web.Mvc;

namespace MBS.Web.Portal.Constants
{
    public static class ReportTypeSelectList
    {
        public static IEnumerable<SelectListItem> GetList()
        {
            var list = new List<SelectListItem>();

            list.Add(new SelectListItem() { Value = "1", Text = "Both Paid and Rejected Claims" });
            list.Add(new SelectListItem() { Value = "2", Text = "Paid Claim" });
            list.Add(new SelectListItem() { Value = "3", Text = "Rejected Claim" });
            list.Add(new SelectListItem() { Value = "4", Text = "Unit Records Summary" });
            list.Add(new SelectListItem() { Value = "5", Text = "Unit Records Summary - Paid Claims" });
            list.Add(new SelectListItem() { Value = "6", Text = "Unit Records Summary - Rejected Claims" });
            return list;
        }
    }
}