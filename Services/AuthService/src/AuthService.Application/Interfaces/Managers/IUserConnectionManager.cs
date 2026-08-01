namespace AuthService.Application.Interfaces.Managers
{
    public interface IUserConnectionManager
    {
        void AddConnection(string userId, string connectionId);
        void RemoveConnection(string userId, string connectionId);
        string GetConnection(string userId);
    }
}
