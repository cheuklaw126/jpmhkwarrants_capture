using System.Web;
using System.Web.Mvc;

namespace jpmhkwarrants_capture
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
