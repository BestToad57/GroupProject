using Microsoft.EntityFrameworkCore;

namespace GroupProject.Code.Models
{
    public class DBModel : DbContext
    {
        public DBModel(DbContextOptions<DBModel> options) : base(options) { }

        public DbSet<Podcast> Podcasts { get; set; }
        public DbSet<Episode> Episodes { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
    }

}