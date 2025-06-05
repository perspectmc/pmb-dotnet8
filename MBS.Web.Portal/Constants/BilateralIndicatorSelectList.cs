using System.Collections.Generic;
using System.Web.Mvc;

namespace MBS.Web.Portal.Constants
{
    public static class BilateralIndicatorSelectList
    {
        public static IEnumerable<SelectListItem> GetList()
        {
            var list = new List<SelectListItem>();
            list.Add(new SelectListItem() { Value = "", Text = "Blank", Selected = true });
            list.Add(new SelectListItem() { Value = "L", Text = "Left" });
            list.Add(new SelectListItem() { Value = "R", Text = "Right" });
            list.Add(new SelectListItem() { Value = "B", Text = "Bilateral" });
            return list;
        }
    }
}