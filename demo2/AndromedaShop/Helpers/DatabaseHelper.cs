/*
 * ╔══════════════════════════════════════════════════════════════════════════╗
 * ║                        DatabaseHelper.cs                                ║
 * ║  Единственный файл, который работает с базой данных.                    ║
 * ║  Строка подключения берётся из App.config (секция connectionStrings).   ║
 * ║                                                                          ║
 * ║  ЧТОБЫ СМЕНИТЬ БАЗУ — отредактируйте только App.config:                ║
 * ║    Data Source    = имя вашего SQL Server                               ║
 * ║    Initial Catalog = название вашей базы данных                         ║
 * ╚══════════════════════════════════════════════════════════════════════════╝
 */

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using AndromedaShop.Models;

namespace AndromedaShop.Helpers
{
    public static class DatabaseHelper
    {
        // ─────────────────────────────────────────────────────────────────────
        // Строка подключения читается из App.config → connectionStrings → "AndromedaDB"
        // Менять здесь НИЧЕГО НЕ НУЖНО — всё настраивается в App.config
        // ─────────────────────────────────────────────────────────────────────
        private static readonly string ConnectionString =
            ConfigurationManager.ConnectionStrings["AndromedaDB"].ConnectionString;

        /// <summary>
        /// Открывает соединение с БД.
        /// При ошибке бросает исключение с понятным описанием причины.
        /// </summary>
        public static SqlConnection GetConnection()
        {
            try
            {
                var conn = new SqlConnection(ConnectionString);
                conn.Open();
                return conn;
            }
            catch (SqlException ex)
            {
                // Расшифровываем типичные коды ошибок SQL Server
                throw new Exception(GetFriendlyError(ex), ex);
            }
            catch (InvalidOperationException)
            {
                // Строка подключения пустая или отсутствует в App.config
                throw new Exception(
                    "Строка подключения не найдена в App.config.\n" +
                    "Убедитесь, что в файле App.config есть секция:\n" +
                    "<connectionStrings>\n" +
                    "  <add name=\"AndromedaDB\" connectionString=\"...\" />\n" +
                    "</connectionStrings>");
            }
        }

        /// <summary>
        /// Переводит код ошибки SQL Server в понятное сообщение на русском.
        /// </summary>
        private static string GetFriendlyError(SqlException ex)
        {
            switch (ex.Number)
            {
                // Сервер не найден / недоступен
                case -1:
                case 2:
                case 53:
                    return
                        "SQL Server не найден или недоступен.\n\n" +
                        "Проверьте:\n" +
                        "• Правильно ли указано имя сервера в App.config (Data Source=...)\n" +
                        "• Запущена ли служба SQL Server (services.msc → SQL Server)\n" +
                        "• Включены ли TCP/IP и Named Pipes в SQL Server Configuration Manager\n" +
                        "• Для локального сервера используйте: .\\SQLEXPRESS\n\n" +
                        $"Техническая ошибка: {ex.Message}";

                // База данных не существует
                case 4060:
                    return
                        "База данных не найдена.\n\n" +
                        "Проверьте:\n" +
                        "• Правильно ли указано название БД в App.config (Initial Catalog=...)\n" +
                        "• Существует ли эта база в SSMS (Object Explorer → Databases)\n" +
                        "• Выполните в SSMS: SELECT name FROM sys.databases\n\n" +
                        $"Техническая ошибка: {ex.Message}";

                // Ошибка авторизации (Windows)
                case 18456:
                    return
                        "Ошибка авторизации на SQL Server.\n\n" +
                        "Проверьте:\n" +
                        "• Если используется Windows-авторизация (Integrated Security=True) —\n" +
                        "  убедитесь, что текущий пользователь Windows имеет доступ к SQL Server\n" +
                        "• Если используется SQL-авторизация —\n" +
                        "  проверьте User ID и Password в App.config\n" +
                        "• В SSMS: Security → Logins — проверьте наличие вашего логина\n\n" +
                        $"Техническая ошибка: {ex.Message}";

                // Объект не найден (таблица / столбец)
                case 208:
                    return
                        "Таблица или объект не найден в базе данных.\n\n" +
                        "Проверьте:\n" +
                        "• Правильная ли база данных указана в App.config (Initial Catalog=...)\n" +
                        "• Совпадают ли названия таблиц со структурой вашей БД\n" +
                        "• Выполните в SSMS: SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES\n\n" +
                        $"Техническая ошибка: {ex.Message}";

                default:
                    return $"Ошибка базы данных (код {ex.Number}):\n{ex.Message}";
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // АВТОРИЗАЦИЯ
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Проверяет логин/пароль, возвращает User или null если не найден.
        /// Таблица: Пользователи$, Роли$
        /// </summary>
        public static User Authenticate(string login, string password)
        {
            const string sql = @"
                SELECT u.id, u.id_роли, r.Название AS Роль,
                       u.Фамилия, u.Имя, u.Отчество, u.Логин
                FROM [Пользователи$] u
                INNER JOIN [Роли$] r ON r.id = u.id_роли
                WHERE u.Логин = @login AND u.Пароль = @password";

            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@login",    login);
                cmd.Parameters.AddWithValue("@password", password);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new User
                        {
                            Id         = (int)reader["id"],
                            RoleId     = (int)reader["id_роли"],
                            RoleName   = reader["Роль"].ToString(),
                            LastName   = reader["Фамилия"].ToString(),
                            FirstName  = reader["Имя"].ToString(),
                            MiddleName = reader["Отчество"].ToString(),
                            Login      = reader["Логин"].ToString()
                        };
                    }
                }
            }
            return null; // пользователь не найден → неверный логин/пароль
        }

