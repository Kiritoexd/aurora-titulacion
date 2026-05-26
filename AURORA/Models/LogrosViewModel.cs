namespace AURORA.Models
{
    // ═══════════════════════════════════════════════════════════════════
    //  AURORA — Modelos del sistema de logros
    //  Los logros se calculan en tiempo real desde la BD y se guardan
    //  en sesión (no tienen tabla propia en SQL Server).
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Definición estática de un logro (datos que no cambian).
    /// Se declara como record para que sea inmutable.
    /// </summary>
    public record LogroDef(
        string Id,      // identificador único, ej: "diario_abrir_libro"
        string Tipo,    // "diario", "permanente" o "especial"
        string Icono,   // SVG inline del ícono
        string Nombre,  // nombre visible al usuario
        string Desc,    // descripción del requisito
        int Meta     // valor que hay que alcanzar para completarlo
    );

    /// <summary>
    /// Estado actual de un logro para un usuario específico.
    /// Se serializa en la sesión HTTP (no en SQL Server).
    /// </summary>
    public class LogroEntrada
    {
        /// <summary>Progreso actual hacia la meta (ej: 7 de 10 páginas).</summary>
        public int Progreso { get; set; } = 0;

        /// <summary>true cuando Progreso >= Meta.</summary>
        public bool Completado { get; set; } = false;

        /// <summary>true cuando el usuario hizo clic en "Reclamar".</summary>
        public bool Reclamado { get; set; } = false;

        /// <summary>Fecha en que el usuario reclamó el logro.</summary>
        public DateTime? FechaReclamo { get; set; } = null;
    }

    /// <summary>
    /// Combina la definición y el estado de un logro para pasarlo a la vista.
    /// </summary>
    public class LogroCard
    {
        public LogroDef Def { get; }
        public LogroEntrada Entrada { get; }

        /// <summary>Porcentaje de avance (0–100) para la barra de progreso.</summary>
        public double Pct => Math.Min((double)Entrada.Progreso / Def.Meta, 1.0) * 100;

        public LogroCard(LogroDef def, LogroEntrada entrada)
        {
            Def = def;
            Entrada = entrada;
        }
    }

    /// <summary>
    /// ViewModel completo de la pantalla de logros.
    /// Agrupa logros diarios, permanentes y los ya completados.
    /// </summary>
    public class LogrosViewModel
    {
        public string FechaHoy { get; set; } = "";

        /// <summary>Logros que se reinician cada día a medianoche.</summary>
        public List<LogroCard> Diarios { get; set; } = new();

        /// <summary>Logros permanentes que se acumulan sin límite de tiempo.</summary>
        public List<LogroCard> Permanentes { get; set; } = new();

        /// <summary>Logros ya reclamados (diarios + permanentes + especiales).</summary>
        public List<LogroCard> Completados { get; set; } = new();

        /// <summary>Cantidad total de logros diarios disponibles.</summary>
        public int TotalDiarios { get; set; }

        /// <summary>Cantidad de logros diarios ya reclamados hoy.</summary>
        public int CompletadosHoy { get; set; }
    }
}