using System.Collections.Generic;
using System.Web.Mvc;

namespace MBS.Web.Portal.Constants
{
    public static class ServiceLocationSelectList
    {
        public static IEnumerable<SelectListItem> GetList()
        {
            var list = new List<SelectListItem>();
            list.Add(new SelectListItem() { Value = "", Text = "Blank", Selected = true });
            list.Add(new SelectListItem() { Value = "R", Text = "Regina" });
            list.Add(new SelectListItem() { Value = "S", Text = "Saskatoon" });
            return list;
        }
    }
}