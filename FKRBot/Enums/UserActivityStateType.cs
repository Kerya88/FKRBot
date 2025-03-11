using System.ComponentModel.DataAnnotations;

namespace FKRBot.Enums
{
    public enum UserActivityStateType
    {
        [Display(Name = "Не указано")]
        NotSet = 0,

        [Display(Name = "ФИО")]
        FIO = 1,

        [Display(Name = "Муниципальное образование")]
        MO = 2,

        [Display(Name = "Тип")]
        Type = 3,

        [Display(Name = "Город")]
        City = 4,

        [Display(Name = "Назначение мастера")]
        AssignMaster = 5,

        [Display(Name = "Сумма сделки")]
        Sum = 6,

        [Display(Name = "Отчет")]
        Report = 7
    }
}
