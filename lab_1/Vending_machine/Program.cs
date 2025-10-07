using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VendingMachineApp
{
    public class Product
    {
        public string Name;
        public decimal Price;
        public string Category;

        public Product(string name, decimal price, string category)
        {
            Name = name;
            Price = price;
            Category = category;
        }

        public virtual string GetDescription()
        {
            return $"{Name} - {Price:C}";
        }

        // По умолчанию не просрочен
        public virtual bool IsExpired()
        {
            return false;
        }
    }

    // Класс напитков
    public class Beverage : Product
    {
        public int Volume;
        public DateTime ExpiryDate;

        public Beverage(string name, decimal price, int volume, DateTime expiryDate)
            : base(name, price, "Напитки")
        {
            Volume = volume;
            ExpiryDate = expiryDate;
        }

        public override string GetDescription()
        {
            return $"{Name} ({Volume}мл) - {Price:C}";
        }

        public override bool IsExpired()
        {
            return DateTime.Now > ExpiryDate;
        }
    }

    // Класс снеков
    public class Snack : Product
    {
        public int Weight;

        public Snack(string name, decimal price, int weight)
            : base(name, price, "Снеки")
        {
            Weight = weight;
        }

        public override string GetDescription()
        {
            return $"{Name} ({Weight}г) - {Price:C}";
        }
    }

    public class ProductSlot
    {
        public Product Product;
        public int Quantity;
        public int SlotNumber;

        public ProductSlot(Product product, int quantity, int slotNumber)
        {
            Product = product;
            Quantity = quantity;
            SlotNumber = slotNumber;
        }

        public bool TryDispense()
        {
            if (Quantity > 0 && !Product.IsExpired())
            {
                Quantity--;
                return true;
            }
            return false;
        }

        public void Refill(int amount)
        {
            Quantity += amount;
        }
    }

    // Интерфейс оплаты
    public interface IPaymentSystem
    {
        bool AcceptCoin(decimal coinValue);
        decimal GetCurrentBalance();
        Dictionary<decimal, int> CalculateChange(decimal changeAmount);
        void ClearBalance();
        bool HasSufficientChange(decimal amount);
    }

    // Менеджера монет
    public class CoinManager : IPaymentSystem
    {
        public Dictionary<decimal, int> Coins;
        public List<decimal> AcceptedDenominations = new() { 0.10m, 0.50m, 1m, 2m, 5m, 10m };
        private decimal _currentBalance;

        public CoinManager()
        {
            Coins = new Dictionary<decimal, int>
            {
                { 0.10m, 50 }, { 0.50m, 20 }, { 1m, 20 },
                { 2m, 10 }, { 5m, 5 }, { 10m, 2 }
            };
            _currentBalance = 0;
        }

        public bool AcceptCoin(decimal coinValue)
        {
            if (AcceptedDenominations.Contains(coinValue))
            {
                _currentBalance += coinValue;
                if (Coins.ContainsKey(coinValue))
                    Coins[coinValue]++;
                else
                    Coins[coinValue] = 1;
                return true;
            }
            return false;
        }

        public decimal GetCurrentBalance()
        {
            return _currentBalance;
        }

        public Dictionary<decimal, int> CalculateChange(decimal changeAmount)
        {
            var temp = new Dictionary<decimal, int>(Coins);
            var result = new Dictionary<decimal, int>();
            decimal remaining = Math.Round(changeAmount, 2);

            foreach (var denom in AcceptedDenominations.OrderByDescending(x => x))
            {
                if (remaining >= denom && temp.ContainsKey(denom) && temp[denom] > 0)
                {
                    int need = (int)(remaining / denom);
                    int take = Math.Min(need, temp[denom]);
                    if (take > 0)
                    {
                        result[denom] = take;
                        remaining -= take * denom;
                        remaining = Math.Round(remaining, 2);
                        temp[denom] -= take;
                    }
                }
            }

            if (remaining == 0)
            {
                foreach (var kv in result)
                {
                    Coins[kv.Key] -= kv.Value;
                }
                return result;
            }

            return null;
        }

        public void ClearBalance()
        {
            _currentBalance = 0;
        }

        public bool HasSufficientChange(decimal amount)
        {
            var change = CalculateChange(amount);
            if (change != null)
            {
                foreach (var kv in change)
                {
                    if (Coins.ContainsKey(kv.Key))
                        Coins[kv.Key] += kv.Value;
                    else
                        Coins[kv.Key] = kv.Value;
                }
                return true;
            }
            return false;
        }

        public Dictionary<decimal, int> GetCoinInventory()
        {
            return new Dictionary<decimal, int>(Coins);
        }

        public void CollectEarnings()
        {
            Console.WriteLine("Собранные средства:");
            decimal total = 0;
            foreach (var coin in Coins)
            {
                var value = coin.Key * coin.Value;
                total += value;
                Console.WriteLine($"{coin.Key:C} x {coin.Value} = {value:C}");
            }
            Console.WriteLine($"Общая сумма: {total:C}");
        }
    }

    // Основной класс автомата
    public class VendingMachine
    {
        private readonly List<ProductSlot> _productSlots;
        private readonly IPaymentSystem _paymentSystem;
        private string _adminPassword = "admin123";

        public VendingMachine()
        {
            _productSlots = new List<ProductSlot>();
            _paymentSystem = new CoinManager();
            InitializeProducts();
        }

        private void InitializeProducts()
        {
            _productSlots.Add(new ProductSlot(new Beverage("Кока-Кола", 2.50m, 330, DateTime.Now.AddMonths(6)), 10, 1));
            _productSlots.Add(new ProductSlot(new Beverage("Вода", 1.00m, 500, DateTime.Now.AddYears(1)), 15, 2));
            _productSlots.Add(new ProductSlot(new Snack("Чипсы", 3.00m, 150), 8, 3));
            _productSlots.Add(new ProductSlot(new Snack("Шоколадка", 2.00m, 50), 12, 4));
        }

        public async Task RunAsync()
        {
            Console.WriteLine("Добро пожаловать в вендинговый автомат!");
            Console.WriteLine("==========================================");

            while (true)
            {
                await DisplayMainMenuAsync();
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        DisplayProducts();
                        break;
                    case "2":
                        await HandleCoinInsertionAsync();
                        break;
                    case "3":
                        await PurchaseProductAsync();
                        break;
                    case "4":
                        ReturnMoney();
                        break;
                    case "5":
                        await AdminModeAsync();
                        break;
                    case "0":
                        Console.WriteLine("До свидания!");
                        return;
                    default:
                        Console.WriteLine("Неверный выбор. Попробуйте снова.");
                        break;
                }

                Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                Console.ReadKey();
                Console.Clear();
            }
        }

        private async Task DisplayMainMenuAsync()
        {
            await Task.Delay(50);
            Console.WriteLine($"Текущий баланс: {_paymentSystem.GetCurrentBalance():C}");
            Console.WriteLine("\n--- ГЛАВНОЕ МЕНЮ ---");
            Console.WriteLine("1. Посмотреть товары");
            Console.WriteLine("2. Вставить монету");
            Console.WriteLine("3. Купить товар");
            Console.WriteLine("4. Вернуть деньги");
            Console.WriteLine("5. Админ-режим");
            Console.WriteLine("0. Выход");
            Console.Write("Выберите действие: ");
        }

        private void DisplayProducts()
        {
            Console.WriteLine("\n--- ДОСТУПНЫЕ ТОВАРЫ ---");
            var grouped = _productSlots.GroupBy(s => s.Product.Category);
            foreach (var grp in grouped)
            {
                Console.WriteLine("\n" + grp.Key + ":");
                foreach (var slot in grp)
                {
                    var status = slot.Quantity > 0 ? $"({slot.Quantity} шт.)" : "(Нет в наличии)";
                    var expired = slot.Product.IsExpired() ? " [ПРОСРОЧЕН]" : "";
                    Console.WriteLine($"  {slot.SlotNumber}. {slot.Product.GetDescription()} {status}{expired}");
                }
            }
        }

        private async Task HandleCoinInsertionAsync()
        {
            Console.WriteLine("\n--- ВСТАВКА МОНЕТЫ ---");
            Console.WriteLine("Принимаемые номиналы: 0.10, 0.50, 1.00, 2.00, 5.00, 10.00");
            Console.Write("Введите номинал монеты: ");
            if (decimal.TryParse(Console.ReadLine(), out decimal coinValue))
            {
                Console.WriteLine("Обработка монеты...");
                await Task.Delay(300);
                if (_paymentSystem.AcceptCoin(coinValue))
                {
                    Console.WriteLine($"Монета {coinValue:C} принята!");
                    Console.WriteLine($"Текущий баланс: {_paymentSystem.GetCurrentBalance():C}");
                }
                else
                {
                    Console.WriteLine("Монета не принята. Неподдерживаемый номинал.");
                }
            }
            else
            {
                Console.WriteLine("Некорректное значение.");
            }
        }

        private async Task PurchaseProductAsync()
        {
            if (_paymentSystem.GetCurrentBalance() == 0)
            {
                Console.WriteLine("Сначала вставьте монеты!");
                return;
            }

            DisplayProducts();
            Console.Write("\nВведите номер товара: ");
            if (int.TryParse(Console.ReadLine(), out int slotNumber))
            {
                var slot = _productSlots.FirstOrDefault(s => s.SlotNumber == slotNumber);
                if (slot == null)
                {
                    Console.WriteLine("Товар с таким номером не найден.");
                    return;
                }

                if (slot.Product.IsExpired())
                {
                    Console.WriteLine("Товар просрочен и недоступен для покупки.");
                    return;
                }

                if (slot.Quantity == 0)
                {
                    Console.WriteLine("Товар закончился.");
                    return;
                }

                if (_paymentSystem.GetCurrentBalance() < slot.Product.Price)
                {
                    var needed = slot.Product.Price - _paymentSystem.GetCurrentBalance();
                    Console.WriteLine($"Недостаточно средств. Нужно ещё: {needed:C}");
                    return;
                }

                var changeAmount = _paymentSystem.GetCurrentBalance() - slot.Product.Price;
                if (changeAmount > 0 && !_paymentSystem.HasSufficientChange(changeAmount))
                {
                    Console.WriteLine("Не могу выдать сдачу. Попробуйте другой товар или вставьте точную сумму.");
                    return;
                }

                Console.WriteLine("Обработка покупки...");
                await Task.Delay(700);

                if (slot.TryDispense())
                {
                    Console.WriteLine($"Вы купили: {slot.Product.GetDescription()}");

                    if (changeAmount > 0)
                    {
                        var change = _paymentSystem.CalculateChange(changeAmount);
                        if (change != null)
                        {
                            Console.WriteLine("Ваша сдача:");
                            foreach (var c in change)
                            {
                                Console.WriteLine($"{c.Key:C} x {c.Value}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Не удалось выдать сдачу, обратитесь к администратору.");
                        }
                    }

                    _paymentSystem.ClearBalance();
                    Console.WriteLine("Спасибо за покупку!");
                }
                else
                {
                    Console.WriteLine("Ошибка при выдаче товара.");
                }
            }
            else
            {
                Console.WriteLine("Некорректный номер товара.");
            }
        }

        private void ReturnMoney()
        {
            var balance = _paymentSystem.GetCurrentBalance();
            if (balance == 0)
            {
                Console.WriteLine("Нет денег для возврата.");
                return;
            }

            var change = _paymentSystem.CalculateChange(balance);
            if (change != null)
            {
                Console.WriteLine("Возвращаем ваши деньги:");
                foreach (var coin in change)
                {
                    Console.WriteLine($"{coin.Key:C} x {coin.Value}");
                }
                _paymentSystem.ClearBalance();
            }
            else
            {
                Console.WriteLine("Не могу выдать сдачу в данный момент.");
            }
        }

        private async Task AdminModeAsync()
        {
            Console.Write("Введите пароль администратора: ");
            if (Console.ReadLine() != _adminPassword)
            {
                Console.WriteLine("Неверный пароль!");
                await Task.Delay(300);
                return;
            }

            while (true)
            {
                Console.WriteLine("\n--- АДМИН-РЕЖИМ ---");
                Console.WriteLine("1. Пополнить товары");
                Console.WriteLine("2. Добавить новый товар");
                Console.WriteLine("3. Собрать деньги");
                Console.WriteLine("4. Статистика автомата");
                Console.WriteLine("0. Выход из админ-режима");
                Console.Write("Выберите действие: ");

                var sel = Console.ReadLine();
                if (sel == "1")
                {
                    await RefillProductsAsync();
                }
                else if (sel == "2")
                {
                    await AddNewProductAsync();
                }
                else if (sel == "3")
                {
                    ((CoinManager)_paymentSystem).CollectEarnings();
                }
                else if (sel == "4")
                {
                    DisplayStatistics();
                }
                else if (sel == "0")
                {
                    return;
                }

                Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                Console.ReadKey();
            }
        }

        private async Task RefillProductsAsync()
        {
            DisplayProducts();
            Console.Write("Введите номер товара для пополнения: ");
            if (int.TryParse(Console.ReadLine(), out int slotNumber))
            {
                var slot = _productSlots.FirstOrDefault(s => s.SlotNumber == slotNumber);
                if (slot != null)
                {
                    Console.Write("Количество для добавления: ");
                    if (int.TryParse(Console.ReadLine(), out int amount) && amount > 0)
                    {
                        Console.WriteLine("Пополнение товара...");
                        await Task.Delay(300);
                        slot.Refill(amount);
                        Console.WriteLine($"Товар пополнен. Новое количество: {slot.Quantity}");
                    }
                }
                else
                {
                    Console.WriteLine("Товар не найден.");
                }
            }
        }

        private async Task AddNewProductAsync()
        {
            Console.WriteLine("Добавление нового товара:");
            Console.WriteLine("1. Напиток");
            Console.WriteLine("2. Снек");
            Console.Write("Выберите тип: ");
            var type = Console.ReadLine();
            Console.Write("Название: ");
            var name = Console.ReadLine();
            Console.Write("Цена: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal price))
            {
                Console.WriteLine("Некорректная цена.");
                return;
            }
            Console.Write("Количество: ");
            if (!int.TryParse(Console.ReadLine(), out int quantity))
            {
                Console.WriteLine("Некорректное количество.");
                return;
            }

            Console.WriteLine("Добавление товара...");
            await Task.Delay(300);

            Product newProduct;
            if (type == "1")
            {
                newProduct = CreateBeverage(name, price);
            }
            else if (type == "2")
            {
                newProduct = new Snack(name, price, 100);
            }
            else
            {
                newProduct = null;
            }

            if (newProduct != null)
            {
                int newSlot = _productSlots.Max(s => s.SlotNumber) + 1;
                _productSlots.Add(new ProductSlot(newProduct, quantity, newSlot));
                Console.WriteLine($"Товар добавлен в слот {newSlot}");
            }
            else
            {
                Console.WriteLine("Неверный тип товара.");
            }
        }

        private Beverage CreateBeverage(string name, decimal price)
        {
            Console.Write("Объём (мл): ");
            int.TryParse(Console.ReadLine(), out int volume);
            // Не выставляем явно дату в прошлом — просто даём 6 месяцев
            return new Beverage(name, price, volume > 0 ? volume : 330, DateTime.Now.AddMonths(6));
        }

        private void DisplayStatistics()
        {
            Console.WriteLine("\n--- СТАТИСТИКА АВТОМАТА ---");
            Console.WriteLine($"Всего товаров: {_productSlots.Sum(s => s.Quantity)}");
            Console.WriteLine($"Товаров в ассортименте: {_productSlots.Count}");

            var emptySlots = _productSlots.Count(s => s.Quantity == 0);
            Console.WriteLine($"Пустых слотов: {emptySlots}");

            var expiredProducts = _productSlots.Count(s => s.Product.IsExpired());
            Console.WriteLine($"Просроченных товаров: {expiredProducts}");
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var vm = new VendingMachine();
                await vm.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
                Console.WriteLine("Нажмите любую клавишу для выхода...");
                Console.ReadKey();
            }
        }
    }
}
