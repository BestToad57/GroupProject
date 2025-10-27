namespace GroupProject.Code.Models
{
    public class Episode
    {
        public int EpisodeID { get; set; }
        public int PodcastID { get; set; }
        public string Title { get; set; } = "";
        public DateTime ReleaseDate { get; set; }
        public TimeSpan Duration { get; set; }
        public int playCount { get; set; }
        public string AudioFileURL { get; set; } = "";
        public int NumberOfViews { get; set; }
    }
}