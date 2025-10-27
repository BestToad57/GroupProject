using GroupProject.Code.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Lab3GroupProject.Code.Data;

namespace GroupProject.Code.Data
{
    public class DatabaseSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DatabaseSeeder(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            await _context.Database.EnsureCreatedAsync();
            await SeedRolesAsync();
            var users = await SeedUsersAsync();
            var podcasts = await SeedPodcastsAsync(users);
            var episodes = await SeedEpisodesAsync(podcasts);
            await SeedSubscriptionsAsync(users, podcasts);
            await SeedCommentsAsync(users, episodes);
            await _context.SaveChangesAsync();
        }

        public async Task ClearSeededDataAsync()
        {
            try
            {
                if (_context.Comments != null)
                {
                    _context.Comments.RemoveRange(_context.Comments);
                }
                
                _context.Subscriptions.RemoveRange(_context.Subscriptions);
                _context.Episodes.RemoveRange(_context.Episodes);
                _context.Podcasts.RemoveRange(_context.Podcasts);
                
                await _context.SaveChangesAsync();
                
                Console.WriteLine("? All seeded podcast data cleared!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? Error clearing seeded data: {ex.Message}");
            }
        }

        private async Task SeedRolesAsync()
        {
            string[] roles = { "Admin", "Podcaster", "Listener" };

            foreach (var roleName in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        private async Task<List<ApplicationUser>> SeedUsersAsync()
        {
            var users = new List<ApplicationUser>();

            var admin = await CreateUserIfNotExists(
                "admin@podcasthub.com",
                "Admin123!",
                "Admin User",
                UserRole.Admin
            );
            if (admin != null) users.Add(admin);

            var podcasters = new[]
            {
                ("john.podcaster@example.com", "John Podcast", "John123!"),
                ("sarah.creator@example.com", "Sarah Creator", "Sarah123!"),
                ("mike.radio@example.com", "Mike Radio", "Mike123!"),
                ("emma.voice@example.com", "Emma Voice", "Emma123!")
            };

            foreach (var (email, username, password) in podcasters)
            {
                var user = await CreateUserIfNotExists(email, password, username, UserRole.Podcaster);
                if (user != null) users.Add(user);
            }

            var listeners = new[]
            {
                ("alice.listener@example.com", "Alice L", "Alice123!"),
                ("bob.fan@example.com", "Bob Fan", "Bob123!"),
                ("carol.user@example.com", "Carol User", "Carol123!"),
                ("david.listener@example.com", "David L", "David123!"),
                ("eve.subscriber@example.com", "Eve Sub", "Eve123!")
            };

            foreach (var (email, username, password) in listeners)
            {
                var user = await CreateUserIfNotExists(email, password, username, UserRole.Listener);
                if (user != null) users.Add(user);
            }

            return users;
        }

        private async Task<ApplicationUser?> CreateUserIfNotExists(
            string email, 
            string password, 
            string username,
            UserRole role)
        {
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
                return existingUser;

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Role = role
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role.ToString());
                return user;
            }

            return null;
        }

        private async Task<List<Podcast>> SeedPodcastsAsync(List<ApplicationUser> users)
        {
            if (_context.Podcasts.Any())
                return await _context.Podcasts.ToListAsync();

            var podcasters = users.Where(u => u.Role == UserRole.Podcaster).ToList();
            
            var podcasts = new List<Podcast>
            {
                new Podcast
                {
                    Title = "Moonshot",
                    Description = "Peter Diamandis explores exponential technologies and moonshot thinking with the world's top entrepreneurs, innovators, and thought leaders.",
                    CreatorID = podcasters[0].Email,
                    CreatedDate = DateTime.Now.AddMonths(-6)
                },
                new Podcast
                {
                    Title = "TED Talks Daily",
                    Description = "Every weekday, TED Talks Daily brings you the latest talks in audio. Join host and journalist Elise Hu for thought-provoking ideas on every subject imaginable.",
                    CreatorID = podcasters[1].Email,
                    CreatedDate = DateTime.Now.AddMonths(-5)
                },
                new Podcast
                {
                    Title = "The Daily",
                    Description = "This is what the news should sound like. The biggest stories of our time, told by the best journalists in the world. Hosted by Michael Barbaro and Sabrina Tavernise.",
                    CreatorID = podcasters[2].Email,
                    CreatedDate = DateTime.Now.AddMonths(-4)
                },
                new Podcast
                {
                    Title = "Stuff You Should Know",
                    Description = "If you've ever wanted to know about champagne, satanism, the Stonewall Uprising, chaos theory, LSD, El Nino, true crime and Rosa Parks then look no further.",
                    CreatorID = podcasters[3].Email,
                    CreatedDate = DateTime.Now.AddMonths(-3)
                },
                new Podcast
                {
                    Title = "Freakonomics Radio",
                    Description = "Discover the hidden side of everything with host Stephen Dubner, co-author of the Freakonomics books. Each week, hear surprising conversations that explore the riddles of everyday life and the weird wrinkles of human nature.",
                    CreatorID = podcasters[0].Email,
                    CreatedDate = DateTime.Now.AddMonths(-2)
                }
            };

            await _context.Podcasts.AddRangeAsync(podcasts);
            await _context.SaveChangesAsync();

            return podcasts;
        }

