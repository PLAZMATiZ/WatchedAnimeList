using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WatchedAnimeList.Logic;
using WatchedAnimeList.Models;
using WatchedAnimeList.Helpers;
public class AnimeItemViewModel : INotifyPropertyChanged
{
    public WachedAnimeData Model { get; }

    public string? AnimeName => Model.AnimeName;
    public string? OriginalName => Model.OriginalName;
    public string? Genres => Model.Genres;

    public BitmapImage? AnimeImage
    {
        get => Model.AnimeImage;
        set
        {
            if (Model.AnimeImage != value)
            {
                Model.AnimeImage = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand CardClickCommand { get; }

    public AnimeItemViewModel(WachedAnimeData model, Action<string>? onClick = null)
    {
        Model = model;
        if (OriginalName is null)
            Debug.Ex("AnimeNameEN is null");

        CardClickCommand = new RelayCommand(_ => onClick?.Invoke(OriginalName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
