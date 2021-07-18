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
    /// Servicio para administar usuarios    
    /// </summary>
    public class UserService
    {
        public LoginService LoginService { get; set; }
        public string Token = Constants.Token;
        public UserService()
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
        /// Agrega todos los usuarios como usuarios de moodle
        /// </summary>
        public void AddUsers() {

            using (var context = new ContextDatabase())
            {
                var users = context
                        .VPersonas
                        .Where(p=> p.Bandera == 0)
                        .ToList();

                var actual = 0;
                var total = users.Count;

                Console.WriteLine($"Usuarios encontrados {total}");

                var itemPerCall =  1;

                var userProcess = users
                       .OrderBy(u => u.UserId)
                       .Take(itemPerCall)
                       .ToList();

                //Procesa por lotes los usuarios 
                while (userProcess.Any())
                {
                    Console.WriteLine($"Procesando lote de {userProcess.Count} registros de {(total - actual)}");

                    var result = AddUsers(
                        userProcess.Select(u => new AddUserRequestModel
                        {
                            username = u.Identificacion,
                            firstname = u.Nombres,
                            lastname = u.Apellidos,
                            email = u.Email,
                            password = $"{u.Email}M2021"
                        }).ToList()
                    );
                    
                    Console.WriteLine($"... Llamada API terminada");

                    if (result.Any())
                    {
                        Console.WriteLine($"... {result.Count} procesados");

                        foreach (var userProces in userProcess)
                        {
                            Console.WriteLine($"Actualizando bandera {userProces.Identificacion} - {++actual}/{total}");

                            var userG = context
                                .UsuarioGs
                                .FirstOrDefault(u => u.UserId == userProces.UserId);

                            var userMoodle = result.FirstOrDefault(u => u.username == userG.Identificacion);
                            if (userMoodle != null)
                            {
                                userG.Bandera = userMoodle.id;
                            }
                            userProces.Processed = true;
                        }

                        context.SaveChanges();
                        Console.WriteLine($"... lote actualizado en base de datos");
                    }
                    else
                    {
                        userProcess.ForEach(u => {
                            u.Processed = true;
                            actual++;
                        });

                        Console.WriteLine($"  ... No se crearon usuarios");
                    }                    

                    userProcess = users
                        .Where(u => !u.Processed)
                        .OrderBy(u => u.UserId)
                        .Take(itemPerCall)
                        .ToList();

                    Console.WriteLine($"");
                };          

                Console.WriteLine($"Proceso Finalizado");
            }        
        }


        /// <summary>
        /// Agrega usuarios en moodle
        /// </summary>
        /// <param name="usersModel"></param>
        /// <returns></returns>
        private List<AddUserResponseModel> AddUsers(List<AddUserRequestModel> usersModel)
        {
            var client = new RestClient(Constants.BaseUrl);
            client.Timeout = -1;
            var request = new
                RestRequest($"webservice/rest/server.php?wstoken={Token}&wsfunction=core_user_create_users&moodlewsrestformat=json",
                            Method.POST);

            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            int i = 0;
            foreach (var userModel in usersModel)
            {
                request.AddParameter($"users[{i}][username]", userModel.username);
                request.AddParameter($"users[{i}][password]", userModel.password);
                request.AddParameter($"users[{i}][firstname]", userModel.firstname);
                request.AddParameter($"users[{i}][lastname]", userModel.lastname);
                request.AddParameter($"users[{i}][email]", userModel.email);
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
                else
                {
                    return JsonConvert.DeserializeObject<List<AddUserResponseModel>>(response.Content);
                }
            }
            else
            {
                Console.WriteLine("Error. No se creó usuario");
                Console.WriteLine(response.Content);
            }
            return new List<AddUserResponseModel>();
        }


        /// <summary>
        /// Valida usuarios entre moodle y la base de datos
        /// </summary>
        public void ValidateUsers()
        {
            using (var context = new ContextDatabase())
            {
                var users = context
                        .VPersonas
                        .Where(p => p.Bandera == 0)
                        .ToList();

                var actual = 0;
                var total = users.Count;

                Console.WriteLine($"Usuarios encontrados a validar {total}");

                var itemPerCall = 1;

                var userProcess = users
                       .OrderBy(u => u.UserId)
                       .Take(itemPerCall)
                       .ToList();

                while (userProcess.Any())
                {
                    Console.WriteLine($"Procesando lote de {userProcess.Count} registros de {(total - actual)}");

                    var result = GetUsers(
                        userProcess.Select(u => u.Identificacion).ToArray()
                    );

                    Console.WriteLine($"... Llamada API terminada");

                    Console.WriteLine($"... {result.users.Count} encontrados");

                    if (result.users.Any())
                    {   
                        foreach (var userProces in userProcess)
                        {
                            Console.WriteLine($"Actualizando bandera {userProces.Identificacion} - {++actual}/{total}");

                            var userG = context
                                .UsuarioGs
                                .FirstOrDefault(u => u.UserId == userProces.UserId);

                            var userMoodle = result.users.FirstOrDefault(u => u.username == userG.Identificacion);
                            if (userMoodle != null)
                            {
                                Console.WriteLine(JsonConvert.SerializeObject(userMoodle));
                                userG.Bandera = userMoodle.id;
                            }
                            userProces.Processed = true;
                        }

                        context.SaveChanges();
                        Console.WriteLine($"... lote actualizado en base de datos");
                    }
                    else
                    {
                        userProcess.ForEach(u => {
                            u.Processed = true;
                            actual++;
                        });

                        Console.WriteLine($"  ... No se encontraron usuarios de este lote");
                    }

                    userProcess = users
                        .Where(u => !u.Processed)
                        .OrderBy(u => u.UserId)
                        .Take(itemPerCall)
                        .ToList();

                    Console.WriteLine($"");
                };

                Console.WriteLine($"Proceso Finalizado");
            }
        }

        /// <summary>
        /// Obtiene usuarios de moodle
        /// </summary>
        /// <param name="usernames">Lista de usuarios(nombres de usuarios)</param>
        /// <returns></returns>
        private GetUserRequestModel GetUsers(string[] usernames)
        {
            var client = new RestClient(Constants.BaseUrl);
            client.Timeout = -1;
            var request = new
                RestRequest($"webservice/rest/server.php?wstoken={Token}&wsfunction=core_user_get_users&moodlewsrestformat=json",
                            Method.POST);

            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            var i = 0;
            foreach (var username in usernames)
            {
                request.AddParameter($"criteria[{i}][key]", "username");
                request.AddParameter($"criteria[{i}][value]", username);
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
                else
                {
                    return JsonConvert.DeserializeObject<GetUserRequestModel>(response.Content);
                }
            }
            else
            {
                Console.WriteLine("Error. No se proceso busqueda de usuarios");
                Console.WriteLine(response.Content);
            }
            return new GetUserRequestModel();
        }
    }

    /// <summary>
    /// Modelo de solicitud de creación de usuario en moodle
    /// </summary>
    public class AddUserRequestModel {
        public string username { get; set; }
        public string password { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string email { get; set; }
    }


    /// <summary>
    /// Modelo de respuesta de usuarios creados en moodle
    /// </summary>
    public class AddUserResponseModel
    {
        public int id { get; set; }
        public string username{ get; set; }        
    }


    /// <summary>
    /// Modelo de solicitud de consulta de usuarios
    /// </summary>
    public class GetUserRequestModel
    {
        public GetUserRequestModel()
        {
            users = new List<UserRequestModel>();
        }
        public List<UserRequestModel> users { get; set; }
    }

    /// <summary>
    /// Modelo de respuesta de usuario
    /// </summary>
    public class UserRequestModel
    {
        public int id { get; set; }
        public string username { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string email { get; set; }
    }
}
