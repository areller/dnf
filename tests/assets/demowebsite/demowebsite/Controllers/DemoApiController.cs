using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace demowebsite.Controllers
{
    public class DemoApiController : Controller
    {
        public ActionResult Index()
        {
            return Json(new
            {
                message = "Hello",
                siteName = System.Web.Hosting.HostingEnvironment.ApplicationHost.GetSiteName()
            }, JsonRequestBehavior.AllowGet);
        }
    }
}