using System.Collections.Generic;
using System.Web.Mvc;

namespace MBS.Web.Portal.Constants
{
    public static class SpecialCircumstancesIndicatorSelectList
    {
        public static IEnumerable<SelectListItem> GetList()
        {
            var list = new List<SelectListItem>();
            list.Add(new SelectListItem() { Value = "", Text = "Blank", Selected = true });
            list.Add(new SelectListItem() { Value = "TF", Text = "Billing Technical Fees Only" });
            list.Add(new SelectListItem() { Value = "PF", Text = "Interpretation Fees Only"});
            list.Add(new SelectListItem() { Value = "CF", Text = "Combined Tech and Interp. Fees" });
            list.Add(new SelectListItem() { Value = "TA", Text = "Takeover - Anesthetic" });
            return list;
        }
    }
}