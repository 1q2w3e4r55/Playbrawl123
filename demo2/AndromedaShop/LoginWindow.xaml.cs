using System;
using System.Windows;
using System.Windows.Input;
using AndromedaShop.Helpers;

namespace AndromedaShop
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            txtLogin.Focus();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            TryLogin();
        }

        // Вход по нажатию Enter в полях ввода
        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                TryLogin();
        }

        private void TryLogin()
        {
            string login    = txtLogin.Text.Trim();
            string password = txtPassword.Password;

            // Базовая валидация
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ShowError("Введите логин и пароль.");
                return;
            }

            try
            {
                var user = DatabaseHelper.Authenticate(login, password);
                if (user == null)
                {
                    ShowError("Неверный логин или пароль.");
                    txtPassword.Clear();
                    return;
                }

                // Сохраняем пользователя в App
                App.CurrentUser = user;

                // Открываем главное окно
                var main = new MainWindow();
                main.Show();
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка подключения к БД:\n{ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            lblError.Text       = message;
            lblError.Visibility = Visibility.Visible;
        }
    }
}
