using CakeHut.Data;
using CakeHut.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Security.Claims;

namespace CakeHut.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IConfiguration configuration;

        public AccountController(UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager, IConfiguration configuration)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
        }
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Register registerDto)
        {
            if (!ModelState.IsValid)
            {
                return View(registerDto);
            }
            try
            {
                var user = new ApplicationUser()
                {
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName,
                    PhoneNumber = registerDto.PhoneNumber,
                    UserName = registerDto.Email,
                    Email = registerDto.Email,
                    CreatedDate = DateTime.Now,
                };
                var result = await userManager.CreateAsync(user, registerDto.Password);


                if (result.Succeeded)
                {
                    string otp = GenerateOTP();
                    Console.WriteLine("OTP GENERATED : " + otp);
                    TempData["OTP"] = otp;
                    TempData["UserId"] = user.Id;
                    TempData["OTPCreatedTime"] = DateTime.Now;

                    string subject = "OTP for Account Verification";
                    string message = $"Dear {user.FirstName},\n\nYour OTP for account verification is: {otp}";

                    string senderName = configuration["BrevoSettings:SenderName"] ?? "";
                    string senderEmail = configuration["BrevoSettings:SenderEmail"] ?? "";

                    EmailSender.SendEmail(senderName, senderEmail, $"{user.FirstName} {user.LastName}", user.Email, subject, message);

                    return RedirectToAction("VerifyOTP");
                }
                ViewBag.ErrorMessage = "User registration failed. Please try again.";
            }
            catch (SmtpException ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
                ViewBag.ErrorMessage = "There was an error sending the verification email. Please try again later.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                ViewBag.ErrorMessage = "An unexpected error occurred. Please try again.";
            }

            return View(registerDto);
        }

        //Generate OTP
        public string GenerateOTP(int length = 6)
        {
            Random random = new Random();
            string otp = "";
            for (int i = 0; i < length; i++)
            {
                otp += random.Next(0, 10).ToString(); 
            }
            return otp;
        }

        [HttpPost]
        public async Task<IActionResult> ResendOTP()
        {
            if (TempData["UserId"] != null)
            {
                string userId = TempData["UserId"].ToString();
                var user = await userManager.FindByIdAsync(userId);

                if (user != null)
                {
                    // Generate new OTP
                    string otp = GenerateOTP();
                    TempData["OTP"] = otp;
                    TempData["OTPCreatedTime"] = DateTime.Now;

                    string senderName = configuration["BrevoSettings:SenderName"] ?? "";
                    string senderEmail = configuration["BrevoSettings:SenderEmail"] ?? "";

                    // Send OTP via email
                    string subject = "New OTP for Account Verification";
                    string message = $"Dear {user.FirstName},\n\nYour new OTP for account verification is: {otp}";
                    EmailSender.SendEmail(senderName, senderEmail, $"{user.FirstName} {user.LastName}", user.Email, subject, message);

                    ViewBag.SuccessMessage = "A new OTP has been sent to your email.";
                    return RedirectToAction("VerifyOTP");
                }
            }

            ViewBag.ErrorMessage = "Unable to resend OTP. Please try again.";
            return View("VerifyOTP");
        }

        public IActionResult VerifyOTP()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOTP(string otp)
        {
            string storedOtp = TempData.Peek("OTP")?.ToString(); 
            string userId = TempData.Peek("UserId")?.ToString(); 
            DateTime? otpCreatedTime = TempData.Peek("OTPCreatedTime") as DateTime?;

            TempData.Keep("OTP");
            TempData.Keep("UserId");
            TempData.Keep("OTPCreatedTime");

            Console.WriteLine("OTP ENTERED : " + otp);

            Console.WriteLine("storedOtp SAVED : " + storedOtp);
            Console.WriteLine("userId SAVED : " + userId);
            Console.WriteLine("otpCreatedTime SAVED : " + otpCreatedTime);

            if (string.IsNullOrEmpty(storedOtp) || string.IsNullOrEmpty(userId) || otpCreatedTime == null)
            {
                ViewBag.ErrorMessage = "OTP verification failed. Please request a new OTP.";
                return View();
            }

            TimeSpan otpExpiryDuration = TimeSpan.FromMinutes(5);
            DateTime otpExpiryTime = otpCreatedTime.Value.Add(otpExpiryDuration);

            ViewBag.ExpiryTime = otpExpiryTime;

            if (DateTime.Now > otpExpiryTime)
            {
                ViewBag.ErrorMessage = "Your OTP has expired. Please request a new OTP.";
                return View(); 
            }

            if (otp == storedOtp)
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    user.EmailConfirmed = true;
                    await userManager.UpdateAsync(user);

                    await userManager.AddToRoleAsync(user, "user");

                    await signInManager.SignInAsync(user, false);

                    TempData.Remove("OTP");
                    TempData.Remove("OTPCreatedTime");

                    ViewBag.SuccessMessage = "Your account has been verified successfully!";
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                ViewBag.ErrorMessage = "Invalid OTP. Please try again.";
                TempData.Keep("OTP"); 
                return View();
            }

            ViewBag.ErrorMessage = "OTP verification failed. Please try again.";
            return View();
        }


        public async Task<IActionResult> Logout()
        {
            if (signInManager.IsSignedIn(User))
            {
                await signInManager.SignOutAsync();
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return View(loginDto);
            }

			var user = await userManager.FindByEmailAsync(loginDto.Email);

			if (user != null && user.IsBlocked)
			{
				ViewBag.ErrorMessage = "Your account has been blocked. Please contact support.";
				return View(loginDto);
			}

			var result = await signInManager.PasswordSignInAsync(loginDto.Email, loginDto.Password, loginDto.RememberMe, false);

            if (result.Succeeded)
            {
                if (await userManager.IsInRoleAsync(user, "admin"))
                {
                    return RedirectToAction("AdminHome", "Home"); 
                }
                else if (await userManager.IsInRoleAsync(user, "user"))
                {
                    return RedirectToAction("Index", "Home"); 
                }
            }
            else
            {
                ViewBag.ErrorMessage = "Invalid login attempt.";
            }

            return View(loginDto);
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var appUser = await userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var profileDto = new ProfileDto()
            {
                FirstName = appUser.FirstName,
                LastName = appUser.LastName,
                PhoneNumber = appUser.PhoneNumber,
                Email = appUser.Email ?? ""
            };

            return View(profileDto);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Profile(ProfileDto profileDto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = "Please fill all the required fields with valid values";
                return View(profileDto);
            }

            var appUser = await userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            appUser.FirstName = profileDto.FirstName;
            appUser.LastName = profileDto.LastName;
            appUser.UserName = profileDto.Email;
            appUser.PhoneNumber = profileDto.PhoneNumber;
            appUser.Email = profileDto.Email;

            var result = await userManager.UpdateAsync(appUser);

            if (result.Succeeded)
            {
                ViewBag.SuccessMessage = "Profile updated successfully";
            }
            else
            {
                ViewBag.ErrorMessage = "Unable to update the profile: " + result.Errors.First().Description;
            }


            return View(profileDto);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [Authorize]
		public IActionResult Password()
		{
			return View();
		}

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Password(PasswordDto passwordDto)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            var appUser =  await userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var result = await userManager.ChangePasswordAsync(appUser,
                passwordDto.CurrentPassword, passwordDto.NewPassword);

            if (result.Succeeded)
            {
                ViewBag.SuccessMessage = "Password updated successfully";
            }
            else
            {
                ViewBag.ErrorMessage = "Unable to update the Password: " + result.Errors.First().Description;
            }

			return RedirectToAction("Profile");
		}

        //Forgot Password
        public IActionResult ForgotPassword()
		{
            if (signInManager.IsSignedIn(User))
            {
				return RedirectToAction("Index", "Home");
			}

			return View();
		}

        [HttpPost]
        public async Task<IActionResult> ForgotPassword([Required, EmailAddress] string email)
        {
            if (signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Email = email;

            if (!ModelState.IsValid) { 
                ViewBag.EmailError = ModelState["email"]?.Errors.First().ErrorMessage ?? "Invalid Email Address";  
                return View();
            }

            var user = await userManager.FindByEmailAsync(email);

            if (user != null) 
            { 
                //generate password reset token
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                string restUrl = Url.ActionLink("ResetPassword", "Account", new { token }) ?? "URL Error";

                Console.WriteLine("PASSWORD RESET LINK:" + restUrl);

                //send url by email
                string senderName = configuration["BrevoSettings:SenderName"] ?? "";
                string senderEmail = configuration["BrevoSettings:SenderEmail"] ?? "";
                string username = user.FirstName + " " + user.LastName;
                string subject = "Password Reset";
                string message = "Dear " + username + ",\n\n" +
                    "You can reset your password using the following link:\n\n" +
                    restUrl + "\n\n" +
                    "Best Regards";

                EmailSender.SendEmail(senderName, senderEmail, username, email, subject, message);
            }

            ViewBag.SuccessMessage = "Please check your Email account and click on the Password Reset link!";

            return View();
        }

        //Reset Password
        public IActionResult ResetPassword(string? token)
        {
            if (signInManager.IsSignedIn(User) || token == null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string? token, PasswordResetDto passwordResetDto)
        {
            if (signInManager.IsSignedIn(User) || token == null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(passwordResetDto);
            }

            var appUser = await userManager.FindByEmailAsync(passwordResetDto.Email);
            if (appUser == null)
            {
                ViewBag.ErrorMessage = "Token not valid";
                return View(passwordResetDto);
            }

            var result = await userManager.ResetPasswordAsync(appUser, token, passwordResetDto.Password);

            if (result.Succeeded)
            {
                ViewBag.SuccessMessage = "Password reset successfully";
            }
            else
            {
                foreach(var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Account");
            var properties = signInManager.ConfigureExternalAuthenticationProperties(GoogleDefaults.AuthenticationScheme, redirectUrl);
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // Handles Google Login Response
        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            // Authenticate user with external provider
            var externalLoginInfo = await signInManager.GetExternalLoginInfoAsync();
            if (externalLoginInfo == null)
            {
                TempData["ErrorMessage"] = "Error loading external login information.";
                return RedirectToAction("Login");
            }

            // Check if user exists in the system
            var user = await userManager.FindByLoginAsync(externalLoginInfo.LoginProvider, externalLoginInfo.ProviderKey);
            if (user == null)
            {
                // Retrieve email from external provider
                var email = externalLoginInfo.Principal.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(email))
                {
                    TempData["ErrorMessage"] = "Email address is required.";
                    return RedirectToAction("Login");
                }

                // Check if email exists in the system
                user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    // Create a new user if not found
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FirstName = externalLoginInfo.Principal.FindFirstValue(ClaimTypes.GivenName),
                        LastName = externalLoginInfo.Principal.FindFirstValue(ClaimTypes.Surname),
                        EmailConfirmed = true,
                        CreatedDate = DateTime.Now
                    };

                    var createResult = await userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        TempData["ErrorMessage"] = "Error creating user: " + string.Join(", ", createResult.Errors.Select(e => e.Description));
                        return RedirectToAction("Login");
                    }
                }

                // Add external login info
                var addLoginResult = await userManager.AddLoginAsync(user, externalLoginInfo);
                if (!addLoginResult.Succeeded)
                {
                    TempData["ErrorMessage"] = "Error adding external login: " + string.Join(", ", addLoginResult.Errors.Select(e => e.Description));
                    return RedirectToAction("Login");
                }
            }

            // Sign in the user
            await signInManager.SignInAsync(user, isPersistent: false);

            // Redirect based on role
            if (await userManager.IsInRoleAsync(user, "admin"))
            {
                return RedirectToAction("AdminHome", "Home");
            }

            return RedirectToAction("Index", "Home");
        }

    }
}
