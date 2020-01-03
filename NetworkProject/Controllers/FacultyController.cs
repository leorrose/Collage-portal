using NetworkProject.Dal;
using NetworkProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NetworkProject.Controllers
{
    public class FacultyController : Controller
    {
        public ActionResult Home()
        {
            /* check if user had logged in */
            if (Session["ID"] == null || Session["password"] == null || Session["type"] == null)
            {
                return RedirectToAction("Login", "Login");
            }
            /* check if user is faculty type */
            else if (!Session["type"].Equals("Faculty"))
            {
                return RedirectToAction("Home", Session["type"].ToString());
            }
            return View();
        }

        public ActionResult ManageCourseParticipants()
        {
            /* check if user had logged in */
            if (Session["ID"] == null || Session["password"] == null || Session["type"] == null)
            {
                return RedirectToAction("Login", "Login");
            }
            /* check if user is faculty type */
            else if (!Session["type"].Equals("Faculty"))
            {
                return RedirectToAction("Home", Session["type"].ToString());
            }
            return View(new CourseParticipant());
        }

        [HttpPost]
        public ActionResult SubmitParticipant( CourseParticipant courseParticipant)
        {
            if (ModelState.IsValid)
            {
                /* add course to student */

                var id = Session["ID"].ToString();

                /* change or add grade */
                using (CoursesDal coursesDb = new CoursesDal())
                using (ExamsDal exmaDb = new ExamsDal())
                using (UsersDal usersDb = new UsersDal())
                using (GradesDal gradesDb = new GradesDal())
                using (CourseParticipantsDal courseParticipantDb = new CourseParticipantsDal())
                {

                    /* check course exist */
                    var courseId = coursesDb.Courses.Where(Course => Course.courseId.Equals(courseParticipant.courseId)).FirstOrDefault();
                    if (courseId == null)
                    {
                        TempData["msg"] = "Invalid course ID";
                        return View("ManageCourseParticipants", courseParticipant);
                    }

                    /* check if student exist */
                    var student =
                           (from row in usersDb.Users
                            where row.ID.Equals(courseParticipant.ID)
                            select row).FirstOrDefault();
                    if (student == null)
                    {
                        TempData["msg"] = "Student doesnt exist";
                        return View("ManageCourseParticipants", courseParticipant);

                    }

                    /* check if student takes this course */
                    var studentTakesCourse =
                           (from row in courseParticipantDb.CourseParticipants
                            where row.ID.Equals(courseParticipant.ID) && row.courseId.Equals(courseParticipant.courseId)
                            select row).FirstOrDefault();
                    if (studentTakesCourse != null)
                    {
                        TempData["msg"] = "Student takes this course";
                        return View("ManageCourseParticipants", courseParticipant);

                    }

                    /* course time doesnt clashes with student courses */
                    var studentCourses =
                           (from row in courseParticipantDb.CourseParticipants
                            where row.ID.Equals(courseParticipant.ID)
                            select row.courseId).ToList();

                    var courses =
                        (from row in coursesDb.Courses
                         where studentCourses.Contains(row.courseId)
                         select row).ToList();


                    foreach (Course x in courses)
                    {
                        if (x.startTime < courseId.endTime && courseId.startTime < x.endTime && x.day.Equals(courseId.day))
                        {
                            TempData["msg"] = "Course time clashes with student schedule";
                            return View("ManageCourseParticipants", courseParticipant);
                        }
                    }


                    /* add course to student */
                    courseParticipantDb.CourseParticipants.Add(courseParticipant);
                    courseParticipantDb.SaveChanges();
                }

                TempData["goodMsg"] = "Inserted\\updated grade";
                /* redirect with succsees message */
                return View("ManageCourseParticipants", new CourseParticipant());
            }
            else
            {
                return View("ManageCourseParticipants",  courseParticipant);
            }
        }

        public ActionResult ManageCourseSchedule()
        {
            /* check if user had logged in */
            if (Session["ID"] == null || Session["password"] == null || Session["type"] == null)
            {
                return RedirectToAction("Login", "Login");
            }
            /* check if user is faculty type */
            else if (!Session["type"].Equals("Faculty"))
            {
                return RedirectToAction("Home", Session["type"].ToString());
            }

            return View(new Course());
        }

        [HttpPost]
        public ActionResult SubmitSchedule(Course course)
        {
            /* change course schedule */
            if (ModelState.IsValid)
            {
                var id = Session["ID"].ToString();

                /* change schedule grade */
                using (CoursesDal coursesDb = new CoursesDal())
                using (ExamsDal exmaDb = new ExamsDal())
                using (UsersDal usersDb = new UsersDal())
                using (GradesDal gradesDb = new GradesDal())
                using (CourseParticipantsDal courseParticipantDb = new CourseParticipantsDal())
                {
                    /* check if course exist */
                    var courseId = coursesDb.Courses.Where(Course => Course.courseId.Equals(course.courseId)).FirstOrDefault();
                    if (courseId != null)
                    {
                        /* check if name is for course id */
                        /* check course name is the same in courses */
                        var courseName = coursesDb.Courses.Where(Course => Course.courseId.Equals(course.courseId) && Course.courseName.Equals(course.courseName)).FirstOrDefault();
                        if (courseName == null)
                        {
                            TempData["msg"] = "Invalid course Name";
                            return View("ManageCourseSchedule", course);
                        }
                        /* check start time is before end time */
                        if (course.startTime >= course.endTime)
                        {
                            TempData["msg"] = "Course end time must be bigger than course start time";
                            return View("ManageCourseSchedule", course);
                        }

                        /* new time doesnt clash with students and lecturer schedule */

                        /* find students that take the course */
                        var studentThatTakeCourse =
                           (from row in courseParticipantDb.CourseParticipants
                            where row.courseId.Equals(course.courseId)
                            select row.ID).ToList();

                        /* find all other courses students take */
                        var studentCourses =
                            (from row in courseParticipantDb.CourseParticipants
                             where studentThatTakeCourse.Contains(row.ID)
                             select row.courseId).ToList();

                        /* get courses schedule + lecturer courses schedule */
                        var courses =
                            (from row in coursesDb.Courses
                             where studentCourses.Contains(row.courseId) || row.lecturer.Equals(course.lecturer)
                             select row).ToList();

                        /* check no clashs between courses */
                        foreach (Course x in courses)
                        {
                            if (x.startTime < course.endTime && course.startTime < x.endTime && x.day.Equals(course.day))
                            {
                                TempData["msg"] = "Course time clashes with student or lecturer schedule";
                                return View("ManageCourseSchedule", course);
                            }
                        }

                        courseId.courseId = course.courseId;
                        courseId.courseName = course.courseName;
                        courseId.startTime = course.startTime;
                        courseId.endTime = course.endTime;
                        courseId.day = course.day;
                        courseId.className = course.className;
                        courseId.lecturer = course.lecturer;

                        coursesDb.SaveChanges();

                        TempData["goodMsg"] = "Inserted\\updated course";
                        /* redirect with succsees message */
                        return View("ManageCourseSchedule", new Course());



                    }
                    /* if course doesnt exist */
                    else
                    {
                        /* check start time is before end time */
                        if (course.startTime >= course.endTime)
                        {
                            TempData["msg"] = "Course end time must be bigger than course start time";
                            return View("ManageCourseSchedule", course);
                        }

                        /* new time doesnt clash with lecturers schedule */

                        /* get lecturer courses schedule */
                        var courses =
                            (from row in coursesDb.Courses
                             where row.lecturer.Equals(course.lecturer)
                             select row).ToList();

                        /* check no clashs between courses */
                        foreach (Course x in courses)
                        {
                            if (x.startTime < course.endTime && course.startTime < x.endTime && x.day.Equals(course.day))
                            {
                                TempData["msg"] = "Course time clashes with lecturer schedule";
                                return View("ManageCourseSchedule", course);
                            }
                        }

                        courseId.courseId = course.courseId;
                        courseId.courseName = course.courseName;
                        courseId.startTime = course.startTime;
                        courseId.endTime = course.endTime;
                        courseId.day = course.day;
                        courseId.className = course.className;
                        courseId.lecturer = course.lecturer;

                        coursesDb.SaveChanges();

                        TempData["goodMsg"] = "Inserted\\updated course";
                        /* redirect with succsees message */
                        return View("ManageCourseSchedule", new Course());
                    }
                }
            }
            else
            {
                return View("ManageCourseSchedule", course);
            }
        }

        public ActionResult InsertGrades()
        {
            /* check if user had logged in */
            if (Session["ID"] == null || Session["password"] == null || Session["type"] == null)
            {
                return RedirectToAction("Login", "Login");
            }
            /* check if user is Faculty type */
            else if (!Session["type"].Equals("Faculty"))
            {
                return RedirectToAction("Home", Session["type"].ToString());
            }
            return View(new Grade());
        }

        [HttpPost]
        public ActionResult InsertGrade()
        {
            Grade grade = new Grade();
            grade.courseId = Request["courseId"].ToString();
            grade.courseName = Request["courseName"].ToString();
            grade.moed = Request["moed"].ToString();
            grade.grade = Int32.Parse(Request["grade"]);
            grade.ID = Request["ID"].ToString();

            var id = Session["ID"].ToString();

            /* change or add grade */
            using (CoursesDal coursesDb = new CoursesDal())
            using (ExamsDal exmaDb = new ExamsDal())
            using (UsersDal usersDb = new UsersDal())
            using (GradesDal gradesDb = new GradesDal())
            using (CourseParticipantsDal courseParticipantDb = new CourseParticipantsDal()) {

                /* check course exist */
                var courseId = coursesDb.Courses.Where(Course => Course.courseId.Equals(grade.courseId)).FirstOrDefault();
                if (courseId == null)
                {
                    TempData["msg"] = "Invalid course ID";
                    return View("InsertGrades", grade);
                }

                /* check course name is the same in courses */
                var courseName = coursesDb.Courses.Where(Course => Course.courseId.Equals(grade.courseId) && Course.courseName.Equals(grade.courseName)).FirstOrDefault();
                if (courseName == null)
                {
                    TempData["msg"] = "Invalid course Name";
                    return View("InsertGrades", grade);
                }

                /* check exam date has passed */
                var exam =
                   (from row in exmaDb.Exams
                    where row.courseId.Equals(courseId.courseId) && row.moed.Equals(grade.moed)
                    select row).FirstOrDefault();

                if (exam == null)
                {
                    TempData["msg"] = "No such exam in system";
                    return View("InsertGrades", grade);
                }

                if (DateTime.Compare(exam.date + exam.endTime, DateTime.Now) > 0)
                {
                    TempData["msg"] = "Exam did not occur yet";
                    return View("InsertGrades", grade);
                }

                /* check if student exist */
                var student =
                       (from row in usersDb.Users
                        where row.ID.Equals(grade.ID)
                        select row).FirstOrDefault();
                if (exam == null)
                {
                    TempData["msg"] = "Student doesnt exist";
                    return View("InsertGrades", grade);

                }
                /* check if student takes this course */
                var studentTakesCourse =
                       (from row in courseParticipantDb.CourseParticipants
                        where row.ID.Equals(grade.ID) && row.courseId.Equals(grade.courseId)
                        select row).FirstOrDefault();
                if (studentTakesCourse == null)
                {
                    TempData["msg"] = "Student doesnt take this course";
                    return View("InsertGrades", grade);

                }

                /* check if student has this course if true update else insert */
                var gradeInDb =
                       (from row in gradesDb.Grades
                        where row.courseId.Equals(grade.courseId) && row.moed.Equals(grade.moed) && row.ID.Equals(grade.ID)
                        select row).FirstOrDefault();
                if (gradeInDb != null)
                {
                    gradeInDb.grade = grade.grade;
                    gradesDb.SaveChanges();
                }
                else
                {
                    gradesDb.Grades.Add(grade);
                    gradesDb.SaveChanges();
                }
            }

            TempData["goodMsg"] = "Inserted\\updated grade";
            /* redirect with succsees message */
            return View("InsertGrades", new Grade());
        }
        
        public ActionResult ManageExams()
        {
            /* check if user had logged in */
            if (Session["ID"] == null || Session["password"] == null || Session["type"] == null)
            {
                return RedirectToAction("Login", "Login");
            }
            /* check if user is lecturer type */
            else if (!Session["type"].Equals("Faculty"))
            {
                return RedirectToAction("Home", Session["type"].ToString());
            }
            return View(new Exam());
        }

        [HttpPost]
        public ActionResult SubmitExam(Exam exam)
        {
            if (ModelState.IsValid)
            {
                /* change or add exam */
                using (CoursesDal coursesDb = new CoursesDal())
                using (ExamsDal exmaDb = new ExamsDal())
                using (CourseParticipantsDal courseParticipantDb = new CourseParticipantsDal())
                {
                    /* check course exist */
                    var courseId = coursesDb.Courses.Where(Course => Course.courseId.Equals(exam.courseId)).FirstOrDefault();
                    if (courseId == null)
                    {
                        TempData["msg"] = "Invalid course ID";
                        return View("ManageExams", exam);
                    }

                    /* check course name is the same in courses */
                    var courseName = coursesDb.Courses.Where(Course => Course.courseId.Equals(exam.courseId) && Course.courseName.Equals(exam.courseName)).FirstOrDefault();
                    if (courseName == null)
                    {
                        TempData["msg"] = "Invalid course Name";
                        return View("ManageExams", exam);
                    }

                    /* check start time is before end time */
                    if (exam.startTime >= exam.endTime)
                    {
                        TempData["msg"] = "Course end time must be bigger than course start time";
                        return View("ManageExams", exam);
                    }

                    /* check date isnt in the past */
                    if (DateTime.Compare(exam.date + exam.startTime, DateTime.Now) < 0)
                    {
                        TempData["msg"] = "Exam cant be in the past";
                        return View("ManageExams", exam);
                    }


                    /* check if exam exist */
                    var examInDb =
                           (from row in exmaDb.Exams
                            where row.courseId.Equals(exam.courseId) && row.moed.Equals(exam.moed)
                            select row).FirstOrDefault();

                    /* if exam exsist update */
                    if (examInDb != null)
                    {
                        examInDb.courseId = exam.courseId;
                        examInDb.courseName = exam.courseName;
                        examInDb.moed = exam.moed;
                        examInDb.date = exam.date;
                        examInDb.startTime = exam.startTime;
                        examInDb.endTime = exam.endTime;
                        examInDb.className = exam.className;

                        exmaDb.SaveChanges();
                    }
                    /*else insert new exam */
                    else
                    {
                        exmaDb.Exams.Add(exam);
                        exmaDb.SaveChanges();
                    }

                    TempData["goodMsg"] = "Inserted\\updated exam";
                    /* redirect with succsees message */
                    return View("ManageExams", new Exam());
                }
            }
            else
            {
                return View("ManageExams", exam);
            }
        }

        public ActionResult CoursesStudentsGrades()
        {
            /* Show all data */
            /* check if user had logged in */
            if (Session["ID"] == null || Session["password"] == null || Session["type"] == null)
            {
                return RedirectToAction("Login", "Login");
            }
            /* check if user is lecturer type */
            else if (!Session["type"].Equals("Faculty"))
            {
                return RedirectToAction("Home", Session["type"].ToString());
            }

            CoursesStudentsGrades viewobj = new CoursesStudentsGrades();
            viewobj.courses = new List<Course>();
            viewobj.users = new List<User>();
            viewobj.grades = new List<Grade>();

            using (CoursesDal coursesDb = new CoursesDal())
            using (UsersDal usersDb = new UsersDal())
            using (GradesDal gradesDb = new GradesDal())
            {
                /* get all courses */
                var courses =
                            (from row in coursesDb.Courses
                             orderby row.courseId
                             select row).ToList();

                foreach (Course x in courses)
                {
                    viewobj.courses.Add(x);
                }

                /* get all students */
                var users =
                            (from row in usersDb.Users
                             where row.type.Equals("Student")
                             select row).ToList();
                foreach (User x in users)
                {
                    viewobj.users.Add(x);
                }

                /* get all grades */
                var grades =
                            (from row in gradesDb.Grades
                             select row);

                foreach (Grade x in grades)
                {
                    viewobj.grades.Add(x);
                }
                return View(viewobj);
            }
        }

    }

}