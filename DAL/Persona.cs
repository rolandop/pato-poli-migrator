using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arpsis.Programs.Migrator.DAL
{
    [Table("persona")]
    public class Persona
    {
        [Column("id"), Key]
        public int Id { get; set; }

        [Column("identificacion")]
        [MaxLength(50)]
        public string Identificacion { get; set; }       

        [Column("id_usuario_moodle")]
        public int? IdUsuarioMoodle { get; set; }

        [Column("id_curso")]
        public int? IdCurso { get; set; }
    }
}