        private async Task<List<Episode>> SeedEpisodesAsync(List<Podcast> podcasts)
        {
            if (_context.Episodes.Any())
                return await _context.Episodes.ToListAsync();

            var episodes = new List<Episode>();
            var random = new Random();

            foreach (var podcast in podcasts)
            {
                var realAudioUrls = GetRealPodcastAudioUrls(podcast.Title);
                var episodeTitles = GetRealEpisodeTitles(podcast.Title);
                
                int episodeCount = Math.Min(realAudioUrls.Length, episodeTitles.Length);
                
                for (int i = 0; i < episodeCount; i++)
                {
                    episodes.Add(new Episode
                    {
                        PodcastID = podcast.PodcastID,
                        Title = episodeTitles[i],
                        ReleaseDate = DateTime.Now.AddDays(-(episodeCount - i) * 7),
                        Duration = TimeSpan.FromMinutes(random.Next(20, 61)),
                        playCount = random.Next(1000, 50000),
                        NumberOfViews = random.Next(1500, 75000),
                        AudioFileURL = realAudioUrls[i]
                    });
                }
            }

            await _context.Episodes.AddRangeAsync(episodes);
            await _context.SaveChangesAsync();

            return episodes;
        }

        private string[] GetRealPodcastAudioUrls(string podcastTitle)
        {
            string bucketUrl = "https://podcasthub-audio.s3.amazonaws.com/podcasts";
            
            return podcastTitle switch
            {
                "Moonshot" => new[]
                {
                    $"{bucketUrl}/moonshot_episode_1.mp3",
                    $"{bucketUrl}/moonshot_episode_2.mp3",
                    $"{bucketUrl}/moonshot_episode_3.mp3",
                    $"{bucketUrl}/moonshot_episode_4.mp3",
                    $"{bucketUrl}/moonshot_episode_5.mp3"
                },
                "TED Talks Daily" => new[]
                {
                    $"{bucketUrl}/ted_episode_1.mp3",
                    $"{bucketUrl}/ted_episode_2.mp3",
                    $"{bucketUrl}/ted_episode_3.mp3",
                    $"{bucketUrl}/ted_episode_4.mp3",
                    $"{bucketUrl}/ted_episode_5.mp3"
                },
                "The Daily" => new[]
                {
                    $"{bucketUrl}/moonshot_episode_1.mp3",
                    $"{bucketUrl}/ted_episode_2.mp3",
                    $"{bucketUrl}/moonshot_episode_3.mp3"
                },
                "Stuff You Should Know" => new[]
                {
                    $"{bucketUrl}/ted_episode_1.mp3",
                    $"{bucketUrl}/moonshot_episode_2.mp3",
                    $"{bucketUrl}/ted_episode_3.mp3"
                },
                "Freakonomics Radio" => new[]
                {
                    $"{bucketUrl}/moonshot_episode_4.mp3",
                    $"{bucketUrl}/ted_episode_5.mp3",
                    $"{bucketUrl}/moonshot_episode_5.mp3"
                },
                _ => new[]
                {
                    $"{bucketUrl}/moonshot_episode_1.mp3"
                }
            };
        }

