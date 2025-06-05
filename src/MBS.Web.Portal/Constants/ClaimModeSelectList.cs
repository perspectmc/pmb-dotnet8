using System.Collections.Generic;
using System.Web.Mvc;

namespace MBS.Web.Portal.Constants
{
    public static class ClaimModeSelectList
    {
        public static IEnumerable<SelectListItem> GetList()
        {
            var list = new List<SelectListItem>();
            list.Add(new SelectListItem() { Value = "1", Text = "Physicians and Dentists", Selected = true });
            list.Add(new SelectListItem() { Value = "6", Text = "Optometrists" });
            list.Add(new SelectListItem() { Value = "0", Text = "Global Budget, Primary Health Physicians / Nurses" });
            list.Add(new SelectListItem() { Value = "9", Text = "Alternate Payment Physicians/Nurses" });
            return list;
        }
    }
}