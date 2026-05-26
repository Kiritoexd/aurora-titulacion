using System.Collections.Generic;
using System.Linq;

namespace AURORA.ViewModels
{
    public class EstadisticasViewModel
    {
        // Ya existentes
        public int LibrosPendientes { get; set; }
        public int LibrosLeyendo { get; set; }
        public int LibrosTerminados { get; set; }

        public List<string> Generos { get; set; }
        public List<int> CantidadPorGenero { get; set; }

        public List<string> Meses { get; set; }
        public List<int> MinutosPorMes { get; set; }

        /// <summary>Minutos totales acumulados de todas las sesiones.</summary>
        public int MinutosTotales => (MinutosPorMes ?? new List<int>()).Sum();

        /// <summary>Tiempo total legible, p.ej. "3h 45m" o "50m".</summary>
        public string TiempoTotalFormateado
        {
            get
            {
                int total = MinutosTotales;
                if (total <= 0) return "0m";
                int horas = total / 60;
                int mins = total % 60;
                return horas > 0 ? $"{horas}h {mins}m" : $"{mins}m";
            }
        }

        // 🔹 Nuevas propiedades para las gráficas adicionales
        public List<string> Anios { get; set; } = new List<string>();
        public List<int> LibrosPorAnio { get; set; } = new List<int>();

        public List<string> Sesiones { get; set; } = new List<string>();
        public List<int> PromedioMinutos { get; set; } = new List<int>();
    }
}