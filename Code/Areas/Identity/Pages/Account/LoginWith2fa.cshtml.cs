using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using CloudCityCakeCo.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using CloudCityCakeCo.Services.Interfaces;

namespace CloudCityCakeCo.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginWith2faModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<LoginWith2faModel> _logger;
        private readonly IAuthyService _authyService;

        public LoginWith2faModel(SignInManager<User> signInManager,
            ILogger<LoginWith2faModel> logger,
            IAuthyService authyService)
        {
            _signInManager = signInManager;
            _logger = logger;
            _authyService = authyService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }

        public bool IsRegistration { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Text)]
            [Display(Name = "Authenticator code")]
            public string TwoFactorCode { get; set; }

            [Display(Name = "Remember this machine")]
            public bool RememberMachine { get; set; }
          
        }

        public async Task<IActionResult> OnGetAsync(
            bool rememberMe,
            string returnUrl = null,
            bool isRegistration = false)
        {
            IsRegistration = isRegistration;
           
            User user = IsRegistration ?
                        await _signInManager.UserManager.GetUserAsync(User) :
                        await _signInManager.GetTwoFactorAuthenticationUserAsync();
            //todo: swap between register and login
            //var user = await _signInManager.UserManager.GetUserAsync(User);
            //Ensure the user has gone through the username & password screen first
            //var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();



            if (user == null)
            {
                throw new InvalidOperationException($"Unable to load two-factor authentication user.");
            }

            var _ = await _authyService.SendSmsAsync(user.AuthyId);
            ReturnUrl = returnUrl;
            RememberMe = rememberMe;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(
            bool rememberMe,
            bool isRegistration,
            string returnUrl = null)
        {

            if (!ModelState.IsValid)
            {
                return Page();
            }
            IsRegistration = isRegistration;
            returnUrl = returnUrl ?? Url.Content("~/");
            
            User user = IsRegistration ?
                        await _signInManager.UserManager.GetUserAsync(User) :
                        await _signInManager.GetTwoFactorAuthenticationUserAsync();

            if (user == null)
            {
               
                throw new InvalidOperationException($"Unable to load two-factor authentication user.");
            }

            var tokenResult =
                await _authyService.VerifyPhoneTokenAsync(user.AuthyId, Input.TwoFactorCode);

            if (!tokenResult.Succeeded)
            {
                //todo: UI error notifications
                return Page();
            }
            //var authenticatorCode = Input.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);
            //todo: check that the response from Authy is Successful then log someone in
            var result =
                await _signInManager.CanSignInAsync(user);


            if (result)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation("User with ID '{UserId}' logged in with 2fa.", user.Id);
                // await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl);
            }
            // else if (result.IsLockedOut)
            // {
            //     _logger.LogWarning("User with ID '{UserId}' account locked out.", user.Id);
            //     return RedirectToPage("./Lockout");
            // }
            else
            {
                _logger.LogWarning("Invalid authenticator code entered for user with ID '{UserId}'.", user.Id);
                ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
                return Page();
            }
        }
    }
}
