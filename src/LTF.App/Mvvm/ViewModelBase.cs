using CommunityToolkit.Mvvm.ComponentModel;

namespace LTF.App.Mvvm;

/// <summary>
/// Base for every view-model in the app. Inherits CommunityToolkit.Mvvm's <see cref="ObservableObject"/>
/// so derived types get <c>INotifyPropertyChanged</c> plus the <c>[ObservableProperty]</c> /
/// <c>[RelayCommand]</c> source generators (ADR-0026). Deliberately empty — it exists only to give the
/// UI one stable base type to hang shared behaviour on later without touching every view-model.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
}
