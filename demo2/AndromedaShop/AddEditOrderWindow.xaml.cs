using System;
using System.Windows;
using AndromedaShop.Helpers;
using AndromedaShop.Models;

namespace AndromedaShop
{
    /// <summary>
    /// Окно добавления и редактирования заказа
    /// </summary>
    public partial class AddEditOrderWindow : Window
    {
        private readonly Order _order; // null = режим добавления

        public AddEditOrderWindow(Order order)
        {
            InitializeComponent();
            _order = order;
            Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadComboBoxes();

            if (_order != null)
            {
                // Режим редактирования — заполняем поля
                lblTitle.Text          = "Редактирование заказа";
                Title                  = "Редактирование заказа";
                dpOrderDate.SelectedDate    = _order.OrderDate;
                dpDeliveryDate.SelectedDate = _order.DeliveryDate;
                txtCode.Text               = _order.ReceiptCode;

                SelectById(cbPickupPoint, _order.PickupPointId);
                SelectById(cbClient,      _order.ClientId);
                SelectById(cbStatus,      _order.StatusId);
            }
            else
            {
                // По умолчанию дата заказа = сегодня
                dpOrderDate.SelectedDate = DateTime.Today;
            }
        }

        private void LoadComboBoxes()
        {
            try
            {
                cbPickupPoint.ItemsSource = DatabaseHelper.GetPickupPoints();
                cbClient.ItemsSource      = DatabaseHelper.GetClients();
                cbStatus.ItemsSource      = DatabaseHelper.GetStatuses();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки справочников:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectById(System.Windows.Controls.ComboBox cb, int id)
        {
            foreach (LookupItem item in cb.Items)
            {
                if (item.Id == id) { cb.SelectedItem = item; return; }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;

            try
            {
                var o = new Order
                {
                    Id             = _order?.Id ?? 0,
                    OrderDate      = dpOrderDate.SelectedDate.Value,
                    DeliveryDate   = dpDeliveryDate.SelectedDate,
                    PickupPointId  = (cbPickupPoint.SelectedItem as LookupItem)?.Id ?? 0,
                    ClientId       = (cbClient.SelectedItem as LookupItem)?.Id ?? 0,
                    StatusId       = (cbStatus.SelectedItem as LookupItem)?.Id ?? 0,
                    ReceiptCode    = txtCode.Text.Trim()
                };

                if (_order == null)
                    DatabaseHelper.InsertOrder(o);
                else
                    DatabaseHelper.UpdateOrder(o);

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения заказа:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool Validate()
        {
            if (dpOrderDate.SelectedDate == null)
            {
                MessageBox.Show("Укажите дату заказа.", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (cbPickupPoint.SelectedItem == null)
            {
                MessageBox.Show("Выберите пункт выдачи.", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (cbClient.SelectedItem == null)
            {
                MessageBox.Show("Выберите клиента.", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (cbStatus.SelectedItem == null)
            {
                MessageBox.Show("Выберите статус заказа.", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