        private string[] GetRealEpisodeTitles(string podcastTitle)
        {
            return podcastTitle switch
            {
                "Moonshot" => new[]
                {
                    "This Week in AI: NVIDIA's Most Powerful Chip, Robotics Reach",
                    "The Singularity is Here: AI is Solving Math, Sora Outpaces Chat-GPT",
                    "Money After AI: Meet the New Digital Dollar Built for the Internet \"Stablecoins\"",
                    "OpenAI vs. Grok: The Race to Build the Everything App",
                    "The AI War: OpenAI Ads & Sora 2, Grok Partners With US Government"
                },
                "TED Talks Daily" => new[]
                {
                    "TED Talks Daily Book Club: You are not alone in your loneliness | Jonny Sun",
                    "Give yourself permission to be creative | Ethan Hawke",
                    "How satellites are supporting farmers across Africa | Catherine Nakalembe",
                    "Touchdown! The flag football movement is here | Troy Vincent Sr.",
                    "How to pull the emergency brake on global warming | Mohamed A. Sultan"
                },
                "The Daily" => new[]
                {
                    "The Latest on Climate Change Policy",
                    "Inside the 2024 Presidential Race",
                    "The Supreme Court's Biggest Cases"
                },
                "Stuff You Should Know" => new[]
                {
                    "How Bitcoin Works",
                    "The Science of Sleep",
                    "What Makes a Cult a Cult?"
                },
                "Freakonomics Radio" => new[]
                {
                    "Why Is My Life So Hard?",
                    "The Economics of Sleep",
                    "How to Make Better Decisions"
                },
                _ => new[]
                {
                    "Episode 1"
                }
            };
        }

        private async Task SeedSubscriptionsAsync(List<ApplicationUser> users, List<Podcast> podcasts)
        {
            if (_context.Subscriptions.Any())
                return;

            var listeners = users.Where(u => u.Role == UserRole.Listener).ToList();
            var subscriptions = new List<Subscription>();
            var random = new Random();

            foreach (var listener in listeners)
            {
                int subscriptionCount = random.Next(2, 5);
                var selectedPodcasts = podcasts.OrderBy(x => random.Next()).Take(subscriptionCount);

                foreach (var podcast in selectedPodcasts)
                {
                    subscriptions.Add(new Subscription
                    {
                        UserID = listener.Email,
                        PodcastID = podcast.PodcastID,
                        SubscriptionDate = DateTime.Now.AddDays(-random.Next(1, 90))
                    });
                }
            }

            await _context.Subscriptions.AddRangeAsync(subscriptions);
            await _context.SaveChangesAsync();
        }

        private async Task SeedCommentsAsync(List<ApplicationUser> users, List<Episode> episodes)
        {
            if (_context.Comments.Any())
                return;

            var listeners = users.Where(u => u.Role == UserRole.Listener).ToList();
            var comments = new List<Comment>();
            var random = new Random();

            var commentTexts = new[]
            {
                "Great episode! Really enjoyed the insights shared here.",
                "This was so informative. Thanks for sharing!",
                "Interesting perspective on the topic. Would love to hear more about this.",
                "One of the best episodes I've listened to in a while!",
                "Learned so much from this. Keep up the great work!",
                "Amazing content! Can't wait for the next episode.",
                "This really made me think differently about the subject.",
                "Excellent discussion. Very well presented.",
                "Loved every minute of this episode!",
                "Thanks for covering this topic. Very relevant and timely.",
                "Fantastic episode! The host did a great job.",
                "This needs more attention. Everyone should listen to this.",
                "Mind-blowing insights! Thanks for this.",
                "Really appreciate the depth of research that went into this.",
                "This episode was exactly what I needed to hear today!"
            };

            foreach (var episode in episodes)
            {
                if (random.Next(100) < 60)
                {
                    int commentCount = random.Next(1, 6);
                    var selectedListeners = listeners.OrderBy(x => random.Next()).Take(commentCount);

                    foreach (var listener in selectedListeners)
                    {
                        comments.Add(new Comment
                        {
                            EpisodeID = episode.EpisodeID,
                            UserID = listener.Email,
                            CommentText = commentTexts[random.Next(commentTexts.Length)],
                            CommentDate = DateTime.Now.AddDays(-random.Next(1, 30)).AddHours(-random.Next(0, 24))
                        });
                    }
                }
            }

            await _context.Comments.AddRangeAsync(comments);
            await _context.SaveChangesAsync();
        }
    }
}