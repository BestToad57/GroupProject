using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lab3GroupProject.Repositories;
using GroupProject.Code.Models;

namespace Lab3GroupProject.Service
{
    public class EpsiodeService
    {
        private readonly EpisodeRepo _episodeRepository;
        public EpsiodeService(EpisodeRepo episodeRepository)
        {
            _episodeRepository = episodeRepository;
        }
        public IEnumerable<Episode> GetAllEpisodes()
        {
            return _episodeRepository.GetAllEpisodes();
        }
        public Episode? GetEpisodeById(int id)
        {
            return _episodeRepository.GetEpisodeById(id);
        }
        public void CreateEpisode(Episode episode)
        {
            _episodeRepository.AddEpisode(episode);
        }
        public void UpdateEpisode(Episode episode)
        {
            _episodeRepository.Update(episode);
        }
        public void DeleteEpisode(int id)
        {
            _episodeRepository.Delete(id);
        }
        public IEnumerable<Episode> GetEpisodesByDate(DateTime? start = null, DateTime? end = null)
        {
            return _episodeRepository.GetEpisodesByDate(start, end);
        }

        public IEnumerable<Episode> GetMostPopularEpisodes(int count = 10)
        {
            return _episodeRepository.GetMostPopularEpisodes(count);
        }

        public IEnumerable<Episode> SearchEpisodesByTopic(string searchTerm)
        {
            return _episodeRepository.SearchEpisodesByTopic(searchTerm);
        }

        public IEnumerable<Episode> SearchEpisodesByHost(string hostEmail)
        {
            return _episodeRepository.SearchEpisodesByHost(hostEmail);
        }

        public IEnumerable<Episode> SearchEpisodes(string searchTerm)
        {
            return _episodeRepository.SearchEpisodes(searchTerm);
        }

        public void IncrementViews(int episodeId)
        {
            _episodeRepository.IncrementViews(episodeId);
        }

        public void IncrementPlayCount(int episodeId)
        {
            _episodeRepository.IncrementPlayCount(episodeId);
        }
    }
}