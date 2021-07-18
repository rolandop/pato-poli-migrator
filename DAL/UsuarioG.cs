using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arpsis.Programs.Migrator.DAL
{
    [Table("usuario_g")]
    public class UsuarioG
    {
        [Column("user_id"), Key]
        public int UserId { get; set; }

        [Column("identificacion")]
        [MaxLength(50)]
        public string Identificacion { get; set; }       

        [Column("bandera")]
        public int Bandera { get; set; }
    }
}
