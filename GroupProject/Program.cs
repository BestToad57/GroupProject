using Microsoft.EntityFrameworkCore;
using Lab3GroupProject.Code.Data;
using Microsoft.AspNetCore.Identity;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SimpleSystemsManagement;
using GroupProject.Code.Models;
using Lab3GroupProject.Repositories;
using Lab3GroupProject.Service;
using GroupProject.Code.Data;
using GroupProject.Code.Services;
using GroupProject.Code.Repositories;

var builder = WebApplication.CreateBuilder(args);

var awsSection = builder.Configuration.GetSection("AWSCredentials");
var credentials = new BasicAWSCredentials(
    awsSection["AccessKeyID"],
    awsSection["SecretAccessKey"]
);
var region = RegionEndpoint.USEast1;

builder.Services.AddSingleton<IAmazonS3>(sp => new AmazonS3Client(credentials, region));
builder.Services.AddSingleton<IAmazonDynamoDB>(sp => new AmazonDynamoDBClient(credentials, region));
builder.Services.AddSingleton<IAmazonSimpleSystemsManagement>(sp => new AmazonSimpleSystemsManagementClient(credentials, region));

builder.Services.AddSingleton<IDynamoDBContext>(sp =>
{
    var dynamoClient = sp.GetRequiredService<IAmazonDynamoDB>();
    return new DynamoDBContext(dynamoClient);
});

builder.Services.AddScoped<ParameterStoreService>();

string connectionString;
try
{
    var tempServiceProvider = builder.Services.BuildServiceProvider();
    var parameterStoreService = tempServiceProvider.GetRequiredService<ParameterStoreService>();
    connectionString = await parameterStoreService.GetDatabaseConnectionStringAsync();
    Console.WriteLine("? Using database connection string from AWS Parameter Store");
}
catch (Exception ex)
{
    Console.WriteLine($"?? Could not retrieve connection string from Parameter Store: {ex.Message}");
    Console.WriteLine("?? Falling back to appsettings.json connection string");
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<CommentRepo>();
builder.Services.AddScoped<DynamoDbCommentRepo>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddScoped<PodcastRepo>();
builder.Services.AddScoped<PodcastService>();
builder.Services.AddScoped<EpisodeRepo>();
builder.Services.AddScoped<EpsiodeService>();
builder.Services.AddScoped<SubscriptionRepo>();
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<S3UploadService>();
builder.Services.AddScoped<DatabaseSeeder>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dynamoRepo = services.GetRequiredService<DynamoDbCommentRepo>();
        await dynamoRepo.EnsureTableExistsAsync();
        Console.WriteLine("? DynamoDB table verified/created");
        
        Console.WriteLine("?? Skipping S3 upload (files uploaded manually)");
        Console.WriteLine("?? Using existing files in S3 bucket: podcasthub-audio/podcasts/");
        
        var seeder = services.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
        Console.WriteLine("? Database seeded successfully!");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during startup.");
        Console.WriteLine($"? Error: {ex.Message}");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.MapRazorPages();

app.Run();
