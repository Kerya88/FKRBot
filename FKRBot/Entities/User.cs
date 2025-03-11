using FKRBot.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FKRBot.Entities
{
    public class User
    {
        public string TelegramID { get; set; }
        public string FIO { get; set; }
        public UserActivityStateType UserActivityStateType { get; set; }
    }
}
