using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.S3;


namespace GroupProject.Code.Models
{
    public class Models
    {
        public Models() {

        }
        private static IAmazonS3 getS3Client()
        {
            return new AmazonS3Client(GetBasicCredentials(), RegionEndpoint.USEast1);
        }
        private static IAmazonDynamoDB GetDynamoDbClient()
        {
            return new AmazonDynamoDBClient(GetBasicCredentials(), RegionEndpoint.USEast1);
        }

        private static BasicAWSCredentials GetBasicCredentials()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("AppSettings.json");

            var accessKeyID = builder.Build().GetSection("AWSCredentials").GetSection("AccessKeyID").Value;
            var secretKey = builder.Build().GetSection("AWSCredentials").GetSection("Secretaccesskey").Value;

            return new BasicAWSCredentials(accessKeyID, secretKey);
        }
    }
}