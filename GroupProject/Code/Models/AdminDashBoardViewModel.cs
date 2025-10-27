using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GroupProject.Code.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalPodcast { get; set; }
        public int TotalEpisodes { get; set; }
        public List<Podcast>? PendingPodcast { get; set; } 
    }
}
