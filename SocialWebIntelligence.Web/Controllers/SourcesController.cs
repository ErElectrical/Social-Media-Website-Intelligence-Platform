using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SocialWebIntelligence.Core.DTOs;
using SocialWebIntelligence.Core.Entities;
using SocialWebIntelligence.Infrastructure.Collectors.Websites;
using SocialWebIntelligence.Infrastructure.Collectors.WebSites;
using SocialWebIntelligence.Infrastructure.Data;
using System.Text.Json;

namespace SocialWebIntelligence.Web.Controllers
{
    public class SourcesController : Controller
    {

        private readonly SocialWebIntelligenceDbContext _context;
        private readonly WebsiteContactCollectionService _collectionService;
        private readonly WebsiteCollectionPersistenceService _persistenceService;
        // Saves About page snapshots and tracks changes between collections.
        private readonly AboutPagePersistenceService _aboutPagePersistenceService;
        private readonly AboutPageCollectionService _aboutPageCollectionService;
        // Discovers Contact, About and other relevant pages on a website.
        private readonly WebsitePageDiscoveryService _pageDiscoveryService;

        private readonly GeneralPageCollectionService _generalPageCollectionService;
        public SourcesController(
            SocialWebIntelligenceDbContext context,
            WebsiteContactCollectionService collectionService,
            WebsiteCollectionPersistenceService persistenceService,
            AboutPagePersistenceService aboutPagePersistenceService,
            AboutPageCollectionService aboutPageCollectionService,
            WebsitePageDiscoveryService pageDiscoveryService,
            GeneralPageCollectionService generalPageCollectionService)
        {
            _context = context;
            _collectionService = collectionService;
            _persistenceService = persistenceService;
            _aboutPagePersistenceService = aboutPagePersistenceService;
            _aboutPageCollectionService = aboutPageCollectionService;
            _pageDiscoveryService = pageDiscoveryService;
            _generalPageCollectionService = generalPageCollectionService;
        }
        public async Task<IActionResult> Index()
        {
            var sources = await _context.MonitoredSources
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(sources);
                       
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SocialWebIntelligence.Core.Entities.MonitoredSource source)
        {
            if (!Uri.TryCreate(source.Url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp &&
                uri.Scheme != Uri.UriSchemeHttps))
            {
                ModelState.AddModelError(
                    nameof(source.Url),
                    "Enter a valid HTTP or HTTPS URL.");
            }

            if (!Enum.IsDefined(typeof(
                SocialWebIntelligence.Core.Entities.SourcePlatform),
                source.Platform))
            {
                ModelState.AddModelError(
                    nameof(source.Platform),
                    "Select a valid platform.");
            }

            if (!ModelState.IsValid)
            {
                return View(source);
            }

            source.CreatedAt = DateTime.UtcNow;
            source.IsActive = true;
            _context.MonitoredSources.Add(source);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));


        }


        [HttpGet]

        public async Task<IActionResult> Edit(int id)
        {
            var source = await _context.MonitoredSources.FindAsync(id);
            if(source == null)
            {
                return NotFound();
            }
            return View(source);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]

        public async Task<IActionResult> Edit( int id , [Bind("Id,CompanyName,Platform,Url")]  SocialWebIntelligence.Core.Entities.MonitoredSource source)
        {
            if(id != source.Id)
            {
                return NotFound();
            }
            if (!Uri.TryCreate(source.Url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp &&
                 uri.Scheme != Uri.UriSchemeHttps))
            {
                ModelState.AddModelError(nameof(source.Url),
                    "Enter a valid HTTP or HTTPS URL.");
            }

            if (!ModelState.IsValid)
                return View(source);

            var existing = await _context.MonitoredSources.FindAsync(id);

            if (existing == null)
                return NotFound();

            existing.CompanyName = source.CompanyName;
            existing.Platform = source.Platform;
            existing.Url = source.Url;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var source = await _context.MonitoredSources.FindAsync(id);

            if (source == null)
                return NotFound();

            _context.MonitoredSources.Remove(source);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var source = await _context.MonitoredSources.FindAsync(id);

            if (source == null)
                return NotFound();

            source.IsActive = !source.IsActive;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Collect(
            int id,
            CancellationToken cancellationToken)
        {
            // Step 1: Find the registered company.
            var source = await _context.MonitoredSources
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (source == null)
                return NotFound();

            // Step 2: Only collect active website sources.
            if (!source.IsActive)
            {
                TempData["Error"] = "This source is disabled.";
                return RedirectToAction(nameof(Index));
            }

            if (source.Platform != SourcePlatform.Website)
            {
                TempData["Error"] = "Only website sources are supported currently.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Step 3: Download website and extract contact information.
                var result = await _collectionService.CollectAsync(
                    source.Url,
                    cancellationToken);


                // Collect About page information for the same company.
                var aboutPage = await _context.WebsiteCollections
                    .Where(x =>
                        x.MonitoredSourceId == source.Id &&
                        x.PageType == "About" &&
                        x.IsSuccessful)
                    .Select(x => x.PageUrl)
                    .FirstOrDefaultAsync(cancellationToken);
                // If no About page was previously saved, discover it from the homepage.
                if (aboutPage == null)
                {
                    var discoveredPages = await _pageDiscoveryService.DiscoverPagesAsync(
                        source.Url,
                        cancellationToken);

                    aboutPage = discoveredPages
                        .FirstOrDefault(x => x.PageType == "About")
                        ?.PageUrl;
                }

                if (result == null)
                {
                    TempData["Error"] =
                        "No Contact Us page was discovered on this website.";

                    return RedirectToAction(nameof(Index));
                }

                // Step 4: Save extracted information into MariaDB.
                //var savedCollection = await _persistenceService.SaveAsync(
                //    source.Id,
                //    result,
                //    cancellationToken);

                var saveresult = await _persistenceService.SaveAsync(source.Id, result, cancellationToken);

                // Collect and persist About information when an About page is available.
                if (!string.IsNullOrWhiteSpace(aboutPage))
                {
                    var aboutResult = await _aboutPageCollectionService.CollectAsync(
                        aboutPage,
                        cancellationToken);

                    var aboutSaveResult = await _aboutPagePersistenceService.SaveAsync(
                        source.Id,
                        aboutResult,
                        cancellationToken);

                    // Keep the About collection outcome visible on the Sources page.
                    TempData["AboutStatus"] = aboutSaveResult.Status.ToString();
                }
                else
                {
                    TempData["AboutStatus"] = "No About page discovered.";
                }
                // Step 5: Update the last collection timestamp.
                source.LastCollectedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                switch (saveresult.Status)
                {
                    case WebsiteCollectionSaveStatus.New:
                        TempData["Success"] =
                            $"First collection saved. Record ID: {saveresult.Collection.Id}";
                        break;

                    case WebsiteCollectionSaveStatus.Changed:
                        TempData["Success"] =
                            $"Website information changed. New snapshot ID: {saveresult.Collection.Id}";
                        break;

                    case WebsiteCollectionSaveStatus.Unchanged:
                        TempData["Success"] =
                            "No changes detected. Existing collection was retained.";
                        break;
                }

                
            }
            catch (HttpRequestException ex)
            {
                TempData["Error"] =
                    $"Website request failed: {ex.Message}";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] =
                    "Website collection timed out or was cancelled.";
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    $"Collection failed: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Temporary endpoint for verifying About page persistence in MariaDB.
        // Keep this endpoint local during development; remove it before deployment.
        [HttpGet]
        public async Task<IActionResult> TestAboutPersistence(
            int monitoredSourceId,
            string pageUrl,
            CancellationToken cancellationToken)
        {
            try
            {
                // Step 1: Download the page and extract About information.
                var result = await _aboutPageCollectionService.CollectAsync(
                    pageUrl,
                    cancellationToken);

                // Step 2: Save the extracted information as a database snapshot.
                var saveResult = await _aboutPagePersistenceService.SaveAsync(
                    monitoredSourceId,
                    result,
                    cancellationToken);

                return Json(new
                {
                    saveResult.Status,
                    CollectionId = saveResult.Collection.Id,
                    saveResult.Collection.PageUrl,
                    saveResult.Collection.PageType,
                    saveResult.Changes
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, ex.Message);
            }
        }


        // Displays all saved About page snapshots for a monitored company.
        [HttpGet]
        public async Task<IActionResult> AboutHistory(int id)
        {
            // Find the company selected from the Sources dashboard.
            var source = await _context.MonitoredSources
                .FirstOrDefaultAsync(x => x.Id == id);

            if (source == null)
                return NotFound();

            // Retrieve only About snapshots belonging to this company.
            // Newest collections appear first.
            var collections = await _context.WebsiteCollections
                .Where(x =>
                    x.MonitoredSourceId == id &&
                    x.PageType == "About" &&
                    x.IsSuccessful)
                .OrderByDescending(x => x.CollectedAt)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            
            ViewBag.CompanyName = source.CompanyName;
            ViewBag.SourceId = source.Id;

            return View(collections);
        }


        // Displays pages discovered by the recursive website crawler.
        [HttpGet]
        public async Task<IActionResult> DiscoveredPages(
            int id,
            CancellationToken cancellationToken)
        {
            // Find the selected monitored company.
            var source = await _context.MonitoredSources
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (source == null)
                return NotFound();

            if (source.Platform != SourcePlatform.Website)
                return BadRequest("Page discovery is supported only for website sources.");

            ViewBag.CompanyName = source.CompanyName;
            ViewBag.SourceId = source.Id;
            ViewBag.WebsiteUrl = source.Url;

            try
            {
                // Run recursive discovery. Results are displayed, not saved.
                var discoveredPages = await _pageDiscoveryService.DiscoverPagesAsync(
                    source.Url,
                    cancellationToken);

                return View(discoveredPages);
            }
            catch (HttpRequestException ex)
            {
                ViewBag.Error = $"Website discovery failed: {ex.Message}";

                return View(new List<DiscoverWebsitePage>());
            }
        }


        // Returns one saved About snapshot for inline display on the history page.
        [HttpGet]
        public async Task<IActionResult> GetAboutDetails(int id)
        {
            // Retrieve the selected snapshot without downloading the website again.
            var snapshot = await _context.WebsiteCollections
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.PageType == "About" &&
                    x.IsSuccessful &&
                    _context.MonitoredSources.Any(s => s.Id == x.MonitoredSourceId));

            if (snapshot == null)
                return NotFound(new { message = "About snapshot not found." });

            // Convert the stored JSON into the original structured About DTO.
            var aboutInformation =
                JsonSerializer.Deserialize<AboutInformation>(
                    snapshot.ExtractedDataJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (aboutInformation == null)
                return BadRequest(new { message = "Unable to read saved About information." });

            return Json(new
            {
                snapshot.Id,
                snapshot.PageUrl,
                snapshot.PageTitle,
                snapshot.HttpStatusCode,
                snapshot.CollectedAt,
                snapshot.ContentHash,
                AboutInformation = aboutInformation
            });
        }



        [HttpGet]
        public async Task<IActionResult> ChangeHistory()
        {
            // Join change records with their snapshots and registered companies.
            var changes = await (
                from change in _context.WebsiteCollectionChanges
                join collection in _context.WebsiteCollections
                    on change.WebsiteCollectionId equals collection.Id
                join source in _context.MonitoredSources
                    on collection.MonitoredSourceId equals source.Id
                orderby change.DetectedAt descending
                select new WebsiteChangeHistoryItem
                {
                    ChangeId = change.Id,
                    WebsiteCollectionId = collection.Id,
                    CompanyName = source.CompanyName,
                    PageType = collection.PageType,
                    PageUrl = collection.PageUrl,
                    Field = change.Field,
                    ChangeType = change.ChangeType,
                    OldValue = change.OldValue,
                    NewValue = change.NewValue,
                    DetectedAt = change.DetectedAt
                })
                .ToListAsync();

            return View(changes);
        }

        // Collects and displays structured information from a discovered
        // Products or Services page.
        [HttpGet]
        public async Task<IActionResult> GeneralPageDetails(
            int id,
            string pageUrl,
            CancellationToken cancellationToken)
        {
            var source = await _context.MonitoredSources
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (source == null)
                return NotFound();

            if (source.Platform != SourcePlatform.Website)
                return BadRequest("This operation is supported only for website sources.");

            if (string.IsNullOrWhiteSpace(pageUrl) ||
                !Uri.TryCreate(pageUrl, UriKind.Absolute, out var requestedUri) ||
                (requestedUri.Scheme != Uri.UriSchemeHttp &&
                 requestedUri.Scheme != Uri.UriSchemeHttps))
            {
                return BadRequest("A valid HTTP or HTTPS page URL is required.");
            }

            ViewBag.CompanyName = source.CompanyName;
            ViewBag.SourceId = source.Id;

            try
            {
                // Verify that the requested URL is actually one of the
                // Products or Services pages discovered for this website.
                var discoveredPages = await _pageDiscoveryService.DiscoverPagesAsync(
                    source.Url,
                    cancellationToken);

                var discoveredPage = discoveredPages.FirstOrDefault(page =>
                    (page.PageType == "Products" || page.PageType == "Services") &&
                    string.Equals(
                        page.PageUrl,
                        requestedUri.AbsoluteUri,
                        StringComparison.OrdinalIgnoreCase));

                if (discoveredPage == null)
                    return BadRequest("The requested page was not discovered for this website.");

                // Collect the discovered page and extract its structured information.
                var result = await _generalPageCollectionService.CollectAsync(
                    discoveredPage.PageUrl,
                    cancellationToken);

                ViewBag.PageType = discoveredPage.PageType;

                var viewName = discoveredPage.PageType == "Products"
                    ? "ProductDetails"
                    : "ServiceDetails";

                return View(viewName, result);
            }
            catch (HttpRequestException ex)
            {
                ViewBag.Error = $"Page collection failed: {ex.Message}";

                var viewName = requestedUri.AbsolutePath.Contains(
                    "product", StringComparison.OrdinalIgnoreCase)
                    ? "ProductDetails"
                    : "ServiceDetails";

                return View(viewName, null);
            }
        }



    }
}

