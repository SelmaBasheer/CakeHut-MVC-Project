using CakeHut.Data;
using CakeHut.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

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
                    // Generate OTP
                    string otp = GenerateOTP();
                    Console.WriteLine("OTP GENERATED : " + otp);
                    // Store the OTP in TempData or Session 
                    TempData["OTP"] = otp;
                    TempData["UserId"] = user.Id;
                    TempData["OTPCreatedTime"] = DateTime.Now;


                    // Send OTP via email
                    string subject = "OTP for Account Verification";
                    string message = $"Dear {user.FirstName},\n\nYour OTP for account verification is: {otp}";

                    //string apiKey = configuration["BrevoSettings:ApiKey"];
                    string senderName = configuration["BrevoSettings:SenderName"] ?? "";
                    string senderEmail = configuration["BrevoSettings:SenderEmail"] ?? "";

                    EmailSender.SendEmail(senderName, senderEmail, $"{user.FirstName} {user.LastName}", user.Email, subject, message);

                    // Redirect to OTP verification page
                    return RedirectToAction("VerifyOTP");

                    //await userManager.AddToRoleAsync(user, "user");

                    //await signInManager.SignInAsync(user, false);

                    //return RedirectToAction("Index", "Home");
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

        public string GenerateOTP(int length = 6)
        {
            Random random = new Random();
            string otp = "";
            for (int i = 0; i < length; i++)
            {
                otp += random.Next(0, 10).ToString(); // Generates a random digit
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
            // Safely check and retrieve values from TempData
            string storedOtp = TempData.Peek("OTP")?.ToString(); // Safely retrieve OTP from TempData
            string userId = TempData.Peek("UserId")?.ToString(); // Safely retrieve UserId from TempData
            DateTime? otpCreatedTime = TempData.Peek("OTPCreatedTime") as DateTime?;

            // Ensure TempData values persist across requests
            TempData.Keep("OTP");
            TempData.Keep("UserId");
            TempData.Keep("OTPCreatedTime");

            Console.WriteLine("OTP ENTERED : " + otp);

            Console.WriteLine("storedOtp SAVED : " + storedOtp);
            Console.WriteLine("userId SAVED : " + userId);
            Console.WriteLine("otpCreatedTime SAVED : " + otpCreatedTime);

            // Check if any of the required TempData values are null
            if (string.IsNullOrEmpty(storedOtp) || string.IsNullOrEmpty(userId) || otpCreatedTime == null)
            {
                ViewBag.ErrorMessage = "OTP verification failed. Please request a new OTP.";
                return View();
            }

            // OTP expiry check
            TimeSpan otpExpiryDuration = TimeSpan.FromMinutes(5);
            DateTime otpExpiryTime = otpCreatedTime.Value.Add(otpExpiryDuration);

            ViewBag.ExpiryTime = otpExpiryTime;

            if (DateTime.Now > otpExpiryTime)
            {
                ViewBag.ErrorMessage = "Your OTP has expired. Please request a new OTP.";
                return View(); // Return to the same view to request a new OTP
            }

            // Null check on OTP before calling Trim()
            //if (!string.IsNullOrEmpty(otp) && storedOtp.Trim() == otp.Trim())
            if (otp == storedOtp)
            {
                // OTP matches, proceed with account verification
                var user = await userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    // Mark email as confirmed
                    user.EmailConfirmed = true;
                    await userManager.UpdateAsync(user);

                    // Assign user role after successful OTP verification
                    await userManager.AddToRoleAsync(user, "user");

                    // Sign in the user
                    await signInManager.SignInAsync(user, false);

                    // Clear OTP from TempData after successful verification
                    TempData.Remove("OTP");
                    TempData.Remove("OTPCreatedTime");

                    ViewBag.SuccessMessage = "Your account has been verified successfully!";
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                ViewBag.ErrorMessage = "Invalid OTP. Please try again.";
                TempData.Keep("OTP"); // Ensure OTP stays in TempData for future attempts
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

			// Find the user by email
			var user = await userManager.FindByEmailAsync(loginDto.Email);

			// Check if the user exists and is blocked
			if (user != null && user.IsBlocked)
			{
				// Blocked user: Display error message
				ViewBag.ErrorMessage = "Your account has been blocked. Please contact support.";
				return View(loginDto);
			}

			var result = await signInManager.PasswordSignInAsync(loginDto.Email, loginDto.Password, loginDto.RememberMe, false);

            if (result.Succeeded)
            {
                if (await userManager.IsInRoleAsync(user, "admin"))
                {
                    return RedirectToAction("AdminHome", "Home"); // Redirect admin to AdminHome
                }
                else if (await userManager.IsInRoleAsync(user, "user"))
                {
                    return RedirectToAction("Index", "Home"); // Redirect regular user to Store
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

            // Get the current user
            var appUser = await userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Update the user profile
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
    }
}
