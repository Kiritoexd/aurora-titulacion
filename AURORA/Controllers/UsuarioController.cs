using AURORA.Data;
using AURORA.Models;
using AURORA.Servicios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AURORA.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsuarioController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ---------------- REGISTRO ----------------
        [HttpGet]
        public IActionResult Registro()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectSegunRol();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Registro(Tb_Usuario usuario)
        {
            usuario.Rol = "Lector";
            usuario.FechaRegistro = DateTime.Now;

            if (!ModelState.IsValid)
                return View(usuario);

            var existe = _context.Usuarios.FirstOrDefault(u => u.Email == usuario.Email);
            if (existe != null)
            {
                ModelState.AddModelError("Email", "Ya existe una cuenta con ese correo.");
                return View(usuario);
            }

            usuario.Password = BCrypt.Net.BCrypt.HashPassword(usuario.Password);
            usuario.ResetToken = null;
            usuario.ResetTokenExpiry = null;

            _context.Usuarios.Add(usuario);
            _context.SaveChanges();

            TempData["Mensaje"] = "Registro exitoso, ahora inicia sesión.";
            return RedirectToAction("Login");
        }

        // ---------------- INDEX ----------------
        [HttpGet]
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectSegunRol();

            return View();
        }

        // ---------------- LOGIN ----------------
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectSegunRol();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Email == model.Email);
            if (usuario == null || !BCrypt.Net.BCrypt.Verify(model.Password, usuario.Password))
            {
                ModelState.AddModelError("", "Correo o contraseña incorrectos");
                return View(model);
            }

            // Cuenta dada de baja
            if (string.IsNullOrEmpty(usuario.Rol))
                return RedirectToAction("CuentaBaja");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{usuario.Nombres} {usuario.ApellidoPaterno}"),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("Email", usuario.Email)
            };

            var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identidad);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (usuario.Rol == "Administrador")
                return RedirectToAction("Index", "Administrador");

            return RedirectToAction("Inicio", "Lector");
        }

        // ---------------- CUENTA BAJA ----------------
        [HttpGet]
        public IActionResult CuentaBaja()
        {
            return View();
        }

        // ---------------- LOGIN ADMIN ----------------
        [HttpGet]
        public IActionResult LoginAdmin()
        {
            if (User.Identity.IsAuthenticated && User.IsInRole("Administrador"))
                return RedirectToAction("Index", "Administrador");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginAdmin(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Busca el usuario en SQL y verifica que sea Administrador
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Email == model.Email);
            if (usuario == null || !BCrypt.Net.BCrypt.Verify(model.Password, usuario.Password))
            {
                ModelState.AddModelError("", "Correo o contraseña incorrectos.");
                return View(model);
            }

            if (usuario.Rol != "Administrador")
            {
                ModelState.AddModelError("", "Acceso denegado. No tienes permisos de administrador.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{usuario.Nombres} {usuario.ApellidoPaterno}"),
                new Claim(ClaimTypes.Role, "Administrador"),
                new Claim("Email", usuario.Email)
            };

            var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identidad);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Administrador");
        }

        // ---------------- LOGOUT ----------------
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // ---------------- NO PERMITIDO ----------------
        [HttpGet]
        public IActionResult NoPermitido()
        {
            return View();
        }

        // ---------------- RECUPERACIÓN DE CONTRASEÑA ----------------
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Email == email);
            if (usuario == null)
            {
                ModelState.AddModelError("", "No existe una cuenta con ese correo.");
                return View();
            }

            var codigo = new Random().Next(100000, 999999).ToString();
            usuario.ResetToken = codigo;
            usuario.ResetTokenExpiry = DateTime.Now.AddMinutes(15);
            _context.SaveChanges();

            var emailService = new EmailService();
            await emailService.SendPasswordRecoveryCodeAsync(email, codigo);

            TempData["Mensaje"] = "Se ha enviado un código de recuperación a tu correo.";
            return RedirectToAction("IngresarCodigo");
        }

        [HttpGet]
        public IActionResult IngresarCodigo()
        {
            return View();
        }

        [HttpPost]
        public IActionResult IngresarCodigo(string codigo)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.ResetToken == codigo && u.ResetTokenExpiry > DateTime.Now);
            if (usuario == null)
            {
                TempData["ErrorCodigo"] = "El código ingresado es incorrecto o ha expirado.";
                return View();
            }

            TempData["CodigoValido"] = codigo;
            return RedirectToAction("CambiarContraseña");
        }

        [HttpGet]
        public IActionResult CambiarContraseña()
        {
            if (TempData["CodigoValido"] == null)
            {
                TempData["Error"] = "Primero ingresa un código válido.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel { Token = TempData["CodigoValido"].ToString() };
            return View(model);
        }

        [HttpPost]
        public IActionResult CambiarContraseña(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var usuario = _context.Usuarios.FirstOrDefault(u => u.ResetToken == model.Token && u.ResetTokenExpiry > DateTime.Now);
            if (usuario == null)
            {
                TempData["Error"] = "El código es inválido o ha expirado.";
                return RedirectToAction("Login");
            }

            usuario.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            usuario.ResetToken = null;
            usuario.ResetTokenExpiry = null;
            _context.SaveChanges();

            TempData["Mensaje"] = "Tu contraseña ha sido restablecida correctamente.";
            return RedirectToAction("Login");
        }

        // ---------------- HELPER ----------------
        private IActionResult RedirectSegunRol()
        {
            if (User.IsInRole("Administrador"))
                return RedirectToAction("Index", "Administrador");

            return RedirectToAction("Inicio", "Lector");
        }
    }
}   