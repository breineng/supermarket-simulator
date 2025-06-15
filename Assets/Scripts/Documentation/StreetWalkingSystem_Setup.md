# Street Walking System Setup Guide

## Обзор системы

Система уличных прогулок позволяет клиентам гулять по заданным waypoints на улице, а затем случайным образом решать, зайти ли в магазин. Это создает более реалистичное поведение, где не все прохожие автоматически идут в магазин.

## Компоненты системы

### 1. Новые состояния клиентов
- `StreetWalking` - клиент гуляет по waypoints
- `ConsideringStore` - клиент решает, зайти ли в магазин
- `Entering` - клиент входит в магазин (затем переходит в `Shopping`)

### 2. Основные сервисы
- `IStreetWaypointService` / `StreetWaypointService` - управление waypoints
- `IShoppingListGeneratorService` / `ShoppingListGeneratorService` - генерация списков покупок

### 3. Обновленные компоненты
- `CustomerController` - добавлена поддержка уличных прогулок
- `CustomerData` - изменено начальное состояние на `StreetWalking`
- `CustomerSaveData` - добавлены поля для сохранения состояния waypoints

## Настройка в сцене

### Шаг 1: Создание waypoints

1. Создайте пустые GameObject'ы в сцене для waypoints
2. Расположите их на улицах вокруг магазина
3. Назовите их осмысленно (например: "Waypoint_Street1", "Waypoint_CrossRoad", etc.)
4. Убедитесь, что все waypoints находятся на NavMesh

### Шаг 2: Настройка StreetWaypointService

1. Создайте GameObject с именем "StreetWaypointService"
2. Добавьте компонент `StreetWaypointService`
3. В инспекторе настройте:
   - **Street Waypoints**: массив всех waypoints на улице
   - **Store Entrance Point**: точка входа в магазин
   - **Prefer Close Waypoints**: предпочитать ближние waypoints
   - **Max Distance For Close**: максимальная дистанция для "близких" waypoints
   - **Max Close Waypoints**: максимум близких waypoints для выбора

### Шаг 3: Настройка ShoppingListGeneratorService

1. Создайте GameObject с именем "ShoppingListGeneratorService"
2. Добавьте компонент `ShoppingListGeneratorService`
3. В инспекторе настройте:
   - **Min Items**: минимум товаров в списке покупок
   - **Max Items**: максимум товаров в списке покупок
   - **Budget Factor**: какую часть денег клиенты тратят (0.7 = 70%)

### Шаг 4: Регистрация сервисов в Context

Добавьте в ваш GameContext или подходящий Context:

```csharp
// Регистрация сервисов уличных прогулок
context.RegisterDependencyAs<StreetWaypointService, IStreetWaypointService>(streetWaypointService);
context.RegisterDependencyAs<ShoppingListGeneratorService, IShoppingListGeneratorService>(shoppingListGenerator);
```

### Шаг 5: Настройка CustomerController

В префабе клиента настройте параметры в разделе "Street Walking Behavior":
- **Waypoint Reach Distance**: дистанция считается достижением waypoint (2м)
- **Wait Time At Waypoint**: базовое время ожидания на waypoint (3с)
- **Store Enter Chance**: шанс зайти в магазин (0.3 = 30%)
- **Waypoint Wait Time Min/Max**: случайное время ожидания на waypoint (1-5с)

## Логика работы

### 1. Жизненный цикл клиента

```
Spawn → StreetWalking → (случайно) → ConsideringStore → Entering → Shopping → ... → Leaving
            ↑              ↓
            ← (продолжить гулять)
```

### 2. Алгоритм уличных прогулок

1. Клиент спавнится и получает случайный waypoint
2. Идет к waypoint'у
3. Достигнув waypoint'а, ждет случайное время
4. После ожидания решает:
   - С вероятностью `Store Enter Chance` - идти к магазину
   - Иначе - продолжить гулять к следующему waypoint

### 3. Генерация списка покупок

При переходе в состояние `ConsideringStore`:
1. Проверяется наличие списка покупок
2. Если списка нет - генерируется новый на основе:
   - Денег клиента
   - Пола клиента (для будущих предпочтений)
   - Доступных товаров и их цен

## Настройки баланса

### Частота заходов в магазин
- Увеличьте `Store Enter Chance` для большего потока клиентов
- Уменьшите для более реалистичного поведения

### Время прогулок
- Увеличьте `Wait Time At Waypoint` для более медленных прогулок
- Настройте диапазон `Waypoint Wait Time Min/Max` для разнообразия

### Размер списков покупок
- Настройте `Min Items` и `Max Items` в `ShoppingListGeneratorService`
- Изменяйте `Budget Factor` для контроля трат клиентов

## Отладка

### Визуализация waypoints
- В режиме Scene waypoints отображаются как синие сферы
- Соединения между близкими waypoints показаны голубыми линиями
- Точка входа в магазин показана зеленым кубом

### Логи
Система выводит подробные логи:
- Движение клиентов между waypoints
- Решения о заходе в магазин
- Генерацию списков покупок

### Консольные команды
Используйте Debug.Log для отслеживания:
```csharp
Debug.Log($"Customer {name}: Moving to waypoint {waypointName}");
Debug.Log($"Customer {name}: Decided to enter store (chance: {random:F2})");
```

## Интеграция с существующей системой

Система полностью совместима с существующими функциями:
- Сохранение/загрузка игры
- Система очередей в кассах
- Анимации клиентов
- Статистика и аналитика

Все существующие клиенты будут автоматически начинать с уличных прогулок вместо немедленного входа в магазин. 