using System.Collections.ObjectModel;
using System.Collections.Specialized;
using WatchedAnimeList;
using WatchedAnimeList.Models;

namespace WatchedAnimeList.ViewModels
{
    public class AnimeViewModel
    {
        public static AnimeViewModel Global = null!;
        public BulkObservableCollection<AnimeItemViewModel> AnimeList { get; } = new();

        public AnimeViewModel()
        {
            Global = this;
        }
    }
}

public class BulkObservableCollection<T> : ObservableCollection<T>
{
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null) return;

        foreach (var item in items)
            Items.Add(item);

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}