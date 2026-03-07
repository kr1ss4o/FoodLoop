using FoodLoop.Data;
using FoodLoop.Models.Entities;
using FoodLoop.Models.ViewModels;
using FoodLoop.Services;
using FoodLoop.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Controllers.Admin;

public class AdminTagsController : AdminBaseController
{
    private const int PageSize = 10;

    private readonly AdminDeleteService _deleteService;

    public AdminTagsController(
        ApplicationDbContext context,
        AdminDeleteService deleteService) : base(context)
    {
        _deleteService = deleteService;
    }

    // =====================================================
    // LIST
    // =====================================================

    public async Task<IActionResult> Tags(string? query, int page = 1)
    {
        var tagsQuery = _context.Tags
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            tagsQuery = tagsQuery.Where(t =>
                t.Name.Contains(query));
        }

        var totalItems = await tagsQuery.CountAsync();

        var tags = await tagsQuery
            .OrderBy(t => t.Name)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Pagination = new PaginationViewModel
        {
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize),
            Controller = "AdminTags",
            Action = "Tags",
            Query = query
        };

        return AdminView("Tags", tags);
    }

    // =====================================================
    // DETAILS
    // =====================================================

    public async Task<IActionResult> Details(Guid id)
    {
        var tag = await _context.Tags
            .Include(t => t.OfferTags)
            .ThenInclude(ot => ot.Offer)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tag == null)
            return NotFound();

        return AdminDetails(tag);
    }

    // =====================================================
    // CREATE
    // =====================================================

    [HttpGet]
    public IActionResult Create()
    {
        var vm = new AdminFormViewModel
        {
            Type = "Tag"
        };

        return AdminCreate(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminFormViewModel vm)
    {
        var tag = new Tag
        {
            Name = vm.Name
        };

        _context.Tags.Add(tag);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Tags));
    }

    // =====================================================
    // EDIT
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var tag = await _context.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tag == null)
            return NotFound();

        var vm = new AdminFormViewModel
        {
            Type = "Tag",
            Id = tag.Id,
            Name = tag.Name
        };

        return AdminEdit(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminFormViewModel vm)
    {
        if (vm.Id == null)
            return BadRequest();

        var tag = await _context.Tags
            .FirstOrDefaultAsync(t => t.Id == vm.Id);

        if (tag == null)
            return NotFound();

        tag.Name = vm.Name;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Tags));
    }

    // =====================================================
    // DELETE
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _deleteService.DeleteTagAsync(id);

        return RedirectToAction(nameof(Tags));
    }
}