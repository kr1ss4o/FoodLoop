using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodLoop.Models.Entities;
using FoodLoop.Models.ViewModels;
using FoodLoop.Data;
using FoodLoop.Services;

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
        var categoriesQuery = _context.Categories.AsQueryable();

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

        var pagination = new PaginationViewModel
        {
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize),
            Controller = "AdminCategories",
            Action = "Categories",
            Query = query
        };

        ViewBag.Pagination = pagination;

        return AdminView("Categories", categories);
    }

    // =====================================================
    // CREATE
    // =====================================================

    public IActionResult Create()
    {
        return AdminCreate(new Category());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category model)
    {
        if (!ModelState.IsValid)
            return AdminCreate(model);

        _context.Categories.Add(model);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Categories));
    }

    // =====================================================
    // EDIT
    // =====================================================

    public async Task<IActionResult> Edit(Guid id)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
            return NotFound();

        return AdminEdit(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Category input)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == input.Id);

        if (category == null)
            return NotFound();

        category.Name = input.Name;
        category.IconUrl = input.IconUrl;

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