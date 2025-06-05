using System.Collections.Generic;
using System.Web.Mvc;

namespace MBS.Web.Portal.Constants
{
    public static class RecordClaimTypeSelectList
    {
        public static IEnumerable<SelectListItem> GetList()
        {
            var list = new List<SelectListItem>();
            list.Add(new SelectListItem() { Value = "", Text = "Blank", Selected = true });
            list.Add(new SelectListItem() { Value = "P", Text = "Prior approval was requested from MSB" });
            list.Add(new SelectListItem() { Value = "W", Text = "Not WCB Responsibility" });
            list.Add(new SelectListItem() { Value = "D", Text = "Not DVA Responsibility" });
            return list;
        }
    }
}