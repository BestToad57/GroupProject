using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace GroupProject.Code.Services
{
    public class ParameterStoreService
    {
        private readonly IAmazonSimpleSystemsManagement _ssmClient;
        private readonly ILogger<ParameterStoreService> _logger;

        public ParameterStoreService(
            IAmazonSimpleSystemsManagement ssmClient,
            ILogger<ParameterStoreService> logger)
        {
            _ssmClient = ssmClient;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a parameter value from AWS Systems Manager Parameter Store
        /// </summary>
        /// <param name="parameterName">The name of the parameter to retrieve</param>
        /// <param name="withDecryption">Whether to decrypt secure string parameters</param>
        /// <returns>The parameter value</returns>
        public async Task<string> GetParameterAsync(string parameterName, bool withDecryption = true)
        {
            try
            {
                var request = new GetParameterRequest
                {
                    Name = parameterName,
                    WithDecryption = withDecryption
                };

                var response = await _ssmClient.GetParameterAsync(request);
                _logger.LogInformation($"Successfully retrieved parameter: {parameterName}");
                
                return response.Parameter.Value;
            }
            catch (ParameterNotFoundException)
            {
                _logger.LogError($"Parameter not found: {parameterName}");
                throw new InvalidOperationException($"Parameter '{parameterName}' not found in Parameter Store. Please create it first.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving parameter: {parameterName}");
                throw;
            }
        }

        /// <summary>
        /// Builds a SQL Server connection string from Parameter Store parameters
        /// </summary>
        /// <returns>Complete connection string</returns>
        public async Task<string> GetDatabaseConnectionStringAsync()
        {
            try
            {
                // Retrieve individual parameters
                var server = await GetParameterAsync("/PodcastHub/Database/Server");
                var database = await GetParameterAsync("/PodcastHub/Database/DatabaseName");
                var userId = await GetParameterAsync("/PodcastHub/Database/UserId");
                var password = await GetParameterAsync("/PodcastHub/Database/Password");

                // Build connection string
                var connectionString = $"Server={server};Database={database};User Id={userId};Password={password};TrustServerCertificate=True;MultipleActiveResultSets=true";
                
                _logger.LogInformation("Database connection string built successfully from Parameter Store");
                
                return connectionString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build database connection string from Parameter Store");
                
                // Fallback to appsettings.json if Parameter Store is not available
                _logger.LogWarning("?? Falling back to appsettings.json for database connection");
                throw;
            }
        }

        /// <summary>
        /// Creates or updates a parameter in Parameter Store (useful for initial setup)
        /// </summary>
        public async Task<bool> CreateOrUpdateParameterAsync(string parameterName, string parameterValue, bool isSecure = false)
        {
            try
            {
                var request = new PutParameterRequest
                {
                    Name = parameterName,
                    Value = parameterValue,
                    Type = isSecure ? ParameterType.SecureString : ParameterType.String,
                    Overwrite = true,
                    Description = $"Auto-created parameter for PodcastHub application"
                };

                await _ssmClient.PutParameterAsync(request);
                _logger.LogInformation($"Successfully created/updated parameter: {parameterName}");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating/updating parameter: {parameterName}");
                return false;
            }
        }
    }
}
