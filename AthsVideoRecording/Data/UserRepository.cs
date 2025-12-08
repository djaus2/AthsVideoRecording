using AthsVideoRecording.Security;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace AthsVideoRecording.Data
{
    public class UserRow
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public byte[] Hash { get; set; } = Array.Empty<byte>();
        public byte[] Salt { get; set; } = Array.Empty<byte>();
        public string Role { get; set; } = string.Empty;
        public bool ForceChange { get; set; }
    }

    public class UserRepository
    {
        private static void AddParam(DbCommand cmd, string name, object? value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        public bool AdminExists(DbConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM Users WHERE Role = 'Admin' LIMIT 1";
            var obj = cmd.ExecuteScalar();
            return obj != null;
        }

        public void CreateUser(DbConnection conn, string username, string role, string password, bool forcePasswordChange)
        {
            var (hash, salt) = PasswordHasher.HashPassword(password);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Users (Username, PasswordHash, PasswordSalt, Role, CreatedAt, ForcePasswordChange)
VALUES (@u, @h, @s, @r, @c, @f);";
            AddParam(cmd, "@u", username);
            AddParam(cmd, "@h", hash);
            AddParam(cmd, "@s", salt);
            AddParam(cmd, "@r", role);
            AddParam(cmd, "@c", DateTime.UtcNow);
            AddParam(cmd, "@f", forcePasswordChange ? 1 : 0);
            cmd.ExecuteNonQuery();
        }

        public UserRow? GetByUsername(DbConnection conn, string username)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Username, PasswordHash, PasswordSalt, Role, ForcePasswordChange FROM Users WHERE Username=@u LIMIT 1";
            AddParam(cmd, "@u", username);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new UserRow
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Hash = (byte[])reader[2],
                    Salt = (byte[])reader[3],
                    Role = reader.GetString(4),
                    ForceChange = reader.GetBoolean(5)
                };
            }
            return null;
        }

        public bool UpdatePassword(DbConnection conn, int userId, string newPassword)
        {
            var (hash, salt) = PasswordHasher.HashPassword(newPassword);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Users SET PasswordHash=@h, PasswordSalt=@s, ForcePasswordChange=FALSE WHERE Id=@id";
            AddParam(cmd, "@h", hash);
            AddParam(cmd, "@s", salt);
            AddParam(cmd, "@id", userId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool ResetPassword(DbConnection conn, int userId, string newPassword, bool forceChange)
        {
            var (hash, salt) = PasswordHasher.HashPassword(newPassword);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Users SET PasswordHash=@h, PasswordSalt=@s, ForcePasswordChange=@f WHERE Id=@id";
            AddParam(cmd, "@h", hash);
            AddParam(cmd, "@s", salt);
            AddParam(cmd, "@f", forceChange ? 1 : 0);
            AddParam(cmd, "@id", userId);
            return cmd.ExecuteNonQuery() > 0;
        }
    }
}
