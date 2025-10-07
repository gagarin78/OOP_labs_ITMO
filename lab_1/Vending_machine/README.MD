# Лабораторная работа №1
В данной лабораторной работе нам было необходимо написать консольную симуляцию вендингового автомата. Функционал автомат должен выглядеть примерно так:
- просмотр списка доступных товаров с их ценами и количеством
- вставка монет разных номиналов
- выбор товара и его получение, если внесенной суммы хватает
- получение сдачи (если необходима) и возвращение неиспользованных монет при отмене операции
- администраторский режим для пополнения ассортимента и сбора собранных средств`

В данной лабораторной работе нам было необходимо написать консольную симуляцию вендингового автомата. Функционал автомат должен выглядеть примерно так:
- просмотр списка доступных товаров с их ценами и количеством
- вставка монет разных номиналов
- выбор товара и его получение, если внесенной суммы хватает
- получение сдачи (если необходима) и возвращение неиспользованных монет при отмене операции
- администраторский режим для пополнения ассортимента и сбор собранных средств

Попробую объяснить по частям/блокам, как работает данная программа

## Класс Product

```C#
public string Name;
public decimal Price;
public string Category;
```

Тут мы объявляем публичные поля для класса Product

```C#
public virtual string GetDescription()
{
    return $"{Name} - {Price:C}";
}
```

GetDescription возвращает в терминал строку с названием и ценой

```C#
public virtual bool IsExpired()
{
    return false;
}
```

Здесь реализовал, чтобы товар по умолчанию был не просрочен (то есть значение false)

## Напиток(Beverage)
```C#
public DateTime ExpiryDate;
...
public override bool IsExpired()
{
    return DateTime.Now > ExpiryDate;
}
```

ExpiryDate хранит дату, до которой напиток годен, а метод IsExpired сравнивает текущую дату с ExpiryDate

```C#
public override string GetDescription()
{
    return $"{Name} ({Volume}мл) - {Price:C}";
}
```

К описанию напитка (товара) добивл объем

## Слот для товара(ProductSlot)
```C#
public Product Product;
public int Quantity;
public int SlotNumber;
```

Поля слота

```C#
public bool TryDispense()
{
    if (Quantity > 0 && !Product.IsExpired())
    {
        Quantity--;
        return true;
    }
    return false;
}
```

TryDispense проверяет, есть ли товар и не просрочен ли он. Если всё норм, то уменьшает количество на 1 и возвращает true. В противном случае возвращает false

```C#
public void Refill(int amount)
{
    Quantity += amount;
}
```

Refill в админском режиме добавляет указанное количество

## Прием монет и баланс(CoinManager)
```C#
public Dictionary<decimal, int> Coins;
private decimal _currentBalance;
public List<decimal> AcceptedDenominations = new() { 0.10m, 0.50m, 1m, 2m, 5m, 10m };
```

Coins - это запас монет в автомате(ключ - номинал, значение - количество). \_currentBalance - сумма, которую внес пользователь. AcceptedDenominations - какие номиналы принимаются 

```C#
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
```

После того, как пользователь вставил монет, происходит проверка(поддерживается такой номинал или нет). Если всё норм, то баланс увеличивается на значение монеты. После этого возвращается true, что означает, что монета принята (в противном случае вернется false, если номинал не поддерживается)

```C#
public decimal GetCurrentBalance()
{
    return _currentBalance;
}
```

Возвращает баланс клиента

## Расчет сдачи
```C#
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
```

Попробую объяснить, что тут вообще происходит...

В общем: temp - копия запасов монет. Берем копию, чтобы сначала затестить, а получится ли вообще собраться сдачу, не изменяя основной Coins. result - сюда записываем, какие монеты будут отданы на сдачу. remaining - это сумма, которая пока не набрана (округление до 2 знаков). По номиналам идем от большего к меньшему (ну то есть сначала пытаем набрать большие монеты). need - сколько таких монет нужно без учета запаса, а take - берем не больше, чем есть в запасе, и ещё уменьшаем remaining и temp\[denom]

```C#
if (remaining == 0)
{
    foreach (var kv in result)
    {
        Coins[kv.Key] -= kv.Value;
    }
    return result;
}
return null;
```

Если собрали точную сумму, то фиксируется выдача (то есть уменьшается Coins и возвращается result). В противном случае null(нельзя дать сдачу)

## Проверка возможности выдать сдачу
```C#
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
```

Такс, тут HasSufficientChange вызывает CalculateChange. Но когда успешна CalculateChange, уже уменьшает Coins. Поэтому HasSufficientChancge восстанавливает монеты обратно(ну то есть добавляет те же самые монеты обратно в Coins), чтобы проверка не изменила состояние автомата

## Покупка товара
```C#
var slot = _productSlots.FirstOrDefault(s => s.SlotNumber == slotNumber);
...
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
...
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
                Console.WriteLine($"{c.Key:C} x {c.Value}");
        }
        else
        {
            Console.WriteLine("Не удалось выдать сдачу, обратитесь к администратору.");
        }
    }

    _paymentSystem.ClearBalance();
    Console.WriteLine("Спасибо за покупку!");
}
```

FirstOrDefault (нахождение слота по номеру) вернут null, если такого номера нет. Также сравниваем баланс и цены, и если денег мало, сообщаем юзеру, сколько не хватает. changeAmount - остаток, который нужно вернуть. Перед тем как пробовать выдавать товар, код вызывает \_paymentSystem.HasSufficientChange(changeAmount), чтобы удостовеериться, что автомат сможет отдать сдачу(если нельзя отдать сдачу - покупка блокируется). Если успешно проходит проверка на просрочку и уменьшение количества, то выводится сообщение о покупке, вызывается CalculateChange и печатается, какие монеты выданы, и очищается баланс

## Возврат денег
```C#
var balance = _paymentSystem.GetCurrentBalance();
if (balance == 0) { Console.WriteLine("Нет денег для возврата."); return; }

