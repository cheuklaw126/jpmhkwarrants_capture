using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using jpmhkwarrants_capture.Models;

namespace jpmhkwarrants_capture
{
    public class SetupsController : Controller
    {
        private StockEntities1 db = new StockEntities1();

        // GET: Setups
        public ActionResult Index()
        {
            return View(db.Setups.ToList());
        }

        // GET: Setups/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Setup setup = db.Setups.Find(id);
            if (setup == null)
            {
                return HttpNotFound();
            }
            return View(setup);
        }

        // GET: Setups/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Setups/Create
        // 若要免於過量張貼攻擊，請啟用想要繫結的特定屬性，如需
        // 詳細資訊，請參閱 https://go.microsoft.com/fwlink/?LinkId=317598。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "id,UpdatePerSec")] Setup setup)
        {
            if (ModelState.IsValid)
            {
                db.Setups.Add(setup);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(setup);
        }

        // GET: Setups/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Setup setup = db.Setups.Find(1);
            if (setup == null)
            {
                return HttpNotFound();
            }
            return View(setup);
        }

        // POST: Setups/Edit/5
        // 若要免於過量張貼攻擊，請啟用想要繫結的特定屬性，如需
        // 詳細資訊，請參閱 https://go.microsoft.com/fwlink/?LinkId=317598。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit( Setup setup)
        {
            if (ModelState.IsValid)
            {
                if (setup.UpdatePerSec <= 5)
                    setup.UpdatePerSec = 5;

                db.Entry(setup).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("index","home");
            }
            return View(setup);
        }

        // GET: Setups/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Setup setup = db.Setups.Find(id);
            if (setup == null)
            {
                return HttpNotFound();
            }
            return View(setup);
        }

        // POST: Setups/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Setup setup = db.Setups.Find(id);
            db.Setups.Remove(setup);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
