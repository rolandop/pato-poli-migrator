/// Autor: Rolando Peña
/// Fecha: 18/07/2021

using System;

namespace Arpsis.Programs.Migrator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("PROCESAMIENTO DE USUARIOS");

            ProcessOption();

            Console.Read();
        }

        /// <summary>
        /// Procesa la opción seleccionada
        /// </summary>

        static void ProcessOption() {

            var option = ShowOptions();
            var userService = new UserService();
            var courseService = new CourseService();

            switch (option)
            {
                case 1:

                    userService.ValidateUsers();

                    break;
                case 2:
                    
                    userService.AddUsers();

                    break;
                case 3:

                    courseService.RoleId = Constants.StudentRoleId;
                    courseService.EnrolUsers();

                    break;
                case 4:

                    courseService.ShowResume();

                    break;

                case 5:
                    Environment.Exit(0);
                    break;

                default:
                    break;
            }

            ProcessOption();
        }

        /// <summary>
        /// Muestra lista de opiones disponibles
        /// </summary>
        /// <returns></returns>
        static int ShowOptions() {

            try
            {
                Console.WriteLine("");
                Console.WriteLine($"Seleccione una opción");
                Console.WriteLine($"1 Validar usuarios");
                Console.WriteLine($"2 Crear usuarios");
                Console.WriteLine($"3 Distribuir estudiantes en cursos");
                Console.WriteLine($"4 Resumen de distribución");
                Console.WriteLine($"5 Salir");
                Console.Write(": ");

                var option = Convert.ToInt32(Console.ReadLine());

                if (option < 1 || option > 5)
                {
                    return ShowOptions();
                }
                return option;
            }
            catch
            {
                Console.WriteLine("Opción incorrecta");
                return ShowOptions();
            }
        }
    }
}
