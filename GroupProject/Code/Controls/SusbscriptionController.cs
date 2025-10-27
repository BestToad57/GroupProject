using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lab3GroupProject.Service;
using GroupProject.Code.Models;

namespace Lab3GroupProject.Code.Controllers
{
    [Authorize] // All authenticated users can manage their subscriptions
    public class SubscriptionController : Controller
    {
        private readonly SubscriptionService _subscriptionService;
        private readonly PodcastService _podcastService;
        private readonly EpsiodeService _episodeService;

        public SubscriptionController(
            SubscriptionService subscriptionService,
            PodcastService podcastService,
            EpsiodeService episodeService)
        {
            this._subscriptionService = subscriptionService;
            this._podcastService = podcastService;
            this._episodeService = episodeService;
        }

        // GET: Show user's subscribed podcasts
        public IActionResult Index()
        {
            var currentUserEmail = User.Identity.Name;
            
            // Get user's subscriptions
            var userSubscriptions = _subscriptionService.GetAllSubscriptions()
                .Where(s => s.UserID == currentUserEmail)
                .ToList();
            
            // Get the podcasts the user is subscribed to
            var subscribedPodcasts = new List<Podcast>();
            foreach (var subscription in userSubscriptions)
            {
                var podcast = _podcastService.GetPodcastById(subscription.PodcastID);
                if (podcast != null)
                {
                    subscribedPodcasts.Add(podcast);
                }
            }
            
            // Get episodes for each subscribed podcast
            var allEpisodes = _episodeService.GetAllEpisodes();
            var podcastEpisodesDict = new Dictionary<int, List<Episode>>();
            
            foreach (var podcast in subscribedPodcasts)
            {
                var podcastEpisodes = allEpisodes.Where(e => e.PodcastID == podcast.PodcastID).ToList();
                podcastEpisodesDict[podcast.PodcastID] = podcastEpisodes;
            }
            
            ViewBag.PodcastEpisodes = podcastEpisodesDict;
            ViewBag.Subscriptions = userSubscriptions;
            
            return View(subscribedPodcasts);
        }

        // POST: Subscribe to a podcast
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(int PodcastID)
        {
            var currentUserEmail = User.Identity.Name;
            
            // Check if already subscribed
            var existingSubscription = _subscriptionService.GetAllSubscriptions()
                .FirstOrDefault(s => s.UserID == currentUserEmail && s.PodcastID == PodcastID);
            
            if (existingSubscription == null)
            {
                // Create new subscription
                var subscription = new Subscription
                {
                    UserID = currentUserEmail,
                    PodcastID = PodcastID,
                    SubscriptionDate = DateTime.Now
                };
                
                _subscriptionService.CreateSubscription(subscription);
                TempData["SuccessMessage"] = "Successfully subscribed to podcast!";
            }
            else
            {
                TempData["InfoMessage"] = "You are already subscribed to this podcast.";
            }
            
            // Redirect back to browse page
            return RedirectToAction("Browse", "Episode");
        }

        // POST: Unsubscribe from a podcast
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Unsubscribe(int podcastId)
        {
            var currentUserEmail = User.Identity.Name;
            
            var subscription = _subscriptionService.GetAllSubscriptions()
                .FirstOrDefault(s => s.UserID == currentUserEmail && s.PodcastID == podcastId);
            
            if (subscription != null)
            {
                _subscriptionService.DeleteSubscription(subscription.SubscriptionID);
                TempData["SuccessMessage"] = "Successfully unsubscribed from podcast.";
            }
            
            return RedirectToAction("Index");
        }
        
        // GET: Check if user is subscribed to a podcast (for AJAX calls)
        [HttpGet]
        public IActionResult IsSubscribed(int podcastId)
        {
            var currentUserEmail = User.Identity.Name;
            
            var isSubscribed = _subscriptionService.GetAllSubscriptions()
                .Any(s => s.UserID == currentUserEmail && s.PodcastID == podcastId);
            
            return Json(new { isSubscribed = isSubscribed });
        }
    }
}