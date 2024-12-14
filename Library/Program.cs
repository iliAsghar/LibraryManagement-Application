using Library.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<MyDBContext>(options =>
{
    options.UseSqlServer(
        "Data Source=.;" +
        "Initial Catalog=Library;" +
        "TrustServerCertificate=True;" +
        "Trusted_Connection=True;"
    );
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(5);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("NormalUser", policy => policy.RequireClaim("Role", "User"))
    .AddPolicy("BookKeeper", policy => policy.RequireClaim("Role", "BookKeeper", "Admin"))
    .AddPolicy("Admin", policy => policy.RequireClaim("Role", "Admin"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "books",
    pattern: "{controller=Books}/{action=BookList}/{id?}");

app.MapControllerRoute(
    name: "members",
    pattern: "{controller=Users}/{action=MemberList}/{id?}");

app.MapControllerRoute(
    name: "transactions",
    pattern: "{controller=Transactions}/{action=TransactionList}/{id?}");

app.MapControllerRoute(
    name: "currentTransaction",
    pattern: "transactions/current",
    defaults: new { controller = "Transactions", action = "ShowTransaction", Id = -1 }
    );

app.Run();
