using FKRBot.Entities;
using System.Security.Cryptography;
using System.Text;
using TestService;

namespace FKRBot.Services
{
    public class EzhkhService
    {
        private readonly FKRService.ServiceClient _serviceClient = new(FKRService.ServiceClient.EndpointConfiguration.BasicHttpBinding_IService);
        private readonly TestService.ServiceClient _testServiceClient = new(TestService.ServiceClient.EndpointConfiguration.BasicHttpBinding_IService);

        public async Task<bool> RegisterTGInspector(User inspector)
        {
            try
            {
                var token = ComputeHash();

                var responce = await _testServiceClient.RegisterTGInspectorAsync(inspector.TelegramID, inspector.FIO, token);

                switch (responce.Result.Code)
                {
                    case "00":
                        {
                            return true;
                        }
                    default:
                        {
                            return false;
                        }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<User>?> GetAllInspectors()
        {
            try
            {
                var token = ComputeHash();

                var responce = await _testServiceClient.GetAllInspectorsAsync(token);

                switch (responce.Result.Code)
                {
                    case "00":
                        {
                            return responce.TGInspectors.Select(x => new User
                            {
                                TelegramID = x.TelegramId,
                                FIO = x.FIO,
                                UserActivityStateType = Enums.UserActivityStateType.NotSet

                            })
                            .ToList();
                        }
                    default:
                        {
                            return null;
                        }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<MunicipalityWithCount[]?> GetMunicipalities(string userId)
        {
            try
            {
                var token = ComputeHash();

                var responce = await _testServiceClient.GetMunicipalitiesAsync(token, userId);

                switch (responce.Result.Code)
                {
                    case "00":
                        {
                            return responce.MunicipalityWithCounts;
                        }
                    default:
                        {
                            return null;
                        }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string ComputeHash()
        {
            var bytes = Encoding.UTF8.GetBytes($"{DateTime.Now:dd.MM.yyyy}_ANV_6966644");
            var byteHash = MD5.HashData(bytes);

            return Convert.ToBase64String(byteHash);
        }
    }
}
