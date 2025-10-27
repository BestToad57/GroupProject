using Amazon.DynamoDBv2.Model;
using GroupProject.Code.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lab3GroupProject.Code.Data;

namespace Lab3GroupProject.Repositories
{
    public class EpisodeRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public EpisodeRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        } 

        public IEnumerable<Episode> GetAllEpisodes()
        {
            return _dbContext.Episodes.AsNoTracking().ToList();
        }
        
        public Episode? GetEpisodeById(int id)
        {
            return _dbContext.Episodes.AsNoTracking().FirstOrDefault(e => e.EpisodeID == id);
        }
        
        public void AddEpisode(Episode episode)
        {
            _dbContext.Episodes.Add(episode);
            _dbContext.SaveChanges();
        }
        
        public void Update(Episode episode)
        {
            var existingEntry = _dbContext.ChangeTracker.Entries<Episode>()
                .FirstOrDefault(e => e.Entity.EpisodeID == episode.EpisodeID);
            
            if (existingEntry != null)
            {
                existingEntry.State = EntityState.Detached;
            }
            
            _dbContext.Episodes.Update(episode);
            _dbContext.SaveChanges();
        }
        
        public void Delete(int id)
        {
            var episode = _dbContext.Episodes.Find(id);
            if (episode != null)
            {
                _dbContext.Episodes.Remove(episode);
                _dbContext.SaveChanges();
            }
        }
        
        public IEnumerable<Episode> GetEpisodesByDate(DateTime? start = null, DateTime? end = null)
        {
            var query = _dbContext.Episodes.AsNoTracking().AsQueryable();
            if (start.HasValue)
                query = query.Where(e => e.ReleaseDate >= start.Value);
            if (end.HasValue)
                query = query.Where(e => e.ReleaseDate <= end.Value);
            return query.OrderByDescending(e => e.ReleaseDate).ToList();
        }

        public IEnumerable<Episode> GetMostPopularEpisodes(int count = 10)
        {
            return _dbContext.Episodes
                .AsNoTracking()
                .OrderByDescending(e => e.NumberOfViews)
                .Take(count)
                .ToList();
        }

        public IEnumerable<Episode> SearchEpisodesByTopic(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<Episode>();

            searchTerm = searchTerm.ToLower();
            
            return _dbContext.Episodes
                .AsNoTracking()
                .Where(e => e.Title.ToLower().Contains(searchTerm))
                .OrderByDescending(e => e.ReleaseDate)
                .ToList();
        }

        public IEnumerable<Episode> SearchEpisodesByHost(string hostEmail)
        {
            if (string.IsNullOrWhiteSpace(hostEmail))
                return Enumerable.Empty<Episode>();

            hostEmail = hostEmail.ToLower();
            
            return _dbContext.Episodes
                .AsNoTracking()
                .Join(
                    _dbContext.Podcasts,
                    episode => episode.PodcastID,
                    podcast => podcast.PodcastID,
                    (episode, podcast) => new { Episode = episode, Podcast = podcast }
                )
                .Where(ep => ep.Podcast.CreatorID.ToLower().Contains(hostEmail))
                .Select(ep => ep.Episode)
                .OrderByDescending(e => e.ReleaseDate)
                .ToList();
        }

        public IEnumerable<Episode> SearchEpisodes(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<Episode>();

            searchTerm = searchTerm.ToLower();
            
            var results = _dbContext.Episodes
                .AsNoTracking()
                .Join(
                    _dbContext.Podcasts,
                    episode => episode.PodcastID,
                    podcast => podcast.PodcastID,
                    (episode, podcast) => new { Episode = episode, Podcast = podcast }
                )
                .Where(ep => 
                    ep.Episode.Title.ToLower().Contains(searchTerm) ||
                    ep.Podcast.CreatorID.ToLower().Contains(searchTerm) ||
                    ep.Podcast.Title.ToLower().Contains(searchTerm)
                )
                .Select(ep => ep.Episode)
                .Distinct()
                .OrderByDescending(e => e.ReleaseDate)
                .ToList();

            return results;
        }

        public void IncrementViews(int episodeId)
        {
            var episode = _dbContext.Episodes.Find(episodeId);
            if (episode != null)
            {
                episode.NumberOfViews++;
                _dbContext.SaveChanges();
            }
        }

        public void IncrementPlayCount(int episodeId)
        {
            var episode = _dbContext.Episodes.Find(episodeId);
            if (episode != null)
            {
                episode.playCount++;
                _dbContext.SaveChanges();
            }
        }
    }
}