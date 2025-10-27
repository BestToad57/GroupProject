using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lab3GroupProject.Repositories;
using GroupProject.Code.Models;

namespace Lab3GroupProject.Service
{
    public class PodcastService
    {
        private readonly PodcastRepo _podcastRepository;
        public PodcastService(PodcastRepo podcastRepository)
        {
            _podcastRepository = podcastRepository;
        }
        public IEnumerable<Podcast> GetAllPodcasts()
        {
            return _podcastRepository.GetAllPodcasts();
        }
        public Podcast? GetPodcastById(int id)
        {
            return _podcastRepository.GetPodcastById(id);
        }
        public void CreatePodcast(Podcast podcast)
        {
            _podcastRepository.AddPodcast(podcast);
        }
        public void UpdatePodcast(Podcast podcast)
        {
            _podcastRepository.Update(podcast);
        }
        public void DeletePodcast(int id)
        {
            _podcastRepository.Delete(id);
        }
        public IEnumerable<Podcast> GetPodcastsByDate(DateTime? start = null, DateTime? end = null)
        {
            return _podcastRepository.GetPodcastsByDate(start, end);
        }
    }
}