using System.Net;
using System.Runtime.CompilerServices;

namespace Meteor_Rest
{
    class ConfigConstants
    {
        public static String OPTIONS_BINDIP;
        public static String OPTIONS_PORT;
        public static String FFXIV_SESSION_LENGTH;
        public static bool OPTIONS_TIMESTAMP = false;

        public static String DATABASE_HOST;
        public static String DATABASE_PORT;
        public static String DATABASE_NAME;
        public static String DATABASE_USERNAME;
        public static String DATABASE_PASSWORD;

        public static String PATCHER_PORT;
        public static String BOOT_VERSION;
        public static String GAME_VERSION;
        public static bool Load(WebApplication app)
        {
            app.Logger.LogInformation("Loading rest_config.ini file");

            if (!File.Exists("./rest_config.ini"))
            {
                
                Console.Error.WriteLine("FILE NOT FOUND!");
                Console.Error.WriteLine("Loading defaults...");
            }

            INIFile configIni = new INIFile("./rest_config.ini");

            ConfigConstants.OPTIONS_BINDIP = configIni.GetValue("General", "server_ip", "0.0.0.0");
            ConfigConstants.OPTIONS_PORT = configIni.GetValue("General", "server_port", "80");
            ConfigConstants.FFXIV_SESSION_LENGTH = configIni.GetValue("General", "session_length", "24");
            ConfigConstants.OPTIONS_TIMESTAMP = configIni.GetValue("General", "showtimestamp", "true").ToLower().Equals("true");

            ConfigConstants.DATABASE_HOST = configIni.GetValue("Database", "host", "127.0.0.1");
            ConfigConstants.DATABASE_PORT = configIni.GetValue("Database", "port", "3306");
            ConfigConstants.DATABASE_NAME = configIni.GetValue("Database", "database", "ffxiv_server");
            ConfigConstants.DATABASE_USERNAME = configIni.GetValue("Database", "username", "root");
            ConfigConstants.DATABASE_PASSWORD = configIni.GetValue("Database", "password", "");

            ConfigConstants.PATCHER_PORT = configIni.GetValue("FFXIV", "patchserver_port", "54996");
            ConfigConstants.BOOT_VERSION = configIni.GetValue("FFXIV", "bootversion", "2010.09.18.0000");
            ConfigConstants.GAME_VERSION = configIni.GetValue("FFXIV", "gameversion", "2012.09.19.0001");
            return true;
        }
        public static void ApplyLaunchArgs(WebApplication app, string[] launchArgs)
        {
            var args = (from arg in launchArgs select arg.ToLower().Trim().TrimStart('-')).ToList();

            for (var i = 0; i + 1 < args.Count; i += 2)
            {
                var arg = args[i];
                var val = args[i + 1];
                var legit = false;

                if (arg == "ip")
                {
                    IPAddress ip;
                    if (IPAddress.TryParse(val, out ip) && (legit = true))
                        OPTIONS_BINDIP = val;
                }
                else if (arg == "port")
                {
                    UInt16 port;
                    if (UInt16.TryParse(val, out port) && (legit = true))
                        OPTIONS_PORT = val;
                }
                else if (arg == "user" && (legit = true))
                {
                    DATABASE_USERNAME = val;
                }
                else if (arg == "p" && (legit = true))
                {
                    DATABASE_PASSWORD = val;
                }
                else if (arg == "db" && (legit = true))
                {
                    DATABASE_NAME = val;
                }
                else if (arg == "host" && (legit = true))
                {
                    DATABASE_HOST = val;
                }
                if (!legit)
                {
                    app.Logger.LogError("Invalid parameter <{0}> for argument: <--{1}> or argument doesnt exist!", val, arg);
                }
            }
        }
    }
}
