using HashLib;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Meteor_Rest
{
    class SqlServer
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public SqlServer(ILogger logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        public bool TestConnection()
        {
            _logger.LogInformation("Testing DB connection to \"{0}\"... ", _configuration["Database:db_host"]);

            string query = String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}",
                _configuration["Database:db_host"], _configuration["Database:db_port"], _configuration["Database:db_schema"],
                _configuration["Database:db_username"], _configuration["Database:db_password"]);

            using (MySqlConnection conn = new MySqlConnection(query))
            {
                try
                {
                    conn.Open();
                    conn.Close();

                    _logger.LogInformation("DB connection ok.");
                    return true;
                }
                catch (MySqlException e)
                {
                    _logger.LogError(e.ToString());
                    return false;
                }
            }
        }
        private MySqlConnection? Connect()
        {
            MySqlConnection? con = null;
            try
            {
                con = new MySqlConnection(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}",
                    _configuration["Database:db_host"], _configuration["Database:db_port"], _configuration["Database:db_schema"],
                    _configuration["Database:db_username"], _configuration["Database:db_password"]));
            }
            catch (MySqlException e)
            {
                _logger.LogError(e.ToString());
                con?.Close();
            }
            return con;
        }
        public bool DoesUsernameExist(string username)
        {
            using MySqlConnection? con = Connect();
            con?.Open();

            MySqlCommand cmd = new MySqlCommand("SELECT name FROM users WHERE name = @username", con);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Prepare();

            
            using MySqlDataReader reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                return true;
            }
            return false;
        }

        public bool CreateAccount(string username, string password, string email, string lang, string region)
        {

            // Taken from SimpleHTTPServer ----

            //generate the salt and password hash
            int random = RandomNumberGenerator.GetInt32(int.MaxValue);

            //hash that random number to generate our salt
            IHash hasher = HashFactory.Crypto.CreateSHA224();
            HashResult hr = hasher.ComputeInt(random);
            string salt = hr.ToString().Replace("-", "").ToLower();

            //fix up hashed password's formatting to match expected formatting
            HashResult r = hasher.ComputeString((password + salt), Encoding.ASCII);
            string hashedpassword = r.ToString().Replace("-", "").ToLower();

            //format a proper language launch setting based on lang
            if (lang.Equals("EN"))
            {
                lang = "en-us";
            }
            else if (lang.Equals("UK"))
            {
                lang = "en-gb";
            }
            else if (lang.Equals("DE"))
            {
                lang = "de-de";
            }
            else if (lang.Equals("FR"))
            {
                lang = "fr-fr";
            }
            else if (lang.Equals("JA"))
            {
                lang = "ja-jp";
            }

            // ---

            using MySqlConnection? con = Connect();
            try
            {
                con?.Open();

                MySqlCommand cmd = new MySqlCommand("INSERT INTO users (name, passhash, salt, email, lang, region)" +
                    "VALUES (@username, @passhash, @salt, @email, @lang, @region)", con);
                

                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@passhash", hashedpassword);
                cmd.Parameters.AddWithValue("@salt", salt);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@lang", lang);
                cmd.Parameters.AddWithValue("@region", region);
                cmd.Prepare();

                int affectedrows = cmd.ExecuteNonQuery();
                if (affectedrows == 1)
                {
                    return true;
                }
                return false;

            }
            catch (MySqlException e)
            {
                _logger.LogError(e.ToString());
                return false;
            }
        }
        public int LoginAccount(string username, string password)
        {
            using MySqlConnection? con = Connect();
            try
            {
                con?.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT id, passhash, salt, lang, region FROM users WHERE name = @username", con);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Prepare();

                using MySqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Read();

                    //check the password
                    string storedpasshash = reader.GetString(1);
                    string salt = reader.GetString(2);

                    string saltedpassword = password + salt;
                    IHash hasher = HashFactory.Crypto.CreateSHA224();

                    //fix up hashed password's formatting to match expected formatting
                    HashResult r = hasher.ComputeString(saltedpassword, Encoding.ASCII);
                    string hashedpassword = r.ToString().Replace("-", "").ToLower();

                    if (storedpasshash.Equals(hashedpassword))
                    {
                        return reader.GetInt32(0);

                    }
                }


            }
            catch (MySqlException e)
            {
                _logger.LogError(e.ToString());
            }
            return -1;
        }
        public string? CreateOrRefreshSession(int uid)
        {
            string? sid = null;
            using MySqlConnection? con = Connect();
            try
            {
                con?.Open();
                sid = GetSessionID(con, uid);
                if (sid != null)
                {
                    RefreshSession(con, sid);
                }
                else
                {
                    sid = CreateSessionID(con, uid);
                }
            }
            catch (MySqlException e)
            {
                _logger.LogError(e.ToString());
            }
            return sid;
        }
        public string? GetSessionID(MySqlConnection? con, int uid)
        {
            MySqlCommand cmd = new MySqlCommand("SELECT id FROM sessions WHERE userId = @uid AND expiration > NOW()", con);
            cmd.Parameters.AddWithValue("@uid", uid);
            cmd.Prepare();

            using MySqlDataReader reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                reader.Read();
                return reader.GetString(0);
            }
            return null;
        }
        public string CreateSessionID(MySqlConnection? con, int uid)
        {

            //make a nice random number
            int random = RandomNumberGenerator.GetInt32(int.MaxValue);

            //hash that random number to generate our sessionid
            IHash hasher = HashFactory.Crypto.CreateSHA224();
            HashResult r = hasher.ComputeInt(random);
            string generatedsessionid = r.ToString().Replace("-", "").ToLower();


            //delete any sessions associated with this uid
            //_logger.LogInformation("Deleting any expired sessions");
            MySqlCommand cmd = new MySqlCommand("DELETE FROM sessions WHERE userId = @uid", con);
            cmd.Parameters.AddWithValue("@uid", uid);
            cmd.Prepare();
            cmd.ExecuteNonQuery();

            //actually create a new session
            //_logger.LogInformation("Creating a new session");
            MySqlCommand cmd2 = new MySqlCommand(
                string.Format("INSERT INTO sessions (id, userid, expiration) VALUES (@sessionID, @uid, NOW() + INTERVAL {0} HOUR)", _configuration["Database:db_session_length"]),
                con);
            cmd2.Parameters.AddWithValue("@sessionID", generatedsessionid);
            cmd2.Parameters.AddWithValue("@uid", uid);
            cmd2.Prepare();
            cmd2.ExecuteNonQuery();

            return generatedsessionid;
        }
        public bool RefreshSession(MySqlConnection? con, string sid)
        {
            if (sid == null || sid.Length == 0)
            {
                return false;
            }
            MySqlCommand cmd = new MySqlCommand(
                string.Format("UPDATE sessions SET expiration = NOW() + INTERVAL {0} HOUR WHERE id = @id", _configuration["Database:db_session_length"]),
                con);
            cmd.Parameters.AddWithValue("@id", sid);
            cmd.Prepare();


            int sessionrowsaffected = cmd.ExecuteNonQuery();
            if (sessionrowsaffected > 0)
            {
                return true;
            }
            return false;
        }
    }
}
