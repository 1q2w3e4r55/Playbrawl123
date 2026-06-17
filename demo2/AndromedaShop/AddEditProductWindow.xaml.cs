using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using AndromedaShop.Helpers;
using AndromedaShop.Models;

namespace AndromedaShop
{
    /// <summary>
    /// Окно добавления и редактирования товара
    /// </summary>
    public partial class AddEditProductWindow : Window
    {
        private readonly Product _product; // null = режим добавления
        private byte[] _photoBytes;

        public AddEditProductWindow(Product product)
        {
            InitializeComponent();
            _product = product;
            Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadComboBoxes();

            if (_product != null)
            {
                // Режим редактирования — заполняем поля
                lblTitle.Text      = "Редактирование товара";
                Title              = "Редактирование товара";
                txtName.Text       = _product.Name;
                txtArticle.Text    = _product.Article;
                txtDescription.Text = _product.Description;
                txtPrice.Text      = _product.Price.ToString("N2");
                txtDiscount.Text   = _product.Discount.ToString();

                SelectById(cbCategory,     _product.CategoryId);
                SelectById(cbManufacturer, _product.ManufacturerId);
                SelectById(cbSupplier,     _product.SupplierId);
                SelectById(cbUnit,         _product.UnitId);

                _photoBytes = _product.Photo;
                ShowPhotoPreview(_photoBytes);
            }
        }

        private void LoadComboBoxes()
        {
            try
            {
                cbCategory.ItemsSource     = DatabaseHelper.GetCategories();
                cbManufacturer.ItemsSource = DatabaseHelper.GetManufacturers();
                cbSupplier.ItemsSource     = DatabaseHelper.GetSuppliers();
                cbUnit.ItemsSource         = DatabaseHelper.GetUnits();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки справочников:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Выбирает элемент в ComboBox по id
        private void SelectById(System.Windows.Controls.ComboBox cb, int id)
        {
            foreach (LookupItem item in cb.Items)
            {
                if (item.Id == id) { cb.SelectedItem = item; return; }
            }
        }

        private void BtnChooseImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title  = "Выберите изображение",
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                _photoBytes = File.ReadAllBytes(dlg.FileName);
                ShowPhotoPreview(_photoBytes);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки изображения:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowPhotoPreview(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return;
            try
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.StreamSource     = new MemoryStream(bytes);
                img.CacheOption      = BitmapCacheOption.OnLoad;
                img.DecodePixelWidth = 200;
                img.EndInit();
                img.Freeze();
                imgPreview.Source = img;
            }
            catch { imgPreview.Source = null; }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;

            try
            {
                var p = new Product
                {
                    Id               = _product?.Id ?? 0,
                    Name             = txtName.Text.Trim(),
                    Article          = txtArticle.Text.Trim(),
                    Description      = txtDescription.Text.Trim(),
                    Price            = decimal.Parse(txtPrice.Text.Trim()),
                    Discount         = int.Parse(txtDiscount.Text.Trim()),
                    CategoryId       = (cbCategory.SelectedItem as LookupItem)?.Id ?? 0,
                    ManufacturerId   = (cbManufacturer.SelectedItem as LookupItem)?.Id ?? 0,
                    SupplierId       = (cbSupplier.SelectedItem as LookupItem)?.Id ?? 0,
                    UnitId           = (cbUnit.SelectedItem as LookupItem)?.Id ?? 0,
                    Photo            = _photoBytes
                };

                if (_product == null)
                    DatabaseHelper.InsertProduct(p);
                else
                    DatabaseHelper.UpdateProduct(p);

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения товара:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool Validate()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Укажите наименование товара.", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus();
                return false;
            }

            if (!decimal.TryParse(txtPrice.Text.Trim(), out decimal price) || price <= 0)
            {
                MessageBox.Show("Цена должна быть числом больше 0.", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPrice.Focus();
                return false;
            }

            if (!int.TryParse(txtDiscount.Text.Trim(), out int discount) || discount < 0 || discount > 100)
            {
                MessageBox.Show("Скидка должна быть целым числом от 0 до 100.", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDiscount.Focus();
                return false;
            }

            if (cbCategory.SelectedItem == null)
            {
                MessageBox.Show("Выберите категорию товара.", "Ошибка валидации",
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
