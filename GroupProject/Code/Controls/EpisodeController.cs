using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lab3GroupProject.Service;
using GroupProject.Code.Models;
using GroupProject.Code.Services;

namespace Lab3GroupProject.Code.Controllers
{
    [Authorize]
    public class EpisodeController : Controller
    {
        private readonly EpsiodeService _episodeService;
        private readonly PodcastService _podcastService;
        private readonly S3UploadService _s3Service;
        private readonly CommentService _commentService;

        public EpisodeController(
            EpsiodeService episodeService, 
            PodcastService podcastService,
            S3UploadService s3Service,
            CommentService commentService)
        {
            this._episodeService = episodeService;
            this._podcastService = podcastService;
            this._s3Service = s3Service;
            this._commentService = commentService;
        }

        public IActionResult Browse()
        {
            var podcasts = _podcastService.GetAllPodcasts();
            var podcastEpisodesDict = new Dictionary<int, List<Episode>>();
            var allEpisodes = _episodeService.GetAllEpisodes();
            
            foreach (var podcast in podcasts)
            {
                var podcastEpisodes = allEpisodes.Where(e => e.PodcastID == podcast.PodcastID).ToList();
                podcastEpisodesDict[podcast.PodcastID] = podcastEpisodes;
            }
            
            ViewBag.PodcastEpisodes = podcastEpisodesDict;
            return View(podcasts);
        }

        [HttpGet]
        public IActionResult Search(string searchTerm, string searchType = "all")
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                TempData["InfoMessage"] = "Please enter a search term.";
                return RedirectToAction("Browse");
            }

            IEnumerable<Episode> results;
            
            switch (searchType.ToLower())
            {
                case "topic":
                    results = _episodeService.SearchEpisodesByTopic(searchTerm);
                    ViewBag.SearchType = "Topic";
                    break;
                case "host":
                    results = _episodeService.SearchEpisodesByHost(searchTerm);
                    ViewBag.SearchType = "Host/Creator";
                    break;
                default:
                    results = _episodeService.SearchEpisodes(searchTerm);
                    ViewBag.SearchType = "All";
                    break;
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.AllPodcasts = _podcastService.GetAllPodcasts();
            
            return View(results);
        }

        [HttpGet]
        public IActionResult Popular(int count = 20)
        {
            var popularEpisodes = _episodeService.GetMostPopularEpisodes(count);
            ViewBag.AllPodcasts = _podcastService.GetAllPodcasts();
            return View(popularEpisodes);
        }

        [Authorize(Roles = "Podcaster,Admin")]
        public IActionResult Index()
        {
            var currentUserEmail = User.Identity.Name;
            var allEpisodes = _episodeService.GetAllEpisodes();
            var allPodcasts = _podcastService.GetAllPodcasts();
            
            IEnumerable<Episode> episodes;
            if (User.IsInRole("Admin"))
            {
                episodes = allEpisodes;
                ViewBag.IsAdmin = true;
            }
            else
            {
                var userPodcastIds = allPodcasts
                    .Where(p => p.CreatorID == currentUserEmail)
                    .Select(p => p.PodcastID)
                    .ToList();
                
                episodes = allEpisodes.Where(e => userPodcastIds.Contains(e.PodcastID));
                ViewBag.IsAdmin = false;
            }
            
            ViewBag.AllPodcasts = allPodcasts;
            return View(episodes);
        }

