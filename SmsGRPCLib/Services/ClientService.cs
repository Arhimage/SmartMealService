using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Sms.Test;

namespace SmsGRPCLib.Services
{
    public class ClientService
    {
        private readonly SmsTestService.SmsTestServiceClient _client;

        public ClientService(string serverAddress)
        {
            var channel = GrpcChannel.ForAddress(serverAddress);
            _client = new SmsTestService.SmsTestServiceClient(channel);
        }

        public async Task<List<MenuItem>> GetMenuAsync(bool withPrice)
        {
            var response = await _client.GetMenuAsync(new BoolValue { Value = withPrice });
            if (!response.Success) throw new Exception(response.ErrorMessage);
            return response.MenuItems.ToList();
        }

        public async Task<bool> SendOrderAsync(Order order)
        {
            var response = await _client.SendOrderAsync(order);
            if (!response.Success) throw new Exception(response.ErrorMessage);
            return true;
        }
    }
}
