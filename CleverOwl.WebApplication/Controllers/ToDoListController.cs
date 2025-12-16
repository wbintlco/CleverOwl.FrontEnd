using CleverOwl.WebApplication.Models;
using RLI.Common.Managers;
using RLI.Common.DataObjects;
using RLI.EntityFramework.EDM;
using RLI.WebApplication.Objects;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Assignment = RLI.EntityFramework.EDM.Assignment;
using Microsoft.Ajax.Utilities;

namespace CleverOwl.WebApplication.Controllers
{
    [Authorize]
    public class ToDoListController : BaseController
    {
        private RLIEntities db1 = new RLIEntities();
        const int DEFAULT_LOAD_COUNT = 10;
        // GET: ToDoList
        public async Task<ActionResult> Index()
        {
            List<RLI.EntityFramework.EDM.SchoolSubjectTeacherGrade> gradesSstg = new List<RLI.EntityFramework.EDM.SchoolSubjectTeacherGrade>();
            if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Teacher")
            {
                 gradesSstg = await db1.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey && sstg.GradeKey != null).ToListAsync();
                ViewBag.SSTGKey = gradesSstg.FirstOrDefault().SchoolSubjectTeacherGradeKey;
                ViewBag.Grades = new SelectList(gradesSstg.Distinct().Select(g => new
                {
                    GradeKey = g.SchoolSubjectTeacherGradeKey,
                    DefaultGrade1 = g.Grade.Grade1,
                    Grade1 = (CurrentLanguageIdentifier == 0 ? g.Grade.Grade1 : g.Grade.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault() == null ? g.Grade.Grade1 : CurrentLanguageIdentifier == 0 ? g.Grade.Grade1 : g.Grade.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value) + "-" + (CurrentLanguageIdentifier == 0 ? g.Subject.Subject1 : g.Subject.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value),
                    GradeIndex = g.Grade.GradeIndex,
                    LocalGradeGUID = g.Grade.LocalGradeGUID
                }).OrderBy(gr => gr.GradeIndex), "GradeKey", "Grade1", ViewBag.SSTGKey);

            }



            ToDoViewModel model = new ToDoViewModel();

            model.TodosDate = DateTime.Now;
            string date = DateTime.Now.ToString("dddd, dd MMMM yyyy");
            DateTime dateParsed = DateTime.Parse(date);

            ViewBag.dateToShare = date;

            List<Assignment> assignments = null;
            List<AssignmentToDoViewModel> assignmentsToSend = new List<AssignmentToDoViewModel>();
            AssignmentToDoViewModel assign = null;
            if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Student")
            {
                var currentStudent = await db1.AspNetUsers.Where(a => a.Id == CurrentUser.Id).ToListAsync();
                var student = currentStudent.FirstOrDefault().Student;
                var studentAssTimeline = await db1.Timelines.Where(ut => ut.UserKey == CurrentUser.Id).Select(ass => ass.TimeLineEntityKey).ToListAsync();
               
                var remoteId = currentStudent.FirstOrDefault().RemoteAuthentications.Where(r => r.ESystem.ESystemName == "Moodle"&&r.Userkey== CurrentUser.Id).FirstOrDefault().EsystemRemoteId;

                assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGrade.SchoolKey == student.SchoolKey && a.SchoolSubjectTeacherGrade.GradeKey == student.GradeKey && studentAssTimeline.Contains(a.AssignmentKey) && a.DueDate == dateParsed).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();

                // check if assignment is submitted
                foreach (var assignment in assignments)
                {
                    SubmissionDetailsViewModel s = await SubmissionsManager.GetAssignSubmissionDetails(assignment.AssignmentId.ToString(), remoteId, HttpContext);
                    assign = new AssignmentToDoViewModel();
                    assign.AssignmentId = assignment.AssignmentId;
                    assign.DueDate = assignment.DueDate;
                    assign.Title = assignment.AssignmentTitle;
                    assign.AssignmentKey = assignment.AssignmentKey;
                    assign.GradeName = assignment.SchoolSubjectTeacherGrade.Grade.Grade1;
                    assign.SubjectName = assignment.SchoolSubjectTeacherGrade.Subject.Subject1;

                    if (s != null && s.SubmissionStatus.Equals("submitted"))
                    {
                        assign.submissionStatus = "submitted";
                    }
                    else
                    {
                        assign.submissionStatus = "new";
                    }
                    assignmentsToSend.Add(assign);
                }

                List<SynchronousSession> sessions = await db1.SynchronousSessions.Where(s => s.SessionDate.Year == dateParsed.Year && s.SessionDate.Month == dateParsed.Month && s.SessionDate.Day == dateParsed.Day && s.SchoolSubjectTeacherGradeKey == student.GradeKey).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                model.Assignments = assignmentsToSend;
                model.Sessions = sessions;
                return View(model);
            }
            else
            {
                assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGrade.TeacherKey == CurrentUser.TeacherKey && a.DueDate == dateParsed).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();
                foreach (var assignment in assignments)
                {
                    assign = new AssignmentToDoViewModel();
                    assign.AssignmentId = assignment.AssignmentId;
                    assign.Title = assignment.AssignmentTitle;
                    assign.DueDate = assignment.DueDate;
                    assign.GradeName = assignment.SchoolSubjectTeacherGrade.Grade.Grade1;
                    assign.SubjectName = assignment.SchoolSubjectTeacherGrade.Subject.Subject1;
                    assignmentsToSend.Add(assign);
                }

                List<SynchronousSession> sessions = await db1.SynchronousSessions.Where(s => s.CreatedByUserKey == CurrentUser.Id && s.SessionDate.Month == dateParsed.Month && s.SessionDate.Day == dateParsed.Day && s.SessionDate.Year == dateParsed.Year).Take(DEFAULT_LOAD_COUNT).ToListAsync();

                foreach (var assig in assignments)
                {
                    if (model.grades != null)
                    {
                        if (!model.grades.Contains(assig.SchoolSubjectTeacherGrade.Grade))
                        {
                            model.grades.Add(assig.SchoolSubjectTeacherGrade.Grade);
                        }
                    }
                    else
                    {
                        model.grades = new List<RLI.EntityFramework.EDM.Grade>();
                        model.grades.Add(assig.SchoolSubjectTeacherGrade.Grade);
                    }
                }

                foreach (var sess in sessions)
                {
                    if (model.grades != null)
                    {
                        if (!model.grades.Contains(sess.SchoolSubjectTeacherGrade.Grade))
                        {
                            model.grades.Add(sess.SchoolSubjectTeacherGrade.Grade);
                        }
                    }
                    else
                    {
                        model.grades = new List<RLI.EntityFramework.EDM.Grade>();
                        model.grades.Add(sess.SchoolSubjectTeacherGrade.Grade);
                    }
                }



