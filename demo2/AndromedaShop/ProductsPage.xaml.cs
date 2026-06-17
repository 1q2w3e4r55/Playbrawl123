using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using AndromedaShop.Helpers;
using AndromedaShop.Models;

namespace AndromedaShop
{
    /// <summary>
    /// Страница управления товарами
    /// </summary>
    public partial class ProductsPage : Page
    {
        private List<ProductViewModel> _products;
        private string _sortOrder = "asc";
        private bool _initialized = false;

        public ProductsPage()
        {
            InitializeComponent();
            Loaded += ProductsPage_Loaded;
        }

        private void ProductsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_initialized) return;
            _initialized = true;

            ConfigureButtonsByRole();
            LoadCategories();
            LoadProducts();
        }

        // Показываем кнопки CRUD только Администратору
        private void ConfigureButtonsByRole()
        {
            bool isAdmin = App.CurrentUser?.RoleName.ToLower().Contains("админ") == true;
            var vis = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            btnAdd.Visibility    = vis;
            btnEdit.Visibility   = vis;
            btnDelete.Visibility = vis;
        }

        private void LoadCategories()
        {
            try
            {
                var cats = DatabaseHelper.GetCategories();
                cats.Insert(0, new LookupItem { Id = 0, Name = "Все категории" });
                cbCategory.ItemsSource   = cats;
                cbCategory.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProducts()
        {
            try
            {
                string search = txtSearch.Text.Trim();
                int catId = (cbCategory.SelectedItem as LookupItem)?.Id ?? 0;

                var list = DatabaseHelper.GetProducts(search, catId, _sortOrder);
                _products = list.Select(p => new ProductViewModel(p)).ToList();
                dgProducts.ItemsSource = _products;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)   => LoadProducts();
        private void CbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadProducts();

        private void CbSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _sortOrder = (cbSort.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "asc";
            LoadProducts();
        }

        private void DgProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = dgProducts.SelectedItem != null;
            btnEdit.IsEnabled   = hasSelection;
            btnDelete.IsEnabled = hasSelection;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddEditProductWindow(null);
            if (win.ShowDialog() == true)
                LoadProducts();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var vm = dgProducts.SelectedItem as ProductViewModel;
            if (vm == null) return;

            var win = new AddEditProductWindow(vm.Source);
            if (win.ShowDialog() == true)
                LoadProducts();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var vm = dgProducts.SelectedItem as ProductViewModel;
            if (vm == null) return;

            var result = MessageBox.Show(
                $"Удалить товар «{vm.Name}»?\nЭто действие нельзя отменить.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                DatabaseHelper.DeleteProduct(vm.Source.Id);
                LoadProducts();
                MessageBox.Show("Товар удалён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления товара:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /*
     * ════════════════════════════════════════════════════════════════════════
     * ProductViewModel — ПОСРЕДНИК между моделью (Product) и таблицей (DataGrid)
     *
     * Каждое публичное свойство здесь = одна колонка в ProductsPage.xaml
     * Binding="{Binding Article}" в XAML ищет свойство Article именно здесь.
     *
     * ЕСЛИ ДОБАВИЛИ НОВОЕ ПОЛЕ В МОДЕЛЬ (AppModels.cs) —
     * добавьте здесь соответствующее свойство:
     *   public string Color => Source.Color;
     *
     * ЕСЛИ УБРАЛИ ПОЛЕ ИЗ МОДЕЛИ —
     * удалите соответствующее свойство здесь и колонку в ProductsPage.xaml
     *
     * ЕСЛИ НУЖНА ДРУГАЯ ПОДСВЕТКА СТРОК —
     * измените условие в IsHighDiscount (сейчас: скидка > 25%)
     * и цвет в ProductsPage.xaml в DataTrigger (сейчас: #23E1EF)
     * ════════════════════════════════════════════════════════════════════════
     */
    public class ProductViewModel
    {
        public Product Source { get; }

        // ── Свойства для колонок DataGrid ──
        // Название свойства здесь ДОЛЖНО совпадать с Binding в ProductsPage.xaml
        public string  Article          => Source.Article;
        public string  Name             => Source.Name;
        public decimal Price            => Source.Price;
        public int     Discount         => Source.Discount;
        public string  CategoryName     => Source.CategoryName;
        public string  ManufacturerName => Source.ManufacturerName;
        public string  PriceDisplay     => Source.PriceDisplay;

        // Подсветка строки цветом #23E1EF если скидка > 25% (требование задания)
        // Чтобы изменить условие — поменяйте "25" на нужное значение
        // Чтобы убрать подсветку — удалите это свойство и DataTrigger в ProductsPage.xaml
        public bool IsHighDiscount => Source.Discount > 25;

        public BitmapImage PhotoSource { get; }

        public ProductViewModel(Product product)
        {
            Source = product;

            if (product.Photo != null && product.Photo.Length > 0)
            {
                try
                {
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.StreamSource     = new MemoryStream(product.Photo);
                    img.CacheOption      = BitmapCacheOption.OnLoad;
                    img.DecodePixelWidth = 100;
                    img.EndInit();
                    img.Freeze();
                    PhotoSource = img;
                }
                catch { /* некорректные данные изображения — оставляем null */ }
            }
        }
    }
}
