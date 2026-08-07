using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LTF.App.ViewModels.Screens;

namespace LTF.App.Views.Screens;

/// <summary>
/// The race-weekend view (M23). Pure playback of the recorded race telemetry; a <see cref="DispatcherTimer"/>
/// drives the lap-by-lap replay while the view model is playing, its interval scaled by the replay speed. The
/// timer is view-only glue — the projection logic (<see cref="RaceWeekendViewModel.SetLap"/> /
/// <see cref="RaceWeekendViewModel.AdvanceLap"/>) is tested directly, without it.
/// </summary>
public partial class RaceWeekendView : UserControl
{
    private readonly DispatcherTimer _timer;
    private RaceWeekendViewModel? _vm;

    public RaceWeekendView()
    {
        AvaloniaXamlLoader.Load(this);
        _timer = new DispatcherTimer();
        _timer.Tick += OnTick;
        DataContextChanged += OnDataContextChanged;
        DetachedFromVisualTree += (_, _) => _timer.Stop();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_vm is not null)
        {
            _vm.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _vm = DataContext as RaceWeekendViewModel;
        if (_vm is not null)
        {
            _vm.PropertyChanged += OnViewModelPropertyChanged;
        }

        Sync();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RaceWeekendViewModel.IsPlaying) or nameof(RaceWeekendViewModel.Speed))
        {
            Sync();
        }
    }

    private void Sync()
    {
        _timer.Stop();
        if (_vm is { IsPlaying: true })
        {
            var speed = _vm.Speed <= 0 ? 1.0 : _vm.Speed;
            _timer.Interval = TimeSpan.FromMilliseconds(1200.0 / speed);
            _timer.Start();
        }
    }

    private void OnTick(object? sender, EventArgs e) => _vm?.AdvanceLap();
}
