using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FavoriteMoviesPCL;
using SQLite;

namespace FavoriteMovies.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        readonly Func<SQLiteConnection> connectionFactory;
        readonly Random random = new Random();

        public ObservableCollection<Movie> TopRated { get; } = new ObservableCollection<Movie>();
        public ObservableCollection<Movie> NowPlaying { get; } = new ObservableCollection<Movie>();
        public ObservableCollection<Movie> Popular { get; } = new ObservableCollection<Movie>();
        public ObservableCollection<Movie> Latest { get; } = new ObservableCollection<Movie>();
        public ObservableCollection<CustomList> CustomLists { get; } = new ObservableCollection<CustomList>();

        public MainViewModel()
            : this(() => new SQLiteConnection(MovieService.Database))
        {
        }

        public MainViewModel(Func<SQLiteConnection> connectionFactory)
        {
            this.connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task InitializeAsync()
        {
            if (TopRated.Any() || IsBusy)
            {
                await LoadCustomListsAsync();
                return;
            }

            await LoadMoviesAsync();
            await LoadCustomListsAsync();
        }

        public async Task LoadMoviesAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            try
            {
                var page = random.Next(1, 5);

                var topRated = await MovieService.GetMoviesAsync(MovieService.MovieType.TopRated, page);
                var nowPlaying = await MovieService.GetMoviesAsync(MovieService.MovieType.NowPlaying, page);
                var popular = await MovieService.GetMoviesAsync(MovieService.MovieType.Popular, page);
                var latest = await MovieService.GetMoviesAsync(MovieService.MovieType.Upcoming, page);

                UpdateCollection(TopRated, topRated);
                UpdateCollection(NowPlaying, nowPlaying);
                UpdateCollection(Popular, popular);
                UpdateCollection(Latest, latest);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task LoadCustomListsAsync()
        {
            var lists = await Task.Run(() => LoadCustomListsFromDatabase(connectionFactory));
            UpdateCollection(CustomLists, lists);
        }

        public static ObservableCollection<CustomList> LoadCustomListsFromDatabase(Func<SQLiteConnection> connectionFactory)
        {
            var result = new ObservableCollection<CustomList>();

            try
            {
                using (var db = connectionFactory())
                {
                    var query = db.Query<CustomList>("SELECT * FROM [CustomList] WHERE [custom] = 1 ORDER BY [Order]");
                    foreach (var list in query)
                    {
                        result.Add(new CustomList
                        {
                            id = list.id,
                            name = list.name,
                            cloudId = list.cloudId,
                            custom = list.custom,
                            order = list.order
                        });
                    }
                }
            }
            catch (SQLiteException e)
            {
                Debug.WriteLine(e.Message);
            }

            return result;
        }

        static void UpdateCollection<T>(ObservableCollection<T> collection, IEnumerable<T> items)
        {
            if (collection == null)
            {
                return;
            }

            collection.Clear();
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                collection.Add(item);
            }
        }
    }
}
