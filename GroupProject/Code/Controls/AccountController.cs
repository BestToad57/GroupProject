using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lab3GroupProject.Service;
using GroupProject.Code.Models;
using Microsoft.AspNetCore.Identity;

namespace Lab3GroupProject.Code.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userService;
        private readonly PodcastService _podcastService;
        private readonly EpsiodeService _episodeService;
        private readonly SubscriptionService _subscriptionService;

        public AccountController(
            SignInManager<ApplicationUser> signInManager, 
            UserManager<ApplicationUser> userService,
            PodcastService podcastService,
            EpsiodeService episodeService,
            SubscriptionService subscriptionService)
        {
            _signInManager = signInManager;
            _userService = userService;
            _podcastService = podcastService;
            _episodeService = episodeService;
            _subscriptionService = subscriptionService;
        }

        [HttpGet]
        [Authorize]
        public IActionResult Profile()
        {
            var currentUserEmail = User.Identity.Name;
            var allPodcasts = _podcastService.GetAllPodcasts();
            var allEpisodes = _episodeService.GetAllEpisodes();
            var userPodcasts = allPodcasts.Where(p => p.CreatorID == currentUserEmail).ToList();
            var userPodcastIds = userPodcasts.Select(p => p.PodcastID).ToList();
            var userEpisodes = allEpisodes.Where(e => userPodcastIds.Contains(e.PodcastID)).ToList();
            var userSubscriptions = _subscriptionService.GetAllSubscriptions()
                .Where(s => s.UserID == currentUserEmail)
                .ToList();
            
            ViewBag.UserPodcasts = userPodcasts;
            ViewBag.UserEpisodes = userEpisodes;
            ViewBag.UserSubscriptions = userSubscriptions;
            ViewBag.SubscribedPodcasts = userSubscriptions
                .Select(s => _podcastService.GetPodcastById(s.PodcastID))
                .Where(p => p != null)
                .ToList();
            
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    if (Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser 
                { 
                    UserName = model.Email, 
                    Email = model.Email,
                    Role = model.Role
                };

                var result = await _userService.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    string roleName = model.Role.ToString();
                    await _userService.AddToRoleAsync(user, roleName);

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToLocal(returnUrl);
                }
                AddErrors(result);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}