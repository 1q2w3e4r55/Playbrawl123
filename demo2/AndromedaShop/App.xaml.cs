using System.Windows;
using AndromedaShop.Models;

namespace AndromedaShop
{
    public partial class App : Application
    {
        // Текущий авторизованный пользователь (доступен из любого окна)
        public static User CurrentUser { get; set; }
    }
}
