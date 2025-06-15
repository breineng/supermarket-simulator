# CustomerLocomotion Setup Guide

## Компоненты для Customer GameObject

1. **CustomerController** - основная логика поведения покупателя
2. **CustomerLocomotion** - управление движением и анимациями
3. **NavMeshAgent** - навигация по NavMesh
4. **Animator** - компонент анимации
5. **Injector** (из BInject) - для внедрения зависимостей

### Структура GameObject
Типичная структура префаба клиента:
```
Customer (GameObject)
├── CustomerController
├── CustomerLocomotion
├── NavMeshAgent
├── Injector
└── Model (дочерний GameObject)
    ├── Animator
    └── SkinnedMeshRenderer
```

**Важно**: 
- NavMeshAgent должен быть на том же объекте, что и CustomerLocomotion
- Animator может быть как на том же объекте, так и на любом дочернем объекте
- CustomerLocomotion автоматически найдет Animator в дочерних объектах

## Настройка Animator Controller

### 1. Параметры (Parameters)
В Animator Controller добавьте следующие параметры:
- **IsWalking** (Bool) - определяет, движется ли персонаж
- **Speed** (Float) - скорость движения (0-1)
- **IsStrafing** (Bool) - боковое движение
- **StrafeDirection** (Float) - направление strafe (-1 влево, 1 вправо)
- **IsTurning** (Bool) - выполняется ли анимация поворота

### 2. Триггеры для анимаций действий (Action Triggers)
- **Pickup** (Trigger) - анимация взятия товара
- **Pay** (Trigger) - анимация оплаты
- **Wave** (Trigger) - анимация приветствия/прощания
- **LeftTurn90** (Trigger) - анимация поворота налево на 90°
- **RightTurn90** (Trigger) - анимация поворота направо на 90°

### 3. Состояния (States)
- **Idle** - состояние покоя
- **Walking** - обычная ходьба
- **Strafe Left** - боковая ходьба влево
- **Strafe Right** - боковая ходьба вправо
- **Left Turn 90** - поворот налево на месте
- **Right Turn 90** - поворот направо на месте
- **Pickup Action** - анимация взятия товара (опционально)
- **Pay Action** - анимация оплаты (опционально)
- **Wave Action** - анимация приветствия (опционально)

### 4. Переходы (Transitions)
#### Основные переходы движения:
- **Idle → Walking**: Condition: IsWalking = true
- **Walking → Idle**: Condition: IsWalking = false
- **Walking → Strafe Left**: Conditions: IsStrafing = true, StrafeDirection < 0
- **Walking → Strafe Right**: Conditions: IsStrafing = true, StrafeDirection > 0
- **Strafe Left/Right → Walking**: Condition: IsStrafing = false

#### Переходы для анимаций поворота:
- **Idle → Left Turn 90**: Trigger: LeftTurn90
- **Idle → Right Turn 90**: Trigger: RightTurn90
- **Left Turn 90 → Idle**: Has Exit Time = true (1.0)
- **Right Turn 90 → Idle**: Has Exit Time = true (1.0)

**Важно**: Добавьте тег "Turn" к состояниям Left Turn 90 и Right Turn 90 для корректного определения завершения анимации.

#### Переходы для анимаций действий:

**Вариант 1 - Простой (рекомендуется):**
Используйте переходы из Any State, чтобы анимации могли запускаться из любого состояния:
- **Any State → Pickup Action**: Trigger: Pickup
- **Any State → Pay Action**: Trigger: Pay
- **Any State → Wave Action**: Trigger: Wave

Для возврата создайте отдельные переходы:
- **Pickup Action → Idle**: Has Exit Time = true (1.0)
- **Pay Action → Idle**: Has Exit Time = true (1.0)
- **Wave Action → Idle**: Has Exit Time = true (1.0)

**Вариант 2 - Сложный (для продвинутых):**
Если нужно возвращаться именно в то состояние, из которого пришли:
- Создайте переходы из каждого состояния (Idle, Walking) в анимации действий
- Создайте обратные переходы из анимаций действий в каждое состояние
- Используйте дополнительные параметры для отслеживания предыдущего состояния

