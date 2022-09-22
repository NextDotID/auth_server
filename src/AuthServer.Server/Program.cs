using AuthServer.Server.Services.Avatar;
using AuthServer.Server.Services.Proof;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOptions();
builder.Services.AddControllersWithViews();

AuthenticationBuilder auth = builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromMinutes(3);
        options.Cookie.MaxAge = null;
        options.LoginPath = "/Authenticate";
        options.LogoutPath = "/SignOut";
    });

if (builder.Configuration.GetSection("Discord").Exists())
{
    auth.AddDiscord(options =>
    {
        options.ClientId = builder.Configuration.GetSection("Discord").GetValue<string>("ClientId");
        options.ClientSecret = builder.Configuration.GetSection("Discord").GetValue<string>("ClientSecret");
    });
}

if (builder.Configuration.GetSection("Twitter").Exists())
{
    auth.AddTwitter(options =>
    {
        options.ClientId = builder.Configuration.GetSection("Twitter").GetValue<string>("ClientId");
        options.ClientSecret = builder.Configuration.GetSection("Twitter").GetValue<string>("ClientSecret");
    });
}

if (builder.Configuration.GetSection("GitHub").Exists())
{
    auth.AddGitHub(options =>
    {
        options.ClientId = builder.Configuration.GetSection("GitHub").GetValue<string>("ClientId");
        options.ClientSecret = builder.Configuration.GetSection("GitHub").GetValue<string>("ClientSecret");
    });
}

builder.Services.AddHttpClient();

builder.Services.AddTransient<IProofService, ProofService>();
builder.Services.AddSingleton<IAvatarService, Secp256K1AvatarService>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.Use((context, next) =>
{
    context.Request.Scheme = "https";
    return next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseForwardedHeaders();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
