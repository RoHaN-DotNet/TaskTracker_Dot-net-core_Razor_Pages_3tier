using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using TaskTrackerBLL;
using TaskTrackerBLL.Authorization;
using TaskTrackerDAL.Constants;
using TaskTrackerDAL.Infrastructure;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructureServices(builder.Configuration);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    // Every page in these folders requires at least authentication by default;
    // specific policies narrow that further where the whole folder shares one rule.
    options.Conventions.AuthorizeFolder("/Dashboard");
    options.Conventions.AuthorizeFolder("/Companies", AppPolicies.ManagerOrAdmin);
    options.Conventions.AuthorizeFolder("/Users", AppPolicies.ManagerOrAdmin);
    options.Conventions.AuthorizeFolder("/Roles", AppPolicies.AdminOnly);
    options.Conventions.AuthorizeFolder("/Employees", AppPolicies.ManagerOrAdmin);

    // Projects and Tasks are open to any authenticated role at the folder level;
    // per-record scoping (own company / own assignment) happens inside each
    // page handler via IAuthorizationService, since different rows in the same
    // list are visible to different roles.
    options.Conventions.AuthorizeFolder("/Projects");
    options.Conventions.AuthorizeFolder("/Tasks");

    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/Register");
    options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");

    
});
//
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
        options.Cookie.Name = "TaskTracker.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddSingleton<IAuthorizationHandler, DataAccessHandler>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppPolicies.AdminOnly, policy =>
        policy.RequireRole(AppRoles.Admin));

    options.AddPolicy(AppPolicies.ManagerOrAdmin, policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.Manager));

    options.AddPolicy(AppPolicies.DataAccess, policy =>
        policy.Requirements.Add(new DataAccessRequirement()));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
