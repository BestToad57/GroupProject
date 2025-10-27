using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lab3GroupProject.Service;
using GroupProject.Code.Models;

namespace Lab3GroupProject.Code.Controllers
{
    [Authorize]
    public class PodcastController : Controller
    {
        private readonly PodcastService _podcastService;
        public PodcastController(PodcastService podcastService)
        {
            this._podcastService = podcastService;
        }

        [Authorize(Roles = "Podcaster,Admin")]
        public IActionResult Index()
        {
            var currentUserEmail = User.Identity.Name;
            var allPodcasts = _podcastService.GetAllPodcasts();
            
            if (User.IsInRole("Admin"))
            {
                ViewBag.IsAdmin = true;
                return View(allPodcasts);
            }
            else
            {
                ViewBag.IsAdmin = false;
                var userPodcasts = allPodcasts.Where(p => p.CreatorID == currentUserEmail).ToList();
                return View(userPodcasts);
            }
        }

        [Authorize(Roles = "Podcaster,Admin")]
        public IActionResult Create()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Podcaster,Admin")]
        public IActionResult Create(Podcast podcast)
        {
            if (ModelState.IsValid)
            {
                podcast.CreatorID = User.Identity.Name;
                podcast.CreatedDate = DateTime.Now;
                
                _podcastService.CreatePodcast(podcast);
                TempData["SuccessMessage"] = "Podcast created successfully!";
                return RedirectToAction("Index");
            }
            return View(podcast);
        }

        [Authorize(Roles = "Podcaster,Admin")]
        public IActionResult Edit(int id)
        {
            var podcast = _podcastService.GetPodcastById(id);
            if (podcast == null)
            {
                return NotFound();
            }
            
            if (!User.IsInRole("Admin") && podcast.CreatorID != User.Identity.Name)
            {
                TempData["ErrorMessage"] = "You can only edit your own podcasts.";
                return RedirectToAction("Index");
            }
            
            return View(podcast);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Podcaster,Admin")]
        public IActionResult Edit(Podcast podcast)
        {
            if (ModelState.IsValid)
            {
                var existingPodcast = _podcastService.GetPodcastById(podcast.PodcastID);
                
                if (!User.IsInRole("Admin") && existingPodcast?.CreatorID != User.Identity.Name)
                {
                    TempData["ErrorMessage"] = "You can only edit your own podcasts.";
                    return RedirectToAction("Index");
                }
                
                podcast.CreatorID = existingPodcast.CreatorID;
                podcast.CreatedDate = existingPodcast.CreatedDate;
                
                _podcastService.UpdatePodcast(podcast);
                TempData["SuccessMessage"] = "Podcast updated successfully!";
                return RedirectToAction("Index");
            }
            return View(podcast);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Podcaster,Admin")]
        public IActionResult Delete(int id)
        {
            var podcast = _podcastService.GetPodcastById(id);
            
            if (!User.IsInRole("Admin") && podcast?.CreatorID != User.Identity.Name)
            {
                TempData["ErrorMessage"] = "You can only delete your own podcasts.";
                return RedirectToAction("Index");
            }
            
            _podcastService.DeletePodcast(id);
            TempData["SuccessMessage"] = "Podcast deleted successfully!";
            return RedirectToAction("Index");
        }

        public IActionResult Details(int id)
        {
            var podcast = _podcastService.GetPodcastById(id);
            if (podcast == null)
            {
                return NotFound();
            }
            return View(podcast);
        }
    }
}