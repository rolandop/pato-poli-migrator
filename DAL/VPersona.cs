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
        [Column("id"), Key]
        public int Id { get; set; }

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

        [Column("id_usuario_moodle")]
        public int? IdUsuarioMoodle { get; set; }       

        [Column("codigo_aula")]
        public string CodigoAula { get; set; }

        [Column("id_curso_moodle")]
        public int? IdCursoMoodle { get; set; }

        [NotMapped]
        public bool Processed { get; set; }

        public override string ToString()
        {
            return $"{Id}-{Identificacion}-{Nombres}-{Email}-{CodigoAula}"; 
        }
    }
}
