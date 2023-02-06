using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WeddingPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WeddingPlanner.Controllers;

public class HomeController : Controller
{
    private MyContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger, MyContext context)
    {
        _context = context;
        _logger = logger;
    }

    public IActionResult Index()
    {
        List<User> AllUsers = _context.Users.ToList();
        ViewBag.AllUsers = AllUsers;
        return View();
    }

    // REGISTER USER

    [HttpPost("users/create")]
    public IActionResult CreateUser(User newUser)
    {
        if (ModelState.IsValid)
        {
            // Hash our password
            PasswordHasher<User> Hasher = new PasswordHasher<User>();
            newUser.Password = Hasher.HashPassword(newUser, newUser.Password);
            _context.Add(newUser);
            _context.SaveChanges();
            HttpContext.Session.SetInt32("UserId", newUser.UserId);
            HttpContext.Session.SetString("Username", newUser.FirstName + " " + newUser.LastName);
            return RedirectToAction("ShowAllWeddings");
        }
        List<User> AllUsers = _context.Users.ToList();
        ViewBag.AllUsers = AllUsers;
        return View("Index");

    }



    // LOGIN USER
    [HttpPost("users/login")]
    public IActionResult LoginUser(LoginUser loginUser)
    {
        if (ModelState.IsValid)
        {
            User? userInDb = _context.Users.FirstOrDefault(u => u.Email == loginUser.LEmail);
            if (userInDb == null)
            {
                ModelState.AddModelError("LEmail", "Invalid Email/Password");
                List<User> AllUsers = _context.Users.ToList();
                ViewBag.AllUsers = AllUsers;
                return View("Index");
            }
            // Verify the password matches what's in the database
            PasswordHasher<LoginUser> hasher = new PasswordHasher<LoginUser>();

            var result = hasher.VerifyHashedPassword(loginUser, userInDb.Password, loginUser.LPassword);
            if (result == 0)
            {
                ModelState.AddModelError("LEmail", "Invalid Email/Password");
                List<User> AllUsers = _context.Users.ToList();
                ViewBag.AllUsers = AllUsers;
                return View("Index");
            }
            else
            {
                HttpContext.Session.SetInt32("UserId", userInDb.UserId);
                HttpContext.Session.SetString("Username", userInDb.FirstName + " " + userInDb.LastName);
                return RedirectToAction("ShowAllWeddings");
            }
            // return RedirectToAction("Success");
        }
        else
        {
            List<User> AllUsers = _context.Users.ToList();
            ViewBag.AllUsers = AllUsers;
            return View("Index");
        }
    }

    // LOGOUT USER
    [HttpPost("users/logout")]
    public IActionResult LogoutUser()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index");
    }

    // VIEW ALL AND VIEW ONE WEDDING

    [SessionCheck]
    [HttpGet("weddings")]
    public IActionResult ShowAllWeddings()
    {
        int? SessionId = HttpContext.Session.GetInt32("UserId");
        if (SessionId == null)
        {
            return RedirectToAction("Index");
        }
        List<Wedding> Rsvped = _context.Weddings
            .Include(w => w.Guests)
            .ThenInclude(rsvp => rsvp.User)
            .ToList();
        ViewBag.Rsvped = Rsvped;
        return View("ShowAllWeddings");
    }

    // SHOW ONE WEDDING
    [HttpGet("weddings/{weddingId}")]
    public IActionResult ShowSingleWedding(int weddingId)
    {
        int? SessionId = HttpContext.Session.GetInt32("UserId");
        if (SessionId == null)
        {
            return RedirectToAction("Index");
        }
        System.Console.WriteLine($" SHOW SINGLE WEDDING ID: =====> {weddingId}");
        List<Wedding> Rsvped = _context.Weddings
            .Include(w => w.Guests)
            .ThenInclude(rsvp => rsvp.User)
            .ToList();
        Wedding? ThisWedding = _context.Weddings.FirstOrDefault(a => a.WeddingId == weddingId);
        List<User> AllUsers = _context.Users
            .Include(a => a.RsvpedWeddings)
            .ThenInclude(a => a.Wedding)
            .ToList();
        MyViewModel MyModel = new MyViewModel();
        MyModel.AllWeddings = Rsvped;
        ViewBag.ThisWedding = ThisWedding;
        ViewBag.AllUsers = AllUsers;
        return View("ShowSingleWedding", MyModel);
    }


    // PLAN WEDDING
    [HttpGet("weddings/new")]
    public IActionResult PlanWedding()
    {
        int? SessionId = HttpContext.Session.GetInt32("UserId");
        if (SessionId == null)
        {
            return RedirectToAction("Index");
        }
        return View();
    }

    [HttpPost("weddings/create")]
    public IActionResult CreateWedding(Wedding newWedding)
    {
        if (ModelState.IsValid)
        {
            _context.Add(newWedding);
            _context.SaveChanges();
            return RedirectToAction("ShowAllWeddings");
        }
        return View("PlanWedding");
    }

    // EDIT AND UPDATE WEDDING

    [HttpGet("wedding/{weddingId}/edit")]
    public IActionResult EditWedding(int weddingId)
    {
        System.Console.WriteLine($"THIS IS WEDDING ID INSIDE EDIT WEDDING: ======> {weddingId}");
        Wedding? WeddingToEdit = _context.Weddings.FirstOrDefault(a => a.WeddingId == weddingId);

        return View("EditWedding", WeddingToEdit);
    }

    [HttpPost("wedding/{weddingId}/update")]
    public IActionResult UpdateWedding(int weddingId, Wedding updateWedding)
    {
        System.Console.WriteLine($"WEDDING ID IN UPDATE WEDDING: ====> {weddingId}");
        Wedding? WeddingToUpdate = _context.Weddings.FirstOrDefault(a => a.WeddingId == weddingId);
        if (WeddingToUpdate == null)
        {
            return RedirectToAction("ShowAllWeddings");
        }
        if (ModelState.IsValid)
        {
            WeddingToUpdate.WedderOne = updateWedding.WedderOne;
            WeddingToUpdate.WedderTwo = updateWedding.WedderTwo;
            WeddingToUpdate.Date = updateWedding.Date;
            WeddingToUpdate.Address = updateWedding.Address;
            WeddingToUpdate.UpdatedAt = DateTime.Now;
            _context.SaveChanges();
            return ShowSingleWedding(updateWedding.WeddingId);
        }
        else
        {

            return View("EditWedding", WeddingToUpdate);
        }
    }



    [HttpPost("weddings/{weddingId}/delete")]
    public IActionResult DestroyWedding(int weddingId)
    {
        Wedding? WeddingToDestroy = _context.Weddings.SingleOrDefault(w => w.WeddingId == weddingId);
        _context.Weddings.Remove(WeddingToDestroy);
        _context.SaveChanges();
        return RedirectToAction("ShowAllWeddings");
    }

    [HttpPost("weddings/rsvp")]
    public IActionResult Rsvp(Rsvp newRsvp)
    {
        System.Console.WriteLine($"userId: {newRsvp.UserId} \nWeddingId: {newRsvp.WeddingId}");
        // return RedirectToAction("ShowAllWeddings");
        Rsvp? RsvpExists = _context.Rsvps.FirstOrDefault(w => w.UserId == newRsvp.UserId && w.WeddingId == newRsvp.WeddingId);

        if (ModelState.IsValid)
        {

            if (RsvpExists == null)
            {
                _context.Add(newRsvp);
                _context.SaveChanges();
                return RedirectToAction("ShowAllWeddings");
            }
            else
            {
                return View("RSVPError");
            }

        }
        return View("ShowAllWeddings");
    }

    [HttpPost("wedding/unrsvp/{rsvpId}")]
    public IActionResult UnRsvp(int rsvpId)
    {
        Rsvp? RsvpToDestroy = _context.Rsvps.SingleOrDefault(r => r.RsvpId == rsvpId);
        System.Console.WriteLine(rsvpId);
        if (RsvpToDestroy != null)
        {
            _context.Rsvps.Remove(RsvpToDestroy);
            _context.SaveChanges();
            return RedirectToAction("ShowAllWeddings");
        }
        return View("ShowAllWeddings");

    }



    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}


public class SessionCheckAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        int? userId = context.HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
    }
}