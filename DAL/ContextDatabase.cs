using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arpsis.Programs.Migrator.DAL
{
    public class ContextDatabase: DbContext
    {
        public ContextDatabase()
            : base("DefaultConnection")
        {

        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Constants.DbShema);

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<VPersona> VPersonas { get; set; }
        public DbSet<Persona> Persona { get; set; }        
    }
}
