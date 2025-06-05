using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MBS.Web.Portal.Models
{
    public class HomeViewModel
    {
        public IEnumerable<Notification> NotificationList { get; set; }

        public SelectList IntervalList { get; set; }

        public TotalInfo ClaimsTotal { get; set; }

        public TotalItem ClaimsLost { get; set; }
    }
   
    public class Notification
    {
        public int Level { get; set; }

        public string Message { get; set; }
    }
}