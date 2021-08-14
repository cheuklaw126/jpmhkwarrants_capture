using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using jpmhkwarrants_capture.Models;
using System.Diagnostics;

namespace jpmhkwarrants_capture.Controllers
{
    [HandleError]
    public class HomeController : AsyncController
    {
        private StockEntities1 db
        {
            get
            {
                if (_db == null)
                    _db = new StockEntities1();

                return this._db;
            }
        }
        private StockEntities1 _db;

        public ActionResult Index()
        {

            //PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Privileged Time", "_Total");
            //PerformanceCounter ramCounter    = new PerformanceCounter("Process", "Working Set", "_Total");

            //cpuCounter.NextValue();
            //ramCounter.NextValue();
            //System.Threading.Thread.Sleep(1000);
            //var cpu = Convert.ToInt32(cpuCounter.NextValue());
            //var ram = Convert.ToInt32(ramCounter.NextValue());

            //ViewData["cpu"] = cpu;
            //ViewData["ram"] = ram;


            //poolHandle();

            ViewData["time"] = db.Setups.FirstOrDefault().UpdatePerSec;

            var list = db.GetConnection.ToList();
            ViewData["list"] = list == null ? 0 : list.Count;

            //if (list.Count >= 3)
            //{
            //    return RedirectToAction("About");
            //}


            return View();
        }

        public void poolHandle()
        {
            string key = Request?.UserHostAddress ?? "local";
            var cur = db.pool.Where(a => a.key == key).FirstOrDefault();
            if (cur == null)
            {
                db.pool.Add(new pool() { key = key, datetime = DateTime.Now });
            }
            else
            {
                cur.datetime = DateTime.Now;
            }

            var dt = DateTime.Now.AddMinutes(-1);
            var remove = db.pool.Where(a => a.datetime < dt);

            db.pool.RemoveRange(remove);
            db.SaveChanges();



        }


        public string GetActUser()
        {
            var list = db.GetConnection.ToList();


            return $@"最大使用人數：{list.Count} /5 人";
        }

        public bool isNew(string newProduct, int currentBatch, int userBatchid)
        {

            if (currentBatch == userBatchid)
                return false;

            var lastBatch = db.JPMorganSyncs.Where(x => x.id != currentBatch).ToList().LastOrDefault();
            if (lastBatch == null)
            {
                return true;
            }




            return db.JPMorgans.Where(x => x.BatchID == lastBatch.id && x.Name == newProduct ).FirstOrDefault() == null;


        }

        public async Task<JObject> GetData(string userBatch)
        {
            if (string.IsNullOrEmpty(userBatch))
                userBatch = "0";
            int userBathid = Convert.ToInt32(userBatch);
            //poolHandle();
            var obj = new JObject();
            try
            {
                var pareTime = DateTime.Now.AddSeconds(-10);

                var batch = db.JPMorganSyncs.ToList().LastOrDefault();

                //avoid every user call jpm
                if (batch != null && batch.LastSync >= pareTime || DateTime.Now.Hour > 16 || DateTime.Now.Hour < 8)
                {
                    var list = db.JPMorgans.Where(x => x.BatchID == batch.id).ToList();

                    JArray arr = new JArray();



                    foreach (var item in list)
                    {

                        var tmp = new JObject();
                        tmp["Name"] = item.Name;
                        tmp["Value"] = item.Value == 0 ? "-" : item.Value.ToString();
                        tmp["IsNew"] = this.isNew(item.Name, batch.id, userBathid) ? 1 : 0;
                        tmp["LastSync"] = item.LastSync.ToString("yyyy-MM-dd HH:mm:ss");
                        arr.Add(tmp);


                    }

                    obj["Data"] = arr;
                    obj["BatchID"] = batch.id;
                    return obj;

                }



                WebClient client = new WebClient();
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                MemoryStream ms = new MemoryStream(client.DownloadData(@"https://www.jpmhkwarrants.com/zh_hk"));

                HtmlDocument doc = new HtmlDocument();
                doc.Load(ms, Encoding.UTF8);

                //HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("/html/body/div[1]/div[7]");

                HtmlDocument docStockContext = new HtmlDocument();

                docStockContext.LoadHtml(doc.DocumentNode.SelectSingleNode("//div[@class='os_over_box jp_pick']").InnerHtml);
                var SyncTimeText = docStockContext.DocumentNode.SelectSingleNode("//div[@class='timer']").InnerText;

                var lastSync = DateTime.ParseExact(SyncTimeText.Replace("最後更新時間:", ""), "yyyy-MM-dd HH:mm", null);

                var table = docStockContext.DocumentNode.SelectSingleNode("//table");
                var cells = table.SelectNodes(@"//tbody/tr/td");

                batch = new JPMorganSync { LastSync = DateTime.Now };
                db.JPMorganSyncs.Add(batch);
                await db.SaveChangesAsync();

                obj["BatchID"] = batch.id;
                JArray objs = new JArray();
                DateTime curr = DateTime.Now;
                int idx = 1;
                string key = "";
                foreach (var item in cells)
                {

                    if (idx % 2 == 0)
                    {
                        var tmp = new JObject();
                        tmp["Name"] = key;
                        tmp["Value"] = item.InnerText;
                        tmp["IsNew"] = this.isNew(key, batch.id, userBathid) ? 1 : 0;
                        tmp["LastSync"] = lastSync.ToString("yyyy-MM-dd HH:mm:ss");
                        objs.Add(tmp);
                        decimal tmpVal = 0;
                        decimal.TryParse(item.InnerText, out tmpVal);


                        db.JPMorgans.Add(new JPMorgan { Name = tmp["Name"].ToString(), Value = tmpVal, LastSync = lastSync, BatchID = batch.id });
                    }
                    else
                    {
                        key = item.InnerHtml;
                    }

                    idx++;
                }

                obj["Data"] = objs;


                await db.SaveChangesAsync();


            }
            catch (Exception ex)
            {

            }


            return obj;
        }
        //blic async Task<JObject> GetData()
        //{
        //    IWebDriver driver;
        //    var driverService = ChromeDriverService.CreateDefaultService();
        //    driverService.HideCommandPromptWindow = true;
        //    var options = new ChromeOptions();
        //    options.AddArguments(new List<string> { { "start-maximized" } });
        //    options.AddArgument("no-sandbox");
        //    //options.AddArgument("disable-dev-shm-usage");
        //    //options.AddArgument("headless");
        //    driver = new ChromeDriver(driverService, options, TimeSpan.FromMinutes(60));

        //    driver.Manage().Timeouts().PageLoad.Add(System.TimeSpan.FromSeconds(300));
        //    IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
        //    driver.Navigate().GoToUrl("https://www.jpmhkwarrants.com/zh_hk");
        //    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(300);
        //    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(300));
        //    wait.Until(ExpectedConditions.ElementExists(By.ClassName("jp_pick")));




        //    return new JObject();
        //}

        public ActionResult About()
        {
            var list = (List<SessionObj>)HttpContext.Application["Test"];
            ViewBag.Message = $@"Connection limit, please close some browser and try again.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}