using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SQLite;

namespace FavoriteMoviesPCL
{
        public static class MovieService
        {

                const string _baseUrl = "https://api.themoviedb.org/3/";
                const string _pageString = "&page=";
                //static string _apiKey = "ab41356b33d100ec61e6c098ecc92140";
                public static string _apiKey = "63ac84758e2d96000620e55b773fc312";
                public static ObservableCollection<Movie> MovieList;

                static readonly HttpClient HttpClient;

                static MovieService ()
                {
                        HttpClient = new HttpClient {
                                BaseAddress = new Uri (_baseUrl, UriKind.Absolute),
                                Timeout = TimeSpan.FromSeconds (30)
                        };
                        HttpClient.DefaultRequestHeaders.Accept.Clear ();
                        HttpClient.DefaultRequestHeaders.Accept.Add (new MediaTypeWithQualityHeaderValue ("application/json"));
                }


		public enum MovieType
		{
			TopRated = 0,
			NowPlaying = 1,
			Popular = 2,
			Similar = 3,
			TVLatest = 4,
			Upcoming = 5


		}
		public static string Database { get; set; }
		public static int TotalPagesTopRated { get; set; }
		public static int TotalPagesNowPlaying { get; set; }
		public static int TotalPagesPopular { get; set; }
		static FavoriteMovieStore store;
		public static FavoriteMovieStore Store {
			get {
				return store != null ? store : store = new FavoriteMovieStore (Database, "MovieEntries.db3");
			}
		}

                public static async Task<string> GetYouTubeMovieId (string url, CancellationToken cancellationToken = default)
                {
                        if (string.IsNullOrWhiteSpace (url))
                                throw new ArgumentException ("Value cannot be null or whitespace.", nameof (url));

                        try {
                                var content = await GetResponseContentAsync (url, cancellationToken).ConfigureAwait (false);
                                if (string.IsNullOrEmpty (content))
                                        return string.Empty;

                                var response = JObject.Parse (content);
                                var video = ParseVideoId (response);
                                return video.Id ?? string.Empty;
                        } catch (OperationCanceledException) {
                                throw;
                        } catch (Exception ex) {
                                //Model Error
                                Debug.WriteLine (ex);
                                return string.Empty;
                        }

                }


                static MoviePOCO ParseVideoId (JObject jresponse)
                {
                        var results = jresponse? ["results"] as JArray;
                        if (results == null)
                                return new MoviePOCO ();

                        try {
                                var video = results.OfType<JObject> ().FirstOrDefault ();
                                return video != null
                                        ? new MoviePOCO { Id = video.Value<string> ("key") ?? string.Empty }
                                        : new MoviePOCO ();
                        } catch (Exception e) {

                                Debug.WriteLine (e.Message);
                                return new MoviePOCO ();

                        }

                }

                static async Task<string> GetResponseContentAsync (string requestUri, CancellationToken cancellationToken)
                {
                        using (var response = await HttpClient.GetAsync (requestUri, cancellationToken).ConfigureAwait (false)) {
                                if (response.IsSuccessStatusCode)
                                        return await response.Content.ReadAsStringAsync ().ConfigureAwait (false);

                                Debug.WriteLine ($"Request to '{requestUri}' failed with status code {response.StatusCode}.");
                                return null;
                        }
                }

                static string BuildMovieEndpoint (MovieType type, int page, string movieId)
                {
                        var pageQuery = _pageString + page;

                        switch (type) {
                        case MovieType.TopRated:
                                return $"movie/top_rated?api_key={_apiKey}{pageQuery}";
                        case MovieType.NowPlaying:
                                return $"movie/now_playing?api_key={_apiKey}{pageQuery}";
                        case MovieType.Popular:
                                return $"movie/popular?api_key={_apiKey}{pageQuery}";
                        case MovieType.Similar:
                                if (string.IsNullOrWhiteSpace (movieId))
                                        return string.Empty;
                                return $"movie/{movieId}/similar?api_key={_apiKey}{pageQuery}";
                        case MovieType.Upcoming:
                                return $"movie/upcoming?api_key={_apiKey}&language=en-US";
                        case MovieType.TVLatest:
                                return $"tv/airing_today?api_key={_apiKey}&language=en-US";
                        default:
                                return string.Empty;
                        }
                }