        // ═════════════════════════════════════════════════════════════════════
        // ТОВАРЫ — таблица [Товары$]
        // ═════════════════════════════════════════════════════════════════════

        /*
         * ════════════════════════════════════════════════════════════════════
         * КАК АДАПТИРОВАТЬ SQL-ЗАПРОСЫ ПОД СВОЮ БД
         *
         * [Товары$]          → замените на имя вашей таблицы товаров
         * [Категория$]       → замените или удалите если нет категорий
         * [Поставщик$]       → замените или удалите если нет поставщиков
         * [Производитель$]   → замените или удалите если нет производителей
         * [ЕдиницаИзмерения$]→ замените или удалите если нет единиц измерения
         *
         * t.Наименование → замените на название вашего текстового поля
         * t.Цена         → замените на название поля с ценой
         * t.ДействующаяСкидка → замените или удалите если нет скидки
         *
         * После изменения SQL — обязательно обновите reader["..."] ниже
         * и свойства модели в AppModels.cs
         *
         * ПРИМЕР для таблицы обуви [Обувь$]:
         *   SELECT t.id, t.Артикул, t.Модель, t.Стоимость, t.Размер, t.Цвет
         *   FROM [Обувь$] t
         *   WHERE (@name = '' OR t.Модель LIKE '%' + @name + '%')
         *   ORDER BY t.Стоимость {orderDir}
         * ════════════════════════════════════════════════════════════════════
         */
        /// <summary>
        /// Возвращает список товаров с поиском, фильтрацией по категории и сортировкой по цене.
        /// </summary>
        public static List<Product> GetProducts(string searchName = "", int categoryId = 0, string sortOrder = "asc")
        {
            var list = new List<Product>();
            string orderDir = sortOrder == "desc" ? "DESC" : "ASC";

            string sql = $@"
                SELECT t.id, t.Артикул, t.Наименование, t.Цена,
                       t.id_категории,     k.Название  AS Категория,
                       t.id_поставщика,    p.Название  AS Поставщик,
                       t.id_производителя, pr.Название AS Производитель,
                       t.id_единицы,       e.Название  AS Единица,
                       t.ОписаниеТовара, t.Фото, t.ДействующаяСкидка
                FROM [Товары$] t
                LEFT JOIN [Категория$]        k  ON k.id  = t.id_категории
                LEFT JOIN [Поставщик$]        p  ON p.id  = t.id_поставщика
                LEFT JOIN [Производитель$]    pr ON pr.id = t.id_производителя
                LEFT JOIN [ЕдиницаИзмерения$] e  ON e.id  = t.id_единицы
                WHERE (@name = '' OR t.Наименование LIKE '%' + @name + '%')
                  AND (@cat  = 0  OR t.id_категории = @cat)
                ORDER BY t.Цена {orderDir}";

            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@name", searchName ?? "");
                cmd.Parameters.AddWithValue("@cat",  categoryId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // reader["НазваниеПоля"] — название должно точно совпадать
                        // с именем столбца в SQL-запросе выше (или алиасом AS ...)
                        // Если добавили новое поле в SELECT — добавьте его и здесь
                        list.Add(new Product
                        {
                            Id               = (int)reader["id"],
                            Article          = reader["Артикул"].ToString(),
                            Name             = reader["Наименование"].ToString(),
                            Price            = (decimal)reader["Цена"],
                            Discount         = reader["ДействующаяСкидка"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ДействующаяСкидка"]),
                            CategoryId       = reader["id_категории"]     == DBNull.Value ? 0 : (int)reader["id_категории"],
                            CategoryName     = reader["Категория"].ToString(),
                            SupplierId       = reader["id_поставщика"]    == DBNull.Value ? 0 : (int)reader["id_поставщика"],
                            SupplierName     = reader["Поставщик"].ToString(),
                            ManufacturerId   = reader["id_производителя"] == DBNull.Value ? 0 : (int)reader["id_производителя"],
                            ManufacturerName = reader["Производитель"].ToString(),
                            UnitId           = reader["id_единицы"]       == DBNull.Value ? 0 : (int)reader["id_единицы"],
                            UnitName         = reader["Единица"].ToString(),
                            Description      = reader["ОписаниеТовара"].ToString(),
                            Photo            = reader["Фото"] == DBNull.Value ? null : (byte[])reader["Фото"]
                        });
                    }
                }
            }
            return list;
        }

        /// <summary>INSERT в [Товары$]</summary>
        public static void InsertProduct(Product p)
        {
            const string sql = @"
                INSERT INTO [Товары$]
                    (Артикул, Наименование, Цена, ДействующаяСкидка,
                     id_категории, id_поставщика, id_производителя, id_единицы, ОписаниеТовара, Фото)
                VALUES
                    (@art, @name, @price, @disc, @cat, @sup, @man, @unit, @desc, @photo)";

            using (var conn = GetConnection())
            using (var cmd  = new SqlCommand(sql, conn))
            {
                SetProductParams(cmd, p);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>UPDATE в [Товары$]</summary>
        public static void UpdateProduct(Product p)
        {
            const string sql = @"
                UPDATE [Товары$] SET
                    Артикул          = @art,
                    Наименование     = @name,
                    Цена             = @price,
                    ДействующаяСкидка = @disc,
                    id_категории     = @cat,
                    id_поставщика    = @sup,
                    id_производителя = @man,
                    id_единицы       = @unit,
                    ОписаниеТовара   = @desc,
                    Фото             = @photo
                WHERE id = @id";

            using (var conn = GetConnection())
            using (var cmd  = new SqlCommand(sql, conn))
            {
                SetProductParams(cmd, p);
                cmd.Parameters.AddWithValue("@id", p.Id);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>DELETE из [Товары$]</summary>
        public static void DeleteProduct(int id)
        {
            using (var conn = GetConnection())
            using (var cmd  = new SqlCommand("DELETE FROM [Товары$] WHERE id = @id", conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private static void SetProductParams(SqlCommand cmd, Product p)
        {
            cmd.Parameters.AddWithValue("@art",   (object)p.Article     ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@name",  p.Name);
            cmd.Parameters.AddWithValue("@price", p.Price);
            cmd.Parameters.AddWithValue("@disc",  p.Discount);
            cmd.Parameters.AddWithValue("@cat",   p.CategoryId     > 0 ? (object)p.CategoryId     : DBNull.Value);
            cmd.Parameters.AddWithValue("@sup",   p.SupplierId     > 0 ? (object)p.SupplierId     : DBNull.Value);
            cmd.Parameters.AddWithValue("@man",   p.ManufacturerId > 0 ? (object)p.ManufacturerId : DBNull.Value);
            cmd.Parameters.AddWithValue("@unit",  p.UnitId         > 0 ? (object)p.UnitId         : DBNull.Value);
            cmd.Parameters.AddWithValue("@desc",  (object)p.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@photo", (object)p.Photo       ?? DBNull.Value);
        }

        // ═════════════════════════════════════════════════════════════════════
        // ЗАКАЗЫ — таблица [Заказы$]
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Возвращает список заказов с фильтрацией по статусу, датам и клиенту.
        /// </summary>
        public static List<Order> GetOrders(int statusId = 0, DateTime? dateFrom = null, DateTime? dateTo = null, int clientId = 0)
        {
            var list = new List<Order>();

            const string sql = @"
                SELECT z.id, z.ДатаЗаказа, z.ДатаДоставки,
                       z.id_пвз,      pv.Адрес AS ПВЗ,
                       z.id_клиента,
                       u.Фамилия + ' ' + u.Имя + ' ' + ISNULL(u.Отчество,'') AS Клиент,
                       z.КодПолучения, z.id_статуса, s.Название AS Статус
                FROM [Заказы$] z
                LEFT JOIN [ПунктыВыдачи$]  pv ON pv.id = z.id_пвз
                LEFT JOIN [Пользователи$]  u  ON u.id  = z.id_клиента
                LEFT JOIN [СтатусЗаказа$]  s  ON s.id  = z.id_статуса
                WHERE (@status   = 0    OR z.id_статуса  = @status)
                  AND (@clientId = 0    OR z.id_клиента  = @clientId)
                  AND (@dateFrom IS NULL OR z.ДатаЗаказа >= @dateFrom)
                  AND (@dateTo   IS NULL OR z.ДатаЗаказа <= @dateTo)
                ORDER BY z.ДатаЗаказа DESC";

            using (var conn = GetConnection())
            using (var cmd  = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@status",   statusId);
                cmd.Parameters.AddWithValue("@clientId", clientId);
                cmd.Parameters.AddWithValue("@dateFrom", (object)dateFrom ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dateTo",   (object)dateTo   ?? DBNull.Value);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Order
                        {
                            Id                 = (int)reader["id"],
                            OrderDate          = (DateTime)reader["ДатаЗаказа"],
                            DeliveryDate       = reader["ДатаДоставки"] == DBNull.Value ? (DateTime?)null : (DateTime)reader["ДатаДоставки"],
                            PickupPointId      = reader["id_пвз"]      == DBNull.Value ? 0 : (int)reader["id_пвз"],
                            PickupPointAddress = reader["ПВЗ"].ToString(),
                            ClientId           = reader["id_клиента"]  == DBNull.Value ? 0 : (int)reader["id_клиента"],
                            ClientName         = reader["Клиент"].ToString().Trim(),
                            ReceiptCode        = reader["КодПолучения"].ToString(),
                            StatusId           = reader["id_статуса"]  == DBNull.Value ? 0 : (int)reader["id_статуса"],
                            StatusName         = reader["Статус"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        /// <summary>INSERT в [Заказы$], возвращает новый id</summary>
        public static int InsertOrder(Order o)
        {
            const string sql = @"
                INSERT INTO [Заказы$] (ДатаЗаказа, ДатаДоставки, id_пвз, id_клиента, КодПолучения, id_статуса)
                VALUES (@date, @deliv, @pvz, @client, @code, @status);
                SELECT SCOPE_IDENTITY();";

            using (var conn = GetConnection())
            using (var cmd  = new SqlCommand(sql, conn))
            {
                SetOrderParams(cmd, o);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        /// <summary>UPDATE в [Заказы$]</summary>
        public static void UpdateOrder(Order o)
        {
            const string sql = @"
                UPDATE [Заказы$] SET
                    ДатаЗаказа   = @date,
                    ДатаДоставки = @deliv,
                    id_пвз       = @pvz,
                    id_клиента   = @client,
                    КодПолучения = @code,
                    id_статуса   = @status
                WHERE id = @id";

            using (var conn = GetConnection())
            using (var cmd  = new SqlCommand(sql, conn))
            {
                SetOrderParams(cmd, o);
                cmd.Parameters.AddWithValue("@id", o.Id);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>DELETE из [Заказы$] и [СоставЗаказа$]</summary>
        public static void DeleteOrder(int id)
        {
            using (var conn = GetConnection())
            {
                // Сначала удаляем состав (дочерние записи)
                using (var cmd = new SqlCommand("DELETE FROM [СоставЗаказа$] WHERE id_заказа = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                // Затем удаляем сам заказ
                using (var cmd = new SqlCommand("DELETE FROM [Заказы$] WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void SetOrderParams(SqlCommand cmd, Order o)
        {
            cmd.Parameters.AddWithValue("@date",   o.OrderDate);
            cmd.Parameters.AddWithValue("@deliv",  (object)o.DeliveryDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pvz",    o.PickupPointId > 0 ? (object)o.PickupPointId : DBNull.Value);
            cmd.Parameters.AddWithValue("@client", o.ClientId      > 0 ? (object)o.ClientId      : DBNull.Value);
            cmd.Parameters.AddWithValue("@code",   (object)o.ReceiptCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@status", o.StatusId      > 0 ? (object)o.StatusId      : DBNull.Value);
        }

        // ═════════════════════════════════════════════════════════════════════
        // СПРАВОЧНИКИ
        // ═════════════════════════════════════════════════════════════════════

        public static List<LookupItem> GetCategories()    => GetLookup("SELECT id, Название FROM [Категория$]        ORDER BY Название");
        public static List<LookupItem> GetSuppliers()     => GetLookup("SELECT id, Название FROM [Поставщик$]        ORDER BY Название");
        public static List<LookupItem> GetManufacturers() => GetLookup("SELECT id, Название FROM [Производитель$]    ORDER BY Название");
        public static List<LookupItem> GetUnits()         => GetLookup("SELECT id, Название FROM [ЕдиницаИзмерения$] ORDER BY Название");
        public static List<LookupItem> GetStatuses()      => GetLookup("SELECT id, Название FROM [СтатусЗаказа$]     ORDER BY Название");
        public static List<LookupItem> GetPickupPoints()  => GetLookup("SELECT id, Адрес AS Название FROM [ПунктыВыдачи$] ORDER BY Адрес");

        public static List<LookupItem> GetClients()
        {
            const string sql = @"
                SELECT u.id,
                       u.Фамилия + ' ' + u.Имя + ' ' + ISNULL(u.Отчество,'') AS Название
                FROM [Пользователи$] u
                INNER JOIN [Роли$] r ON r.id = u.id_роли
                WHERE r.Название = 'Клиент' OR r.Название LIKE '%клиент%'
                ORDER BY u.Фамилия";
            return GetLookup(sql);
        }

        private static List<LookupItem> GetLookup(string sql)
        {
            var list = new List<LookupItem>();
            using (var conn = GetConnection())
            using (var cmd  = new SqlCommand(sql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    list.Add(new LookupItem
                    {
                        Id   = (int)reader["id"],
                        Name = reader["Название"].ToString().Trim()
                    });
            }
            return list;
        }
    }
}