                model.Assignments = assignmentsToSend;
                model.Sessions = sessions;
                return View(model);
            }


        }

        [AllowAnonymous]
        public async Task<ActionResult> View(string id,string dateShared)
        {
            string date = string.Empty;
            ToDoViewModel model = new ToDoViewModel();
            DateTime dateParsed = new DateTime();
            if (dateShared != null)
            {
                //date = dateShared.ToString("dddd, dd MMMM yyyy");
                dateParsed = DateTime.Parse(dateShared);
                model.TodosDate = dateParsed;
            }
            else
            {
                model.TodosDate = DateTime.Now;
                date = DateTime.Now.ToString("dddd, dd MMMM yyyy");
                 dateParsed = DateTime.Parse(date);
            }

            int decodedId = int.Parse(RLI.Common.Managers.UtilitiesManager.Base64Decode(id));
            ViewBag.SSTGKey = decodedId;
            ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("ToDoList", "Index", CurrentLanguageIdentifier);
            ViewBag.dateShared = dateShared;
            //string date = DateTime.Now.ToString("dddd, dd MMMM yyyy");
            //DateTime dateParsed = DateTime.Parse(date);

            List<Assignment> assignments = null;
            List<AssignmentToDoViewModel> assignmentsToSend = new List<AssignmentToDoViewModel>();
            AssignmentToDoViewModel assign = null;

                assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGradeKey == decodedId && a.DueDate == dateParsed).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();
                // check if assignment is submitted
                foreach (var assignment in assignments)
                {
                    //SubmissionDetailsViewModel s = await SubmissionsManager.GetAssignSubmissionDetails(assignment.AssignmentId.ToString(), remoteId, HttpContext);
                    assign = new AssignmentToDoViewModel();
                    assign.AssignmentId = assignment.AssignmentId;
                    assign.DueDate = assignment.DueDate;
                    assign.Title = assignment.AssignmentTitle;
                    assign.AssignmentKey = assignment.AssignmentKey;
                    assign.GradeName = assignment.SchoolSubjectTeacherGrade.Grade.Grade1;
                    assign.SubjectName = assignment.SchoolSubjectTeacherGrade.Subject.Subject1;
                    assign.submissionStatus = "";
                    assignmentsToSend.Add(assign);
                }

                List<SynchronousSession> sessions = await db1.SynchronousSessions.Where(s => s.SchoolSubjectTeacherGradeKey == decodedId && s.SessionDate.Year == dateParsed.Year && s.SessionDate.Month == dateParsed.Month && s.SessionDate.Day == dateParsed.Day).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                model.Assignments = assignmentsToSend;
                model.Sessions = sessions;

            return View("View", model);


        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> Search(string query, string date, string gradeKey)
        {
            ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("ToDoList", "Index", CurrentLanguageIdentifier);

            ToDoViewModel model = new ToDoViewModel();
            List<Assignment> assignments = null;
            List<SynchronousSession> sessions = null;
            List<AssignmentToDoViewModel> assignmentsToSend = new List<AssignmentToDoViewModel>();
            AssignmentToDoViewModel assign = null;

            DateTime dateToSearchFrom = DateTime.Parse(date);

            if (query.Length == 0)
            {
                
                if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Student")
                {


                    var currentStudent = await db1.AspNetUsers.Where(a => a.Id == CurrentUser.Id).ToListAsync();
                    var student = currentStudent.FirstOrDefault().Student;
                    var studentAssTimeline = await db1.Timelines.Where(ut => ut.UserKey == CurrentUser.Id).Select(ass => ass.TimeLineEntityKey).ToListAsync();
                    var remoteId = currentStudent.FirstOrDefault().RemoteAuthentications.Where(r => r.ESystem.ESystemName == "Moodle"&&r.Userkey==CurrentUser.Id).FirstOrDefault().EsystemRemoteId;

                    assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGrade.SchoolKey == student.SchoolKey && a.SchoolSubjectTeacherGrade.GradeKey == student.GradeKey && studentAssTimeline.Contains(a.AssignmentKey) && a.DueDate == dateToSearchFrom).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();

                    // check if assignment is submitted
                    foreach (var assignment in assignments)
                    {
                        SubmissionDetailsViewModel s = await SubmissionsManager.GetAssignSubmissionDetails(assignment.AssignmentId.ToString(), remoteId, HttpContext);
                        assign = new AssignmentToDoViewModel();
                        assign.AssignmentId = assignment.AssignmentId;
                        assign.DueDate = assignment.DueDate;
                        assign.Title = assignment.AssignmentTitle;
                        assign.AssignmentKey = assignment.AssignmentKey;
                        assign.GradeName = assignment.SchoolSubjectTeacherGrade.Grade.Grade1;
                        assign.SubjectName = assignment.SchoolSubjectTeacherGrade.Subject.Subject1;

                        if (s != null && s.SubmissionStatus.Equals("submitted"))
                        {
                            assign.submissionStatus = "submitted";
                        }
                        else
                        {
                            assign.submissionStatus = "new";
                        }
                        assignmentsToSend.Add(assign);
                    }

                    sessions = await db1.SynchronousSessions.Where(s => s.SessionDate.Year == dateToSearchFrom.Year && s.SessionDate.Month == dateToSearchFrom.Month && s.SessionDate.Day == dateToSearchFrom.Day && s.SchoolSubjectTeacherGrade.GradeKey == student.GradeKey).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                    model.Assignments = assignmentsToSend;
                    model.Sessions = sessions;
                    return PartialView("_ToDoItem", model);


                }

                else
                {
                    if (gradeKey == "-1")
                    {
                        assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGrade.TeacherKey == CurrentUser.TeacherKey && a.DueDate == dateToSearchFrom).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();
                        sessions = await db1.SynchronousSessions.Where(s => s.CreatedByUserKey == CurrentUser.Id && s.SessionDate.Year == dateToSearchFrom.Year && s.SessionDate.Month == dateToSearchFrom.Month && s.SessionDate.Day == dateToSearchFrom.Day).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                    }
                    else
                    {
                        assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGrade.TeacherKey == CurrentUser.TeacherKey && a.DueDate == dateToSearchFrom && a.SchoolSubjectTeacherGrade.GradeKey.ToString() == gradeKey).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();
                        sessions = await db1.SynchronousSessions.Where(s => s.CreatedByUserKey == CurrentUser.Id && s.SessionDate.Year == dateToSearchFrom.Year && s.SessionDate.Month == dateToSearchFrom.Month && s.SessionDate.Day == dateToSearchFrom.Day && s.SchoolSubjectTeacherGrade.GradeKey.ToString() == gradeKey).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                    }

                    foreach (var assignment in assignments)
                    {

                        assign = new AssignmentToDoViewModel();
                        assign.AssignmentId = assignment.AssignmentId;
                        assign.Title = assignment.AssignmentTitle;
                        assign.DueDate = assignment.DueDate;
                        assign.GradeName = assignment.SchoolSubjectTeacherGrade.Grade.Grade1;

                        assign.SubjectName = assignment.SchoolSubjectTeacherGrade.Subject.Subject1;
                        assignmentsToSend.Add(assign);
                    }

                    model.Assignments = assignmentsToSend;
                    model.Sessions = sessions;
                    return PartialView("_ToDoItem", model);
                }

            }

            //if user entered a text
            else
            {
                if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Student")
                {


                    var currentStudent = await db1.AspNetUsers.Where(a => a.Id == CurrentUser.Id).ToListAsync();
                    var student = currentStudent.FirstOrDefault().Student;
                    var studentAssTimeline = await db1.Timelines.Where(ut => ut.UserKey == CurrentUser.Id).Select(ass => ass.TimeLineEntityKey).ToListAsync();
                    var remoteId = currentStudent.FirstOrDefault().RemoteAuthentications.Where(r => r.ESystem.ESystemName == "Moodle"&& r.Userkey==CurrentUser.Id).FirstOrDefault().EsystemRemoteId;

                    assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGrade.SchoolKey == student.SchoolKey && a.SchoolSubjectTeacherGrade.GradeKey == student.GradeKey && studentAssTimeline.Contains(a.AssignmentKey) && a.AssignmentTitle.ToLower().Contains(query.ToLower()) && a.DueDate == dateToSearchFrom).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();

                    // check if assignment is submitted
                    foreach (var assignment in assignments)
                    {
                        SubmissionDetailsViewModel s = await SubmissionsManager.GetAssignSubmissionDetails(assignment.AssignmentId.ToString(), remoteId, HttpContext);
                        assign = new AssignmentToDoViewModel();
                        assign.AssignmentId = assignment.AssignmentId;
                        assign.DueDate = assignment.DueDate;
                        assign.Title = assignment.AssignmentTitle;
                        assign.AssignmentKey = assignment.AssignmentKey;
                        assign.GradeName = assignment.SchoolSubjectTeacherGrade.Grade.Grade1;
                        assign.SubjectName = assignment.SchoolSubjectTeacherGrade.Subject.Subject1;

                        if (s != null && s.SubmissionStatus.Equals("submitted"))
                        {
                            assign.submissionStatus = "submitted";
                        }
                        else
                        {
                            assign.submissionStatus = "new";
                        }
                        assignmentsToSend.Add(assign);
                    }

                    sessions = await db1.SynchronousSessions.Where(s => s.Topic.ToLower().Contains(query.ToLower()) && s.SessionDate.Year == dateToSearchFrom.Year && s.SessionDate.Month == dateToSearchFrom.Month && s.SessionDate.Day == dateToSearchFrom.Day && s.SchoolSubjectTeacherGrade.GradeKey == student.GradeKey).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                    model.Assignments = assignmentsToSend;
                    model.Sessions = sessions;
                    return PartialView("_ToDoItem", model);


                }

                else
                {
                    if (gradeKey == "-1")
                    {
                        assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGrade.TeacherKey == CurrentUser.TeacherKey && a.AssignmentTitle.ToLower().Contains(query.ToLower()) && a.DueDate == dateToSearchFrom).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                        sessions = await db1.SynchronousSessions.Where(s => s.CreatedByUserKey == CurrentUser.Id && s.SessionDate.Year == dateToSearchFrom.Year && s.SessionDate.Month == dateToSearchFrom.Month && s.SessionDate.Day == dateToSearchFrom.Day).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                    }
                    else
                    {
                        assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGrade.TeacherKey == CurrentUser.TeacherKey && a.AssignmentTitle.ToLower().Contains(query.ToLower()) && a.DueDate == dateToSearchFrom && a.SchoolSubjectTeacherGrade.GradeKey.ToString() == gradeKey).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                        sessions = await db1.SynchronousSessions.Where(s => s.CreatedByUserKey == CurrentUser.Id && s.Topic.ToLower().Contains(query.ToLower()) && s.SessionDate.Year == dateToSearchFrom.Year && s.SessionDate.Month == dateToSearchFrom.Month && s.SessionDate.Day == dateToSearchFrom.Day && s.SchoolSubjectTeacherGrade.GradeKey.ToString() == gradeKey).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                    }
                    foreach (var assignment in assignments)
                    {

                        assign = new AssignmentToDoViewModel();
                        assign.AssignmentId = assignment.AssignmentId;
                        assign.Title = assignment.AssignmentTitle;
                        assign.DueDate = assignment.DueDate;
                        assign.GradeName = assignment.SchoolSubjectTeacherGrade.Grade.Grade1;

                        assign.SubjectName = assignment.SchoolSubjectTeacherGrade.Subject.Subject1;
                        assignmentsToSend.Add(assign);
                    }

                    model.Assignments = assignmentsToSend;
                    model.Sessions = sessions;
                    return PartialView("_ToDoItem", model);
                }

            }

            return Content("404");

        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> SearchView(string query, string date,int sstgKey)
        {
            ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("ToDoList", "Index", CurrentLanguageIdentifier);

            ToDoViewModel model = new ToDoViewModel();
            List<Assignment> assignments = null;
            List<SynchronousSession> sessions = null;
            List<AssignmentToDoViewModel> assignmentsToSend = new List<AssignmentToDoViewModel>();
            AssignmentToDoViewModel assign = null;

            DateTime dateToSearchFrom = DateTime.Parse(date);

            if (query.Length == 0)
            {


               
                    assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGradeKey == sstgKey && a.DueDate == dateToSearchFrom).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();

                    // check if assignment is submitted
                    foreach (var assignment in assignments)
                    {
                        assign = new AssignmentToDoViewModel();
                        assign.AssignmentId = assignment.AssignmentId;
                        assign.DueDate = assignment.DueDate;
                        assign.Title = assignment.AssignmentTitle;
                        assign.AssignmentKey = assignment.AssignmentKey;
                        assign.GradeName = assignment.SchoolSubjectTeacherGrade.Grade.Grade1;
                        assign.SubjectName = assignment.SchoolSubjectTeacherGrade.Subject.Subject1;


                        assignmentsToSend.Add(assign);
                    }

                    sessions = await db1.SynchronousSessions.Where(s => s.SessionDate.Year == dateToSearchFrom.Year && s.SessionDate.Month == dateToSearchFrom.Month && s.SessionDate.Day == dateToSearchFrom.Day && s.SchoolSubjectTeacherGradeKey == sstgKey).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                    model.Assignments = assignmentsToSend;
                    model.Sessions = sessions;
                    return PartialView("_ToDoItemTest", model);



           



            }

            else
            {
                  assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGradeKey == sstgKey && a.AssignmentTitle.ToLower().Contains(query.ToLower()) && a.DueDate == dateToSearchFrom).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();

                    // check if assignment is submitted
                    foreach (var assignment in assignments)
                    {
                        assign = new AssignmentToDoViewModel();
                        assign.AssignmentId = assignment.AssignmentId;
                        assign.DueDate = assignment.DueDate;
                        assign.Title = assignment.AssignmentTitle;
                        assign.AssignmentKey = assignment.AssignmentKey;
                        assign.GradeName = assignment.SchoolSubjectTeacherGrade.Grade.Grade1;
                        assign.SubjectName = assignment.SchoolSubjectTeacherGrade.Subject.Subject1;


                        assignmentsToSend.Add(assign);
                    }

                    sessions = await db1.SynchronousSessions.Where(s => s.Topic.ToLower().Contains(query.ToLower()) && s.SchoolSubjectTeacherGradeKey == sstgKey && s.SessionDate.Year == dateToSearchFrom.Year && s.SessionDate.Month == dateToSearchFrom.Month && s.SessionDate.Day == dateToSearchFrom.Day).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                    model.Assignments = assignmentsToSend;
                    model.Sessions = sessions;
                    return PartialView("_ToDoItemTest", model);
                
            }

            return Content("404");
                }

 

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> DateManipulation(string optionNeeded, string date, string gradeKey)
        {
            ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("ToDoList", "Index", CurrentLanguageIdentifier);

            if (optionNeeded != null)
            {
                if (optionNeeded.Equals("prev"))
                {
                    ToDoViewModel todo = new ToDoViewModel();
                    DateTime oldDate = DateTime.Parse(date);
                    DateTime newDate = oldDate.AddDays(-1);

                    List<Assignment> assignments = null;
                    List<SynchronousSession> sessions = null;
                    List<AssignmentToDoViewModel> assignmentsToSend = new List<AssignmentToDoViewModel>();
                    AssignmentToDoViewModel assign = null;


                    if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Student")
                    {

                        var currentStudent = await db1.AspNetUsers.Where(a => a.Id == CurrentUser.Id).ToListAsync();
                        var student = currentStudent.FirstOrDefault().Student;
                        var studentAssTimeline = await db1.Timelines.Where(ut => ut.UserKey == CurrentUser.Id).Select(ass => ass.TimeLineEntityKey).ToListAsync();
                        var remoteId = currentStudent.FirstOrDefault().RemoteAuthentications.Where(r => r.ESystem.ESystemName == "Moodle" && r.Userkey == CurrentUser.Id).FirstOrDefault().EsystemRemoteId;

                        assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGrade.SchoolKey == student.SchoolKey && a.SchoolSubjectTeacherGrade.GradeKey == student.GradeKey && studentAssTimeline.Contains(a.AssignmentKey) && a.DueDate == newDate).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();

                        // check if assignment is submitted
                        foreach (var assignment in assignments)
                        {
                            SubmissionDetailsViewModel s = await SubmissionsManager.GetAssignSubmissionDetails(assignment.AssignmentId.ToString(), remoteId, HttpContext);
                            assign = new AssignmentToDoViewModel();
                            assign.AssignmentId = assignment.AssignmentId;
                            assign.DueDate = assignment.DueDate;
                            assign.Title = assignment.AssignmentTitle;
                            assign.AssignmentKey = assignment.AssignmentKey;
                            assign.GradeName = assignment.SchoolSubjectTeacherGrade.Grade.Grade1;
                            assign.SubjectName = assignment.SchoolSubjectTeacherGrade.Subject.Subject1;

                            if (s != null && s.SubmissionStatus.Equals("submitted"))
                            {
                                assign.submissionStatus = "submitted";
                            }
                            else
                            {
                                assign.submissionStatus = "new";
                            }
                            assignmentsToSend.Add(assign);
                        }

                        sessions = await db1.SynchronousSessions.Where(s => s.SessionDate.Year == newDate.Year && s.SessionDate.Month == newDate.Month && s.SessionDate.Day == newDate.Day && s.SchoolSubjectTeacherGradeKey == student.GradeKey).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                        todo.Assignments = assignmentsToSend;
                        todo.Sessions = sessions;
                        todo.TodosDate = newDate;
                        
                        return PartialView("_UpperPartPartial", todo);
                    }

                    else
                    {
                        if (gradeKey == "-1")
                        {

                            assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGrade.TeacherKey == CurrentUser.TeacherKey && a.DueDate == newDate).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();
                            sessions = await db1.SynchronousSessions.Where(s => s.CreatedByUserKey == CurrentUser.Id && s.SessionDate.Year == newDate.Year && s.SessionDate.Month == newDate.Month && s.SessionDate.Day == newDate.Day).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                        }
                        else
                        {
                            assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGrade.TeacherKey == CurrentUser.TeacherKey && a.DueDate == newDate && a.SchoolSubjectTeacherGrade.GradeKey.ToString() == gradeKey).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();
                            sessions = await db1.SynchronousSessions.Where(s => s.CreatedByUserKey == CurrentUser.Id && s.SessionDate.Year == newDate.Year && s.SessionDate.Month == newDate.Month && s.SessionDate.Day == newDate.Day && s.SchoolSubjectTeacherGrade.GradeKey.ToString() == gradeKey).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                        }

                        foreach (var assignment in assignments)
                        {

                            assign = new AssignmentToDoViewModel();
                            assign.AssignmentId = assignment.AssignmentId;
                            assign.Title = assignment.AssignmentTitle;
                            assign.DueDate = assignment.DueDate;
                            assign.GradeName = assignment.SchoolSubjectTeacherGrade.Grade.Grade1;

                            assign.SubjectName = assignment.SchoolSubjectTeacherGrade.Subject.Subject1;
                            assignmentsToSend.Add(assign);
                        }

                        foreach (var assig in assignments)
                        {
                            if (todo.grades != null)
                            {
                                if (!todo.grades.Contains(assig.SchoolSubjectTeacherGrade.Grade))
                                {
                                    todo.grades.Add(assig.SchoolSubjectTeacherGrade.Grade);
                                }
                            }
                            else
                            {
                                todo.grades = new List<RLI.EntityFramework.EDM.Grade>();
                                todo.grades.Add(assig.SchoolSubjectTeacherGrade.Grade);
                            }
                        }

                        foreach (var sess in sessions)
                        {
                            if (todo.grades != null)
                            {
                                if (!todo.grades.Contains(sess.SchoolSubjectTeacherGrade.Grade))
                                {
                                    todo.grades.Add(sess.SchoolSubjectTeacherGrade.Grade);
                                }
                            }
                            else
                            {
                                todo.grades = new List<RLI.EntityFramework.EDM.Grade>();
                                todo.grades.Add(sess.SchoolSubjectTeacherGrade.Grade);
                            }
                        }


                        todo.Assignments = assignmentsToSend;
                        todo.Sessions = sessions;
                        todo.TodosDate = newDate;
                        List<RLI.EntityFramework.EDM.SchoolSubjectTeacherGrade> gradesSstg = new List<RLI.EntityFramework.EDM.SchoolSubjectTeacherGrade>();
                        gradesSstg = await db1.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey && sstg.GradeKey != null).ToListAsync();
                        ViewBag.SSTGKey = gradesSstg.FirstOrDefault().SchoolSubjectTeacherGradeKey;
                        ViewBag.Grades = new SelectList(gradesSstg.Distinct().Select(g => new
                        {
                            GradeKey = g.SchoolSubjectTeacherGradeKey,
                            DefaultGrade1 = g.Grade.Grade1,
                            Grade1 = (CurrentLanguageIdentifier == 0 ? g.Grade.Grade1 : g.Grade.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value) + "-" + (CurrentLanguageIdentifier == 0 ? g.Subject.Subject1 : g.Subject.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value),
                            GradeIndex = g.Grade.GradeIndex,
                            LocalGradeGUID = g.Grade.LocalGradeGUID
                        }).OrderBy(gr => gr.GradeIndex), "GradeKey", "Grade1", ViewBag.SSTGKey);
                        return PartialView("_UpperPartPartial", todo);
                    }


                }

                if (optionNeeded.Equals("next"))
                {
                    ToDoViewModel todo = new ToDoViewModel();
                    DateTime oldDate = DateTime.Parse(date);
                    DateTime newDate = oldDate.AddDays(1);

                    List<Assignment> assignments = null;
                    List<SynchronousSession> sessions = null;
                    List<AssignmentToDoViewModel> assignmentsToSend = new List<AssignmentToDoViewModel>();
                    AssignmentToDoViewModel assign = null;

                    if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Student")
                    {


                        var currentStudent = await db1.AspNetUsers.Where(a => a.Id == CurrentUser.Id).ToListAsync();
                        var student = currentStudent.FirstOrDefault().Student;
                        var studentAssTimeline = await db1.Timelines.Where(ut => ut.UserKey == CurrentUser.Id).Select(ass => ass.TimeLineEntityKey).ToListAsync();

                        var remoteId = currentStudent.FirstOrDefault().RemoteAuthentications.Where(r => r.ESystem.ESystemName == "Moodle" && r.Userkey == CurrentUser.Id).FirstOrDefault().EsystemRemoteId;

                        assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGrade.SchoolKey == student.SchoolKey && a.SchoolSubjectTeacherGrade.GradeKey == student.GradeKey && studentAssTimeline.Contains(a.AssignmentKey) && a.DueDate == newDate).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();

                        // check if assignment is submitted
                        foreach (var assignment in assignments)
                        {
                            SubmissionDetailsViewModel s = await SubmissionsManager.GetAssignSubmissionDetails(assignment.AssignmentKey.ToString(), remoteId, HttpContext);
                            assign = new AssignmentToDoViewModel();
                            assign.AssignmentId = assignment.AssignmentId;
                            assign.DueDate = assignment.DueDate;
                            assign.Title = assignment.AssignmentTitle;
                            assign.AssignmentKey = assignment.AssignmentKey;
                            assign.GradeName = assignment.SchoolSubjectTeacherGrade.Grade.Grade1;
                            assign.SubjectName = assignment.SchoolSubjectTeacherGrade.Subject.Subject1;

                            if (s != null && s.SubmissionStatus.Equals("submitted"))
                            {
                                assign.submissionStatus = "submitted";
                            }
                            else
                            {
                                assign.submissionStatus = "new";
                            }
                            assignmentsToSend.Add(assign);
                        }

                        sessions = await db1.SynchronousSessions.Where(s => s.SessionDate.Year == newDate.Year && s.SessionDate.Month == newDate.Month && s.SessionDate.Day == newDate.Day && s.SchoolSubjectTeacherGrade.GradeKey == student.GradeKey).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                        todo.Assignments = assignmentsToSend;
                        todo.Sessions = sessions;
                        todo.TodosDate = newDate;
                        List<RLI.EntityFramework.EDM.SchoolSubjectTeacherGrade> gradesSstg = new List<RLI.EntityFramework.EDM.SchoolSubjectTeacherGrade>();
                        if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Teacher")
                        {
                            gradesSstg = await db1.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey && sstg.GradeKey != null).ToListAsync();
                            ViewBag.SSTGKey = gradesSstg.FirstOrDefault().SchoolSubjectTeacherGradeKey;
                            ViewBag.Grades = new SelectList(gradesSstg.Distinct().Select(g => new
                            {
                                GradeKey = g.SchoolSubjectTeacherGradeKey,
                                DefaultGrade1 = g.Grade.Grade1,
                                Grade1 = (CurrentLanguageIdentifier == 0 ? g.Grade.Grade1 : g.Grade.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value) + "-" + (CurrentLanguageIdentifier == 0 ? g.Subject.Subject1 : g.Subject.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value),
                                GradeIndex = g.Grade.GradeIndex,
                                LocalGradeGUID = g.Grade.LocalGradeGUID
                            }).OrderBy(gr => gr.GradeIndex), "GradeKey", "Grade1", ViewBag.SSTGKey);

                        }
                        return PartialView("_UpperPartPartial", todo);
                    }
                    else
                    {
                        if (gradeKey == "-1")
                        {
                            assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGrade.TeacherKey == CurrentUser.TeacherKey && a.DueDate == newDate).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();
                            sessions = await db1.SynchronousSessions.Where(s => s.CreatedByUserKey == CurrentUser.Id && s.SessionDate.Year == newDate.Year && s.SessionDate.Month == newDate.Month && s.SessionDate.Day == newDate.Day).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                        }
                        else
                        {
                            assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGrade.TeacherKey == CurrentUser.TeacherKey && a.DueDate == newDate && a.SchoolSubjectTeacherGrade.GradeKey.ToString() == gradeKey).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();
                            sessions = await db1.SynchronousSessions.Where(s => s.CreatedByUserKey == CurrentUser.Id && s.SessionDate.Year == newDate.Year && s.SessionDate.Month == newDate.Month && s.SessionDate.Day == newDate.Day && s.SchoolSubjectTeacherGrade.GradeKey.ToString() == gradeKey).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                        }

                        foreach (var assignment in assignments)
                        {

                            assign = new AssignmentToDoViewModel();
                            assign.AssignmentId = assignment.AssignmentId;
                            assign.Title = assignment.AssignmentTitle;
                            assign.DueDate = assignment.DueDate;
                            assign.GradeName = assignment.SchoolSubjectTeacherGrade.Grade.Grade1;

                            assign.SubjectName = assignment.SchoolSubjectTeacherGrade.Subject.Subject1;
                            assignmentsToSend.Add(assign);
                        }

                        foreach (var assig in assignments)
                        {
                            if (todo.grades != null)
                            {
                                if (!todo.grades.Contains(assig.SchoolSubjectTeacherGrade.Grade))
                                {
                                    todo.grades.Add(assig.SchoolSubjectTeacherGrade.Grade);
                                }
                            }
                            else
                            {
                                todo.grades = new List<RLI.EntityFramework.EDM.Grade>();
                                todo.grades.Add(assig.SchoolSubjectTeacherGrade.Grade);
                            }
                        }

                        foreach (var sess in sessions)
                        {
                            if (todo.grades != null)
                            {
                                if (!todo.grades.Contains(sess.SchoolSubjectTeacherGrade.Grade))
                                {
                                    todo.grades.Add(sess.SchoolSubjectTeacherGrade.Grade);
                                }
                            }
                            else
                            {
                                todo.grades = new List<RLI.EntityFramework.EDM.Grade>();
                                todo.grades.Add(sess.SchoolSubjectTeacherGrade.Grade);
                            }
                        }


                        todo.Assignments = assignmentsToSend;
                        todo.Sessions = sessions;
                        todo.TodosDate = newDate;
                        List<RLI.EntityFramework.EDM.SchoolSubjectTeacherGrade> gradesSstg = new List<RLI.EntityFramework.EDM.SchoolSubjectTeacherGrade>();
                        if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Teacher")
                        {
                            gradesSstg = await db1.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey && sstg.GradeKey != null).ToListAsync();
                            ViewBag.SSTGKey = gradesSstg.FirstOrDefault().SchoolSubjectTeacherGradeKey;
                            ViewBag.Grades = new SelectList(gradesSstg.Distinct().Select(g => new
                            {
                                GradeKey = g.SchoolSubjectTeacherGradeKey,
                                DefaultGrade1 = g.Grade.Grade1,
                                Grade1 = (CurrentLanguageIdentifier == 0 ? g.Grade.Grade1 : g.Grade.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value) + "-" + (CurrentLanguageIdentifier == 0 ? g.Subject.Subject1 : g.Subject.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value),
                                GradeIndex = g.Grade.GradeIndex,
                                LocalGradeGUID = g.Grade.LocalGradeGUID
                            }).OrderBy(gr => gr.GradeIndex), "GradeKey", "Grade1", ViewBag.SSTGKey);

                        }
                        return PartialView("_UpperPartPartial", todo);
                    }

                }

                //this means when we choose a date from the calendar
                if (optionNeeded.Equals("none"))
                {
                    ToDoViewModel todo = new ToDoViewModel();
                    DateTime newDate = DateTime.Parse(date);

                    List<Assignment> assignments = null;
                    List<SynchronousSession> sessions = null;
                    List<AssignmentToDoViewModel> assignmentsToSend = new List<AssignmentToDoViewModel>();
                    AssignmentToDoViewModel assign = null;


                    if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Student")
                    {


                        var currentStudent = await db1.AspNetUsers.Where(a => a.Id == CurrentUser.Id).ToListAsync();
                        var student = currentStudent.FirstOrDefault().Student;
                        var studentAssTimeline = await db1.Timelines.Where(ut => ut.UserKey == CurrentUser.Id).Select(ass => ass.TimeLineEntityKey).ToListAsync();
                        var remoteId = currentStudent.FirstOrDefault().RemoteAuthentications.Where(r => r.ESystem.ESystemName == "Moodle" && r.Userkey == CurrentUser.Id).FirstOrDefault().EsystemRemoteId;

                        assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGrade.SchoolKey == student.SchoolKey && a.SchoolSubjectTeacherGrade.GradeKey == student.GradeKey && studentAssTimeline.Contains(a.AssignmentKey) && a.DueDate == newDate).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();

                        // check if assignment is submitted
                        foreach (var assignment in assignments)
                        {
                            SubmissionDetailsViewModel s = await SubmissionsManager.GetAssignSubmissionDetails(assignment.AssignmentId.ToString(), remoteId, HttpContext);
                            assign = new AssignmentToDoViewModel();
                            assign.AssignmentId = assignment.AssignmentId;
                            assign.Description = assignment.Description;
                            assign.DueDate = assignment.DueDate;
                            assign.Title = assignment.AssignmentTitle;
                            assign.AssignmentKey = assignment.AssignmentKey;
                            assign.GradeName = assignment.SchoolSubjectTeacherGrade.Grade.Grade1;
                            assign.SubjectName = assignment.SchoolSubjectTeacherGrade.Subject.Subject1;

                            if (s != null && s.SubmissionStatus.Equals("submitted"))
                            {
                                assign.submissionStatus = "submitted";
                            }
                            else
                            {
                                assign.submissionStatus = "new";
                            }
                            assignmentsToSend.Add(assign);
                        }

                        sessions = await db1.SynchronousSessions.Where(s => s.SessionDate.Year == newDate.Year && s.SessionDate.Month == newDate.Month && s.SessionDate.Day == newDate.Day && s.SchoolSubjectTeacherGrade.GradeKey == student.GradeKey).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                        todo.Assignments = assignmentsToSend;
                        todo.Sessions = sessions;
                        todo.TodosDate = newDate;
                        List<RLI.EntityFramework.EDM.SchoolSubjectTeacherGrade> gradesSstg = new List<RLI.EntityFramework.EDM.SchoolSubjectTeacherGrade>();
                        if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Teacher")
                        {
                            gradesSstg = await db1.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey && sstg.GradeKey != null).ToListAsync();
                            ViewBag.SSTGKey = gradesSstg.FirstOrDefault().SchoolSubjectTeacherGradeKey;
                            ViewBag.Grades = new SelectList(gradesSstg.Distinct().Select(g => new
                            {
                                GradeKey = g.SchoolSubjectTeacherGradeKey,
                                DefaultGrade1 = g.Grade.Grade1,
                                Grade1 = (CurrentLanguageIdentifier == 0 ? g.Grade.Grade1 : g.Grade.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value) + "-" + (CurrentLanguageIdentifier == 0 ? g.Subject.Subject1 : g.Subject.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value),
                                GradeIndex = g.Grade.GradeIndex,
                                LocalGradeGUID = g.Grade.LocalGradeGUID
                            }).OrderBy(gr => gr.GradeIndex), "GradeKey", "Grade1", ViewBag.SSTGKey);

                        }
                        return PartialView("_UpperPartPartial", todo);
                    }

                    else
                    {
                        if (gradeKey == "-1")
                        {
                            assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGrade.TeacherKey == CurrentUser.TeacherKey && a.DueDate == newDate).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();
                            sessions = await db1.SynchronousSessions.Where(s => s.CreatedByUserKey == CurrentUser.Id && s.SessionDate.Year == newDate.Year && s.SessionDate.Month == newDate.Month && s.SessionDate.Day == newDate.Day).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                        }
                        else
                        {
                            assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGrade.TeacherKey == CurrentUser.TeacherKey && a.DueDate == newDate && a.SchoolSubjectTeacherGrade.GradeKey.ToString() == gradeKey).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();
                            sessions = await db1.SynchronousSessions.Where(s => s.CreatedByUserKey == CurrentUser.Id && s.SessionDate.Year == newDate.Year && s.SessionDate.Month == newDate.Month && s.SessionDate.Day == newDate.Day && s.SchoolSubjectTeacherGradeKey.ToString() == gradeKey).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                        }

                        foreach (var assignment in assignments)
                        {

                            assign = new AssignmentToDoViewModel();
                            assign.AssignmentId = assignment.AssignmentId;
                            assign.Title = assignment.AssignmentTitle;
                            assign.DueDate = assignment.DueDate;
                            assign.GradeName = assignment.SchoolSubjectTeacherGrade.Grade.Grade1;


                            assign.SubjectName = assignment.SchoolSubjectTeacherGrade.Subject.Subject1;
                            assignmentsToSend.Add(assign);
                        }


                        foreach (var assig in assignments)
                        {
                            if (todo.grades != null)
                            {
                                if (!todo.grades.Contains(assig.SchoolSubjectTeacherGrade.Grade))
                                {
                                    todo.grades.Add(assig.SchoolSubjectTeacherGrade.Grade);
                                }
                            }
                            else
                            {
                                todo.grades = new List<RLI.EntityFramework.EDM.Grade>();
                                todo.grades.Add(assig.SchoolSubjectTeacherGrade.Grade);
                            }
                        }

                        foreach (var sess in sessions)
                        {
                            if (todo.grades != null)
                            {
                                if (!todo.grades.Contains(sess.SchoolSubjectTeacherGrade.Grade))
                                {
                                    todo.grades.Add(sess.SchoolSubjectTeacherGrade.Grade);
                                }
                            }
                            else
                            {
                                todo.grades = new List<RLI.EntityFramework.EDM.Grade>();
                                todo.grades.Add(sess.SchoolSubjectTeacherGrade.Grade);
                            }
                        }


                        todo.Assignments = assignmentsToSend;
                        todo.Sessions = sessions;
                        todo.TodosDate = newDate;
                        List<RLI.EntityFramework.EDM.SchoolSubjectTeacherGrade> gradesSstg = new List<RLI.EntityFramework.EDM.SchoolSubjectTeacherGrade>();
                        if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Teacher")
                        {
                            gradesSstg = await db1.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey && sstg.GradeKey != null).ToListAsync();
                            ViewBag.SSTGKey = gradesSstg.FirstOrDefault().SchoolSubjectTeacherGradeKey;
                            ViewBag.Grades = new SelectList(gradesSstg.Distinct().Select(g => new
                            {
                                GradeKey = g.SchoolSubjectTeacherGradeKey,
                                DefaultGrade1 = g.Grade.Grade1,
                                Grade1 = (CurrentLanguageIdentifier == 0 ? g.Grade.Grade1 : g.Grade.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value) + "-" + (CurrentLanguageIdentifier == 0 ? g.Subject.Subject1 : g.Subject.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value),
                                GradeIndex = g.Grade.GradeIndex,
                                LocalGradeGUID = g.Grade.LocalGradeGUID
                            }).OrderBy(gr => gr.GradeIndex), "GradeKey", "Grade1", ViewBag.SSTGKey);

                        }
                        return PartialView("_UpperPartPartial", todo);
                    }

                }

            }
            return Content("404");

        }


        [ValidateAntiForgeryToken]
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> DateManipulationView(string optionNeeded, string date, int SSTGKey)
        {
            ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("ToDoList", "Index", CurrentLanguageIdentifier);

            if (optionNeeded != null)
            {
                if (optionNeeded.Equals("prev"))
                {
                    ToDoViewModel todo = new ToDoViewModel();
                    DateTime oldDate = DateTime.Parse(date);
                    DateTime newDate = oldDate.AddDays(-1);

                    List<Assignment> assignments = null;
                    List<SynchronousSession> sessions = null;
                    List<AssignmentToDoViewModel> assignmentsToSend = new List<AssignmentToDoViewModel>();
                    AssignmentToDoViewModel assign = null;
                    
                        assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGradeKey == SSTGKey && a.DueDate == newDate).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();

                        // check if assignment is submitted
                        foreach (var assignment in assignments)
                        {
                            assign = new AssignmentToDoViewModel();
                            assign.AssignmentId = assignment.AssignmentId;
                            assign.DueDate = assignment.DueDate;
                            assign.Title = assignment.AssignmentTitle;
                            assign.AssignmentKey = assignment.AssignmentKey;
                            assign.GradeName = assignment.SchoolSubjectTeacherGrade.Grade.Grade1;
                            assign.SubjectName = assignment.SchoolSubjectTeacherGrade.Subject.Subject1;
                            assign.submissionStatus = "";
                            assignmentsToSend.Add(assign);
                        }

                        sessions = await db1.SynchronousSessions.Where(s => s.SessionDate.Year == newDate.Year && s.SessionDate.Month == newDate.Month && s.SessionDate.Day == newDate.Day && s.SchoolSubjectTeacherGradeKey == SSTGKey).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                        todo.Assignments = assignmentsToSend;
                        todo.Sessions = sessions;
                        todo.TodosDate = newDate;
                        List<RLI.EntityFramework.EDM.SchoolSubjectTeacherGrade> gradesSstg = new List<RLI.EntityFramework.EDM.SchoolSubjectTeacherGrade>();
                        if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Teacher")
                        {
                            gradesSstg = await db1.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey && sstg.GradeKey != null).ToListAsync();
                            ViewBag.SSTGKey = gradesSstg.FirstOrDefault().SchoolSubjectTeacherGradeKey;
                            ViewBag.Grades = new SelectList(gradesSstg.Distinct().Select(g => new
                            {
                                GradeKey = g.SchoolSubjectTeacherGradeKey,
                                DefaultGrade1 = g.Grade.Grade1,
                                Grade1 = (CurrentLanguageIdentifier == 0 ? g.Grade.Grade1 : g.Grade.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value) + "-" + (CurrentLanguageIdentifier == 0 ? g.Subject.Subject1 : g.Subject.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value),
                                GradeIndex = g.Grade.GradeIndex,
                                LocalGradeGUID = g.Grade.LocalGradeGUID
                            }).OrderBy(gr => gr.GradeIndex), "GradeKey", "Grade1", ViewBag.SSTGKey);

                        }
                  
                    
                    return PartialView("_UpperPartPartialTest", todo);
                }

                if (optionNeeded.Equals("next"))
                {
                    ToDoViewModel todo = new ToDoViewModel();
                    DateTime oldDate = DateTime.Parse(date);
                    DateTime newDate = oldDate.AddDays(1);

                    List<Assignment> assignments = null;
                    List<SynchronousSession> sessions = null;
                    List<AssignmentToDoViewModel> assignmentsToSend = new List<AssignmentToDoViewModel>();
                    AssignmentToDoViewModel assign = null;
                   
                        assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGradeKey == SSTGKey && a.DueDate == newDate).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();

                        // check if assignment is submitted
                        foreach (var assignment in assignments)
                        {
                            assign = new AssignmentToDoViewModel();
                            assign.AssignmentId = assignment.AssignmentId;
                            assign.DueDate = assignment.DueDate;
                            assign.Title = assignment.AssignmentTitle;
                            assign.AssignmentKey = assignment.AssignmentKey;
                            assign.GradeName = assignment.SchoolSubjectTeacherGrade.Grade.Grade1;
                            assign.SubjectName = assignment.SchoolSubjectTeacherGrade.Subject.Subject1;
                            assign.submissionStatus = "";
                            assignmentsToSend.Add(assign);
                        }

                        sessions = await db1.SynchronousSessions.Where(s => s.SessionDate.Year == newDate.Year && s.SessionDate.Month == newDate.Month && s.SessionDate.Day == newDate.Day && s.SchoolSubjectTeacherGradeKey == SSTGKey).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                        todo.Assignments = assignmentsToSend;
                        todo.Sessions = sessions;
                        todo.TodosDate = newDate;
                        List<RLI.EntityFramework.EDM.SchoolSubjectTeacherGrade> gradesSstg = new List<RLI.EntityFramework.EDM.SchoolSubjectTeacherGrade>();
                        if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Teacher")
                        {
                            gradesSstg = await db1.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey && sstg.GradeKey != null).ToListAsync();
                            ViewBag.SSTGKey = gradesSstg.FirstOrDefault().SchoolSubjectTeacherGradeKey;
                            ViewBag.Grades = new SelectList(gradesSstg.Distinct().Select(g => new
                            {
                                GradeKey = g.SchoolSubjectTeacherGradeKey,
                                DefaultGrade1 = g.Grade.Grade1,
                                Grade1 = (CurrentLanguageIdentifier == 0 ? g.Grade.Grade1 : g.Grade.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value) + "-" + (CurrentLanguageIdentifier == 0 ? g.Subject.Subject1 : g.Subject.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value),
                                GradeIndex = g.Grade.GradeIndex,
                                LocalGradeGUID = g.Grade.LocalGradeGUID
                            }).OrderBy(gr => gr.GradeIndex), "GradeKey", "Grade1", ViewBag.SSTGKey);

                        }
                   
                     
                    return PartialView("_UpperPartPartialTest", todo);
                }

                //this means when we choose a date from the calendar
                if (optionNeeded.Equals("none"))
                {

                    ToDoViewModel todo = new ToDoViewModel();
                    DateTime newDate = DateTime.Parse(date);

                    List<Assignment> assignments = null;
                    List<SynchronousSession> sessions = null;
                    List<AssignmentToDoViewModel> assignmentsToSend = new List<AssignmentToDoViewModel>();
                    AssignmentToDoViewModel assign = null;
                    
                        assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGradeKey == SSTGKey && a.DueDate == newDate).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();

                        // check if assignment is submitted
                        foreach (var assignment in assignments)
                        {
                            assign = new AssignmentToDoViewModel();
                            assign.AssignmentId = assignment.AssignmentId;
                            assign.DueDate = assignment.DueDate;
                            assign.Title = assignment.AssignmentTitle;
                            assign.AssignmentKey = assignment.AssignmentKey;
                            assign.GradeName = assignment.SchoolSubjectTeacherGrade.Grade.Grade1;
                            assign.SubjectName = assignment.SchoolSubjectTeacherGrade.Subject.Subject1;
                            assign.submissionStatus = "";
                            assignmentsToSend.Add(assign);
                        }

                        sessions = await db1.SynchronousSessions.Where(s => s.SessionDate.Year == newDate.Year && s.SessionDate.Month == newDate.Month && s.SessionDate.Day == newDate.Day && s.SchoolSubjectTeacherGradeKey == SSTGKey).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                        todo.Assignments = assignmentsToSend;
                        todo.Sessions = sessions;
                        todo.TodosDate = newDate;
                        List<RLI.EntityFramework.EDM.SchoolSubjectTeacherGrade> gradesSstg = new List<RLI.EntityFramework.EDM.SchoolSubjectTeacherGrade>();
                        if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Teacher")
                        {
                            gradesSstg = await db1.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey && sstg.GradeKey != null).ToListAsync();
                            ViewBag.SSTGKey = gradesSstg.FirstOrDefault().SchoolSubjectTeacherGradeKey;
                            ViewBag.Grades = new SelectList(gradesSstg.Distinct().Select(g => new
                            {
                                GradeKey = g.SchoolSubjectTeacherGradeKey,
                                DefaultGrade1 = g.Grade.Grade1,
                                Grade1 = (CurrentLanguageIdentifier == 0 ? g.Grade.Grade1 : g.Grade.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value) + "-" + (CurrentLanguageIdentifier == 0 ? g.Subject.Subject1 : g.Subject.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value),
                                GradeIndex = g.Grade.GradeIndex,
                                LocalGradeGUID = g.Grade.LocalGradeGUID
                            }).OrderBy(gr => gr.GradeIndex), "GradeKey", "Grade1", ViewBag.SSTGKey);

                        }
                        return PartialView("_UpperPartPartialTest", todo);
                    
                       
                }

            }
            return Content("404");

        }


        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> DaysManipulation(string date)
        {
            List<string> days = new List<string>();
            DateTime d = DateTime.Parse(date);
            if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Teacher")
            {
                var assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGrade.TeacherKey == CurrentUser.TeacherKey && a.DueDate.Value.Month == d.Month && a.DueDate.Value.Year == d.Year).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();
                var sessions = await db1.SynchronousSessions.Where(s => s.CreatedByUserKey == CurrentUser.Id && s.SessionDate.Month == d.Month && s.SessionDate.Year == d.Year).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                if (assignments.Count() != 0)
                {
                    //loop on assignments
                    foreach (var assignment in assignments)
                    {
                        if (!days.Contains(assignment.DueDate.Value.Day + ""))
                            days.Add(assignment.DueDate.Value.Day + "");
                    }
                }

                if (sessions.Count() != 0)
                {
                    //loop on assignments
                    foreach (var session in sessions)
                    {
                        if (!days.Contains(session.SessionDate.Day + ""))
                            days.Add(session.SessionDate.Day + "");
                    }
                }


                return Json(days);
            }
            else
            {
                var currentStudent = await db1.AspNetUsers.Where(a => a.Id == CurrentUser.Id).ToListAsync();
                var student = currentStudent.FirstOrDefault().Student;
                var studentAssTimeline = await db1.Timelines.Where(ut => ut.UserKey == CurrentUser.Id).Select(ass => ass.TimeLineEntityKey).ToListAsync();
                var remoteId = currentStudent.FirstOrDefault().RemoteAuthentications.Where(r => r.ESystem.ESystemName == "Moodle" && r.Userkey == CurrentUser.Id).FirstOrDefault().EsystemRemoteId;

                var assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGrade.SchoolKey == student.SchoolKey && a.SchoolSubjectTeacherGrade.GradeKey == student.GradeKey && studentAssTimeline.Contains(a.AssignmentKey) && a.DueDate.Value.Month == d.Month && a.DueDate.Value.Year == d.Year).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();
                var sessions = await db1.SynchronousSessions.Where(s => s.SessionDate.Month == d.Month && s.SessionDate.Year == d.Year && s.SchoolSubjectTeacherGradeKey == student.GradeKey).Take(DEFAULT_LOAD_COUNT).ToListAsync();

                if (assignments.Count() != 0)
                {
                    //loop on assignments
                    foreach (var assignment in assignments)
                    {
                        if (!days.Contains(assignment.DueDate.Value.Day + ""))
                            days.Add(assignment.DueDate.Value.Day + "");
                    }
                }

                if (sessions.Count() != 0)
                {
                    //loop on assignments
                    foreach (var session in sessions)
                    {
                        if (!days.Contains(session.SessionDate.Day + ""))
                            days.Add(session.SessionDate.Day + "");
                    }
                }
                return Json(days);
            }
            return Content("404");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> DaysManipulationView(string date,int sstgKey)
        {
            try
            {
                List<string> days = new List<string>();
                DateTime d = DateTime.Parse(date);
              
                    var hfd = await db1.Assignments.Take(2).ToListAsync();
                    var assignments = await db1.Assignments.Where(a => a.SchoolSubjectTeacherGradeKey == sstgKey && a.DueDate.Value.Month == d.Month && a.DueDate.Value.Year == d.Year).OrderByDescending(a => a.TimeModifed).Take(20).ToListAsync();
                    var sessions = await db1.SynchronousSessions.Where(s => s.SchoolSubjectTeacherGradeKey == sstgKey && s.SessionDate.Month == d.Month && s.SessionDate.Year == d.Year).Take(DEFAULT_LOAD_COUNT).ToListAsync();
                    if (assignments.Count() != 0)
                    {
                        //loop on assignments
                        foreach (var assignment in assignments)
                        {
                            if (!days.Contains(assignment.DueDate.Value.Day + ""))
                                days.Add(assignment.DueDate.Value.Day + "");
                        }
                    }

                    if (sessions.Count() != 0)
                    {
                        //loop on assignments
                        foreach (var session in sessions)
                        {
                            if (!days.Contains(session.SessionDate.Day + ""))
                                days.Add(session.SessionDate.Day + "");
                        }
                    }

                    return Json(days);
                
                   
            }catch(Exception e)
            {
                return Content("404");
            }
            return Content("404");
        }
    }


}