                static void UpdateTotalPages (MovieType type, JObject jresponse)
                {
                        if (jresponse == null)
                                return;

                        var totalPages = jresponse.Value<int?> ("total_pages");
                        if (!totalPages.HasValue)
                                return;

                        switch (type) {
                        case MovieType.TopRated:
                                TotalPagesTopRated = totalPages.Value;
                                break;
                        case MovieType.NowPlaying:
                                TotalPagesNowPlaying = totalPages.Value;
                                break;
                        case MovieType.Popular:
                                TotalPagesPopular = totalPages.Value;
                                break;
                        }
                }
                public static async Task<ObservableCollection<Movie>> GetMoviesAsyncString (MovieType type, int page = 1, string movieId = "", CancellationToken cancellationToken = default)
                {
                        var requestUri = BuildMovieEndpoint (type, page, movieId);
                        if (string.IsNullOrEmpty (requestUri))
                                return new ObservableCollection<Movie> ();

                        try {
                                var content = await GetResponseContentAsync (requestUri, cancellationToken).ConfigureAwait (false);
                                if (string.IsNullOrEmpty (content))
                                        return new ObservableCollection<Movie> ();

                                var jresponse = JObject.Parse (content);
                                UpdateTotalPages (type, jresponse);
                                MovieList = GetJsonData (jresponse);
                                return MovieList ?? new ObservableCollection<Movie> ();
                        } catch (OperationCanceledException) {
                                throw;
                        } catch (Exception ex) {
                                //Model Error
                                Debug.WriteLine (ex);
                                return new ObservableCollection<Movie> ();

                        }
                }
		//GET movies from service
                public static async Task<ObservableCollection<Movie>> GetMoviesAsync (MovieType type, int page = 1, int movieId = 0, CancellationToken cancellationToken = default)
                {
                        var movieIdValue = movieId > 0 ? movieId.ToString (CultureInfo.InvariantCulture) : string.Empty;
                        var requestUri = BuildMovieEndpoint (type, page, movieIdValue);
                        if (string.IsNullOrEmpty (requestUri))
                                return new ObservableCollection<Movie> ();

                        try {
                                var content = await GetResponseContentAsync (requestUri, cancellationToken).ConfigureAwait (false);
                                if (string.IsNullOrEmpty (content))
                                        return new ObservableCollection<Movie> ();

                                var jresponse = JObject.Parse (content);
                                UpdateTotalPages (type, jresponse);
                                MovieList = GetJsonData (jresponse);
                                AddListToLocalDB (MovieList, type);
                                return MovieList ?? new ObservableCollection<Movie> ();
                        } catch (OperationCanceledException) {
                                throw;
                        } catch (WebException we) {
                                //Server Error or no internet connection.
                                Debug.WriteLine (we);
                                return GetListFromLocalDB (GetCustomIdFromType (type.ToString ()));
                        } catch (Exception ex) {
                                //Model Error
                                Debug.WriteLine (ex);
                                return GetListFromLocalDB (GetCustomIdFromType (type.ToString ()));
                        }
                }

		static string GetCustomIdFromType (string type)
		{
			var retId = "";
			try {
				
				using (var db = new SQLite.SQLiteConnection (MovieService.Database)) {
					var task = Task.Run (() => {
						// there is a sqllite bug here https://forums.xamarin.com/discussion/52822/sqlite-error-deleting-a-record-no-primary-keydb.Delete<Movie> (movieDetail);

						var query = db.Query<CustomList> ("SELECT [id] FROM [CustomList] WHERE [name] = '" + type + "'");
						retId = query.FirstOrDefault ().id.ToString ();
					});
					task.Wait ();
				}
			} catch (Exception ex) 
			{
				Debug.WriteLine (ex.Message);
			}

			return retId;

		}

