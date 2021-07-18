/// Autor: Rolando Peña
/// Fecha: 18/07/2021

using RestSharp;

namespace Arpsis.Programs.Migrator
{
    /// <summary>
    /// Servicio para obtener token de autenticación
    /// </summary>
    public class LoginService
    {
        /// <summary>
        /// Obtiene un token de autenticación
        /// </summary>
        /// <returns></returns>
        public LoginResponseModel GetToken() {

            var client = new RestClient(Constants.BaseUrl);
            client.Timeout = -1;
            var request = new 
                RestRequest($"login/token.php?username={Constants.Username}&password={Constants.Password}&service={Constants.Service}",
                            Method.GET);

            var response = client.Execute<LoginResponseModel>(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return response.Data;
            }
            return null;
        }
    }


    /// <summary>
    /// Modelo de respuesta de token
    /// </summary>
    public class LoginResponseModel
    {
        public string token { get; set; }
        public string privatetoken { get; set; }
    }
}
