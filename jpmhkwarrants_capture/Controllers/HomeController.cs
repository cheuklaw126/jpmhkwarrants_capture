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

        public bool isHighLight(string newProduct, int currentBatchID, int screenBatchID)
        {
            if (currentBatchID == screenBatchID)
                return true;

            var lastBatch = db.JPMorganSyncs.Where(x => x.id != currentBatchID).ToList().LastOrDefault();
            if (lastBatch == null)
            {
                return true;
            }
            return db.JPMorgans.Where(x => x.BatchID == lastBatch.id && x.Name == newProduct).FirstOrDefault() == null;
        }


        public bool isListChange(string newProduct, int currentBatchID, int screenBatchID)
        {
            if (currentBatchID == screenBatchID)
                return false;

            var currentItems = db.JPMorgans.Where(x => x.BatchID == currentBatchID && x.Name == newProduct).ToList();
            var screenItems = db.JPMorgans.Where(x => x.BatchID == screenBatchID && x.Name == newProduct).ToList();

            if (screenItems.Count == 0)
                return true;

            return string.Join("", currentItems.Select(x => x.Name)) != string.Join("", screenItems.Select(x => x.Name));
        }

        public JObject GetCompare(string newProduct, int currentBatchID, int screenBatchID)
        {
            var jobj = new JObject();

            bool isListChange = false;
            bool isHighLight = true;
            var currentItems = db.JPMorgans.Where(x => x.BatchID == currentBatchID).ToList();
            var screenItems = db.JPMorgans.Where(x => x.BatchID == screenBatchID).ToList();


            if (currentBatchID != screenBatchID)
            {
                //   return false;1
                if (screenItems.Count == 0)
                    isListChange = true;
                else
                {
                    isListChange = string.Join("", currentItems.OrderBy(x => x.Name).Select(x => x.Name)) != string.Join("", screenItems.OrderBy(x => x.Name).Select(x => x.Name));
                    if (!isListChange)
                    {

                    }
                    else
                    {


                    }

                }
            }

            var ScreenItemObj = screenItems.Where(x => x.Name == newProduct).FirstOrDefault();
            var currentItemObj = currentItems.Where(x => x.Name == newProduct).FirstOrDefault();
            if (isListChange && ScreenItemObj != null)
            {
                isHighLight = false;
            }
            decimal difference = 0;

            if (ScreenItemObj != null && ScreenItemObj.Value != 0)
            {
                difference = currentItemObj.Value - ScreenItemObj.Value;


            }


            jobj["isNew"] = ScreenItemObj == null;

            jobj["isListChange"] = isListChange;
            jobj["isHighLight"] = isHighLight;
            jobj["OldValue"] = screenItems.Where(x => x.Name == newProduct).FirstOrDefault()?.Value ?? 0;
            jobj["Difference"] = difference;
            jobj["OldLastSync"] = screenItems.Where(x => x.Name == newProduct).FirstOrDefault()?.LastSync.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
            //jobj["isHighLight"] = !isListChange


            return jobj;
        }


        public async Task<JObject> GetCompareNew(string newProduct, int currentBatchID, int screenBatchID)
        {
            var jobj = new JObject();

            var currentItems = db.JPMorgans.Where(x => x.BatchID == currentBatchID).ToList();
            var screenItems = db.JPMorgans.Where(x => x.BatchID == screenBatchID).ToList();

            bool isNew = false;
            bool isListChange = string.Join("", currentItems.OrderBy(x => x.Name).Select(x => x.Name)) != string.Join("", screenItems.OrderBy(x => x.Name).Select(x => x.Name));
            string listString = string.Join("", currentItems.OrderBy(x => x.Name).Select(x => x.Name));

            foreach (var item in db.JPMorganSyncs.OrderByDescending(x => x.id == currentBatchID - 1).ThenByDescending(c => c.id))
            {
                //if (item.id == currentBatchID || item.id == screenBatchID)
                //    continue;
                var oldItems = db.JPMorgans.Where(x => x.BatchID == item.id).ToList();

                string oldListString = string.Join("", oldItems.OrderBy(x => x.Name).Select(x => x.Name));

                if (listString != oldListString)
                {
                    if (currentItems.OrderBy(x => x.Name).Select(x => x.Name).Except(
                        oldItems.OrderBy(x => x.Name).Select(x => x.Name)

                        ).ToList().Count == 0)
                    {
                        isListChange = false;
                        jobj["ExceptList"] = JArray.FromObject(oldItems.OrderBy(x => x.Name).Select(x => x.Name).Except(
                     currentItems.OrderBy(x => x.Name).Select(x => x.Name)
                     ).ToList());
                        continue;
                    }
                   
                    //  isListChange=true;



                    jobj["oldBatchId"] = item.id;
                    //found the last difference batch items
                    if (oldItems.Where(x => x.Name == newProduct).ToList().Count == 0)
                    {
                        isNew = true;
                        break;
                    }
                    else
                    {
                        break;

                    }
                }

            }



            jobj["isNew"] = isNew;
            jobj["isListChange"] = isListChange;




            return jobj;
        }


        public async Task<JObject> GetData(string strScreenBatchID)
        {
            if (string.IsNullOrEmpty(strScreenBatchID))
                strScreenBatchID = "0";
            int ScreenBatchID = Convert.ToInt32(strScreenBatchID);
            //poolHandle();
            var obj = new JObject();
            try
            {
                var pareTime = DateTime.Now.AddSeconds(-10);

                var latestBatch = db.JPMorganSyncs.ToList().LastOrDefault();

                //avoid every user call jpm
                if (latestBatch != null && latestBatch.LastSync >= pareTime || DateTime.Now.Hour > 16 || DateTime.Now.Hour < 8)
                {
                    var latestBatchBdy = db.JPMorgans.Where(x => x.BatchID == latestBatch.id).ToList();

                    JArray arr = new JArray();



                    foreach (var bdy in latestBatchBdy)
                    {

                        var tmp = new JObject();

                        var comObj = await this.GetCompareNew(bdy.Name, latestBatch.id, ScreenBatchID);

                        tmp["Name"] = bdy.Name.Replace("<a href=\"", "<a href=\"https://www.jpmhkwarrants.com").Replace("\">", "\" target='_blank' >");
                        tmp["Value"] = bdy.Value == 0 ? "-" : bdy.Value.ToString();
                        tmp["isHighLight"] = comObj["isNew"];
                        tmp["isListChange"] = comObj["isListChange"];
                        tmp["oldBatchId"] = comObj["oldBatchId"];
                        //tmp["OldLastSync"] = comObj["OldLastSync"];
                        //tmp["Difference"] = comObj["Difference"];
                        tmp["ExceptList"] = comObj["ExceptList"];
                        tmp["LastSync"] = bdy.LastSync.ToString("yyyy-MM-dd HH:mm:ss");
                        arr.Add(tmp);
                        

                    }
                    obj["SyncTime"] = latestBatch.LastSync;
                    obj["Data"] = arr;
                    obj["BatchID"] = latestBatch.id;
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

                latestBatch = new JPMorganSync { LastSync = DateTime.Now };
                db.JPMorganSyncs.Add(latestBatch);
                await db.SaveChangesAsync();

                obj["BatchID"] = latestBatch.id;
                obj["SyncTime"] = latestBatch.LastSync;

                DateTime curr = DateTime.Now;
                int idx = 1;
                string ProductName = "";
                foreach (var item in cells)
                {

                    if (idx % 2 == 0)
                    {
                        decimal tmpVal = 0;
                        decimal.TryParse(item.InnerText, out tmpVal);
                        db.JPMorgans.Add(new JPMorgan { Name = ProductName, Value = tmpVal, LastSync = lastSync, BatchID = latestBatch.id });


                    }
                    else
                    {
                        ProductName = item.InnerHtml;
                    }

                    idx++;
                }
                await db.SaveChangesAsync();
                var curBatchBdy = db.JPMorgans.Where(x => x.BatchID == latestBatch.id).ToList();
                JArray objs = new JArray();

                foreach (var item in curBatchBdy)
                {
                    var comObj = await this.GetCompareNew(item.Name, latestBatch.id, ScreenBatchID);
                    var tmp = new JObject();
                    tmp["Name"] = item.Name.Replace("<a href=\"", "<a href=\"https://www.jpmhkwarrants.com").Replace("\">", "\" target='_blank' >");
                    tmp["Value"] = item.Value;
                    tmp["isHighLight"] = comObj["isNew"];
                    tmp["isListChange"] = comObj["isListChange"];
                    tmp["oldBatchId"] = comObj["oldBatchId"];
                    //tmp["OldLastSync"] = comObj["OldLastSync"];
                    //tmp["Difference"] = comObj["Difference"];
                    tmp["LastSync"] = lastSync.ToString("yyyy-MM-dd HH:mm:ss");
                    tmp["ExceptList"] = comObj["ExceptList"];
                    objs.Add(tmp);
                  
                }

                obj["Data"] = objs;

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