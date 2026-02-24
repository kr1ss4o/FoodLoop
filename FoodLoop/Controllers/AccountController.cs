using FoodLoop.Models.Entities;
using FoodLoop.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // ---------------------------------------
        // REGISTER (GET)
        // ---------------------------------------
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // ---------------------------------------
        // REGISTER (POST)
        // ---------------------------------------
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check if email already exists
            var existingEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingEmail != null)
            {
                ModelState.AddModelError("Email", "Email is already in use.");
                return View(model);
            }

            // Check if phone already exists
            var existingPhone = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == model.Phone);

            if (existingPhone != null)
            {
                ModelState.AddModelError("Phone", "Phone number is already in use.");
                return View(model);
            }

            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                UserName = model.Email,
                PhoneNumber = model.Phone,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }

            // Add default role
            await _userManager.AddToRoleAsync(user, "Client");

            // Instead of auto-login → redirect to Login page
            return RedirectToAction("Login", "Account");
        }

        // ---------------------------------------
        // LOGIN (GET)
        // ---------------------------------------
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // ---------------------------------------
        // LOGIN (POST)
        // ---------------------------------------
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                TempData["Error"] = "Invalid email or password.";
                return View(model);
            }

            // Successful login
            TempData["Success"] = "Logged in successfully!";

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return RedirectToAction("Index", "Admin");

            if (await _userManager.IsInRoleAsync(user, "Restaurant"))
                return RedirectToAction("Index", "RestaurantDashboard");

            return RedirectToAction("Index", "Profile");
        }

        // ---------------------------------------
        // LOGOUT
        // ---------------------------------------
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["Success"] = "You have logged out.";
            return RedirectToAction("Login", "Account");
        }

        //
        //  Forgot Password (GET)
        //
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        //
        // Forgot Password (POST)
        //

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["Error"] = "No account found with this email.";
                return View();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Create reset URL
            var resetUrl = Url.Action(
                "ResetPassword",
                "Account",
                new { email = user.Email, token = token },
                Request.Scheme
            );

            ViewBag.ResetLink = resetUrl;
            TempData["Success"] = "Password reset link has been generated.";

            return View("ForgotPasswordConfirmation");
        }

        //
        //  Reset Password (GET)
        //

        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (email == null || token == null)
                return RedirectToAction("Login");

            return View(new ResetPasswordViewModel { Email = email, Token = token });
        }

        //
        //  Reset Password (POST)
        //

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["Error"] = "Invalid request.";
                return RedirectToAction("Login");
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }

            TempData["Success"] = "Password has been reset successfully!";
            return RedirectToAction("Login");
        }

    }
}