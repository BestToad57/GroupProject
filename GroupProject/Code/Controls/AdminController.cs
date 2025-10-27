using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lab3GroupProject.Service;
using GroupProject.Code.Models;
using Microsoft.AspNetCore.Identity;
using Lab3GroupProject.Code.Data;

namespace Lab3GroupProject.Code.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PodcastService _podcastService;
        private readonly EpsiodeService _episodeService;
        private readonly SubscriptionService _subscriptionService;
        private readonly CommentService _commentService;

        public AdminController(
            ApplicationDbContext dbContext, 
            UserManager<ApplicationUser> userManager,
            PodcastService podcastService,
            EpsiodeService episodeService,
            SubscriptionService subscriptionService,
            CommentService commentService)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _podcastService = podcastService;
            _episodeService = episodeService;
            _subscriptionService = subscriptionService;
            _commentService = commentService;
        }

        public IActionResult Dashboard()
        {
            var allUsers = _userManager.Users.ToList();
            var allPodcasts = _podcastService.GetAllPodcasts();
            var allEpisodes = _episodeService.GetAllEpisodes();
            var allSubscriptions = _subscriptionService.GetAllSubscriptions();

            ViewBag.TotalUsers = allUsers.Count;
            ViewBag.TotalPodcasts = allPodcasts.Count();
            ViewBag.TotalEpisodes = allEpisodes.Count();
            ViewBag.TotalSubscriptions = allSubscriptions.Count();
            ViewBag.AdminCount = allUsers.Count(u => u.Role == UserRole.Admin);
            ViewBag.PodcasterCount = allUsers.Count(u => u.Role == UserRole.Podcaster);
            ViewBag.ListenerCount = allUsers.Count(u => u.Role == UserRole.Listener);
            ViewBag.RecentPodcasts = allPodcasts.OrderByDescending(p => p.CreatedDate).Take(5);
            ViewBag.RecentEpisodes = allEpisodes.OrderByDescending(e => e.ReleaseDate).Take(5);

            return View();
        }

        public IActionResult Users()
        {
            var allUsers = _userManager.Users.ToList();
            return View(allUsers);
        }

        public IActionResult Podcasts()
        {
            var allPodcasts = _podcastService.GetAllPodcasts();
            var allEpisodes = _episodeService.GetAllEpisodes();
            var podcastEpisodeCounts = allPodcasts.ToDictionary(
                p => p.PodcastID,
                p => allEpisodes.Count(e => e.PodcastID == p.PodcastID)
            );
            
            ViewBag.EpisodeCounts = podcastEpisodeCounts;
            return View(allPodcasts);
        }

        public IActionResult Episodes()
        {
            var allEpisodes = _episodeService.GetAllEpisodes();
            var allPodcasts = _podcastService.GetAllPodcasts();
            
            ViewBag.AllPodcasts = allPodcasts;
            return View(allEpisodes);
        }

        public IActionResult ModerateComments()
        {
            var allComments = _commentService.GetAllComments();
            var allEpisodes = _episodeService.GetAllEpisodes();
            var allPodcasts = _podcastService.GetAllPodcasts();

            var episodeComments = allComments.GroupBy(c => c.EpisodeID).ToList();

            ViewBag.AllEpisodes = allEpisodes;
            ViewBag.AllPodcasts = allPodcasts;
            ViewBag.TotalComments = allComments.Count;
            ViewBag.EpisodesWithComments = episodeComments.Count;

            return View(allComments.OrderByDescending(c => c.CommentDate));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                if (user.Email == User.Identity.Name)
                {
                    TempData["ErrorMessage"] = "You cannot delete your own account.";
                    return RedirectToAction("Users");
                }

                await _userManager.DeleteAsync(user);
                TempData["SuccessMessage"] = $"User {user.Email} deleted successfully.";
            }
            
            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserRole(string userId, UserRole newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                if (user.Email == User.Identity.Name)
                {
                    TempData["ErrorMessage"] = "You cannot change your own role.";
                    return RedirectToAction("Users");
                }

                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                user.Role = newRole;
                await _userManager.UpdateAsync(user);
                await _userManager.AddToRoleAsync(user, newRole.ToString());

                TempData["SuccessMessage"] = $"User role changed to {newRole}.";
            }
            
            return RedirectToAction("Users");
        }
    }
}