### 5. Настройки переходов
- **Для движения**: Has Exit Time = false, Duration = 0.1-0.2
- **Для анимаций действий (вход)**: Has Exit Time = false, Duration = 0.1
- **Для анимаций действий (выход)**: Has Exit Time = true, Duration = 0.1-0.2
- **Для анимаций поворота**: Duration = 0 (мгновенный переход)

### 6. Настройки Any State
При использовании переходов из Any State:
- **Can Transition To Self**: отключено (false)
- Это предотвратит повторный запуск анимации, если она уже проигрывается

## Настройка CustomerLocomotion компонента

### Movement Configuration
- **Walk Speed**: 3.5 (стандартная скорость ходьбы)
- **Run Speed**: 5.5 (скорость бега, если потребуется)
- **Rotation Speed**: 10 (скорость поворота)
- **Animation Damp Time**: 0.1 (сглаживание анимаций)

### Animation Parameters
Имена параметров должны совпадать с параметрами в Animator Controller:
- **Speed Parameter**: "Speed"
- **Is Walking Parameter**: "IsWalking"
- **Is Strafing Parameter**: "IsStrafing"
- **Strafe Direction Parameter**: "StrafeDirection"

### Turn Animation Parameters
- **Left Turn Trigger**: "LeftTurn90"
- **Right Turn Trigger**: "RightTurn90"
- **Is Turning Parameter**: "IsTurning"
- **Turn Animation Threshold**: 45 (минимальный угол для активации анимации поворота)
- **Turn Animation Cooldown**: 0.5 (задержка между анимациями поворота)

### Action Animation Triggers
Имена триггеров для анимаций действий:
- **Pickup Trigger**: "Pickup"
- **Pay Trigger**: "Pay"
- **Wave Trigger**: "Wave"

### Movement Detection
- **Movement Threshold**: 0.1 (минимальная скорость для определения движения)
- **Angle Threshold**: 30 (угол для определения бокового движения)

## Настройка NavMeshAgent

1. **Speed**: будет установлен автоматически из CustomerLocomotion
2. **Stopping Distance**: 0.5-1.0 (расстояние остановки)
3. **Auto Braking**: true
4. **Radius**: 0.3-0.5 (радиус агента)
5. **Height**: 1.8-2.0 (высота персонажа)

## Дополнительные настройки

### Для корректной работы анимаций
1. Убедитесь, что анимации настроены как Loop для ходьбы
2. Root Motion должен быть отключен (движение контролируется NavMeshAgent)
3. Анимации должны быть в Humanoid формате
4. Анимации действий (Pickup, Pay, Wave) НЕ должны быть Loop
5. Анимации поворота (Left Turn 90, Right Turn 90) НЕ должны быть Loop
6. Добавьте тег "Turn" к состояниям поворота в Animator Controller

### Для плавного движения
1. В Project Settings → Time → Fixed Timestep: 0.02 или меньше
2. NavMesh должен быть правильно запечен (baked) для сцены
3. Убедитесь, что на полу есть NavMesh Surface

## Использование в коде

### Управление движением:
```csharp
_locomotion.SetDestination(targetPosition);
_locomotion.Stop();
_locomotion.Resume();
_locomotion.FaceTarget(targetTransform); // С анимацией поворота
_locomotion.FaceTarget(targetTransform, false); // Без анимации поворота
```

### Проигрывание анимаций:
```csharp
_locomotion.PlayPickupAnimation();
_locomotion.PlayPayAnimation();
_locomotion.PlayWaveAnimation();
_locomotion.PlayActionAnimation("CustomTrigger");
```

### Установка параметров анимации:
```csharp
_locomotion.SetAnimationBool("IsRunning", true);
_locomotion.SetAnimationFloat("Mood", 0.8f);
```

### Проверка состояний:
```csharp
if (_locomotion.IsTurning) 
{
    // Персонаж выполняет анимацию поворота
}
```

## Примечания
- CustomerLocomotion автоматически синхронизирует NavMeshAgent с анимациями
- Компонент поддерживает плавные повороты и переходы между состояниями
- Анимации поворота активируются автоматически при больших углах поворота
- Во время анимации поворота движение приостанавливается
- Можно отключить анимации поворота, передав `false` в методы `FaceTarget` и `FaceDirection`
- Анимации действий проигрываются поверх текущего состояния движения 