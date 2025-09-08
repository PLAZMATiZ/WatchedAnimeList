using System;
using System.Collections.Generic;
using WatchedAnimeList.Controls;

namespace WatchedAnimeList.Helpers
{
    public static class PagesHelper
    {
        private static readonly Stack<IPage> BackStack = new();
        private static readonly Stack<IPage> ForwardStack = new();
        private static MainWindow mainWindow = MainWindow.Global;

        public static string[]? GetPreviousPageName { get {
                
                var previous = BackStack.Select(x => x.PageName).ToList();
                var current = (IPage)mainWindow.MainContent.Content;

                previous.Add(current.PageName);
                return [.. previous];
            } }

        public static void GoToMainPage()
        {
            GoToNewPage(mainWindow.mainPage);
        }

        public static void GoToPage(IPage page)
        {
            if (mainWindow.MainContent.Content is IPage current)
                BackStack.Push(current);

            ForwardStack.Clear();
            mainWindow.MainContent.Content = page;
            page.Initialize();
        }

        public static void GoToNewPage(IPage page)
        {
            DisposeAll();

            BackStack.Clear();
            ForwardStack.Clear();

            mainWindow.MainContent.Content = page;
            page.Initialize();
        }

        public static void GoBack()
        {
            if (BackStack.Count > 0)
            {
                if (mainWindow.MainContent.Content is IDisposable disposable)
                    disposable.Dispose();

                var current = (IPage)mainWindow.MainContent.Content;
                ForwardStack.Push(current);

                var previous = BackStack.Pop();
                mainWindow.MainContent.Content = previous;
            }
        }

        public static void GoForward()
        {
            if (ForwardStack.Count > 0)
            {
                if (mainWindow.MainContent.Content is IDisposable disposable)
                    disposable.Dispose();

                var current = (IPage)mainWindow.MainContent.Content;
                BackStack.Push(current);

                var next = ForwardStack.Pop();
                mainWindow.MainContent.Content = next;
            }
        }

        private static void DisposeAll()
        {
            foreach (var page in BackStack)
                if (page is IDisposable disposable)
                    disposable.Dispose();

            foreach (var page in ForwardStack)
                if (page is IDisposable disposable)
                    disposable.Dispose();

            if (mainWindow.MainContent.Content is IDisposable current)
                current.Dispose();
        }
    }

    public interface IPage
    {
        public string PageName { get; }
        public void Initialize() { }
    }
}
