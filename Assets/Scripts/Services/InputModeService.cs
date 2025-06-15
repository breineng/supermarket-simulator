using UnityEngine; // Для Cursor

public class InputModeService : IInputModeService
{
    public InputMode CurrentMode { get; private set; }
    public event System.Action<InputMode> OnModeChanged;

    public InputModeService() // Конструктор по умолчанию
    {
        // Устанавливаем начальный режим при создании сервиса
        // Однако, фактическое применение lockState/visible лучше делать в SetInputMode,
        // а здесь просто задать начальное значение CurrentMode.
        // Вызовем SetInputMode позже, после инъекции, например, в AppStartup или PlayerController.
        CurrentMode = InputMode.Game; // Предположим, игра начинается в игровом режиме
    }

    public void SetInputMode(InputMode mode)
    {
        Debug.Log($"InputModeService: Attempting to set mode to {mode}. Current mode is {CurrentMode}. Caller: {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.FullName}");
        if (CurrentMode == mode && mode == InputMode.Game && UnityEngine.Cursor.lockState == CursorLockMode.Locked && !UnityEngine.Cursor.visible) {
            // Если мы уже в игровом режиме и курсор уже заблокирован/скрыт, ничего не делаем, чтобы избежать лишних логов
            // Однако, если мы УЖЕ в UI режиме, то позволим переключиться на UI еще раз (хотя CurrentMode == mode это предотвратит выше)
            // Этот блок больше для предотвращения спама Debug.Log при многократном вызове SetInputMode(Game) когда он уже Game.
            // return; 
            // Убираем return, чтобы всегда видеть лог об изменении состояния курсора, если он реально меняется.
        }
        if (CurrentMode == mode && mode == InputMode.UI && UnityEngine.Cursor.lockState == CursorLockMode.None && UnityEngine.Cursor.visible) {
            // Аналогично для UI режима
            // return;
        }

        // Даже если режим тот же, но состояние курсора отличается, мы должны его применить.
        // if (CurrentMode == mode) return; // Убираем этот ранний выход, чтобы принудительно применить состояние курсора

        InputMode previousMode = CurrentMode;
        CurrentMode = mode;
        Debug.Log($"InputModeService: CurrentMode changed from {previousMode} to {CurrentMode}.");

        switch (mode)
        {
            case InputMode.Game:
            case InputMode.CashDeskOperation:
            case InputMode.MovingToCashDesk:
                Debug.Log("InputModeService: Setting Cursor.lockState = Locked, Cursor.visible = false for Game, CashDeskOperation, or MovingToCashDesk Mode");
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;
                break;
            case InputMode.UI:
                Debug.Log("InputModeService: Setting Cursor.lockState = None, Cursor.visible = true");
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
                break;
        }
        Debug.Log($"InputModeService: Cursor state after update: lockState = {UnityEngine.Cursor.lockState}, visible = {UnityEngine.Cursor.visible}");
        
        if (previousMode != CurrentMode || (mode == InputMode.Game && (UnityEngine.Cursor.lockState != CursorLockMode.Locked || UnityEngine.Cursor.visible)) || (mode == InputMode.UI && (UnityEngine.Cursor.lockState != CursorLockMode.None || !UnityEngine.Cursor.visible)) ) {
            OnModeChanged?.Invoke(CurrentMode);
        } else if (previousMode == CurrentMode) {
             Debug.Log("InputModeService: Mode did not change, OnModeChanged not invoked, but cursor state might have been reapplied.");
        }
    }
} 