/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║                           AppModels.cs                                      ║
 * ║                                                                              ║
 * ║  Здесь описаны C#-модели (классы), которые соответствуют таблицам БД.       ║
 * ║                                                                              ║
 * ║  ЕСЛИ У ВАС ДРУГИЕ ПОЛЯ В БД — меняйте свойства классов здесь.             ║
 * ║                                                                              ║
 * ║  КАК АДАПТИРОВАТЬ ПОД СВОЮ БД:                                             ║
 * ║                                                                              ║
 * ║  Шаг 1. Найдите класс Product (товар) или Order (заказ).                    ║
 * ║  Шаг 2. Переименуйте / добавьте / удалите свойства под ваши поля.          ║
 * ║  Шаг 3. После изменения модели — также обновите:                            ║
 * ║    → DatabaseHelper.cs   (SQL-запросы и reader["НазваниеПоля"])             ║
 * ║    → ProductsPage.xaml   (колонки DataGrid: Binding="{Binding Свойство}")   ║
 * ║    → ProductsPage.xaml.cs (свойства ProductViewModel)                       ║
 * ║    → AddEditProductWindow.xaml / .xaml.cs (поля формы добавления)           ║
 * ║                                                                              ║
 * ║  ПРИМЕР — замена "Товары" на "Обувь":                                      ║
 * ║    Было:  public string Name { get; set; }      // Наименование             ║
 * ║           public int Discount { get; set; }     // Скидка                   ║
 * ║    Стало: public string Model { get; set; }     // Модель обуви             ║
 * ║           public int Size { get; set; }         // Размер                   ║
 * ║           public string Color { get; set; }     // Цвет                     ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

using System;

namespace AndromedaShop.Models
{
    // ─────────────────────────────────────────────────────────────────────────
    // ПОЛЬЗОВАТЕЛЬ — соответствует таблице [Пользователи$]
    // Эту модель обычно НЕ нужно менять — пользователи и роли есть в любой системе
    // ─────────────────────────────────────────────────────────────────────────
    public class User
    {
        public int    Id         { get; set; }
        public int    RoleId     { get; set; }
        public string RoleName   { get; set; }
        public string LastName   { get; set; }
        public string FirstName  { get; set; }
        public string MiddleName { get; set; }
        public string Login      { get; set; }
        public string FullName   => $"{LastName} {FirstName} {MiddleName}".Trim();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ТОВАР — соответствует таблице [Товары$]
    //
    // ЕСЛИ ВАША ТАБЛИЦА НАЗЫВАЕТСЯ ИНАЧЕ — меняйте в DatabaseHelper.cs:
    //   FROM [Товары$]  →  FROM [ВашаТаблица$]
    //
    // ЕСЛИ У ВАС ДРУГИЕ ПОЛЯ — меняйте свойства ниже И одновременно в:
    //   1. DatabaseHelper.cs  → reader["НазваниеПоляВБД"]
    //   2. ProductsPage.xaml  → Binding="{Binding НазваниеСвойства}"
    //   3. ProductsPage.xaml.cs → класс ProductViewModel (публичные свойства)
    //   4. AddEditProductWindow.xaml / .xaml.cs → поля формы редактирования
    //
    // ПРИМЕРЫ замены полей:
    //   Книги:  Name→Title, Discount→SalePercent, добавить Author, Genre, ISBN
    //   Обувь:  Name→Model, добавить Size, Color, Material
    //   Техника: Name→ModelName, добавить Brand, WarrantyMonths, PowerWatts
    // ─────────────────────────────────────────────────────────────────────────
    public class Product
    {
        public int     Id               { get; set; }

        // Артикул — уникальный код товара. Если в вашей БД нет артикула — уберите это свойство
        // и удалите его из DataGrid в ProductsPage.xaml и из SQL в DatabaseHelper.cs
        public string  Article          { get; set; }

        // Наименование товара. Переименуйте если нужно (например: Model, Title, ProductName)
        // При переименовании — поменяйте Binding в ProductsPage.xaml и reader в DatabaseHelper.cs
        public string  Name             { get; set; }

        // Цена. Это поле используется в PriceDisplay ниже — не удаляйте без замены
        public decimal Price            { get; set; }

        // Скидка в процентах (0-100). Если в вашей БД нет скидки:
        //   - удалите это свойство
        //   - удалите IsHighDiscount в ProductViewModel (ProductsPage.xaml.cs)
        //   - удалите DataTrigger в ProductsPage.xaml
        //   - удалите поле скидки из AddEditProductWindow
        public int     Discount         { get; set; }

        // ── Внешние ключи — связи с другими таблицами ──
        // Если у вас нет какой-то категории/поставщика — просто удалите пару Id+Name свойств
        // и убeрите соответствующий LEFT JOIN в SQL (DatabaseHelper.cs, метод GetProducts)

        public int     CategoryId       { get; set; }
        public string  CategoryName     { get; set; }   // читается через JOIN с [Категория$]

        public int     SupplierId       { get; set; }
        public string  SupplierName     { get; set; }   // читается через JOIN с [Поставщик$]

        public int     ManufacturerId   { get; set; }
        public string  ManufacturerName { get; set; }   // читается через JOIN с [Производитель$]

        public int     UnitId           { get; set; }
        public string  UnitName         { get; set; }   // читается через JOIN с [ЕдиницаИзмерения$]

        public string  Description      { get; set; }
        public byte[]  Photo            { get; set; }   // хранится как массив байт в БД (тип image/varbinary)

        // ── Вычисляемые свойства (не хранятся в БД, вычисляются в C#) ──
        // Если убрали Discount — замените формулу на просто Price
        public decimal FinalPrice   => Discount > 0 ? Price * (1 - Discount / 100m) : Price;
        public string  PriceDisplay => Discount > 0
            ? $"{FinalPrice:N2} руб. (-{Discount}%)"
            : $"{Price:N2} руб.";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ЗАКАЗ — соответствует таблице [Заказы$]
    //
    // ЕСЛИ У ВАС ДРУГИЕ ПОЛЯ В ЗАКАЗАХ — меняйте свойства здесь И в:
    //   1. DatabaseHelper.cs  → SQL запросы GetOrders, InsertOrder, UpdateOrder
    //   2. OrdersPage.xaml    → колонки DataGrid
    //   3. OrdersPage.xaml.cs → класс OrderViewModel
    //   4. AddEditOrderWindow.xaml / .xaml.cs → поля формы
    // ─────────────────────────────────────────────────────────────────────────
    public class Order
    {
        public int       Id                   { get; set; }
        public DateTime  OrderDate            { get; set; }
        public DateTime? DeliveryDate         { get; set; }   // nullable — может быть не заполнено
        public int       PickupPointId        { get; set; }
        public string    PickupPointAddress   { get; set; }   // читается через JOIN с [ПунктыВыдачи$]
        public int       ClientId             { get; set; }
        public string    ClientName           { get; set; }   // читается через JOIN с [Пользователи$]
        public string    ReceiptCode          { get; set; }
        public int       StatusId             { get; set; }
        public string    StatusName           { get; set; }   // читается через JOIN с [СтатусЗаказа$]
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ВСПОМОГАТЕЛЬНЫЙ КЛАСС для ComboBox (справочники)
    // Используется для: Категории, Поставщики, Производители, Единицы, Статусы, ПВЗ
    // Менять не нужно — универсальный для любых справочных таблиц
    // ─────────────────────────────────────────────────────────────────────────
    public class LookupItem
    {
        public int    Id   { get; set; }
        public string Name { get; set; }
        public override string ToString() => Name;
    }
}
