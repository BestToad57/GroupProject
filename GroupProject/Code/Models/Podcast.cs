namespace GroupProject.Code.Models
{
    public class Podcast
    {
        public int PodcastID { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string CreatorID { get; set; } = "";
        public DateTime CreatedDate { get; set; }
    }
}