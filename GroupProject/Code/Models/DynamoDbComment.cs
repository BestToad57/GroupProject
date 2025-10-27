using Amazon.DynamoDBv2.DataModel;

namespace GroupProject.Code.Models
{
    [DynamoDBTable("PodcastHub-Comments")]
    public class DynamoDbComment
    {
        [DynamoDBHashKey("EpisodeID")]
        public int EpisodeID { get; set; }

        [DynamoDBRangeKey("CommentID")]
        public string CommentID { get; set; } = Guid.NewGuid().ToString();

        [DynamoDBProperty("PodcastID")]
        public int PodcastID { get; set; }

        [DynamoDBProperty("UserID")]
        public string UserID { get; set; } = "";

        [DynamoDBProperty("Text")]
        public string CommentText { get; set; } = "";

        [DynamoDBProperty("Timestamp")]
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");

        [DynamoDBIgnore]
        public DateTime CommentDate 
        { 
            get => DateTime.Parse(Timestamp);
            set => Timestamp = value.ToString("o");
        }

        // For easier conversion to/from SQL Comment model
        public static DynamoDbComment FromComment(Comment comment)
        {
            return new DynamoDbComment
            {
                EpisodeID = comment.EpisodeID,
                CommentID = comment.CommentID > 0 ? comment.CommentID.ToString() : Guid.NewGuid().ToString(),
                UserID = comment.UserID,
                CommentText = comment.CommentText,
                CommentDate = comment.CommentDate
            };
        }

        public Comment ToComment()
        {
            return new Comment
            {
                CommentID = int.TryParse(CommentID, out var id) ? id : 0,
                EpisodeID = EpisodeID,
                UserID = UserID,
                CommentText = CommentText,
                CommentDate = CommentDate
            };
        }
    }
}
