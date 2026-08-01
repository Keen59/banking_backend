namespace AuthService.Application.Interfaces.Managers
{
    public interface IRedisManager
    {
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
        Task SetListAsync<T>(string key,List<T> value, TimeSpan? expiry = null);
        Task<T?> GetAsync<T>(string key);
        Task<List<T?>?> GetListAsync<T>(string key);
        Task<bool> ExistsAsync(string key);
        Task<bool> RemoveAsync(string key);
    }
}
