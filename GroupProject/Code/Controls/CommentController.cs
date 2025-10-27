using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lab3GroupProject.Service;
using GroupProject.Code.Models;

namespace Lab3GroupProject.Code.Controllers
{
    [Authorize]
    public class CommentController : Controller
    {
        private readonly CommentService _commentService;
        private readonly EpsiodeService _episodeService;
        private readonly PodcastService _podcastService;

        public CommentController(
            CommentService commentService,
            EpsiodeService episodeService,
            PodcastService podcastService)
        {
            _commentService = commentService;
            _episodeService = episodeService;
            _podcastService = podcastService;
        }

        [HttpGet]
        public IActionResult Index(int episodeId)
        {
            var episode = _episodeService.GetEpisodeById(episodeId);
            if (episode == null)
            {
                return NotFound();
            }

            var podcast = _podcastService.GetPodcastById(episode.PodcastID);
            var comments = _commentService.GetCommentsByEpisodeId(episodeId);

            ViewBag.Episode = episode;
            ViewBag.Podcast = podcast;
            ViewBag.CurrentUserId = User.Identity?.Name;
            ViewBag.IsPodcaster = User.IsInRole("Podcaster") && podcast?.CreatorID == User.Identity?.Name;
            ViewBag.IsAdmin = User.IsInRole("Admin");

            return View(comments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(int episodeId, string commentText)
        {
            if (string.IsNullOrWhiteSpace(commentText))
            {
                TempData["ErrorMessage"] = "Comment cannot be empty.";
                return RedirectToAction("Index", new { episodeId });
            }

            var comment = new Comment
            {
                EpisodeID = episodeId,
                CommentText = commentText,
                UserID = User.Identity?.Name ?? "Anonymous",
                CommentDate = DateTime.Now
            };

            _commentService.AddComment(comment);
            TempData["SuccessMessage"] = "Comment added successfully!";

            return RedirectToAction("Index", new { episodeId });
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var comment = _commentService.GetCommentById(id);
            if (comment == null)
            {
                return NotFound();
            }

            if (comment.UserID != User.Identity?.Name)
            {
                TempData["ErrorMessage"] = "You can only edit your own comments.";
                return RedirectToAction("Index", new { episodeId = comment.EpisodeID });
            }

            var hoursSincePosted = (DateTime.Now - comment.CommentDate).TotalHours;
            if (hoursSincePosted > 24)
            {
                TempData["ErrorMessage"] = $"Comments can only be edited within 24 hours of posting. This comment was posted {Math.Floor(hoursSincePosted)} hours ago.";
                return RedirectToAction("Index", new { episodeId = comment.EpisodeID });
            }

            var episode = _episodeService.GetEpisodeById(comment.EpisodeID);
            ViewBag.Episode = episode;
            ViewBag.HoursRemaining = 24 - hoursSincePosted;

            return View(comment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Comment comment)
        {
            var existingComment = _commentService.GetCommentById(comment.CommentID);
            if (existingComment == null)
            {
                return NotFound();
            }

            if (existingComment.UserID != User.Identity?.Name)
            {
                TempData["ErrorMessage"] = "You can only edit your own comments.";
                return RedirectToAction("Index", new { episodeId = existingComment.EpisodeID });
            }

            var hoursSincePosted = (DateTime.Now - existingComment.CommentDate).TotalHours;
            if (hoursSincePosted > 24)
            {
                TempData["ErrorMessage"] = $"Comments can only be edited within 24 hours of posting. This comment was posted {Math.Floor(hoursSincePosted)} hours ago.";
                return RedirectToAction("Index", new { episodeId = existingComment.EpisodeID });
            }

            if (ModelState.IsValid)
            {
                existingComment.CommentText = comment.CommentText;
                _commentService.UpdateComment(existingComment);
                TempData["SuccessMessage"] = "Comment updated successfully!";
                return RedirectToAction("Index", new { episodeId = existingComment.EpisodeID });
            }

            return View(comment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id, int episodeId)
        {
            var comment = _commentService.GetCommentById(id);
            if (comment == null)
            {
                TempData["ErrorMessage"] = "Comment not found.";
                return RedirectToAction("Browse", "Episode");
            }

            bool canDelete = false;
            var currentUserId = User.Identity?.Name;

            if (User.IsInRole("Admin"))
            {
                canDelete = true;
            }
            else if (comment.UserID == currentUserId)
            {
                canDelete = true;
            }
            else if (User.IsInRole("Podcaster"))
            {
                var episode = _episodeService.GetEpisodeById(comment.EpisodeID);
                if (episode != null)
                {
                    var podcast = _podcastService.GetPodcastById(episode.PodcastID);
                    if (podcast?.CreatorID == currentUserId)
                    {
                        canDelete = true;
                    }
                }
            }

            if (!canDelete)
            {
                TempData["ErrorMessage"] = "You don't have permission to delete this comment.";
                return RedirectToAction("Index", new { episodeId = comment.EpisodeID });
            }

            _commentService.DeleteComment(id);
            TempData["SuccessMessage"] = "Comment deleted successfully!";
            
            if (User.IsInRole("Admin") && Request.Headers["Referer"].ToString().Contains("ModerateComments"))
            {
                return RedirectToAction("ModerateComments", "Admin");
            }
            
            return RedirectToAction("Index", new { episodeId });
        }

        [HttpGet]
        [Authorize(Roles = "Podcaster")]
        public IActionResult MyComments()
        {
            var currentUserId = User.Identity?.Name;
            var myPodcasts = _podcastService.GetAllPodcasts()
                .Where(p => p.CreatorID == currentUserId);

            var myEpisodes = _episodeService.GetAllEpisodes()
                .Where(e => myPodcasts.Any(p => p.PodcastID == e.PodcastID))
                .ToList();

            var allComments = _commentService.GetAllComments();
            var myEpisodeComments = allComments
                .Where(c => myEpisodes.Any(e => e.EpisodeID == c.EpisodeID))
                .OrderByDescending(c => c.CommentDate)
                .ToList();

            ViewBag.Episodes = myEpisodes;
            ViewBag.Podcasts = myPodcasts.ToList();

            return View(myEpisodeComments);
        }
    }
}