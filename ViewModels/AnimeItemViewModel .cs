using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WatchedAnimeList.Logic;
using WatchedAnimeList.Models;

public class AnimeItemViewModel : INotifyPropertyChanged
{
    private BitmapImage? _animeImage;

    public WachedAnimeData Model { get; }

    public string AnimeName => Model.AnimeName;
    public string AnimeNameEN => Model.AnimeNameEN;
    public int Rating => Model.Rating;
    public string Genre => Model.Genre;
    public string ConnectedAnimeName => Model.ConnectedAnimeName;

    public BitmapImage? AnimeImage
    {
        get => _animeImage;
        set
        {
            if (_animeImage != value)
            {
                _animeImage = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand CardClickCommand { get; }

    public AnimeItemViewModel(WachedAnimeData model, Action<string>? onClick = null)
    {
        Model = model;
        CardClickCommand = new RelayCommand(_ => onClick?.Invoke(AnimeNameEN));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
