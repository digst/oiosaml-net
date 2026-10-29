using DK.Gov.Oio.Saml.AspNetCore.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration.Json;
using DK.Gov.Oio.Saml.TestServiceProvider;

var builder = WebApplication.CreateBuilder(args);

// The sample is registered as two SPs, one per OIOSAML version; appsettings.{oiosamlProfile}.json holds what differs.
var oiosamlProfile = builder.Configuration["AppSettings:OiosamlProfile"] ?? "oiosaml4";
var sources = builder.Configuration.Sources;
sources.Insert(sources.IndexOf(sources.OfType<JsonConfigurationSource>().Last()) + 1,
    new JsonConfigurationSource { Path = $"appsettings.{oiosamlProfile}.json", Optional = false });

// The login cookie is SameSite=None, so every state-changing request must carry an antiforgery token.
// The library's /auth endpoints opt out: the IdP posts to them cross-site.
var mvcBuilder = builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}

builder.Services
    .AddOioSaml(builder.Configuration.GetSection("oiosaml"))
    .AddOioSamlCookieAuthentication();

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("appSettings"));

builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    // The IdP posts back cross-site, so the session cookie must be SameSite=None to come along.
    options.Cookie.SameSite = SameSiteMode.None; 
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    // Only spans a login or logout round trip; how long a login lasts is oiosaml:session.
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// The sample's pages may not be framed by other sites. The IdP's logout propagation may call /auth/logout from a frame.
app.Use((context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/auth"))
        context.Response.Headers.XFrameOptions = "DENY";
    return next(context);
});

var provider = new FileExtensionContentTypeProvider();

// The stylesheet's .eot fonts are not in the default mappings.
provider.Mappings[".eot"] = "application/vnd.ms-fontobject";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

app.UseRouting();
app.UseCookiePolicy();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();