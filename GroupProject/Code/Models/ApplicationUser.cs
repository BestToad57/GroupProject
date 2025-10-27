using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace GroupProject.Code.Models
{
    public enum UserRole
    {
        Admin,
        Podcaster,
        Listener
    }
    public class ApplicationUser : IdentityUser
    {
        public UserRole Role { get; set; }
    }
}
