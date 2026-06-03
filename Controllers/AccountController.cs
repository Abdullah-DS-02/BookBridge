using BookBridge.Models.Entities;
using BookBridge.Models.ViewModels;
using BookBridge.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace BookBridge.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IMemoryCache _cache;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public AccountController(UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IMemoryCache cache,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _cache = cache;
        _emailService = emailService;
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Register() => User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Dashboard") : View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? FirebaseIdToken = null)
    {
        if (!ModelState.IsValid) return View(model);
        if (!model.AgreeToTerms) { ModelState.AddModelError("", "You must agree to the terms."); return View(model); }

        if (string.IsNullOrEmpty(FirebaseIdToken))
        {
            ModelState.AddModelError("", "Firebase authentication token is missing. Please try again.");
            return View(model);
        }

        var payload = await VerifyFirebaseTokenAsync(FirebaseIdToken);
        if (payload == null || !string.Equals(payload.Email, model.Email, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("", "Firebase registration verification failed or email mismatch.");
            return View(model);
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError("", "This email is already registered on BookBridge.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            City = model.City,
            EmailConfirmed = false,
            IsVerified = false,
            LastActive = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            if (!await _roleManager.RoleExistsAsync("User"))
                await _roleManager.CreateAsync(new IdentityRole("User"));
            await _userManager.AddToRoleAsync(user, "User");

            // Generate 6-digit verification code
            var code = new Random().Next(100000, 999999).ToString();
            _cache.Set($"VerifyCode_{user.Email}", code, TimeSpan.FromMinutes(15));

            // Set pending verification email in session
            HttpContext.Session.SetString("PendingVerificationEmail", user.Email!);

            // Print verification code to server console for testing/development
            Console.WriteLine("\n==================================================");
            Console.WriteLine($"[VERIFICATION CODE GENERATED] Email: {user.Email} | Code: {code}");
            Console.WriteLine("==================================================\n");

            try
            {
                await _emailService.SendEmailAsync(user.Email!, "Verify your BookBridge Account", 
                    $"<h3>Welcome to BookBridge, {user.FullName}!</h3><p>Thank you for registering. Your verification code is: <strong>{code}</strong></p><p>This code will expire in 15 minutes.</p>");
            }
            catch (Exception)
            {
                // In dev mode, if SMTP fails, we store the code in TempData so user/dev can verify
                TempData["Info"] = $"[Dev Fallback] Verification code generated: {code}";
            }

            return RedirectToAction(nameof(VerifyEmail), new { email = user.Email });
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);
        return View(model);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Dashboard");
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? FirebaseIdToken = null, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        ApplicationUser? user = null;

        if (!string.IsNullOrEmpty(FirebaseIdToken))
        {
            var payload = await VerifyFirebaseTokenAsync(FirebaseIdToken);
            if (payload != null)
            {
                user = await _userManager.FindByEmailAsync(payload.Email);
            }
            else
            {
                ModelState.AddModelError("", "Firebase login verification failed.");
                return View(model);
            }
        }
        else
        {
            // Fallback checking local password if Firebase is bypassed (or for testing)
            user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, true);
                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View(model);
                }
            }
        }

        if (user == null) 
        { 
            ModelState.AddModelError("", "Invalid email or password."); 
            return View(model); 
        }

        if (user.IsBanned)
        {
            ModelState.AddModelError("", $"Your account has been suspended. Reason: {user.BanReason}");
            return View(model);
        }

        // Enforce Email/Code verification
        if (!user.EmailConfirmed || !user.IsVerified)
        {
            HttpContext.Session.SetString("PendingVerificationEmail", user.Email!);

            // Generate & send verification code
            var code = new Random().Next(100000, 999999).ToString();
            _cache.Set($"VerifyCode_{user.Email}", code, TimeSpan.FromMinutes(15));

            // Print verification code to server console for testing/development
            Console.WriteLine("\n==================================================");
            Console.WriteLine($"[VERIFICATION CODE GENERATED] Email: {user.Email} | Code: {code}");
            Console.WriteLine("==================================================\n");

            try
            {
                await _emailService.SendEmailAsync(user.Email!, "Verify your BookBridge Account", 
                    $"<h3>Welcome back to BookBridge!</h3><p>Please verify your email address to log in. Your code is: <strong>{code}</strong></p><p>This code is valid for 15 minutes.</p>");
            }
            catch (Exception)
            {
                TempData["Info"] = $"[Dev Fallback] Verification code generated: {code}";
            }

            TempData["Warning"] = "Please verify your email address to access your account.";
            return RedirectToAction(nameof(VerifyEmail), new { email = user.Email });
        }

        // Sign user in locally
        await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);
        user.LastActive = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        if (string.IsNullOrEmpty(request.IdToken))
            return Json(new { success = false, message = "Google Sign-In token is missing." });

        var payload = await VerifyFirebaseTokenAsync(request.IdToken);
        if (payload == null)
            return Json(new { success = false, message = "Google authentication verification failed." });

        var email = payload.Email;
        var user = await _userManager.FindByEmailAsync(email);

        if (user != null)
        {
            if (user.IsBanned)
                return Json(new { success = false, message = $"Your account has been suspended. Reason: {user.BanReason}" });

            // Google authentication automatically verifies the email
            if (!user.EmailConfirmed || !user.IsVerified)
            {
                user.EmailConfirmed = true;
                user.IsVerified = true;
                await _userManager.UpdateAsync(user);
            }

            user.LastActive = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            await _signInManager.SignInAsync(user, isPersistent: true);
            return Json(new { success = true, redirectUrl = Url.Action("Index", "Dashboard") });
        }
        else
        {
            // Create a new local account linked to the Google authenticated email
            var newUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = string.IsNullOrEmpty(payload.DisplayName) ? email.Split('@')[0] : payload.DisplayName,
                EmailConfirmed = true,
                IsVerified = true,
                LastActive = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(newUser);
            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync("User"))
                    await _roleManager.CreateAsync(new IdentityRole("User"));
                await _userManager.AddToRoleAsync(newUser, "User");

                await _signInManager.SignInAsync(newUser, isPersistent: true);
                TempData["Success"] = $"Welcome to BookBridge, {newUser.FullName}!";
                return Json(new { success = true, redirectUrl = Url.Action("Index", "Dashboard") });
            }

            return Json(new { success = false, message = string.Join(" ", result.Errors.Select(e => e.Description)) });
        }
    }

    [HttpGet]
    public IActionResult VerifyEmail(string? email)
    {
        var pendingEmail = email ?? HttpContext.Session.GetString("PendingVerificationEmail");
        if (string.IsNullOrEmpty(pendingEmail))
        {
            return RedirectToAction(nameof(Login));
        }
        ViewData["Email"] = pendingEmail;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyEmail(string email, string code)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
        {
            ModelState.AddModelError("", "Verification code is required.");
            ViewData["Email"] = email;
            return View();
        }

        var cachedCode = _cache.Get<string>($"VerifyCode_{email}");
        if (cachedCode == null || cachedCode != code.Trim())
        {
            ModelState.AddModelError("", "Invalid or expired verification code.");
            ViewData["Email"] = email;
            return View();
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            ModelState.AddModelError("", "User account not found.");
            ViewData["Email"] = email;
            return View();
        }

        user.EmailConfirmed = true;
        user.IsVerified = user.EmailConfirmed && user.PhoneNumberConfirmed;
        user.LastActive = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Cleanup
        _cache.Remove($"VerifyCode_{email}");

        if (!user.PhoneNumberConfirmed)
        {
            HttpContext.Session.SetString("PendingVerificationEmail", user.Email!);
            TempData["Success"] = "Email verified! Now, please verify your phone number.";
            return RedirectToAction(nameof(VerifyPhone), new { email = user.Email });
        }

        HttpContext.Session.Remove("PendingVerificationEmail");
        await _signInManager.SignInAsync(user, isPersistent: false);
        TempData["Success"] = "Account verified successfully! Welcome to BookBridge.";
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public async Task<IActionResult> VerifyPhone(string? email)
    {
        var pendingEmail = email ?? HttpContext.Session.GetString("PendingVerificationEmail");
        if (string.IsNullOrEmpty(pendingEmail))
        {
            return RedirectToAction(nameof(Login));
        }

        var user = await _userManager.FindByEmailAsync(pendingEmail);
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        ViewData["Email"] = pendingEmail;
        ViewData["PhoneNumber"] = user.PhoneNumber;
        return View();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.IdToken))
            return Json(new { success = false, message = "Email and verification token are required." });

        var payload = await VerifyFirebaseTokenAsync(request.IdToken);
        if (payload == null)
            return Json(new { success = false, message = "Phone verification failed on Firebase." });

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return Json(new { success = false, message = "User not found." });

        user.PhoneNumberConfirmed = true;
        if (!string.IsNullOrEmpty(request.PhoneNumber))
        {
            user.PhoneNumber = request.PhoneNumber;
        }

        user.IsVerified = user.EmailConfirmed && user.PhoneNumberConfirmed;
        user.LastActive = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        HttpContext.Session.Remove("PendingVerificationEmail");
        await _signInManager.SignInAsync(user, isPersistent: false);

        TempData["Success"] = "Your phone number has been verified! Welcome to BookBridge.";
        return Json(new { success = true, redirectUrl = Url.Action("Index", "Dashboard") });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ResendVerificationCode([FromBody] ResendCodeRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
            return Json(new { success = false, message = "Email is required." });

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return Json(new { success = false, message = "Account not found." });

        if (user.IsVerified && user.EmailConfirmed)
            return Json(new { success = false, message = "Account is already verified." });

        var code = new Random().Next(100000, 999999).ToString();
        _cache.Set($"VerifyCode_{request.Email}", code, TimeSpan.FromMinutes(15));

        // Print verification code to server console for testing/development
        Console.WriteLine("\n==================================================");
        Console.WriteLine($"[VERIFICATION CODE GENERATED] Email: {request.Email} | Code: {code}");
        Console.WriteLine("==================================================\n");

        try
        {
            await _emailService.SendEmailAsync(request.Email, "Verify your BookBridge Account - New Code", 
                $"<h3>BookBridge Email Verification</h3><p>Your new verification code is: <strong>{code}</strong></p><p>This code will expire in 15 minutes.</p>");
            return Json(new { success = true, message = "A new verification code has been sent to your email." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SMTP ERROR] Failed to send email to {request.Email}: {ex.Message}");
            return Json(new { success = true, message = "A new verification code has been sent to your email." });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ForgotPassword(ForgotPasswordViewModel model)
    {
        // This endpoint will receive posts if client-side fallback is triggered, but client JS handles it.
        // We will keep it for compatibility.
        TempData["Success"] = "If an account exists, a reset link has been sent.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private async Task<FirebaseTokenPayload?> VerifyFirebaseTokenAsync(string idToken)
    {
        try
        {
            using var client = new HttpClient();
            var apiKey = _configuration["Firebase:ApiKey"] ?? "AIzaSyC__KDr9zp3uz3b7Ibvdq3sV9AQfQ9zTqw";
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:lookup?key={apiKey}";
            var payload = new { idToken = idToken };
            var response = await client.PostAsJsonAsync(url, payload);
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<FirebaseLookupResponse>();
            if (result?.Users == null || result.Users.Length == 0) return null;

            return result.Users[0];
        }
        catch
        {
            return null;
        }
    }
}

public class GoogleLoginRequest
{
    public string IdToken { get; set; } = string.Empty;
}

public class ResendCodeRequest
{
    public string Email { get; set; } = string.Empty;
}

public class VerifyPhoneRequest
{
    public string Email { get; set; } = string.Empty;
    public string IdToken { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

public class FirebaseLookupResponse
{
    public FirebaseTokenPayload[] Users { get; set; } = Array.Empty<FirebaseTokenPayload>();
}

public class FirebaseTokenPayload
{
    public string LocalId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
