using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodLoop.Models.Entities;
using FoodLoop.Models.ViewModels;
using FoodLoop.Data;
using FoodLoop.Services;
using FoodLoop.ViewModels.Admin;

namespace FoodLoop.Controllers.Admin;

public class AdminCategoriesController : AdminBaseController
{
    private const int PageSize = 10;
    private readonly AdminDeleteService _deleteService;

    public AdminCategoriesController(
        ApplicationDbContext context,
        AdminDeleteService deleteService)
        : base(context)
    {
        _deleteService = deleteService;
    }

    // =====================================================
    // LIST
    // =====================================================

    public async Task<IActionResult> Categories(string? query, int page = 1)
    {
        var categoriesQuery = _context.Categories
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            categoriesQuery = categoriesQuery.Where(c =>
                c.Name.Contains(query));
        }

        var totalItems = await categoriesQuery.CountAsync();

        var categories = await categoriesQuery
            .OrderBy(c => c.Name)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Pagination = new PaginationViewModel
        {
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize),
            Controller = "AdminCategories",
            Action = "Categories",
            Query = query
        };

        return AdminView("Categories", categories);
    }

    // DETAILS

    public async Task<IActionResult> Details(Guid id)
    {
        var category = await _context.Categories
            .Include(c => c.Offers)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
            return NotFound();

        return AdminDetails(category);
    }

    // =====================================================
    // CREATE
    // =====================================================

    [HttpGet]
    public IActionResult Create()
    {
        var vm = new AdminFormViewModel
        {
            Type = "Category"
        };

        return AdminCreate(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return AdminCreate(vm);

        var category = new Category
        {
            Name = vm.Name,
            IconUrl = vm.Icon
        };

        _context.Categories.Add(category);

        await _context.SaveChangesAsync();

        return RedirectToAction("Categories", "AdminCategories");
    }

    // =====================================================
    // EDIT
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
            return NotFound();

        var vm = new AdminFormViewModel
        {
            Type = "Category",
            Id = category.Id,
            Name = category.Name,
            Icon = category.IconUrl
        };

        return AdminEdit(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminFormViewModel vm)
    {
        if (vm.Id == null)
            return BadRequest();

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == vm.Id);

        if (category == null)
            return NotFound();

        category.Name = vm.Name;
        category.IconUrl = vm.Icon;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Categories));
    }

    // =====================================================
    // DELETE
    // =====================================================

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _deleteService.DeleteCategoryAsync(id);

        return RedirectToAction(nameof(Categories));
    }
}