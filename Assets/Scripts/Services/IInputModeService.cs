public enum InputMode
{
    Game, // Курсор заблокирован, ввод для игрока
    UI,    // Курсор свободен, ввод для UI
    CashDeskOperation, // Курсор заблокирован, камера вращается, игрок не движется
    MovingToCashDesk // Курсор заблокирован, игрок автоматически перемещается, управление заблокировано
}

public interface IInputModeService
{
    InputMode CurrentMode { get; }
    void SetInputMode(InputMode mode);
    event System.Action<InputMode> OnModeChanged;
} 