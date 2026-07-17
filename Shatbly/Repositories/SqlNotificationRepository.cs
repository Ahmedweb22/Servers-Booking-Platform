using Microsoft.Data.SqlClient;

namespace Shtbly.Repositories
{
public class SqlNotificationRepository(IConfiguration configuration) : INotificationRepository
{
    private readonly string _connectionString =
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Notifications (Title, Message, Type, IsRead, CreatedAt, UserId, BookingId)
            OUTPUT INSERTED.Id
            VALUES (@Title, @Message, @Type, @IsRead, @CreatedAt, @UserId, @BookingId);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        AddNotificationParameters(command, notification);

        var id = await command.ExecuteScalarAsync(cancellationToken);
        notification.Id = Convert.ToInt32(id);
    }

    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, Title, Message, Type, IsRead, CreatedAt, UserId, BookingId
            FROM Notifications
            WHERE UserId = @UserId
            ORDER BY CreatedAt DESC;
            """;

        var notifications = new List<Notification>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            notifications.Add(ReadNotification(reader));
        }

        return notifications;
    }

    public async Task<Notification?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, Title, Message, Type, IsRead, CreatedAt, UserId, BookingId
            FROM Notifications
            WHERE Id = @Id;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadNotification(reader) : null;
    }

    public async Task MarkAsReadAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Notifications SET IsRead = 1 WHERE Id = @Id;";
        await ExecuteByIdAsync(sql, id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM Notifications WHERE Id = @Id;";
        await ExecuteByIdAsync(sql, id, cancellationToken);
    }

    private async Task ExecuteByIdAsync(string sql, int id, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddNotificationParameters(SqlCommand command, Notification notification)
    {
        command.Parameters.AddWithValue("@Title", notification.Title);
        command.Parameters.AddWithValue("@Message", notification.Message);
        command.Parameters.AddWithValue("@Type", (int)notification.Type);
        command.Parameters.AddWithValue("@IsRead", notification.IsRead);
        command.Parameters.AddWithValue("@CreatedAt", notification.CreatedAt);
        command.Parameters.AddWithValue("@UserId", notification.UserId);
        command.Parameters.AddWithValue("@BookingId", notification.BookingId ?? (object)DBNull.Value);
    }

    private static Notification ReadNotification(SqlDataReader reader)
    {
        return new Notification
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Title = reader.GetString(reader.GetOrdinal("Title")),
            Message = reader.GetString(reader.GetOrdinal("Message")),
            Type = (NotificationType)reader.GetInt32(reader.GetOrdinal("Type")),
            IsRead = reader.GetBoolean(reader.GetOrdinal("IsRead")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UserId = reader.GetString(reader.GetOrdinal("UserId")),
            BookingId = reader.IsDBNull(reader.GetOrdinal("BookingId"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("BookingId"))
        };
    }
}

}