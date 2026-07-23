namespace RocketStats;

public static class TextBoxExtensions
{
    private static readonly Dictionary<TextBox, string> _placeholders = new();

    public static string GetPlaceholderText(this TextBox textBox)
    {
        return _placeholders.TryGetValue(textBox, out var placeholder) ? placeholder : string.Empty;
    }

    public static void SetPlaceholderText(this TextBox textBox, string placeholder)
    {
        _placeholders[textBox] = placeholder;
        textBox.Text = placeholder;
        textBox.ForeColor = Color.FromArgb(100, 100, 100);

        textBox.Enter += (sender, e) => {
            if (textBox.Text == placeholder)
            {
                textBox.Text = string.Empty;
                textBox.ForeColor = Color.White;
            }
        };

        textBox.Leave += (sender, e) => {
            if (string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = placeholder;
                textBox.ForeColor = Color.FromArgb(100, 100, 100);
            }
        };
    }
}
