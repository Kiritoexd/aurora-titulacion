namespace AURORA.Models
{
    public class Tb_Libro
    {
        public int Id { get; set; }

        public string? Titulo { get; set; }

        public string? Autor { get; set; }

        public string? Genero { get; set; }

        public int? Paginas { get; set; }

        public string? Editorial { get; set; }

        public int? Año { get; set; }
        public string? RutaPdf { get; set; }
        public string? PortadaUrl { get; set; }
    }
}