		static ObservableCollection<Movie> GetListFromLocalDB (string id)
		{
			var returnList = new ObservableCollection<Movie> ();
			using (var db = new SQLite.SQLiteConnection (MovieService.Database)) {
				var task = Task.Run (() => 
				{
					// there is a sqllite bug here https://forums.xamarin.com/discussion/52822/sqlite-error-deleting-a-record-no-primary-keydb.Delete<Movie> (movieDetail);

						var query = db.Query<Movie> ("SELECT * FROM [Movie] WHERE [CustomListID] = " + id);

						foreach (var movie in query) 
						{
							var item = new Movie ();
							item.id = movie.id;
							item.name = movie.name;
							item.BackdropPath = movie.BackdropPath;
							item.CustomListID = movie.CustomListID;
							item.Favorite = movie.Favorite;
							item.HighResPosterPath = movie.HighResPosterPath;
							item.OriginalLanguage = movie.OriginalLanguage;
							item.Overview = movie.Overview;
							item.Popularity = movie.Popularity;
							item.PosterPath = movie.PosterPath;
							item.ReleaseDate = movie.ReleaseDate;
							item.VoteAverage = movie.VoteAverage;
							item.UserReview = movie.UserReview;
							item.UserRating = movie.UserRating;
							item.OriginalId = movie.OriginalId;
							item.cloudId = movie.cloudId;
							item.order = movie.order;
							returnList.Add (item);
						}


				});
				task.Wait ();

				return returnList;
			}
		
		}

		static void AddListToLocalDB (ObservableCollection<Movie> movieList, MovieType type)
		{

			var listItem = new CustomList ();
			listItem.shared = false;
			listItem.order = 0;
			listItem.custom = false;
			listItem.name = type.ToString ();

			using (var db = new SQLiteConnection (MovieService.Database)) {
				try {
					var list = db.Query<CustomList> ("SELECT id FROM [CustomList] WHERE name = '" + listItem.name + "'");
					if (list.Count > 0) {
						var id = list.FirstOrDefault ().id;
						db.Query<Movie> ("DELETE FROM [Movie] WHERE CustomListID = " + id);
						db.Query<CustomList> ("DELETE FROM [CustomList] WHERE name = '" + listItem.name + "'");
					}
					db.InsertOrReplace (listItem, typeof (CustomList));
					string sql = "select last_insert_rowid()";
					var scalarValue = db.ExecuteScalar<string> (sql);
					int value = scalarValue == null ? 0 : Convert.ToInt32 (scalarValue);
					foreach (var movie in movieList) {
						if (value > 0) {
							movie.CustomListID = value;
							db.InsertOrReplace (movie, typeof (Movie));

						}
					}
				} catch (Exception ex) {
					try {
						Debug.WriteLine (ex);
						using (var conn = new SQLite.SQLiteConnection (MovieService.Database)) 
						{
							conn.CreateTable<Movie> (CreateFlags.ImplicitPK | CreateFlags.AutoIncPK);
							conn.CreateTable<CustomList> (CreateFlags.ImplicitPK | CreateFlags.AutoIncPK);
						}
						db.InsertOrReplace (listItem, typeof (CustomList));
						string sql = "select last_insert_rowid()";
						var scalarValue = db.ExecuteScalar<string> (sql);
						int value = scalarValue == null ? 0 : Convert.ToInt32 (scalarValue);
						foreach (var movie in movieList) {
							if (value > 0) {
								movie.CustomListID = value;
								db.InsertOrReplace (movie, typeof (Movie));

							}
						}
					} catch (Exception e) {
						Debug.WriteLine (e);
					}
				}

			}
		}

                public static async Task<ObservableCollection<CastCrew>> MovieCreditsAsync (string query, CancellationToken cancellationToken = default)
                {
                        if (string.IsNullOrWhiteSpace (query))
                                return new ObservableCollection<CastCrew> ();

                        try {
                                var content = await GetResponseContentAsync ($"movie/{query}/credits?api_key={_apiKey}", cancellationToken).ConfigureAwait (false);
                                if (string.IsNullOrEmpty (content))
                                        return new ObservableCollection<CastCrew> ();

                                var jresponse = JObject.Parse (content);
                                return GetCastJsonData (jresponse);
                        } catch (OperationCanceledException) {
                                throw;
                        } catch (Exception ex) {
                                //Model Error
                                Debug.WriteLine (ex);
                                return new ObservableCollection<CastCrew> ();

                        }
                }
                public static async Task<ObservableCollection<Movie>> MovieSearch (string query, CancellationToken cancellationToken = default)
                {
                        if (string.IsNullOrWhiteSpace (query))
                                return new ObservableCollection<Movie> ();

                        try {
                                var encodedQuery = Uri.EscapeDataString (query);
                                var content = await GetResponseContentAsync ($"search/movie?api_key={_apiKey}&language=en-US&query={encodedQuery}", cancellationToken).ConfigureAwait (false);
                                if (string.IsNullOrEmpty (content))
                                        return new ObservableCollection<Movie> ();

                                var jresponse = JObject.Parse (content);
                                MovieList = GetJsonData (jresponse);
                                return MovieList ?? new ObservableCollection<Movie> ();
                        } catch (OperationCanceledException) {
                                throw;
                        } catch (Exception ex) {
                                //Model Error
                                Debug.WriteLine (ex);
                                return new ObservableCollection<Movie> ();

                        }
                }

