/// Autor: Rolando Peña
/// Fecha: 18/07/2021

using Arpsis.Programs.Migrator.DAL;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arpsis.Programs.Migrator
{
    /// <summary>
    /// Servicio para administar cursos    
    /// </summary>
    public class CourseService
    {
        public LoginService LoginService { get; set; }
        public string Token = Constants.Token;
        public int RoleId { get; set; }
        public CourseService()
        {
            System.Net
                .ServicePointManager
                .ServerCertificateValidationCallback = delegate { return true; };            

            if (string.IsNullOrEmpty(Token))
            {
                LoginService = new LoginService();
                var tokenModel = LoginService.GetToken();
                if (tokenModel == null)
                {
                    throw new Exception("Error de autenticación");
                }
                Token = tokenModel.token;
            }
        }

        /// <summary>
        /// Muestra un resumen de cursos asignados a estudianes
        /// </summary>
        public void ShowResume() {

            using (var context = new ContextDatabase())
            {
                
                var courses = context
                        .VDistributivos
                        .ToList();


                foreach (var course in courses)
                {
                    var users = context
                        .VPersonas
                        .Where(p => p.Bandera > 0
                            && p.Aula == course.Aula)
                        .ToList();

                    Console.WriteLine($"Curso: {course.Aula}");
                    Console.WriteLine($"Ciudad: {course.CiudadSede}");
                    Console.WriteLine($"Carrera: {course.Carrera}");
                    Console.WriteLine($"Capacidad: {course.Capacidad}");
                    Console.WriteLine($"Utilizado: {course.Utilizado}");
                    Console.WriteLine($"Acceso: {course.Acceso}");
                    Console.WriteLine($"Total Discapacidatos: {users.Where(u=> u.Discapacidad != "NINGUNA").Count()}");
                    Console.WriteLine($"Total Extranjeros: {users.Where(u => u.PaisNacionalidad != "ECUADOR").Count()}");
                    Console.WriteLine($"====================================");

                    Console.WriteLine($"N°\tIDENTIFICACIÓN\tEMAIL           \tCIUDAD\tCARRERA        \tNACIONALIDAD \tDISCAPACIDAD");
                    var i = 1;
                    foreach (var user in users)
                    {
                        Console.WriteLine($"{i++}\t{user.Identificacion}\t{user.Email}\t{user.CiudadSede}\t{user.Carrera}\t{user.PaisNacionalidad} \t{user.Discapacidad}");
                    }

                    Console.WriteLine($"Presione una tecla para continuar...");
                    Console.ReadKey();
                    Console.WriteLine();
                }

            }
        }

        /// <summary>
        /// Asigna usuarios a curss disponibes
        /// </summary>
        public void EnrolUsers()
        {
            using (var context = new ContextDatabase())
            {
                using (var tran = context.Database.BeginTransaction())
                {
                    try
                    {
                        var users = context
                            .VPersonas
                            .Where(p => p.Bandera > 0
                                && (p.Aula == null || p.Aula == ""))
                            .ToList();

                        var totalUsers = users.Count;
                        Console.WriteLine($"Usuarios encontrados {totalUsers}");

                        var distributivo = context
                                .VDistributivos
                                .ToList();

                        var courseId = 1;
                        distributivo.ForEach(c => c.Bandera = courseId++);

                        var totalCourses = distributivo.Count;
                        Console.WriteLine($"Cursos encontrados {totalCourses}");


                        //Procesa extranjeros equitativamente

                        foreach (var user in users.Where(u => u.PaisNacionalidad != "ECUADOR" && string.IsNullOrWhiteSpace(u.Aula)))
                        {
                            var course = distributivo
                                            .Where(c => c.Carrera == user.Carrera
                                                        && c.CiudadSede == user.CiudadSede
                                                        && c.Capacidad > c.Utilizado)
                                            .OrderBy(c => c.Utilizado)
                                            .FirstOrDefault();

                            if (course != null)
                            {
                                user.Aula = course.Aula;
                                course.Utilizado++;
                            }
                        }

                        //Procesa primero las aulas con personas con algún tipo de discapacidad

                        foreach (var user in users.Where(u => u.Discapacidad != "NINGUNA"))
                        {
                            var course = distributivo
                                            .Where(c => c.Carrera == user.Carrera
                                                        && c.CiudadSede == user.CiudadSede
                                                        && c.Capacidad > c.Utilizado
                                                        && c.Acceso == 1)
                                            .OrderBy(c => c.Utilizado)
                                            .FirstOrDefault();

                            if (course != null)
                            {
                                user.Aula = course.Aula;
                                course.Utilizado++;
                            }
                        }

                        //Procesa resto de estudiantes

                        foreach (var user in users.Where(u => string.IsNullOrWhiteSpace(u.Aula)))
                        {
                            var course = distributivo
                                            .Where(c => c.Carrera == user.Carrera
                                                        && c.CiudadSede == user.CiudadSede
                                                        && c.Capacidad > c.Utilizado)
                                            .OrderBy(c => c.Utilizado)
                                            .FirstOrDefault();

                            if (course != null)
                            {
                                user.Aula = course.Aula;
                                course.Utilizado++;
                            }
                        }

                        //Mapeo de cursos de moodle vs distributivo
                        var courses = GetCourses();

                        distributivo.ForEach(d =>
                            d.Bandera = courses
                                .FirstOrDefault(c=> c.shortname.ToUpper().Equals(d.Aula.ToUpper().Trim()))?.id ?? 0
                        );

                        //Agisna a los estudiantes a los cursos en moodle
                        users
                            .Join(courses,
                                u => u.Aula,
                                c => c.shortname,
                                (u, c) => new { 
                                    user = u,
                                    course = c
                                })
                            .GroupBy(u => u.course.id)
                            .ToList()
                            .ForEach(g=> EnrolUsers(
                                g.Select(u2 => new EnrolUserRequestModel{
                                    roleid = RoleId,
                                    courseid = g.Key,
                                    userid = u2.user.Bandera,
                                    shortname = u2.user.Aula
                                }).ToList())
                            );

                        context.SaveChanges();

                        tran.Commit();

                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();

                        Console.WriteLine($"Error al asignar cursos. {ex.Message}");
                    }
                }

                Console.WriteLine($"Proceso Finalizado");
            }
        }

        /// <summary>
        /// Asigna a un curso a los usuarios de la ase de datos
        /// </summary>
        /// <param name="roleid"></param>
        /// <param name="courseid"></param>
        public void EnrolUsers(int roleid, int courseid) {

            using (var context = new ContextDatabase())
            {
                var users = context
                        .VPersonas
                        .Where(p=> p.Bandera > 0)
                        .ToList();
                
                var total = users.Count;

                Console.WriteLine($"Usuarios encontrados {total}");

                var userProcess = users
                        .OrderBy(u => u.UserId)
                        .Take(Constants.ItemPerCall)
                        .ToList();

                while (userProcess.Any())
                {                    
                    EnrolUsers(
                        userProcess.Select(u=> new EnrolUserRequestModel {
                            roleid = roleid,
                            userid = u.Bandera,
                            courseid = courseid
                        }).ToList()
                    );

                    userProcess.ForEach(u => u.Processed = true);

                    userProcess = users
                        .Where(u=> !u.Processed)
                        .OrderBy(u => u.UserId)
                        .Take(Constants.ItemPerCall)
                        .ToList();
                };
                
                Console.WriteLine($"Proceso Finalizado");
            }        
        }

        /// <summary>
        /// Asigna a cursos a usuarios en moodle
        /// </summary>
        /// <param name="enrolUsers">Lista de usuarios a asignar</param>
        /// <returns></returns>
        private bool EnrolUsers(List<EnrolUserRequestModel> enrolUsers)
        {
            if (!enrolUsers.Any())
            {
                return false;
            }

            var client = new RestClient(Constants.BaseUrl);
            client.Timeout = -1;
            var request = new
                RestRequest($"webservice/rest/server.php?wstoken={Token}&wsfunction=enrol_manual_enrol_users&moodlewsrestformat=json",
                            Method.POST);

            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            var i = 0;
            foreach (var enrolUser in enrolUsers)
            {
                request.AddParameter($"enrolments[{i}][roleid]", enrolUser.roleid);
                request.AddParameter($"enrolments[{i}][userid]", enrolUser.userid);
                request.AddParameter($"enrolments[{i}][courseid]", enrolUser.courseid);
                i++;
            }

            Console.WriteLine($"Procesando curso {enrolUsers.FirstOrDefault().shortname}");

            var response = client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                if (response.Content.IndexOf("exception") > -1)
                {
                    Console.WriteLine("Error al procesar llamada");
                    Console.WriteLine(response.Content);
                    return false;
                }
                Console.WriteLine($"  {enrolUsers.Count}... Processed");
            }
            else
            {
                Console.WriteLine("Error. No se asigno rol");
                Console.WriteLine(response.Content);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Obtiene una lista de cursos disponibles en moodle
        /// </summary>
        /// <returns></returns>
        private List<CourseResponseModel> GetCourses()
        {
            var client = new RestClient(Constants.BaseUrl);
            client.Timeout = -1;
            var request = new
                RestRequest($"webservice/rest/server.php?wstoken={Token}&wsfunction=core_course_get_courses&moodlewsrestformat=json",
                            Method.POST);

            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            var response = client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                if (response.Content.IndexOf("exception") > -1)
                {
                    Console.WriteLine("Error al procesar llamada");
                    Console.WriteLine(response.Content);
                }

                return JsonConvert.DeserializeObject<List<CourseResponseModel>>(response.Content);
            }
            else
            {
                Console.WriteLine("Error. No se pudo obtener cursos");
                Console.WriteLine(response.Content);
            }
            return new List<CourseResponseModel>();
        }
    }

    public class EnrolUserRequestModel {
        public int roleid { get; set; }
        public int userid { get; set; }
        public int courseid { get; set; }
        public string shortname { get; set; }
    }

    public class CourseResponseModel
    {
        public int id { get; set; }
        public string fullname { get; set; }
        public string shortname { get; set; }
    }
}
