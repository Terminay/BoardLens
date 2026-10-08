namespace BoardLens.App.ViewModels;

public sealed class PropertyRow(string key, string value)
{
    public string Key { get; } = key;
    public string Value { get; } = value;
}
