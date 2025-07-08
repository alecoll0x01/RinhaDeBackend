namespace RinhaDeBackend.Models
{
    public class ServiceHealthResponse
    {
        public bool Failing { get; set; }
        public int MinResponseTime { get; set; }
    }
}
