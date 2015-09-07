﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using HelpOut.Models;
using Microsoft.AspNet.Identity;

namespace HelpOut.Controllers
{
    public class EventController : Controller
    {
        private ApplicationDbContext db2 = new ApplicationDbContext();

        // GET: Event

        public ActionResult Index(string sortOrder, string searchString)
        {
            var events = from e in db2.Events
                         select new EventDTO()
                         {
                             EventID = e.EventID,
                             Name = e.Name,
                             DateTime = e.DateTime,
                             Address = e.Address,
                             City = e.City,
                             State = e.State,
                             Country = e.Country,
                             ZipCode = e.ZipCode,
                             Description = e.Description,
                             OrganizationName = e.Organization.FullName
                         };

            ViewBag.DateSortParm = String.IsNullOrEmpty(sortOrder) ? "Date" : "";
            ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";

            if (!String.IsNullOrEmpty(searchString))
            {
                events = events.Where(e => e.Name.ToUpper().Contains(searchString.ToUpper())
                                       || e.Address.ToUpper().Contains(searchString.ToUpper())
                                       || e.City.ToUpper().Contains(searchString.ToUpper())
                                       || e.State.ToUpper().Contains(searchString.ToUpper())
                                       || e.ZipCode.Contains(searchString)
                                       || e.Country.ToUpper().Contains(searchString.ToUpper())
                                       || e.OrganizationName.ToUpper().Contains(searchString.ToUpper()));
            }


            switch (sortOrder)
            {
                case "Date":
                    events = events.OrderBy(e => e.DateTime);
                    break;
                case "date_desc":
                    events = events.OrderByDescending(e => e.DateTime);
                    break;
                default:
                    events = events.OrderBy(e => e.DateTime);
                    break;
            }
           return View(events.ToList());
        }

        // GET: Event/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            
            var @event = (from e in db2.Events
                          where e.EventID == id
                          select e).Include("Organization").Include("Attendees").Single();

            if (@event == null)
            {
                return HttpNotFound();
            }

            string userID = User.Identity.GetUserId();
            var user = (from u in db2.Users
                        where u.Id == userID
                        select u).SingleOrDefault();

            if (user == null || !@event.Attendees.Contains(user))
                ViewBag.Attending = false;
            else
                ViewBag.Attending = true;
                    
            ViewBag.rsvpText = "";
            return View(@event);
        }

        
        [HttpPost, ActionName("Details")]
        [Authorize(Roles = "Volunteer")]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateSignups(int eventID, string volunteerID)
        {
            ApplicationUser volunteer = (from u in db2.Users
                             where u.Id == volunteerID
                             select u).Include("EventsAttending").Single();


            Event @event = (from e in db2.Events
                            where e.EventID == eventID
                            select e).Include("Attendees").Include("Organization").Single();
            
            //adding to respective lists ==> automatically updates signup table
            volunteer.EventsAttending.Add(@event);
            @event.Attendees.Add(volunteer);

            //changing RSVP btn text to "Attending!"
            ViewBag.Attending = true;
            db2.SaveChanges();

            return View(@event);

        }

        // GET: Event/Create
        [Authorize (Roles="Organization")]
        public ActionResult Create()
        {
            ViewBag.OrganizationID = new SelectList(db2.Users, "Id", "Email");
            return View();
        }

        // POST: Event/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "EventID,Name,DateTime,Address,City,State,ZipCode,Country,Description")] Event @event)
        {
            
            if (ModelState.IsValid)
            {
                @event.OrganizationID = User.Identity.GetUserId();
                db2.Events.Add(@event);
                db2.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.OrganizationID = new SelectList(db2.Users, "Id", "Email", @event.OrganizationID);
            return View(@event);
        }

        // GET: Event/Edit/5
        [Authorize]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Event @event = db2.Events.Find(id);
            if (@event == null)
            {
                return HttpNotFound();
            }
            ViewBag.OrganizationID = new SelectList(db2.Users, "Id", "Email", @event.OrganizationID);
            return View(@event);
        }

        // POST: Event/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "EventID,Name,DateTime,Address,City,State,ZipCode,Country,Description,OrganizationID")] Event @event)
        {
            if (ModelState.IsValid)
            {
                db2.Entry(@event).State = EntityState.Modified;
                db2.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.OrganizationID = new SelectList(db2.Users, "Id", "Email", @event.OrganizationID);
            return View(@event);
        }


        // GET: Event/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Event @event = db2.Events.Find(id);
            if (@event == null)
            {
                return HttpNotFound();
            }
            return View(@event);
        }

        // POST: Event/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Event @event = db2.Events.Find(id);
            db2.Events.Remove(@event);
            db2.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db2.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
