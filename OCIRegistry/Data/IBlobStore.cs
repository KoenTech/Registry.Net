namespace OCIRegistry.Data
{
    public interface IBlobStore
    {
        Task<Stream> GetAsync(string digest);
        Task PutAsync(string digest, Stream stream);
        Task DeleteAsync(string digest);
        Task<bool> ExistsAsync(string digest);
    }
}
