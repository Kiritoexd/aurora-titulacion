namespace AURORA.Models
{
    public class Tb_UsuarioLibro
    {
        public int Id { get; set; }

        public int UsuarioId { get; set; }
        public int LibroId { get; set; }
        public int Progreso { get; set; }

        public int UltimaPagina { get; set; }

        public DateTime? UltimoAcceso { get; set; }


        public Tb_Usuario Usuario { get; set; }
        public Tb_Libro Libro { get; set; }
        public TimeSpan? TiempoLectura { get; set; }
public DateTime? UltimoInicioLectura { get; set; } // guarda el inicio de lectura

        public int Posicion { get; set; }


    }

}
