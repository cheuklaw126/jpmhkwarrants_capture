using jpmhkwarrants_capture.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace jpmhkwarrants_capture
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            Application["OnlineUsers"] = 0;
            Application["Test"] = new List<SessionObj>();
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);






        }

        void Session_Start(object sender, EventArgs e)
        {
            //DropDead();
            //啟動新工作階段時執行的程式碼
            if (Session.IsNewSession)
            {
                string sessionId = HttpContext.Current?.Request?.UserHostAddress ?? "local";


                Application.Lock();
                var list = (List<SessionObj>)Application["Test"];

                var Ses = list.Where(x => x.key == sessionId).FirstOrDefault();
                if (Ses==null)
                {
                    list.Add(new SessionObj { key = sessionId,lastAct= DateTime.Now});
                }
                else
                {
                    Ses.lastAct = DateTime.Now;
                }


                Application["OnlineUsers"] = (int)Application["OnlineUsers"] + 1;
                Application.UnLock();
            }
        }

        void Session_End(object sender, EventArgs e)
        {
            //工作階段結束時執行的程式碼。
            //注意: 只有在 Web.config 檔將 sessionstate 模式設定為 InProc 時，
            //才會引發 Session_End 事件。如果將工作階段模式設定為 StateServer
            //或 SQLServer，就不會引發這個事件。
            Application.Lock();

            var list = (List<SessionObj>)Application["Test"];
            string sessionId = HttpContext.Current?.Request?.UserHostAddress??"local";
            DropDead(sessionId);
            Application["OnlineUsers"] = (int)Application["OnlineUsers"] - 1;
            Application.UnLock();
        }

        public void DropDead(string key)
        {
            Application.Lock();
            var list = (List<SessionObj>)Application["Test"];

            list.RemoveAll(x => x.key == key);

            Application.UnLock();

            DropDead();

        }
        public void DropDead()
        {
            Application.Lock();
            var list = (List<SessionObj>)Application["Test"];
            DateTime pare = DateTime.Now.AddMinutes(-1);
            list.RemoveAll(x => x.lastAct < pare);
            Application.UnLock();
        }
        protected void Application_End()//当回收IIS池或更改bin文件夹或web.config文件时，将触发该事件。您应该更改默认的IIS设置，以便在非高峰时段每天安排一次回收。
        {

        }
 
    }
}
