using System.Collections.Generic;
using System.Web.Mvc;

namespace MBS.Web.Portal.Constants
{
    public static class ProvinceSelectList
    {
        public static IEnumerable<SelectListItem> GetList()
        {
            var list = new List<SelectListItem>();
            list.Add(new SelectListItem() { Value = "SK", Text = "Saskatchewan" });
            list.Add(new SelectListItem() { Value = "BC", Text = "British Columbia" });
            list.Add(new SelectListItem() { Value = "AB", Text = "Alberta" });
            list.Add(new SelectListItem() { Value = "MB", Text = "Manitoba" });
            list.Add(new SelectListItem() { Value = "ON", Text = "Ontario" });
            list.Add(new SelectListItem() { Value = "NB", Text = "New Brunswick" });
            list.Add(new SelectListItem() { Value = "NS", Text = "Nova Scotia" });
            list.Add(new SelectListItem() { Value = "PE", Text = "Prince Edward Island" });
            list.Add(new SelectListItem() { Value = "NL", Text = "Newfoundland and Labrador" });
            list.Add(new SelectListItem() { Value = "YT", Text = "Yukon" });
            list.Add(new SelectListItem() { Value = "NT", Text = "North West Territory" });
            list.Add(new SelectListItem() { Value = "NU", Text = "Nunavut" });
            list.Add(new SelectListItem() { Value = "QC", Text = "Quebec - Not Allowed" });

            return list;
        }
    }
}