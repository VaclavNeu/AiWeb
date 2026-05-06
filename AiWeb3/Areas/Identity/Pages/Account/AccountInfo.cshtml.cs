using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace AiWeb3.Areas.Identity.Pages.Account
{
    [Authorize]
    public class AccountInfoModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<AccountInfoModel> _logger;

        public AccountInfoModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, ILogger<AccountInfoModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        public string Email { get; private set; } = "";
        public string UserName { get; private set; } = "";
        public bool EmailConfirmed { get; private set; }
        public bool TwoFactorEnabled { get; private set; }
        [TempData] public string? ErrorMessage { get; set; }
        [TempData] public string? SuccessMessage { get; set; }

        [BindProperty]
        public bool ShowChangePassword { get; set; }
        [BindProperty]
        public ChangePasswordInput Input { get; set; } = new();

        public class ChangePasswordInput
        {
            [Required(ErrorMessage = "Zadej aktuální heslo.")]
            [DataType(DataType.Password)]
            public string CurrentPassword { get; set; } = "";
            [Required(ErrorMessage = "Zadej nové heslo.")]
            public string NewPassword { get; set; } = "";
            [Required(ErrorMessage = "Potvrď nové heslo.")]
            [Compare(nameof(NewPassword), ErrorMessage = "Hesla se neshodují.")]
            public string ConfirmNewPassword { get; set; } = "";
        }
        public async Task LoadUserInfoAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return;

            Email = await _userManager.GetEmailAsync(user) ?? "";
            EmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
            UserName = await _userManager.GetUserNameAsync(user) ?? "";
            TwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            await LoadUserInfoAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            _logger.LogInformation("OnPostDelete hit");
            var user = await _userManager.GetUserAsync(User);
            _logger.LogInformation("User = {UserId}", user?.Id);

            if (user == null)
            {
                _logger.LogWarning("User not found in delete");
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToPage();
            }

            try
            {
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    _logger.LogWarning("Delete failed for {UserId}: {Errors}", user.Id, errors);
                    TempData["ErrorMessage"] = errors;
                    return RedirectToPage();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while deleting {UserId}", user.Id);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage();
            }

            await _signInManager.SignOutAsync();
            _logger.LogInformation("User {UserId} deleted & signed out", user.Id);
            return Redirect("/");
        }

        public async Task<IActionResult> OnPostShowChangePassword()
        {
            ShowChangePassword = true;

            ModelState.Clear(); // aby se hned neukázaly required chyby
            Input = new();

            await LoadUserInfoAsync();  
            return Page();
        }
        public async Task<IActionResult> OnPostCancelChangePassword()
        {
            ShowChangePassword = false;
            ModelState.Clear();
            Input = new();

            await LoadUserInfoAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
           
            ShowChangePassword = true;

            await LoadUserInfoAsync();

            if (!ModelState.IsValid) 
            {
                Input.NewPassword = "";
                Input.ConfirmNewPassword = "";
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if(user == null)
            {
                TempData["ErrorMessage"] = "Uživatel nebyl nalezen";
                return RedirectToPage("/Account/Login", new {area = "Identity"});
            }

            var result = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);

                

                Input.NewPassword = "";
                Input.ConfirmNewPassword = ""; 
                return Page();
            }


            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Heslo bylo úspěšně změněno.";
            return RedirectToPage();
        }
    }
    }
