using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AndromedaShop.Helpers;
using AndromedaShop.Models;

namespace AndromedaShop
{
    /// <summary>
    /// Страница управления заказами
    /// </summary>
    public partial class OrdersPage : Page
    {
        private bool _initialized = false;

        public OrdersPage()
        {
            InitializeComponent();
            Loaded += OrdersPage_Loaded;
        }

        private void OrdersPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_initialized) return;
            _initialized = true;

            ConfigureButtonsByRole();
            LoadStatuses();
            LoadOrders();
        }

        // Кнопки CRUD — Администратор и Менеджер; Клиент только смотрит
        private void ConfigureButtonsByRole()
        {
            string role = App.CurrentUser?.RoleName.ToLower() ?? "";
            bool canEdit = role.Contains("админ") || role.Contains("менедж");
            var vis = canEdit ? Visibility.Visible : Visibility.Collapsed;
            btnAdd.Visibility    = vis;
            btnEdit.Visibility   = vis;
            btnDelete.Visibility = vis;
        }

        private void LoadStatuses()
        {
            try
            {
                var statuses = DatabaseHelper.GetStatuses();
                statuses.Insert(0, new LookupItem { Id = 0, Name = "Все статусы" });
                cbStatus.ItemsSource   = statuses;
                cbStatus.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статусов:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadOrders()
        {
            try
            {
                int statusId = (cbStatus.SelectedItem as LookupItem)?.Id ?? 0;
                DateTime? from = dpFrom.SelectedDate;
                DateTime? to   = dpTo.SelectedDate?.AddDays(1).AddSeconds(-1); // включаем весь день

                // Клиент видит только свои заказы
                string role = App.CurrentUser?.RoleName.ToLower() ?? "";
                int clientId = role.Contains("клиент") ? App.CurrentUser.Id : 0;

                var list = DatabaseHelper.GetOrders(statusId, from, to, clientId);
                dgOrders.ItemsSource = list.Select(o => new OrderViewModel(o)).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Filter_Changed(object sender, EventArgs e) => LoadOrders();

        private void DgOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool has = dgOrders.SelectedItem != null;
            btnEdit.IsEnabled   = has;
            btnDelete.IsEnabled = has;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddEditOrderWindow(null);
            if (win.ShowDialog() == true)
                LoadOrders();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var vm = dgOrders.SelectedItem as OrderViewModel;
            if (vm == null) return;

            var win = new AddEditOrderWindow(vm.Source);
            if (win.ShowDialog() == true)
                LoadOrders();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var vm = dgOrders.SelectedItem as OrderViewModel;
            if (vm == null) return;

            var result = MessageBox.Show(
                $"Удалить заказ №{vm.Id}?\nСостав заказа также будет удалён.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                DatabaseHelper.DeleteOrder(vm.Source.Id);
                LoadOrders();
                MessageBox.Show("Заказ удалён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления заказа:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>ViewModel для строки DataGrid заказов</summary>
    public class OrderViewModel
    {
        public Order Source { get; }

        public int    Id                   => Source.Id;
        public string OrderDateStr         => Source.OrderDate.ToString("dd.MM.yyyy");
        public string DeliveryDateStr      => Source.DeliveryDate?.ToString("dd.MM.yyyy") ?? "—";
        public string ClientName           => Source.ClientName;
        public string PickupPointAddress   => Source.PickupPointAddress;
        public string StatusName           => Source.StatusName;
        public string ReceiptCode          => Source.ReceiptCode;

        public OrderViewModel(Order order) => Source = order;
    }
}
