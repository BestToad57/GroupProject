using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using GroupProject.Code.Models;

namespace GroupProject.Code.Repositories
{
    public class DynamoDbCommentRepo
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly DynamoDBContext _context;
        private readonly ILogger<DynamoDbCommentRepo> _logger;
        private const string TableName = "PodcastHub-Comments";

        public DynamoDbCommentRepo(
            IAmazonDynamoDB dynamoDbClient,
            ILogger<DynamoDbCommentRepo> logger)
        {
            _dynamoDbClient = dynamoDbClient;
            _context = new DynamoDBContext(_dynamoDbClient);
            _logger = logger;
        }

        /// <summary>
        /// Ensures the DynamoDB table exists, creates it if it doesn't
        /// </summary>
        public async Task EnsureTableExistsAsync()
        {
            try
            {
                var tableDescription = await _dynamoDbClient.DescribeTableAsync(TableName);
                _logger.LogInformation($"? DynamoDB table '{TableName}' already exists");
            }
            catch (ResourceNotFoundException)
            {
                _logger.LogWarning($"?? Table '{TableName}' not found. Creating...");
                await CreateTableAsync();
            }
        }

        private async Task CreateTableAsync()
        {
            var request = new CreateTableRequest
            {
                TableName = TableName,
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement { AttributeName = "EpisodeID", KeyType = KeyType.HASH },
                    new KeySchemaElement { AttributeName = "CommentID", KeyType = KeyType.RANGE }
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition { AttributeName = "EpisodeID", AttributeType = ScalarAttributeType.N },
                    new AttributeDefinition { AttributeName = "CommentID", AttributeType = ScalarAttributeType.S }
                },
                BillingMode = BillingMode.PAY_PER_REQUEST
            };

            await _dynamoDbClient.CreateTableAsync(request);
            
            // Wait for table to be active
            var tableActive = false;
            while (!tableActive)
            {
                await Task.Delay(2000);
                var response = await _dynamoDbClient.DescribeTableAsync(TableName);
                tableActive = response.Table.TableStatus == TableStatus.ACTIVE;
            }
            
            _logger.LogInformation($"? Successfully created DynamoDB table: {TableName}");
        }

        public async Task<List<DynamoDbComment>> GetAllCommentsAsync()
        {
            try
            {
                var conditions = new List<ScanCondition>(); // Empty conditions to get all items
                var search = _context.ScanAsync<DynamoDbComment>(conditions);
                var comments = await search.GetRemainingAsync();
                return comments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all comments from DynamoDB");
                return new List<DynamoDbComment>();
            }
        }

        public async Task<List<DynamoDbComment>> GetCommentsByEpisodeIdAsync(int episodeId)
        {
            try
            {
                var search = _context.QueryAsync<DynamoDbComment>(episodeId);
                var comments = await search.GetRemainingAsync();
                return comments.OrderByDescending(c => c.CommentDate).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving comments for episode {episodeId} from DynamoDB");
                return new List<DynamoDbComment>();
            }
        }

        public async Task<DynamoDbComment?> GetCommentByIdAsync(int episodeId, string commentId)
        {
            try
            {
                return await _context.LoadAsync<DynamoDbComment>(episodeId, commentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving comment {commentId} from DynamoDB");
                return null;
            }
        }

        public async Task<bool> AddCommentAsync(DynamoDbComment comment)
        {
            try
            {
                await _context.SaveAsync(comment);
                _logger.LogInformation($"? Comment added to DynamoDB: {comment.CommentID}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to DynamoDB");
                return false;
            }
        }

        public async Task<bool> UpdateCommentAsync(DynamoDbComment comment)
        {
            try
            {
                await _context.SaveAsync(comment);
                _logger.LogInformation($"? Comment updated in DynamoDB: {comment.CommentID}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating comment {comment.CommentID} in DynamoDB");
                return false;
            }
        }

        public async Task<bool> DeleteCommentAsync(int episodeId, string commentId)
        {
            try
            {
                await _context.DeleteAsync<DynamoDbComment>(episodeId, commentId);
                _logger.LogInformation($"? Comment deleted from DynamoDB: {commentId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting comment {commentId} from DynamoDB");
                return false;
            }
        }

        public async Task<int> GetCommentCountAsync()
        {
            try
            {
                var conditions = new List<ScanCondition>(); // Empty conditions to get all items
                var search = _context.ScanAsync<DynamoDbComment>(conditions);
                var comments = await search.GetRemainingAsync();
                return comments.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comment count from DynamoDB");
                return 0;
            }
        }
    }
}
