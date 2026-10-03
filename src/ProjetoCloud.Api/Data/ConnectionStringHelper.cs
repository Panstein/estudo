using Npgsql;

namespace ProjetoCloud.Api.Data;

public static class ConnectionStringHelper
{
    /// <summary>
    /// Aceita tanto o formato Npgsql ("Host=...;Username=...") quanto a URL
    /// que o Neon fornece ("postgresql://user:pass@host/db?sslmode=require").
    /// </summary>
    public static string Normalize(string connectionString)
    {
        if (!connectionString.StartsWith("postgres://") && !connectionString.StartsWith("postgresql://"))
            return connectionString;

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
            SslMode = SslMode.Require,
        };

        return builder.ConnectionString;
    }
}
