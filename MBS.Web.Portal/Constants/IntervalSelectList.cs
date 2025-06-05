using System.Collections.Generic;
using System.Web.Mvc;

namespace MBS.Web.Portal.Constants
{
    public static class IntervalSelectList
    {
        public static IEnumerable<SelectListItem> GetList()
        {
            var list = new List<SelectListItem>();
            list.Add(new SelectListItem() { Value = "ytd", Text = "Year To Date", Selected = true });
            list.Add(new SelectListItem() { Value = "last12month", Text = "Last 12 Months" });
            list.Add(new SelectListItem() { Value = "last3month", Text = "Last 3 Months" });
            list.Add(new SelectListItem() { Value = "last2week", Text = "Last 2 Weeks" });
            list.Add(new SelectListItem() { Value = "all", Text = "All" });
            return list;
        }
    }
}