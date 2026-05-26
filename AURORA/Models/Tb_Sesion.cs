using AURORA.Models;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Tb_Sesion")]
public class Tb_Sesion
{
    public int Id { get; set; }
    public int UsuarioLibroId { get; set; }
    public Tb_UsuarioLibro UsuarioLibro { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }
    public int Minutos { get; set; }
}
