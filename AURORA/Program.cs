using Microsoft.EntityFrameworkCore;
using AURORA.Models;
using AURORA.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace AURORA
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ── Servicios ────────────────────────────────────────────
            builder.Services.AddControllersWithViews();

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
    builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(8);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
      .AddCookie(options =>
      {
          options.LoginPath = "/Usuario/Login";
          options.AccessDeniedPath = "/Usuario/Login"; // ← cambia NoPermitido por Login
          options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
      });

            builder.Services.AddHttpClient<AURORA.Servicios.GroqService>();

            // ── Pipeline ─────────────────────────────────────────────
            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();          // ← debe ir ANTES de Auth
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Usuario}/{action=Index}/{id?}");

            app.Run();                 // ← nada va después de aquí
        }
    }
}