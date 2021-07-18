using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arpsis.Programs.Migrator
{
    public class Constants
    {
        public static string BaseUrl {
            get {
                return ConfigurationManager.AppSettings["BaseUrl"];
            }
        }

        public static string Username
        {
            get
            {
                return ConfigurationManager.AppSettings["Username"];
            }
        }

        public static string Password
        {
            get
            {
                return ConfigurationManager.AppSettings["Password"];
            }
        }

        public static string Token
        {
            get
            {
                return ConfigurationManager.AppSettings["Token"];
            }
        }

        public static string Service
        {
            get
            {
                return ConfigurationManager.AppSettings["Service"];
            }
        }

        public static string DbShema
        {
            get
            {
                return ConfigurationManager.AppSettings["DbShema"];
            }
        }

        public static int ItemPerCall
        {
            get
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings["ItemPerCall"]);
            }
        }

        public static int StudentRoleId
        {
            get
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings["StudentRoleId"]);
            }
        }

        public static int CourseId
        {
            get
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings["CourseId"]);
            }
        }
    }
}
