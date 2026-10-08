using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RigSpec.App.ViewModels;

public sealed partial class SectionViewModel : ObservableObject
{
    public string Title { get; }
    public ObservableCollection<PropertyRow> Rows { get; }

    [ObservableProperty]
    private bool _isExpanded = true;

    public SectionViewModel(string title, IEnumerable<PropertyRow> rows, bool isExpanded = true)
    {
        Title = title;
        Rows = new(rows);
        _isExpanded = isExpanded;
    }

    [RelayCommand]
    private void ToggleExpand() => IsExpanded = !IsExpanded;
}
