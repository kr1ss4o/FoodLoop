using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FoodLoop.Models.Entities;
using FoodLoop.Models.ViewModels;
using FoodLoop.Data;
using FoodLoop.ViewModels.Admin;

namespace FoodLoop.Controllers.Admin;

public class AdminClientsController : AdminBaseController
{
    private const int PageSize = 10;
    private readonly UserManager<User> _userManager;

    public AdminClientsController(
        ApplicationDbContext context,
        UserManager<User> userManager)
        : base(context)
    {
        _userManager = userManager;
    }

    // =====================================================
    // LIST
    // =====================================================

    public async Task<IActionResult> Clients(string? query, int page = 1)
    {
        // Вземи само потребители с роля "Client" (без ресторантьори)
        var clientRoleUsers = await _userManager.GetUsersInRoleAsync("Client");

        var clientsQuery = clientRoleUsers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            clientsQuery = clientsQuery.Where(u =>
                (u.FullName != null && u.FullName.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (u.Email != null && u.Email.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        var totalItems = clientsQuery.Count();

        var clients = clientsQuery
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        ViewBag.Pagination = new PaginationViewModel
        {
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize),
            Controller = "AdminClients",
            Action = "Clients",
            Query = query
        };

        return AdminView("Clients", clients);
    }

    // =====================================================
    // DETAILS
    // =====================================================

    public async Task<IActionResult> Details(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
            return NotFound();

        // Ако е бизнес роля — пренасочи към ресторантите
        if (await _userManager.IsInRoleAsync(user, "Restaurant"))
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Owner.Id == user.Id);

            if (restaurant != null)
                return RedirectToAction("Details", "AdminRestaurants", new { id = restaurant.Id });
        }

        return AdminDetails(user);
    }

    // =====================================================
    // EDIT
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
            return NotFound();

        // Ако е бизнес роля — пренасочи
        if (await _userManager.IsInRoleAsync(user, "Restaurant"))
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.OwnerUserId == user.Id);

            if (restaurant != null)
                return RedirectToAction("Edit", "AdminRestaurants", new { id = restaurant.Id });
        }

        var vm = new AdminFormViewModel
        {
            Type = "Client",
            Id = Guid.Parse(user.Id),
            ClientFullName = user.FullName,
            ClientEmail = user.Email,
            ClientPhone = user.PhoneNumber,
            ClientImageUrl = user.ProfileImageUrl,
            ClientCreatedAt = user.CreatedAt
        };

        return AdminEdit(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminFormViewModel vm)
    {
        if (vm.Id == null)
        {
            Error("Невалиден идентификатор.");
            return RedirectToAction(nameof(Clients));
        }

        var user = await _userManager.FindByIdAsync(vm.Id.ToString()!);

        if (user == null)
        {
            Error("Потребителят не беше намерен.");
            return RedirectToAction(nameof(Clients));
        }

        if (!ModelState.IsValid)
        {
            Warning("Невалидни данни при редакция на профил.");
            return AdminEdit(vm);
        }

        user.FullName = vm.ClientFullName!;
        user.PhoneNumber = vm.ClientPhone;
        user.ProfileImageUrl = vm.ClientImageUrl;

        // Ако имейлът е сменен — обнови го през Identity
        if (user.Email != vm.ClientEmail)
        {
            var setEmailResult = await _userManager.SetEmailAsync(user, vm.ClientEmail);
            if (!setEmailResult.Succeeded)
            {
                foreach (var err in setEmailResult.Errors)
                    ModelState.AddModelError("", err.Description);

                Error("Грешка при смяна на имейл.");
                return AdminEdit(vm);
            }

            await _userManager.SetUserNameAsync(user, vm.ClientEmail);
        }

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            foreach (var err in updateResult.Errors)
                ModelState.AddModelError("", err.Description);

            Error("Грешка при запис.");
            return AdminEdit(vm);
        }

        Success("Профилът беше редактиран успешно.");
        return RedirectToAction(nameof(Clients));
    }

    // =====================================================
    // DELETE
    // =====================================================

    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user != null)
        {
            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
                Info("Профилът беше изтрит.");
            else
                Error("Грешка при изтриване на профила.");
        }

        return RedirectToAction(nameof(Clients));
    }
}