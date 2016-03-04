using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;

using System.Configuration;
using System.Web.Script.Serialization;

using HISWebClient.Models;

using HISWebClient.Models.DataManager;

using HISWebClient.Util;

namespace HISWebClient.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationUserManager _userManager;

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager )
        {
            UserManager = userManager;
            UserManager.UserValidator = new UserValidator<ApplicationUser>(UserManager) { AllowOnlyAlphanumericUserNames = false };
    
            SignInManager = signInManager;
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        private ApplicationSignInManager _signInManager;

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set { _signInManager = value; }
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
            }
        }

        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            var user = await UserManager.FindByIdAsync(await SignInManager.GetVerifiedUserIdAsync());
            if (user != null)
            {
                var code = await UserManager.GenerateTwoFactorTokenAsync(user.Id, provider);
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent:  model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await SignInManager.SignInAsync(user, isPersistent:false, rememberBrowser:false);
                    
                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                    return RedirectToAction("Index", "Home");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);
                if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                // string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                // var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);		
                // await UserManager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
                // return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider

			//Retrieve the absolure (complete) URI from the request...
			string absoluteUri = Request.Url.AbsoluteUri;

			if (absoluteUri.Contains("localhostdev.org", StringComparison.CurrentCultureIgnoreCase))
			{
				//Special case - running locally under IIS Express with host-file based domain...

				//Retrieve port and scheme from request...
				int port = Request.Url.Port;
				string query = Request.Url.Query;

				//Create an absolute (complete) URL...
				string url = String.Format( "http://localhostdev.org:{0}/Account/ExternalLoginCallback{1}", port, query );  

				//Return a ChallengeResult with a complete URL...
				return new ChallengeResult( provider, url);
			}

			//No special case - return a ChallengeResult with a relative URL...
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
		// POST: /Account/ExternalLogOut
		//NOTE: Redirects reset the ViewBag (source: http://www.codeproject.com/Articles/476967/What-is-ViewData-ViewBag-and-TempData-MVC-Option) 
		//		Added returnUrl parameter to the method signature to avoid this issue...
		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public ActionResult ExternalLogOut(string provider, string returnUrl, string localLogout)
		{
			//Reset external login information for the current session
			resetSessionExternalLogin();

			//NOTE: The Authentication.SignOut() does a local sign-out of the user but does not invalidate the Google cookies.
			//		 Thus the user is still logged in to Google...

			//Replacing the original SignOut() call...
			//Source: http://stackoverflow.com/questions/28999318/owin-authentication-signout-doesnt-seem-to-remove-the-cookie

			//AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie, DefaultAuthenticationTypes.ExternalCookie);
			string[] authenticationTypes = { Microsoft.AspNet.Identity.DefaultAuthenticationTypes.ApplicationCookie, 
										     Microsoft.AspNet.Identity.DefaultAuthenticationTypes.ExternalCookie
										   };

			Request.GetOwinContext().Authentication.SignOut(authenticationTypes);

			//Expire miscellaneous Google cookies related to the external login...
			//Source: https://msdn.microsoft.com/en-us/library/ms178195.aspx
			//
			//NOTE: Cannot expire cookies from a different domain.  
			//		 So this attempt to log the user out from Google does not work...
			//
			//string[] cookieNames = {"APISID", "HSID", "SAPISID", "SID", "SSID"};

			//foreach (string cookieName in cookieNames) {
			//	HttpCookie cookie = new HttpCookie(cookieName);
			//	cookie.Domain = ".google.com";
			//	cookie.Expires = DateTime.Now.AddDays(-1);
			//	Response.Cookies.Add(cookie);
			//}

			//To invalidate the user's Google credentials (and thereby log the user out of Google) redirect to the google/accounts web-site 
			//	and supply a full return URL...
			//Source: http://stackoverflow.com/questions/16238327/logging-out-from-google-external-service
			//Google URL pattern: https://www.google.com/accounts/Logout?continue=https://appengine.google.com/_ah/logout?continue=https://www.yourapp.com 

			//string googleUrl = "https://www.google.com/accounts/Logout?continue=https://appengine.google.com/_ah/logout?continue=";

			string googleUrl = ConfigurationManager.AppSettings["GoogleExternalLogoutUrlBase"];
			string scheme = Request.Url.Scheme;
			string authority = Request.Url.Authority;

			string redirectUrl = String.Format("{0}{1}://{2}/{3}", googleUrl, scheme, authority, returnUrl);

			if (!String.IsNullOrWhiteSpace(localLogout) && "true" == localLogout)
			{
				//Local logout only - reset redirect URL...
				redirectUrl = String.Format("{0}://{1}/{2}", scheme, authority, returnUrl);
			}

			return Redirect(redirectUrl);
		}

        //
        // GET: /Account/SendCode
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                return View("Error");
            }
            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generate the token and send it
            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var externalIdentity = HttpContext.GetOwinContext().Authentication.GetExternalIdentityAsync(DefaultAuthenticationTypes.ExternalCookie);

			//Retain external username and email values for later reference 
			var emailClaim = externalIdentity.Result.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var email = emailClaim.Value;

			var nameClaim = externalIdentity.Result.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
			var userName = nameClaim.Value;  //Same as: externalIdentity.Result.GetUserName(); ??
          
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);

			var authResult = await SignInManager.AuthenticationManager.AuthenticateAsync(DefaultAuthenticationTypes.ExternalCookie);
			
			switch (result)
            {
                case SignInStatus.Success:
					//set external login values for session
					setSessionExternalLogin(userName, email);

					//Set return URL for later reference...
					ViewBag.ReturnUrl = returnUrl;
                   
					return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case SignInStatus.Failure:
                default:
                    // If the user does not have an account, then write a new entry into user database
                    ViewBag.ReturnUrl = returnUrl;
                    //ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    //return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });

                //var user = UserManager.FindByName(email);
				var user = UserManager.FindByEmail(email);

                if (user != null)
                {
                    await SignInManager.SignInAsync(user, false, false);

					//set external login values for session
					setSessionExternalLogin(userName, email);

					//Set return URL for later reference...
					ViewBag.ReturnUrl = returnUrl;

					return RedirectToLocal(returnUrl);
                }
                else
                {
                    var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                    if (info == null)
                    {
                        return View("ExternalLoginFailure");
                    }

					//BCC - 30-Nov-2015 - Set email field to avoid error: 'Email cannot be null or empty...
					//BCC - TEST - 03-Dec-2015 - r: UserName = email w: UserName = userName
					var newUser = new ApplicationUser() { UserName = userName, Email = email, UserEmail = email };

                    var rst = await UserManager.CreateAsync(newUser);
					
					if (rst.Succeeded)
                    {
                        var r = await UserManager.AddLoginAsync(newUser.Id, info.Login);
						
						if (r.Succeeded)
                        {
                            await SignInManager.SignInAsync(newUser, false, false);

							//set external login values for session
							setSessionExternalLogin(userName, email);

							//Set return URL for later reference...
							ViewBag.ReturnUrl = returnUrl;
							
							return RedirectToLocal(returnUrl);
                        }
                    }
                    //AddErrors(result);
                    return View("ExternalLoginFailure");
                }
            }
        }


		//Set information for the current external login in the current session...
		private void setSessionExternalLogin( string userName, string eMail, bool authenticated = true)
		{
			//Validate/initialize input parameters...
			if ( String.IsNullOrWhiteSpace(userName) || String.IsNullOrWhiteSpace(eMail))
			{
				return;		//Invalid parameter return early...
			}

			CurrentUser cu = new CurrentUser(userName, eMail, authenticated);

			var httpContext = new HttpContextWrapper(System.Web.HttpContext.Current);

			httpContext.Session[httpContext.Session.SessionID] = cu;
		}

		//Reset external login information for the current session
 		private void resetSessionExternalLogin()
		{
			var httpContext = new HttpContextWrapper(System.Web.HttpContext.Current);
			httpContext.Session[httpContext.Session.SessionID] = null;
		}


		//Return the current user, if any
		[HttpGet]
		public ActionResult CurrentUser()
		{

			//Does the User object exist here?
			//if (User.Identity.IsAuthenticated)
			//{
			//	string userId = User.Identity.GetUserId();
			//	string userName = User.Identity.GetUserName();
			//	string userName2 = User.Identity.Name;


			//	var claims = UserManager.GetClaims(userId);

			//	var emailClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
			//	var nameClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

			//	var user = UserManager.Users.FirstOrDefault( u => u.Id.Equals(userId));

			//	var email = user.UserEmail;
			//	var userName3 = user.UserName;

			//	//var externalIdentity = HttpContext.GetOwinContext().Authentication.GetExternalIdentityAsync(DefaultAuthenticationTypes.ExternalCookie);

			//	////Retain external username and email values for later reference 
			//	//var emailClaim = externalIdentity.Result.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
			//	//var email = emailClaim.Value;

			//	//var nameClaim = externalIdentity.Result.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
			//	//var userName3 = nameClaim.Value;  //Same as: externalIdentity.Result.GetUserName(); ??


			//	int n = 5;

			//	++n;
			//}

			//Check current session for current user
			var httpContext = new HttpContextWrapper(System.Web.HttpContext.Current);
			CurrentUser cu = httpContext.Session[httpContext.Session.SessionID] as CurrentUser;
			if ( null == cu )
			{
				//Not found - current session may have authenticated user per a previously established Google 'cookie'
				bool bFound = false;	//Assume authenticated user does not exist...
				if (User.Identity.IsAuthenticated)
				{
					//Authenticated user exists - retrieve User instance via current user Id...
					string userId = User.Identity.GetUserId();
					if (!String.IsNullOrWhiteSpace(userId))
					{
						var user = UserManager.Users.FirstOrDefault(u => u.Id.Equals(userId));
						if (null != user)
						{
							//User found - attempt to retrieve user name and email...
							string userEmail = user.UserEmail;
							string userName = user.UserName;
							if ((!String.IsNullOrWhiteSpace(userEmail)) && (!String.IsNullOrWhiteSpace(userName)))
							{
								//User name and email found - set current user for current session...
								setSessionExternalLogin(userName, userEmail);
								cu = httpContext.Session[httpContext.Session.SessionID] as CurrentUser;
								bFound = (null != cu);
							}

						}
					}
				}
								
				if (! bFound)
				{
					//No authenticated user found - return 'Not Found'...
				return new HttpStatusCodeResult(System.Net.HttpStatusCode.NotFound, "No current user!!");
			}
			}

			//Sucess - convert retrieved data to JSON and return...
			var javaScriptSerializer = new JavaScriptSerializer();
			var json = javaScriptSerializer.Serialize(cu);

			Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
			return Json(json, "application/json", JsonRequestBehavior.AllowGet);
		}

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
		[AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {

			//Reset external login information for the current session
			resetSessionExternalLogin();

			//AuthenticationManager.SignOut();
			AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie, DefaultAuthenticationTypes.ExternalCookie);
			//AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);

//          return RedirectToAction("Index", "Home");

			var returnUrl = ViewBag.ReturnUrl;

			ViewBag.ReturnUrl = null;
			return RedirectToAction(returnUrl);
		
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}