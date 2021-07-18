using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arpsis.Programs.Migrator.DAL
{
    [Table("v_distributivo")]
    public class VDistributivo
    {
        [Column("dis_id"), Key]
        public int DisId { get; set; }

        [Column("ciudad_sede")]
        public string CiudadSede { get; set; }

        [Column("edificio")]
        public string Edificio { get; set; }

        [Column("carrera")]
        public string Carrera { get; set; }

        [Column("aula")]
        public string Aula { get; set; }

        [Column("capacidad")]
        public int Capacidad { get; set; }

        [Column("utilizado")]
        public int Utilizado { get; set; }

        [Column("acceso")]
        public int Acceso { get; set; }

        [Column("bandera")]
        public int Bandera { get; set; }

        public override string ToString()
        {
            return $"({Bandera}){Aula}-{Carrera}-{CiudadSede}-{Acceso}-{Capacidad}-{Utilizado}";
        }
    }
}
