using AURORA.Data;
using AURORA.Models;
using AURORA.ViewModels;
using iText.Kernel.Pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using AURORA.ViewModels;
using System.Text.Json;

using System.Security.Claims;

namespace AURORA.Controllers
{
    public class LectorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LectorController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Lector")]
        public IActionResult Inicio()
        {
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var libros = _context.UsuarioLibros
                .Include(u => u.Libro)
                .Where(u => u.UsuarioId == usuarioId)
                .ToList();

            var racha = _context.Tb_Racha
                .FirstOrDefault(r => r.UsuarioId == usuarioId);

            var vm = new InicioViewModel
            {
                NombreUsuario = User.Identity?.Name ?? "Usuario",
                LibrosTerminados = libros.Count(l => l.Progreso == 100),
                LibrosLeyendo = libros.Count(l => l.Progreso > 0 && l.Progreso < 100),
                LibrosPendientes = libros.Count(l => l.Progreso == 0),
                DiasRacha = racha?.DiasConsecutivos ?? 0
            };

            return View(vm);
        }

        [Authorize(Roles = "Lector")]
        public IActionResult Biblioteca()
        {
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var libros = _context.UsuarioLibros
                .Include(u => u.Libro)
                .Where(u => u.UsuarioId == usuarioId)
                .ToList();

            var vm = new LibrosViewModel
            {
                Leyendo = libros.Where(l => l.Progreso > 0 && l.Progreso < 100).ToList(),
                Pendientes = libros.Where(l => l.Progreso == 0).ToList(),
                Completados = libros.Where(l => l.Progreso == 100).ToList()
            };

            return View(vm);
        }

