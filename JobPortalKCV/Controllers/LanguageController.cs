using System;
using System.Web;
using System.Web.Mvc;
using JobPortalKCV.Helpers;

namespace JobPortalKCV.Controllers
{
    public class LanguageController : Controller
    {
        [HttpGet]
        public ActionResult Set(string lang, string returnUrl)
        {
            var selectedLanguage = UiText.NormalizeLanguage(lang);

            Response.Cookies.Add(new HttpCookie(UiText.CookieName, selectedLanguage)
            {
                Expires = DateTime.UtcNow.AddYears(1),
                HttpOnly = false,
                SameSite = SameSiteMode.Lax
            });

            Response.Cookies.Add(new HttpCookie("kcv_docs_lang", selectedLanguage)
            {
                Expires = DateTime.UtcNow.AddYears(1),
                HttpOnly = false,
                SameSite = SameSiteMode.Lax
            });

            return Redirect(GetSafeReturnUrl(returnUrl));
        }

        private string GetSafeReturnUrl(string returnUrl)
        {
            if (String.IsNullOrWhiteSpace(returnUrl))
            {
                return Url.Action("Index", "Home");
            }

            return Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Index", "Home");
        }
    }
}
