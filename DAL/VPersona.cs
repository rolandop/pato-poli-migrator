using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arpsis.Programs.Migrator.DAL
{
    [Table("v_persona")]
    public class VPersona
    {
        [Column("user_id"), Key]
        public int UserId { get; set; }

        [Column("carrera")]
        public string Carrera { get; set; }

        [Column("identificacion")]
        public string Identificacion { get; set; }

        [Column("apellidos")]
        public string Apellidos { get; set; }

        [Column("nombres")]
        public string Nombres { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("bandera")]
        public int Bandera { get; set; }

        [Column("ciudad_sede")]
        public string CiudadSede { get; set; }

        [Column("discapacidad")]
        public string Discapacidad { get; set; }

        [Column("pais_nacionalidad")]
        public string PaisNacionalidad { get; set; }

        [Column("aula")]
        public string Aula { get; set; }

        [NotMapped]
        public bool Processed { get; set; }

        public override string ToString()
        {
            return $"{Carrera}-{CiudadSede}-{Discapacidad}-{PaisNacionalidad}-{Aula}"; 
        }
    }
}