        [Authorize(Roles = "Lector")]
        public async Task<IActionResult> Perfil()
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario is null) return NotFound();

            // Misma lógica que RachaViewModel
            var racha = await _context.Tb_Racha
     .FirstOrDefaultAsync(r => r.UsuarioId == usuarioId);

            var vm = new PerfilViewModel
            {
                Id = usuario.Id,
                Nombres = usuario.Nombres,
                ApellidoPaterno = usuario.ApellidoPaterno,
                ApellidoMaterno = usuario.ApellidoMaterno,
                Email = usuario.Email,
                FotoUrl = usuario.FotoUrl ?? "",
                FechaRegistro = usuario.FechaRegistro,
                DiasSeguidos = racha?.DiasConsecutivos ?? 0,  // ← corregido
                MetaDias = racha?.MetaDias ?? 30,
            };
            return View(vm);
        }
        [HttpPost]
        public IActionResult EditarPerfil(PerfilViewModel model)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Id == model.Id);
            if (usuario != null)
            {
                usuario.Nombres = model.Nombres;
                usuario.ApellidoPaterno = model.ApellidoPaterno;
                usuario.ApellidoMaterno = model.ApellidoMaterno;
                usuario.Email = model.Email;

                _context.SaveChanges();
            }
            return RedirectToAction("Perfil");
        }

        [HttpPost]
        public async Task<IActionResult> SubirFoto(IFormFile foto)
        {
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Id == usuarioId);

            if (usuario != null && foto != null && foto.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(foto.FileName);
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/perfiles", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await foto.CopyToAsync(stream);
                }

                usuario.FotoUrl = "/img/perfiles/" + fileName;
                _context.Update(usuario);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Perfil");
        }



        [Authorize(Roles = "Lector")]
        public IActionResult AbrirLibro(int id)
        {
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var usuarioLibro = _context.UsuarioLibros
                .Include(ul => ul.Libro)
                .FirstOrDefault(ul => ul.UsuarioId == usuarioId && ul.LibroId == id);

            if (usuarioLibro == null)
                return RedirectToAction("Biblioteca");

            usuarioLibro.Progreso = CalcularProgreso(usuarioLibro.Libro.Paginas, usuarioLibro.UltimaPagina);

            return View(usuarioLibro);
        }
        [Authorize(Roles = "Lector")]
        public IActionResult Estadisticas()
        {
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var usuarioLibros = _context.UsuarioLibros
                .Include(u => u.Libro)
                .Where(u => u.UsuarioId == usuarioId)
                .ToList();

            var ulIds = usuarioLibros.Select(ul => ul.Id).ToHashSet();

            // ── NUEVO: sesiones reales por mes ───────────────────
            var sesiones = _context.Sesiones
                .Where(s => ulIds.Contains(s.UsuarioLibroId))
                .ToList();

            // Agrupar minutos por mes (usando el Inicio real de la sesión)
            var minutosPorMes = sesiones
                .GroupBy(s => new { s.Inicio.Year, s.Inicio.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => g.Sum(s => s.Minutos))
                .ToList();

            var meses = sesiones
                .GroupBy(s => new { s.Inicio.Year, s.Inicio.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new DateTime(g.Key.Year, g.Key.Month, 1)
                              .ToString("MMMM", new System.Globalization.CultureInfo("es-MX")))
                .ToList();

            // Minutos por sesión individual (para gráfica de promedio)
            var minutosPorSesion = sesiones
                .OrderBy(s => s.Inicio)
                .Select(s => s.Minutos)
                .ToList();

            var fechasSesiones = sesiones
                .OrderBy(s => s.Inicio)
                .Select(s => s.Inicio.ToShortDateString())
                .ToList();
            // ────────────────────────────────────────────────────

            var model = new EstadisticasViewModel
            {
                LibrosPendientes = usuarioLibros.Count(u => u.Progreso == 0),
                LibrosLeyendo = usuarioLibros.Count(u => u.Progreso > 0 && u.Progreso < 100),
                LibrosTerminados = usuarioLibros.Count(u => u.Progreso == 100),

                Generos = usuarioLibros
                    .Where(u => u.Libro?.Genero != null)
                    .Select(u => u.Libro.Genero)
                    .Distinct()
                    .ToList(),

                CantidadPorGenero = usuarioLibros
                    .Where(u => u.Libro?.Genero != null)
                    .GroupBy(u => u.Libro.Genero)
                    .Select(g => g.Count())
                    .ToList(),

                // ── Ahora usan datos reales de Tb_Sesion ────────
                Meses = meses,
                MinutosPorMes = minutosPorMes,

                Sesiones = fechasSesiones,
                PromedioMinutos = minutosPorSesion,
                // ────────────────────────────────────────────────

                Anios = usuarioLibros
                    .Where(u => u.UltimoAcceso.HasValue)
                    .GroupBy(u => u.UltimoAcceso.Value.Year)
                    .Select(g => g.Key.ToString())
                    .ToList(),

                LibrosPorAnio = usuarioLibros
                    .Where(u => u.UltimoAcceso.HasValue)
                    .GroupBy(u => u.UltimoAcceso.Value.Year)
                    .Select(g => g.Count())
                    .ToList(),
            };

            return View(model);
        }


        public async Task<IActionResult> Top()
        {
            int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var top = await _context.UsuarioLibros
                .Include(u => u.Libro)
                .Where(u => u.UsuarioId == usuarioId)
                .OrderBy(u => u.Posicion) // ✅ ahora usa Posicion
                .Take(6)
                .Select(u => new TopLibroViewModel
                {
                    LibroId = u.Libro.Id,
                    Titulo = u.Libro.Titulo ?? "Sin título",
                    Autor = u.Libro.Autor ?? "Desconocido",
                    Genero = u.Libro.Genero ?? "N/A",
                    Progreso = u.Progreso,
                    Posicion = u.Posicion
                })
                .ToListAsync();

            return View(top);
        }


        [HttpPost]
        [Authorize(Roles = "Lector")]
        public IActionResult EliminarPdf(int id)
        {
            var libro = _context.Libros.FirstOrDefault(l => l.Id == id);
            if (libro != null)
            {
                // Eliminar archivo físico
                var ruta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/pdfs", Path.GetFileName(libro.RutaPdf ?? ""));
                if (System.IO.File.Exists(ruta))
                {
                    System.IO.File.Delete(ruta);
                }

                // Eliminar registros relacionados en UsuarioLibros
                var usuarioLibros = _context.UsuarioLibros.Where(ul => ul.LibroId == id);
                _context.UsuarioLibros.RemoveRange(usuarioLibros);

                // Eliminar libro de la BD
                _context.Libros.Remove(libro);
                _context.SaveChanges();
            }

            return RedirectToAction("Biblioteca");
        }

        [Authorize(Roles = "Lector")]
        [HttpPost]
        public IActionResult GuardarProgreso(int id, int pagina)
        {
            var usuarioLibro = _context.UsuarioLibros
                .Include(ul => ul.Libro)
                .FirstOrDefault(ul => ul.Id == id);

            if (usuarioLibro != null)
            {
                usuarioLibro.UltimaPagina = pagina;

                // Si Libro.Paginas es null, usamos 0
                var paginas = usuarioLibro.Libro.Paginas ?? 0m;

                if (paginas > 0)
                {
                    usuarioLibro.Progreso = (int)Math.Round(
                        (decimal)pagina * 100m / paginas
                    );
                }
                else
                {
                    usuarioLibro.Progreso = 0;
                }

                usuarioLibro.UltimoAcceso = DateTime.Now;
                _context.SaveChanges();
            }


            // 🔹 Redirige a AbrirLibro para ver progreso actualizado
            return RedirectToAction("AbrirLibro", new { id = id });
        }


        private int CalcularProgreso(int? totalPaginas, int paginaActual)
        {
            if (!totalPaginas.HasValue || totalPaginas.Value == 0) return 0;
            if (paginaActual > totalPaginas.Value) paginaActual = totalPaginas.Value;

            return (int)((paginaActual * 100.0) / totalPaginas.Value);
        }
        [Authorize(Roles = "Lector")]
        public IActionResult LeerLibro(int id)
        {
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var usuarioLibro = _context.UsuarioLibros
                .Include(ul => ul.Libro)
                .FirstOrDefault(ul => ul.UsuarioId == usuarioId && ul.LibroId == id);

            if (usuarioLibro == null)
                return RedirectToAction("Biblioteca");

            // Guardamos inicio de lectura
            usuarioLibro.UltimoAcceso = DateTime.Now;
            _context.SaveChanges();

            return View(usuarioLibro);
        }
        [HttpPost]
        [Authorize(Roles = "Lector")]
        public IActionResult CerrarLectura(int idLibro)
        {
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var usuarioLibro = _context.UsuarioLibros
                .FirstOrDefault(x => x.LibroId == idLibro && x.UsuarioId == usuarioId);

            if (usuarioLibro != null && usuarioLibro.UltimoInicioLectura.HasValue)
            {
                var inicio = usuarioLibro.UltimoInicioLectura.Value;
                var fin = DateTime.Now;
                var tiempo = fin - inicio;

                // Acumular en el libro (retrocompatibilidad)
                usuarioLibro.TiempoLectura += tiempo;
                usuarioLibro.UltimoInicioLectura = null;

                // ── NUEVO: guardar sesión individual ─────────────
                var sesion = new Tb_Sesion
                {
                    UsuarioLibroId = usuarioLibro.Id,
                    Inicio = inicio,
                    Fin = fin,
                    Minutos = (int)tiempo.TotalMinutes
                };
                _context.Sesiones.Add(sesion);
                // ─────────────────────────────────────────────────

                _context.SaveChanges();
            }

            return RedirectToAction("Estadisticas", "Lector");
        }

        [Authorize(Roles = "Lector")]
        [HttpPost]
        public IActionResult ImportarPdf(IFormFile pdfLibro, string genero)
        {
            if (pdfLibro == null || pdfLibro.Length == 0 || string.IsNullOrEmpty(genero))
                return RedirectToAction("Biblioteca");

            var rutaCarpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/pdfs");
            if (!Directory.Exists(rutaCarpeta))
                Directory.CreateDirectory(rutaCarpeta);

            var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(pdfLibro.FileName);
            var rutaCompleta = Path.Combine(rutaCarpeta, nombreArchivo);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                pdfLibro.CopyTo(stream);
            }

            int totalPaginas = 0;
            using (var pdfReader = new PdfReader(rutaCompleta))
            using (var pdfDoc = new PdfDocument(pdfReader))
            {
                totalPaginas = pdfDoc.GetNumberOfPages();
            }

            var libro = new Tb_Libro
            {
                Titulo = Path.GetFileNameWithoutExtension(pdfLibro.FileName),
                Autor = "Autor desconocido",
                Genero = genero, // yo ahora se guarda el género elegido
                Editorial = "Usuario",
                Año = DateTime.Now.Year,
                Paginas = totalPaginas,
                RutaPdf = nombreArchivo
            };

            _context.Libros.Add(libro);
            _context.SaveChanges();

            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var usuarioLibro = new Tb_UsuarioLibro
            {
                UsuarioId = usuarioId,
                LibroId = libro.Id,
                Progreso = 0,
                UltimaPagina = 1,
                UltimoAcceso = DateTime.Now
            };

            _context.UsuarioLibros.Add(usuarioLibro);
            _context.SaveChanges();

            return RedirectToAction("Biblioteca");
        }
        public IActionResult Leer(int idLibro)
        {
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var usuarioLibro = _context.UsuarioLibros
                .FirstOrDefault(x => x.LibroId == idLibro && x.UsuarioId == usuarioId);

            if (usuarioLibro != null)
            {
                usuarioLibro.UltimoInicioLectura = DateTime.Now;
                _context.SaveChanges();
            }

            return View(usuarioLibro);
        }

        public IActionResult Recomendaciones()
        {
            return View();
        }


        [HttpGet]
        public IActionResult Racha()
        {
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var racha = _context.Rachas.FirstOrDefault(r => r.UsuarioId == usuarioId);

            // Crear racha inicial si no existe
            if (racha == null)
            {
                racha = new Tb_Racha
                {
                    UsuarioId = usuarioId,
                    DiasConsecutivos = 0,
                    UltimaLectura = null,
                    MetaDias = 30
                };
                _context.Rachas.Add(racha);
                _context.SaveChanges();
            }

            // Actualizar racha solo si hoy no se ha contabilizado
            if (racha.UltimaLectura?.Date != DateTime.Today)
            {
                racha.DiasConsecutivos = racha.UltimaLectura?.Date == DateTime.Today.AddDays(-1)
                    ? racha.DiasConsecutivos + 1  // continúa la racha
                    : 1;                          // reinicia la racha

                racha.UltimaLectura = DateTime.Today;
                _context.SaveChanges();
            }

            // Logros fijos basados en días consecutivos
            var logros = new List<LogroViewModel>
    {
        new() { Nombre = "Primera semana",   Dias = 7,   Desbloqueado = racha.DiasConsecutivos >= 7   },
        new() { Nombre = "Lectura constante", Dias = 15,  Desbloqueado = racha.DiasConsecutivos >= 15  },
        new() { Nombre = "Meta mensual",      Dias = 30,  Desbloqueado = racha.DiasConsecutivos >= 30  },
        new() { Nombre = "Super lector",      Dias = 60,  Desbloqueado = racha.DiasConsecutivos >= 60  },
        new() { Nombre = "Leyenda",           Dias = 100, Desbloqueado = racha.DiasConsecutivos >= 100 },
    };

            var model = new RachaViewModel
            {
                DiasSeguidos = racha.DiasConsecutivos,
                MetaDias = racha.MetaDias,
                Logros = logros
            };

            return View(model);
        }
        // ═══════════════════════════════════════════════════════
        //  LOGROS — definición real basada en lectura
        // ═══════════════════════════════════════════════════════
        private static readonly List<LogroDef> LOGROS_DEF = new()
{// ── DIARIOS ──────────────────────────────────────────
new("diario_abrir_libro", "diario",
    """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z"/><path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z"/></svg>""",
    "Abre un libro", "Abre cualquier libro hoy.", 1),

new("diario_10_paginas", "diario",
    """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/><polyline points="10 9 9 9 8 9"/></svg>""",
    "10 páginas", "Lee al menos 10 páginas en el día.", 10),

new("diario_25_paginas", "diario",
    """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M19 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V5a2 2 0 0 0-2-2z"/><polyline points="9 11 12 14 22 4"/></svg>""",
    "Lector activo", "Lee al menos 25 páginas en el día.", 25),

new("diario_guardar_progreso", "diario",
    """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/><polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/></svg>""",
    "Guarda tu avance", "Guarda el progreso de lectura al menos una vez.", 1),

// ── PERMANENTES ───────────────────────────────────────
new("perm_primer_libro", "permanente",
    """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="8" r="6"/><path d="M9 14v7l3-2 3 2v-7"/></svg>""",
    "Primer libro", "Completa tu primer libro al 100%.", 1),

new("perm_5_libros", "permanente",
    """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"/></svg>""",
    "Bibliófilo", "Completa 5 libros al 100%.", 5),

new("perm_100_paginas", "permanente",
    """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/><line x1="6" y1="20" x2="6" y2="14"/></svg>""",
    "100 páginas", "Acumula 100 páginas leídas en total.", 100),

new("perm_500_paginas", "permanente",
    """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"/></svg>""",
    "500 páginas", "Acumula 500 páginas leídas en total.", 500),

new("perm_racha_7", "permanente",
    """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M8.5 14.5A2.5 2.5 0 0 0 11 12c0-1.38-.5-2-1-3-1.072-2.143-.224-4.054 2-6 .5 2.5 2 4.9 4 6.5 2 1.6 3 3.5 3 5.5a7 7 0 1 1-14 0c0-1.153.433-2.294 1-3a2.5 2.5 0 0 0 2.5 2.5z"/></svg>""",
    "Racha 7 días", "Mantén una racha de lectura de 7 días.", 7),

new("perm_racha_30", "permanente",
    """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"/><circle cx="12" cy="12" r="3"/></svg>""",
    "Racha 30 días", "Mantén una racha de lectura de 30 días.", 30),

// ── ESPECIALES ────────────────────────────────────────
new("esp_madrugador", "especial",
    """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="5"/><line x1="12" y1="1" x2="12" y2="3"/><line x1="12" y1="21" x2="12" y2="23"/><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/><line x1="1" y1="12" x2="3" y2="12"/><line x1="21" y1="12" x2="23" y2="12"/><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/><line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/></svg>""",
    "Madrugador", "Lee antes de las 8 AM.", 1),

new("esp_nocturno", "especial",
    """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>""",
    "Lectura nocturna", "Lee después de las 10 PM.", 1),
};

        private const string SESSION_ESTADO = "logros_estado_v1";
        private const string SESSION_FECHA = "logros_ultima_fecha";

        // ── Vista principal ──────────────────────────────────────
        public IActionResult Logros()
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var estado = CargarEstado();

            VerificarResetDiario(ref estado);
            SincronizarLogrosDesdeDB(ref estado, usuarioId);
            GuardarEstado(estado);

            return View(BuildViewModel(estado));
        }

        // ── Sincroniza logros con datos reales de BD ─────────────
        private void SincronizarLogrosDesdeDB(
            ref Dictionary<string, LogroEntrada> estado, int usuarioId)
        {
            var hoy = DateTime.Today;

            // Páginas leídas HOY (diferencia de UltimaPagina desde UltimoAcceso de hoy)
            var librosHoy = _context.UsuarioLibros
                .Where(ul => ul.UsuarioId == usuarioId
                          && ul.UltimoAcceso.HasValue
                          && ul.UltimoAcceso.Value.Date == hoy)
                .ToList();

            int paginasHoy = librosHoy.Sum(ul => ul.UltimaPagina);
            bool abrioLibro = librosHoy.Any();
            bool guardo = librosHoy.Any(ul => ul.UltimoAcceso.HasValue
                                                && ul.UltimoAcceso.Value.Date == hoy);

            // Logros DIARIOS
            SetProgreso(ref estado, "diario_abrir_libro", abrioLibro ? 1 : 0);
            SetProgreso(ref estado, "diario_10_paginas", Math.Min(paginasHoy, 10));
            SetProgreso(ref estado, "diario_25_paginas", Math.Min(paginasHoy, 25));
            SetProgreso(ref estado, "diario_guardar_progreso", guardo ? 1 : 0);

            // Logros especiales por hora
            var hora = DateTime.Now.Hour;
            if (abrioLibro && hora < 8)
                SetProgreso(ref estado, "esp_madrugador", 1);
            if (abrioLibro && hora >= 22)
                SetProgreso(ref estado, "esp_nocturno", 1);

            // Logros PERMANENTES — páginas totales acumuladas
            var totalPaginas = _context.UsuarioLibros
                .Where(ul => ul.UsuarioId == usuarioId)
                .Sum(ul => (int?)ul.UltimaPagina) ?? 0;

            SetProgreso(ref estado, "perm_100_paginas", Math.Min(totalPaginas, 100));
            SetProgreso(ref estado, "perm_500_paginas", Math.Min(totalPaginas, 500));

            // Libros completados al 100%
            var librosCompletos = _context.UsuarioLibros
                .Where(ul => ul.UsuarioId == usuarioId && ul.Progreso >= 100)
                .Count();

            SetProgreso(ref estado, "perm_primer_libro", Math.Min(librosCompletos, 1));
            SetProgreso(ref estado, "perm_5_libros", Math.Min(librosCompletos, 5));

            // Racha desde Tb_Racha
            var racha = _context.Tb_Racha
                .FirstOrDefault(r => r.UsuarioId == usuarioId);

            int diasRacha = racha?.DiasConsecutivos ?? 0;
            SetProgreso(ref estado, "perm_racha_7", Math.Min(diasRacha, 7));
            SetProgreso(ref estado, "perm_racha_30", Math.Min(diasRacha, 30));
        }

        private void SetProgreso(
            ref Dictionary<string, LogroEntrada> estado, string id, int valor)
        {
            var def = LOGROS_DEF.FirstOrDefault(d => d.Id == id);
            if (def is null) return;

            var entrada = InitEntrada(estado, def);
            if (entrada.Reclamado) return;

            entrada.Progreso = valor;
            if (entrada.Progreso >= def.Meta && !entrada.Completado)
                entrada.Completado = true;
        }

        // ── Reclamar [POST] ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reclamar(string id)
        {
            var estado = CargarEstado();
            var def = LOGROS_DEF.FirstOrDefault(d => d.Id == id);
            if (def is null)
                return Json(new { ok = false, msg = "Logro no encontrado." });

            var entrada = InitEntrada(estado, def);
            if (!entrada.Completado || entrada.Reclamado)
                return Json(new { ok = false, msg = "No se puede reclamar." });

            entrada.Reclamado = true;
            entrada.FechaReclamo = DateTime.UtcNow;

            // Activa "Primer libro" si es el primer reclamo permanente
            if (def.Tipo != "permanente")
            {
                var primerDef = LOGROS_DEF.First(d => d.Id == "perm_primer_libro");
                var primerEntrada = InitEntrada(estado, primerDef);
                if (!primerEntrada.Completado)
                {
                    primerEntrada.Progreso = 1;
                    primerEntrada.Completado = true;
                }
            }

            GuardarEstado(estado);
            return Json(new { ok = true, nombre = def.Nombre });
        }

        // ── Reset manual [POST] ──────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetearDiarios()
        {
            var estado = CargarEstado();
            foreach (var def in LOGROS_DEF.Where(d => d.Tipo == "diario"))
            {
                InitEntrada(estado, def);
                estado[def.Id].Progreso = 0;
                estado[def.Id].Completado = false;
                estado[def.Id].Reclamado = false;
            }
            HttpContext.Session.SetString(SESSION_FECHA, "");
            GuardarEstado(estado);
            return Json(new { ok = true });
        }

        // ── Helpers ──────────────────────────────────────────────
        private void VerificarResetDiario(ref Dictionary<string, LogroEntrada> estado)
        {
            var guardada = HttpContext.Session.GetString(SESSION_FECHA);
            var actual = DateTime.Now.ToString("yyyy-MM-dd");
            if (guardada == actual) return;

            foreach (var def in LOGROS_DEF.Where(d => d.Tipo == "diario"))
                if (estado.TryGetValue(def.Id, out var e))
                { e.Progreso = 0; e.Completado = false; e.Reclamado = false; }

            HttpContext.Session.SetString(SESSION_FECHA, actual);
            GuardarEstado(estado);
        }

        private LogroEntrada InitEntrada(
            Dictionary<string, LogroEntrada> estado, LogroDef def)
        {
            if (!estado.ContainsKey(def.Id))
                estado[def.Id] = new LogroEntrada();
            return estado[def.Id];
        }

        private Dictionary<string, LogroEntrada> CargarEstado()
        {
            var raw = HttpContext.Session.GetString(SESSION_ESTADO);
            if (string.IsNullOrEmpty(raw)) return new();
            try { return JsonSerializer.Deserialize<Dictionary<string, LogroEntrada>>(raw)!; }
            catch { return new(); }
        }

        private void GuardarEstado(Dictionary<string, LogroEntrada> estado) =>
            HttpContext.Session.SetString(SESSION_ESTADO, JsonSerializer.Serialize(estado));

        private LogrosViewModel BuildViewModel(Dictionary<string, LogroEntrada> estado)
        {
            var vm = new LogrosViewModel
            {
                FechaHoy = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy",
                              new System.Globalization.CultureInfo("es-MX")),
                Diarios = new(),
                Permanentes = new(),
                Completados = new(),
            };

            foreach (var def in LOGROS_DEF)
            {
                var entrada = InitEntrada(estado, def);
                var card = new LogroCard(def, entrada);

                if (def.Tipo == "diario") vm.Diarios.Add(card);
                else vm.Permanentes.Add(card);

                if (entrada.Reclamado) vm.Completados.Add(card);
            }

            vm.TotalDiarios = vm.Diarios.Count;
            vm.CompletadosHoy = vm.Diarios.Count(c => c.Entrada.Reclamado);
            return vm;
        }
        [HttpPost]
        public IActionResult GuardarOrden([FromBody] List<OrdenTopDto> orden)
        {
            int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var librosUsuario = _context.UsuarioLibros
                .Where(u => u.UsuarioId == usuarioId)
                .ToList();

            foreach (var item in orden)
            {
                var libro = librosUsuario.FirstOrDefault(l => l.LibroId == item.LibroId);
                if (libro != null)
                {
                    libro.Posicion = item.Posicion; // ✅ se guarda en DB
                }
            }

            _context.SaveChanges();
            return Ok(new { mensaje = "TOP guardado correctamente" });
        }


    }
}