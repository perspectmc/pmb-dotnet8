using System.Collections.Generic;
using System.Web.Mvc;

namespace MBS.Web.Portal.Constants
{
    public static class IndicatorSelectList
    {
        public static IEnumerable<SelectListItem> GetList()
        {
            var list = new List<SelectListItem>();
            list.Add(new SelectListItem() { Value = " ", Text = "Practitioner billing", Selected = true });
            list.Add(new SelectListItem() { Value = "A", Text = "Physician Corporation 1 billing" });
            list.Add(new SelectListItem() { Value = "B", Text = "Physician Corporation 2 billing" });
            list.Add(new SelectListItem() { Value = "C", Text = "Physician Corporation 3 billing" });
            return list;
        }
    }
}