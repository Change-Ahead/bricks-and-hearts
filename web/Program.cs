using BricksAndHearts.Auth;
using BricksAndHearts.Controllers;
using BricksAndHearts.Database;
using BricksAndHearts.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BricksAndHeartsDbContext>();

/*
 * We ask users to sign in with Google, then after that we set a cookie and use that to authenticate subsequent requests.
 */
builder.Services.AddAuthentication(options => options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // If a user hits an [Authorize] endpoint without a cookie, redirect here
        options.LoginPath = "/login/google";
        // When a user doesn't have the right role (like "Admin") return a 403
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
    })
    .AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        googleOptions.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    });

builder.Services.AddTransient<IClaimsTransformation, ClaimsTransformer>();
builder.Services.AddScoped<ILandlordService, LandlordService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<IMailService, MailService>();

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddControllersWithViews(options => { options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); });

builder.Services.AddOptions<EmailConfigOptions>()
    .Bind(builder.Configuration.GetSection(EmailConfigOptions.Email));

var app = builder.Build();

if (app.Configuration.GetValue<bool>("MigrateOnStartup"))
{
    BricksAndHeartsDbContext.MigrateDatabase(app);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error/500");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Error/{0}");


app.UseCookiePolicy(
    new CookiePolicyOptions
    {
        HttpOnly = HttpOnlyPolicy.Always,
        Secure = CookieSecurePolicy.Always
    });

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    "default",
    "{controller=Home}/{action=Index}/{id?}");

app.Run();