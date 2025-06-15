namespace Core.Models
{
    public enum PromptType
    {
        /// <summary>
        /// Подсказка уже содержит полную информацию, включая клавиши действия,
        /// или является самодостаточным статусным сообщением.
        /// </summary>
        Complete,
        /// <summary>
        /// Подсказка описывает только действие (например, "взять предмет")
        /// и требует добавления префикса с клавишей (например, "Нажмите [E] чтобы...").
        /// </summary>
        RawAction
    }

    public readonly struct InteractionPromptData
    {
        public string Text { get; }
        public PromptType Type { get; }

        public bool IsEmpty => string.IsNullOrEmpty(Text);

        public InteractionPromptData(string text, PromptType type)
        {
            Text = text;
            Type = type;
        }

        public static InteractionPromptData Empty => new InteractionPromptData(string.Empty, PromptType.Complete);
    }
} 