var change = _paymentSystem.CalculateChange(balance);
if (change != null)
{
    Console.WriteLine("Возвращаем ваши деньги:");
    foreach (var coin in change)
        Console.WriteLine($"{coin.Key:C} x {coin.Value}");
    _paymentSystem.ClearBalance();
}
else
{
    Console.WriteLine("Не могу выдать сдачу в данный момент.");
}
```

Если приходит запрос на возврат денег, то сначала получаем баланс, пытаемся выдать сдачу на полную сумму, и если CalculateChange вернул список монет, то печатаем их и обнуляем баланс(в противном случае сообщаем, что возврат невозможен в силу нехватки конкретных монет)

## Админ-режим
```C#
Console.Write("Введите пароль администратора: ");
if (Console.ReadLine() != _adminPassword)
{
    Console.WriteLine("Неверный пароль!");
    await Task.Delay(300);
    return;
}
```

Запрашиваем пароль(admin123)

```C#
((CoinManager)_paymentSystem).CollectEarnings();
```

Тут строка приводит (cast)\_paymentSystem к конкретному типу CoinManager и вызывает CollectEarnings, который печатает текущий запас монет и общую сумму

## Создание нового напитка
```C#
Console.Write("Объём (мл): ");
int.TryParse(Console.ReadLine(), out int volume);
return new Beverage(name, price, volume > 0 ? volume : 330, DateTime.Now.AddMonths(6));
```

Эта штука запрашивает объем у админа и пытается распарсить число. Если ввод невалиден (или 0), то используются дефолтные 330 мл. DateTime.Now.AddMonths(6) дата годности устанавливается на 6 месяцев от текущего момента

## Главный цикл меню
```C#
while (true)
{
    await DisplayMainMenuAsync();
    var choice = Console.ReadLine();

    switch (choice)
    {
        case "1": DisplayProducts(); break;
        case "2": await HandleCoinInsertionAsync(); break;
        case "3": await PurchaseProductAsync(); break;
        case "4": ReturnMoney(); break;
        case "5": await AdminModeAsync(); break;
        case "0": Console.WriteLine("До свидания!"); return;
        default: Console.WriteLine("Неверный выбор."); break;
    }
    Console.WriteLine("\nНажмите любую клавишу для продолжения...");
    Console.ReadKey();
    Console.Clear();
}
```

Тут бесконечный цикл, который показывает основное меню, считывает ввод и выполняет выбранное действие. После каждого шага ждет нажатия клавиши и очищает консоль, чтобы пользователь увидел результат перед следующим меню