using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FavoriteMoviesPCL;

namespace FavoriteMovies.ViewModels
{
    public class MovieDetailsViewModel : BaseViewModel
    {
        readonly Movie movie;
        string trailerId = string.Empty;

        public MovieDetailsViewModel(Movie movie)
        {
            this.movie = movie ?? throw new ArgumentNullException(nameof(movie));
        }

        public Movie Movie => movie;

        public string TrailerId
        {
            get => trailerId;
            private set => SetProperty(ref trailerId, value);
        }

        public async Task InitializeAsync()
        {
            if (!string.IsNullOrEmpty(TrailerId))
            {
                return;
            }

            try
            {
                var imdbUrl = $"http://api.themoviedb.org/3/movie/{movie.Id}/videos?api_key={MovieService._apiKey}";
                TrailerId = await MovieService.GetYouTubeMovieId(imdbUrl);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                TrailerId = string.Empty;
            }
        }
    }
}
