using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialWebIntelligence.Core.Entities;
using SocialWebIntelligence.Infrastructure.Collectors.Websites;
using SocialWebIntelligence.Infrastructure.Data;

namespace SocialWebIntelligence.Web.Controllers
{
    public class SourcesController : Controller
    {

        private readonly SocialWebIntelligenceDbContext _context;
        private readonly WebsiteContactCollectionService _collectionService;
        private readonly WebsiteCollectionPersistenceService _persistenceService;

        public SourcesController(
            SocialWebIntelligenceDbContext context,
            WebsiteContactCollectionService collectionService,
            WebsiteCollectionPersistenceService persistenceService)
        {
            _context = context;
            _collectionService = collectionService;
            _persistenceService = persistenceService;
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

                if (result == null)
                {
                    TempData["Error"] =
                        "No Contact Us page was discovered on this website.";

                    return RedirectToAction(nameof(Index));
                }

                // Step 4: Save extracted information into MariaDB.
                var savedCollection = await _persistenceService.SaveAsync(
                    source.Id,
                    result,
                    cancellationToken);

                // Step 5: Update the last collection timestamp.
                source.LastCollectedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                TempData["Success"] =
                    $"Collection completed successfully. Database record ID: {savedCollection.Id}";
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
    }
}