                static ObservableCollection<CastCrew> GetCastJsonData (JObject jresponse)
                {

                        var results = jresponse? ["cast"] as JArray;

                        var castList = new ObservableCollection<CastCrew> ();
                        if (results == null)
                                return castList;

                        try {
                                foreach (var jObj in results.OfType<JObject> ()) {
                                        var castCrew = new CastCrew ();
                                        castCrew.Character = jObj.Value<string> ("character");
                                        castCrew.ProfilePath = jObj.Value<string> ("profile_path") ?? string.Empty;
                                        castCrew.Actor = jObj.Value<string> ("name");

                                        castList.Add (castCrew);
                                }
                        } catch (Exception e) {

                                Debug.WriteLine (e.Message);

                        }
                        return castList;


                }

                static ObservableCollection<Movie> GetJsonData (JObject jresponse)
                {
                        var results = jresponse? ["results"] as JArray;
                        var movieList = new ObservableCollection<Movie> ();
                        if (results == null)
                                return movieList;

                        try {
                                foreach (var jObj in results.OfType<JObject> ()) {
                                        var newMovie = new Movie {
                                                name = jObj.Value<string> ("title"),
                                                PosterPath = jObj.Value<string> ("poster_path"),
                                                HighResPosterPath = jObj.Value<string> ("poster_path"),
                                                OriginalId = jObj.Value<int?> ("id") ?? 0,
                                                id = null,
                                                Overview = jObj.Value<string> ("overview"),
                                                VoteCount = jObj.Value<double?> ("vote_count") ?? 0,
                                                ReleaseDate = jObj.Value<string> ("release_date"),
                                                VoteAverage = Convert.ToSingle (jObj.Value<double?> ("vote_average") ?? 0),
                                                Adult = jObj.Value<bool?> ("adult") ?? false,
                                                BackdropPath = jObj.Value<string> ("backdrop_path")
                                        };
                                        movieList.Add (newMovie);
                                }
                        } catch (Exception e) {

                                Debug.WriteLine (e.Message);

                        }
                        return movieList;


                }

                //POST Example:
                public static async Task<bool> PostFavoriteMovieAsync (Movie movie, string sessionId, CancellationToken cancellationToken = default)
                {
                        if (movie == null)
                                throw new ArgumentNullException (nameof (movie));
                        if (string.IsNullOrWhiteSpace (sessionId))
                                throw new ArgumentException ("Value cannot be null or whitespace.", nameof (sessionId));

                        try {
                                var requestUri = $"account/id/favorite?api_key={_apiKey}&session_id={sessionId}";
                                var postBody = JsonConvert.SerializeObject (new Favorite {
                                        favorite = !movie.Favorite,
                                        media_id = movie.id ?? movie.OriginalId
                                });

                                using (var request = new HttpRequestMessage (HttpMethod.Post, requestUri)) {
                                        request.Content = new StringContent (postBody, Encoding.UTF8, "application/json");
                                        using (var response = await HttpClient.SendAsync (request, cancellationToken).ConfigureAwait (false)) {
                                                return response.IsSuccessStatusCode;
                                        }
                                }
                        } catch (OperationCanceledException) {
                                throw;
                        } catch (Exception ex) {
                                Debug.WriteLine (ex.Message);
                                return false;
                        }
                }
	}
}
