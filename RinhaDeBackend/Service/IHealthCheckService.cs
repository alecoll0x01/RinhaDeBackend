namespace RinhaDeBackend.Service
{
    public interface IHealthCheckService
    {
        Task<string> GetBestProcessorAsync();
    }

}
