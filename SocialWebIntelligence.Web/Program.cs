using Microsoft.EntityFrameworkCore;
using SocialWebIntelligence.Infrastructure.Collectors.Websites;
using SocialWebIntelligence.Infrastructure.Collectors.WebSites;
using SocialWebIntelligence.Infrastructure.Data;


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<SocialWebIntelligenceDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    ));

//Dependency Injection separates the responsibility of using a dependency from the responsibility of creating and managing that dependency.
//Registration Lifetime	Meaning
//AddTransient	Transient	New instance whenever requested
//AddScoped	Scoped	One instance per HTTP request scope
//AddSingleton	Singleton	One shared instance for the application lifetime
//AddHttpClient	Typed HTTP client	Creates a typed client with managed HTTP handler lifecycle

//We chose transient for the stateless extractor and typed HTTP clients for the two services that perform HTTP operations.


//builder.Services.AddHttpClient<WebsitePageDiscoveryService>();
builder.Services.AddTransient<ContactInformationExtractor>();
//builder.Services.AddHttpClient<WebsiteContactCollectionService>();
//builder.Services.AddScoped<WebsiteCollectionPersistenceService>();
builder.Services.AddHttpClient<WebsitePageDiscoveryService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);

    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "SocialWebIntelligence/1.0");
});

builder.Services.AddHttpClient<WebsiteContactCollectionService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);

    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "SocialWebIntelligence/1.0");
});

// Register the About extractor so it can be injected into the collector.
builder.Services.AddTransient<AboutInformationExtractor>();

// Use a typed HttpClient to provide timeout and User-Agent configuration.
builder.Services.AddHttpClient<AboutPageCollectionService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "SocialWebIntelligence/1.0");
});

// Persistence service uses EF Core DbContext, not HttpClient.
builder.Services.AddScoped<AboutPagePersistenceService>();
builder.Services.AddScoped<WebsiteCollectionPersistenceService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