        [Authorize(Roles = "Podcaster,Admin")]
        public IActionResult Create()
        {
            var currentUserEmail = User.Identity.Name;
            var allPodcasts = _podcastService.GetAllPodcasts();
            
            if (User.IsInRole("Admin"))
            {
                ViewBag.Podcasts = allPodcasts;
            }
            else
            {
                ViewBag.Podcasts = allPodcasts.Where(p => p.CreatorID == currentUserEmail);
            }
            
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Podcaster,Admin")]
        public async Task<IActionResult> Create(Episode episode, IFormFile audioFile)
        {
            if (ModelState.IsValid)
            {
                var podcast = _podcastService.GetPodcastById(episode.PodcastID);
                if (!User.IsInRole("Admin") && podcast?.CreatorID != User.Identity.Name)
                {
                    TempData["ErrorMessage"] = "You can only add episodes to your own podcasts.";
                    return RedirectToAction("Index");
                }
                
                if (audioFile != null && audioFile.Length > 0)
                {
                    try
                    {
                        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                        var safeFileName = Path.GetFileNameWithoutExtension(audioFile.FileName)
                            .Replace(" ", "_")
                            .Replace("-", "_");
                        var extension = Path.GetExtension(audioFile.FileName);
                        var uniqueFileName = $"{safeFileName}_{timestamp}{extension}";
                        
                        var tempPath = Path.Combine(Path.GetTempPath(), uniqueFileName);
                        using (var stream = new FileStream(tempPath, FileMode.Create))
                        {
                            await audioFile.CopyToAsync(stream);
                        }
                        
                        var s3Key = $"podcasts/episodes/{uniqueFileName}";
                        var s3Url = await _s3Service.UploadFileAsync(tempPath, s3Key);
                        System.IO.File.Delete(tempPath);
                        episode.AudioFileURL = s3Url;
                        
                        TempData["SuccessMessage"] = "Episode created successfully with audio file!";
                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = $"Error uploading audio file: {ex.Message}";
                        ViewBag.Podcasts = _podcastService.GetAllPodcasts()
                            .Where(p => User.IsInRole("Admin") || p.CreatorID == User.Identity.Name);
                        return View(episode);
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Please select an audio file to upload.";
                    ViewBag.Podcasts = _podcastService.GetAllPodcasts()
                        .Where(p => User.IsInRole("Admin") || p.CreatorID == User.Identity.Name);
                    return View(episode);
                }
                
                _episodeService.CreateEpisode(episode);
                return RedirectToAction("Index");
            }
            
            ViewBag.Podcasts = _podcastService.GetAllPodcasts()
                .Where(p => User.IsInRole("Admin") || p.CreatorID == User.Identity.Name);
            return View(episode);
        }

        [Authorize(Roles = "Podcaster,Admin")]
        public IActionResult Edit(int id)
        {
            var episode = _episodeService.GetEpisodeById(id);
            if (episode == null)
            {
                return NotFound();
            }
            
            var podcast = _podcastService.GetPodcastById(episode.PodcastID);
            if (!User.IsInRole("Admin") && podcast?.CreatorID != User.Identity.Name)
            {
                TempData["ErrorMessage"] = "You can only edit episodes from your own podcasts.";
                return RedirectToAction("Index");
            }
            
            ViewBag.Podcasts = _podcastService.GetAllPodcasts();
            return View(episode);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Podcaster,Admin")]
        public async Task<IActionResult> Edit(Episode episode, IFormFile? audioFile)
        {
            if (ModelState.IsValid)
            {
                var podcast = _podcastService.GetPodcastById(episode.PodcastID);
                if (!User.IsInRole("Admin") && podcast?.CreatorID != User.Identity.Name)
                {
                    TempData["ErrorMessage"] = "You can only edit episodes from your own podcasts.";
                    return RedirectToAction("Index");
                }

                if (audioFile != null && audioFile.Length > 0)
                {
                    try
                    {
                        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                        var safeFileName = Path.GetFileNameWithoutExtension(audioFile.FileName)
                            .Replace(" ", "_")
                            .Replace("-", "_");
                        var extension = Path.GetExtension(audioFile.FileName);
                        var uniqueFileName = $"{safeFileName}_{timestamp}{extension}";
                        
                        var tempPath = Path.Combine(Path.GetTempPath(), uniqueFileName);
                        using (var stream = new FileStream(tempPath, FileMode.Create))
                        {
                            await audioFile.CopyToAsync(stream);
                        }
                        
                        var s3Key = $"podcasts/episodes/{uniqueFileName}";
                        var s3Url = await _s3Service.UploadFileAsync(tempPath, s3Key);
                        System.IO.File.Delete(tempPath);
                        episode.AudioFileURL = s3Url;
                        
                        TempData["SuccessMessage"] = "Episode and audio file updated successfully!";
                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = $"Error uploading audio file: {ex.Message}";
                        ViewBag.Podcasts = _podcastService.GetAllPodcasts();
                        return View(episode);
                    }
                }
                else
                {
                    TempData["SuccessMessage"] = "Episode updated successfully!";
                }
                
                _episodeService.UpdateEpisode(episode);
                return RedirectToAction("Index");
            }
            ViewBag.Podcasts = _podcastService.GetAllPodcasts();
            return View(episode);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Podcaster,Admin")]
        public IActionResult Delete(int id)
        {
            var episode = _episodeService.GetEpisodeById(id);
            if (episode == null)
            {
                return NotFound();
            }
            
            var podcast = _podcastService.GetPodcastById(episode.PodcastID);
            if (!User.IsInRole("Admin") && podcast?.CreatorID != User.Identity.Name)
            {
                TempData["ErrorMessage"] = "You can only delete episodes from your own podcasts.";
                return RedirectToAction("Index");
            }
            
            _episodeService.DeleteEpisode(id);
            TempData["SuccessMessage"] = "Episode deleted successfully!";
            return RedirectToAction("Index");
        }

        public IActionResult Details(int id)
        {
            var episode = _episodeService.GetEpisodeById(id);
            if (episode == null)
            {
                return NotFound();
            }

            _episodeService.IncrementViews(id);
            
            var podcast = _podcastService.GetPodcastById(episode.PodcastID);
            ViewBag.Podcast = podcast;
            
            var comments = _commentService.GetCommentsByEpisodeId(id);
            ViewBag.Comments = comments;
            
            return View(episode);
        }

        [HttpPost]
        public IActionResult TrackPlay(int episodeId)
        {
            _episodeService.IncrementPlayCount(episodeId);
            return Json(new { success = true });
        }

        [HttpPost]
        [Route("Episode/TrackView/{episodeId}")]
        public IActionResult TrackView(int episodeId)
        {
            try
            {
                _episodeService.IncrementViews(episodeId);
                var episode = _episodeService.GetEpisodeById(episodeId);
                var viewCount = episode?.NumberOfViews ?? 0;
                
                return Json(new { success = true, viewCount = viewCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}