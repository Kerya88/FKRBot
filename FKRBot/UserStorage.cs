using FKRBot.Entities;
using FKRBot.Services;

namespace FKRBot
{
    public static class UserStorage
    {
        private static EzhkhService _ezhkhService;

        public static Dictionary<long, User> InspectorStore { get; set; }

        public static async void UpdateState()
        {
            _ezhkhService = new EzhkhService();

            var users = await _ezhkhService.GetAllInspectors();

            if (users != null)
            {
                InspectorStore = users.ToDictionary(x => long.Parse(x.TelegramID), x => x);
            }
            else
            {
                throw new Exception("Произошла ошибка получения списка пользователей");
            }
        }
    }
}
