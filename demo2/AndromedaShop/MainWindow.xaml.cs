using System.Windows;
using System.Windows.Controls;

namespace AndromedaShop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeUser();
        }

        private void InitializeUser()
        {
            var user = App.CurrentUser;
            lblUser.Text = $"{user.FullName}  |  {user.RoleName}";

            // Клиент видит только свои заказы — скрываем кнопку Товары
            // Администратор и Менеджер имеют доступ ко всему
            bool isClient = user.RoleName.ToLower().Contains("клиент");
            btnProducts.Visibility = isClient ? Visibility.Collapsed : Visibility.Visible;

            // По умолчанию открываем страницу товаров (или заказов для клиента)
            if (isClient)
                NavigateOrders();
            else
                NavigateProducts();
        }

        private void BtnProducts_Click(object sender, RoutedEventArgs e) => NavigateProducts();
        private void BtnOrders_Click(object sender, RoutedEventArgs e)   => NavigateOrders();

        private void NavigateProducts() => MainFrame.Navigate(new ProductsPage());
        private void NavigateOrders()   => MainFrame.Navigate(new OrdersPage());

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            App.CurrentUser = null;
            var login = new LoginWindow();
            login.Show();
            Close();
        }
    }
}
