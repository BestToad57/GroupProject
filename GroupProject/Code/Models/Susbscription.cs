namespace GroupProject.Code.Models
{
    public class Subscription
    {
        public int SubscriptionID { get; set; }
        public string UserID { get; set; } = "";
        public int PodcastID { get; set; }
        public DateTime SubscriptionDate { get; set; }
    }
}