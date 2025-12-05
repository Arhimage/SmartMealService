namespace SmsLib.Models
{
    public class ServerResponse<T>
    {
        public string Command { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public T Data { get; set; }
    }
}
