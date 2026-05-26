using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AURORA.Models
{
    [Table("Tb_Usuario")]
    public class Tb_Usuario
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Es obligatorio ingresar el/los nombre(s).")]
        [Display(Name = "Nombre(s)")]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Es obligatorio ingresar el apellido paterno.")]
        [Display(Name = "Apellido Paterno")]
        public string ApellidoPaterno { get; set; }
        [Display(Name = "Apellido Materno")]
        public string? ApellidoMaterno { get; set; }

        [Required(ErrorMessage = "Es obligatorio el correo electrónico.")]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Correo electrónico")]
        [EmailAddress(ErrorMessage = "El formato de Email es inválido.")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$",
            ErrorMessage = "La contraseña debe contener al menos: una mayúscula, una minúscula, un número y un carácter especial."
        )]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        [MaxLength(255)]
        public string Password { get; set; }

        [Display(Name = "Rol")]
        public string? Rol { get; set; }

        public string? FotoUrl { get; set; }

        public DateTime FechaRegistro { get; set; }
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }



    }
}
