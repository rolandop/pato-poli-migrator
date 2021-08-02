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

                var courseGroups = context
                           .VPersonas
                           .Where(p => p.IdUsuarioMoodle > 0
                               && p.IdCursoMoodle > 0)
                           .GroupBy(g => g.CodigoAula)
                           .Select(r => new {
                               Shorname = r.Key,
                               Users = r.ToList()
                           })
                           .ToList();

                if (courseGroups.Count() > 0)
                {
                    foreach (var course in courseGroups)
                    {

                        Console.WriteLine($"Curso: {course.Shorname}");
                        Console.WriteLine($"Carrera: {course.Users.FirstOrDefault().Carrera}");
                        Console.WriteLine($"Utilizado: {course.Users.Count}");
                        Console.WriteLine($"====================================");
                        Console.WriteLine($"N°\tIDENTIFICACIÓN\tEMAIL           \tCARRERA");
                        var i = 1;
                        foreach (var user in course.Users)
                        {
                            Console.WriteLine($"{i++}\t{user.Identificacion}\t{user.Email}\t{user.Carrera}");
                        }

                        Console.WriteLine($"Presione una tecla para continuar...");
                        Console.ReadKey();
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.WriteLine($"No existen cursos asignados");
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
                        var courseGroups = context
                            .VPersonas
                            .Where(p => p.IdUsuarioMoodle > 0
                                && (p.IdCursoMoodle == null || p.IdCursoMoodle == 0))
                            .GroupBy(g=> g.CodigoAula)
                            .Select(r=> new { 
                                Shorname = r.Key,
                                Users = r.ToList()
                            })
                            .ToList();

                        var totalGroups = courseGroups.Count;
                        Console.WriteLine($"Cursos encontrados {totalGroups}");

                        //Obtiene cursos actuales para validar si ya existe alguno creado
                        var courses = GetCourses();

                        foreach (var group in courseGroups)
                        {
                            //Busca el curso en la lista de cursos existentes
                            var course = 
                                    courses
                                        .FirstOrDefault(c => c.shortname.ToUpper() == group.Shorname.ToUpper());

                            //Si no encuentra el curso lo crea en moodle
                            if (course == null)
                            {
                                var courseResponseModel = CreateCourses(new List<CreateCourseRequestModel> { 
                                    new CreateCourseRequestModel {
                                        shortname = group.Shorname,
                                        fullname = group.Shorname
                                    }
                                });

                                if (courseResponseModel.Any())
                                {
                                    course = courseResponseModel.FirstOrDefault();
                                }
                            }
                            if (course == null)
                            {
                                Console.WriteLine($"No se pudo crear curso {group.Shorname}");
                                continue;
                            }

                            EnrolUsers(
                                group.Users.Select(u => new EnrolUserRequestModel
                                {
                                    roleid = RoleId,
                                    courseid = course.id,
                                    userid = u.IdUsuarioMoodle ?? 0,
                                    shortname = course.shortname
                                }).ToList());

                            var ids = group.Users.Select(u => u.IdUsuarioMoodle).ToList();
                            var users = context.Persona.Where(u=> ids.Contains(u.IdUsuarioMoodle)).ToList();
                            users.ForEach(u=> {
                                u.IdCurso = course.id;
                            });
                        }
                  
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
                        .Where(p=> p.IdUsuarioMoodle > 0)
                        .ToList();
                
                var total = users.Count;

                Console.WriteLine($"Usuarios encontrados {total}");

                var userProcess = users
                        .OrderBy(u => u.Id)
                        .Take(Constants.ItemPerCall)
                        .ToList();

                while (userProcess.Any())
                {                    
                    EnrolUsers(
                        userProcess.Select(u=> new EnrolUserRequestModel {
                            roleid = roleid,
                            userid = u.IdUsuarioMoodle ?? 0,
                            courseid = courseid
                        }).ToList()
                    );

                    userProcess.ForEach(u => u.Processed = true);

                    userProcess = users
                        .Where(u=> !u.Processed)
                        .OrderBy(u => u.Id)
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


        /// <summary>
        /// Creación de Cursos
        /// </summary>
        /// <returns></returns>
        private List<CourseResponseModel> CreateCourses(List<CreateCourseRequestModel> courses)
        {
            var client = new RestClient(Constants.BaseUrl);
            client.Timeout = -1;
            var request = new
                RestRequest($"webservice/rest/server.php?wstoken={Token}&wsfunction=core_course_create_courses&moodlewsrestformat=json",
                            Method.POST);

            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            var i = 0;
            foreach (var course in courses)
            {
                request.AddParameter($"courses[{i}][fullname]", course.fullname);
                request.AddParameter($"courses[{i}][shortname]", course.shortname);
                request.AddParameter($"courses[{i}][categoryid]", 1);
                i++;
            }

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
                Console.WriteLine("Error. No se pudo crear curso");
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

    public class CreateCourseRequestModel
    {        
        public string fullname { get; set; }
        public string shortname { get; set; }
    }
}
