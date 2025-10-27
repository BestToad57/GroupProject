using Amazon.DynamoDBv2.Model;
using GroupProject.Code.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lab3GroupProject.Code.Data;

namespace Lab3GroupProject.Repositories
{
    public class PodcastRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public PodcastRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IEnumerable<Podcast> GetAllPodcasts()
        {
            return _dbContext.Podcasts.AsNoTracking().ToList();
        }

        public Podcast? GetPodcastById(int id)
        {
            return _dbContext.Podcasts.AsNoTracking().FirstOrDefault(p => p.PodcastID == id);
        }

        public void AddPodcast(Podcast podcast)
        {
            _dbContext.Podcasts.Add(podcast);
            _dbContext.SaveChanges();
        }
        
        public void Update(Podcast podcast)
        {
            var existingEntry = _dbContext.ChangeTracker.Entries<Podcast>()
                .FirstOrDefault(e => e.Entity.PodcastID == podcast.PodcastID);
            
            if (existingEntry != null)
            {
                existingEntry.State = EntityState.Detached;
            }
            
            _dbContext.Podcasts.Update(podcast);
            _dbContext.SaveChanges();
        }
        
        public void Delete(int id)
        {
            var podcast = _dbContext.Podcasts.Find(id);
            if (podcast != null)
            {
                _dbContext.Podcasts.Remove(podcast);
                _dbContext.SaveChanges();
            }
        }
        
        public IEnumerable<Podcast> GetPodcastsByDate(DateTime? start = null, DateTime? end = null)
        {
            var query = _dbContext.Podcasts.AsNoTracking().AsQueryable();

            if (start.HasValue)
                query = query.Where(p => p.CreatedDate >= start.Value);
            if (end.HasValue)
                query = query.Where(p => p.CreatedDate <= end.Value);

            return query.OrderByDescending(p => p.CreatedDate).ToList();
        }
    }
}