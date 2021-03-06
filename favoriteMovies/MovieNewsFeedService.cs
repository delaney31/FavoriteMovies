﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using FavoriteMoviesPCL;
using MovieFriends;
using SDWebImage;
using SQLite;
using UIKit;

namespace FavoriteMovies
{
	internal static class MovieNewsFeedService
	{
		static string url = "http://movieweb.com/rss/movie-news/";

		
		internal static List<MDCard> GetMDCardItems ()
		{
			List<MDCard> feedItemsList = new List<MDCard> ();
			AzureTablesService postService = AzureTablesService.DefaultService;
			List<PostItem> result = new List<PostItem> ();

			Task.Run (async () => {
				postService.InitializeStore ();
			}).Wait ();
			try {
				WebRequest webRequest = WebRequest.Create (url);
				WebResponse webResponse = webRequest.GetResponse ();
				Stream stream = webResponse.GetResponseStream ();
				XmlDocument xmlDocument = new XmlDocument ();
				xmlDocument.Load (stream);
				XmlNamespaceManager nsmgr = new XmlNamespaceManager (xmlDocument.NameTable);
				nsmgr.AddNamespace ("dc", xmlDocument.DocumentElement.GetNamespaceOfPrefix ("dc"));
				nsmgr.AddNamespace ("content", xmlDocument.DocumentElement.GetNamespaceOfPrefix ("content"));
				XmlNodeList itemNodes = xmlDocument.SelectNodes ("rss/channel/item");

				DeleteAllFeedItems ();


				//HNKCacheFormat format = (HNKCacheFormat)HNKCache.SharedCache ().Formats ["thumbnail"];
				//if (format == null) {
				//  format = new HNKCacheFormat ("thumbnail") {
				//      Size = new SizeF (320, 240),
				//      ScaleMode = HNKScaleMode.AspectFill,
				//      CompressionQuality = 0.5f,
				//      DiskCapacity = 1 * 1024 * 1024,
				//      PreloadPolicy = HNKPreloadPolicy.LastSession
				//  };
				//}

				//if (format == null) {


				for (int i = 0; i < itemNodes.Count; i++) {
					var feedItem = new MDCard (UITableViewCellStyle.Default, @"CardCell");
					var feed = new FeedItem ();

					if (itemNodes [i].SelectSingleNode ("title") != null) {
						feedItem.titleLabel.Text = itemNodes [i].SelectSingleNode ("title").InnerText;
						feed.Title = itemNodes [i].SelectSingleNode ("title").InnerText;
					}
					if (itemNodes [i].SelectSingleNode ("link") != null) {
						feedItem.Link = itemNodes [i].SelectSingleNode ("link").InnerText;
						feed.Link = itemNodes [i].SelectSingleNode ("link").InnerText;
						//feedItem.Link = itemNodes [i].SelectSingleNode ("enclosure").Attributes ["url"].Value;
					}
					if (itemNodes [i].SelectSingleNode ("link") != null) {
						//feedItem.Link = itemNodes [i].SelectSingleNode ("link").InnerText;
						//feedItem.profileImage.SetCacheFormat (new HNKCacheFormat ("thumbnail") 
						//{
						//  Size = new SizeF (320, 240),
						//  ScaleMode = HNKScaleMode.AspectFill,
						//  CompressionQuality = 0.5f,
						//  DiskCapacity = 1 * 1024 * 1024,
						//  PreloadPolicy = HNKPreloadPolicy.LastSession
						//});

						feedItem.profileImage.SetImage (MovieCell.GetImageUrl (itemNodes [i].SelectSingleNode ("enclosure").Attributes ["url"].Value));
						feed.ImageLink = itemNodes [i].SelectSingleNode ("enclosure").Attributes ["url"].Value;
					}
					if (itemNodes [i].SelectSingleNode ("pubDate") != null) {
						feedItem.PubDate = itemNodes [i].SelectSingleNode ("pubDate").InnerText;
						feed.PubDate = itemNodes [i].SelectSingleNode ("pubDate").InnerText;
					}
					if (itemNodes [i].SelectSingleNode ("dc:creator", nsmgr) != null) {
						feedItem.Creator = itemNodes [i].SelectSingleNode ("dc:creator", nsmgr).InnerText;
						feed.Creator = itemNodes [i].SelectSingleNode ("dc:creator", nsmgr).InnerText;
					}
					if (itemNodes [i].SelectSingleNode ("category") != null) {
						feedItem.Category = itemNodes [i].SelectSingleNode ("category").InnerText;
						feed.Category = itemNodes [i].SelectSingleNode ("category").InnerText;
					}
					if (itemNodes [i].SelectSingleNode ("description") != null) {
						feedItem.descriptionLabel.Text = itemNodes [i].SelectSingleNode ("description").InnerText;
						feed.Description = itemNodes [i].SelectSingleNode ("description").InnerText;
					}
					if (itemNodes [i].SelectSingleNode ("content:encoded", nsmgr) != null) {
						feedItem.Content = itemNodes [i].SelectSingleNode ("content:encoded", nsmgr).InnerText;
						feed.Content = itemNodes [i].SelectSingleNode ("content:encoded", nsmgr).InnerText;
					} else {
						feedItem.Content = feedItem.Description;
						feed.Content = feedItem.Description;
					}

					Task.Run (async () => {
						result = await postService.GetCloudLike (feed.Title);
					}).Wait ();


					if (result.Count > 0) {
						feedItem.likeLabel.Text = result.Where (x => x.UserId == ColorExtensions.CurrentUser.Id).Count () > 0 ? "Unlike" : "Like";
						feedItem.likes = result [0].Likes;
						feedItem.id = result [0].Id;
					} else
						feedItem.likeLabel.Text = "Like";

					feedItemsList.Add (feedItem);

				}

			} catch (WebException e) {
				Console.WriteLine (@"Error{0}", e.Message + " No internet connection");
				ShowAlert ("Limited Internet", "Your internet connection is down. Many items will not be available.", "Ok");
				ColorExtensions.NoInternet = true;
				//ColorExtensions.CurrentUser.suggestmovies = false;
			} catch (Exception e) {
				Console.WriteLine (@"Error{0}", e.Message);
			}

            ColorExtensions.NewsFeed = feedItemsList;
			return feedItemsList;

		}

		static void  ShowAlert (string title, string message, params string [] buttons)
		{
			var tcs = new TaskCompletionSource<int> ();
			var alert = new UIAlertView {
				Title = title,
				Message = message
			};
			foreach (var button in buttons)
				alert.AddButton (button);
			alert.Clicked += (s, e) => tcs.TrySetResult ((int)e.ButtonIndex);
			alert.Show ();

		}

		public static void DeleteAllFeedItems ()
		{
			//var ts = new CancellationTokenSource ();
			//CancellationToken ct = ts.Token;
			var task = Task.Run (() => {
				try {
					using (var db = new SQLite.SQLiteConnection (MovieService.Database)) {
						// there is a sqllite bug here https://forums.xamarin.com/discussion/52822/sqlite-error-deleting-a-record-no-primary-keydb.Delete<Movie> (movieDetail);

						db.Query<FeedItem> ("DELETE FROM [FeedItem]");

					}
				} catch (SQLite.SQLiteException e) {
					//first time in no favorites yet.

					Debug.Write (e.Message);
				//	ts.Cancel ();
				}
			});
			task.Wait();
		}
		static int? InsertNewsFeed (FeedItem feedItem)
		{
			try {
				
				using (var db = new SQLiteConnection (MovieService.Database)) 
				{
					// there is a sqllite bug here https://forums.xamarin.com/discussion/
					//52822/sqlite-error-deleting-a-record-no-primary-keydb.Delete<Movie> (movieDetail);
					//var query = db.Table<CustomList> ();

					//foreach (var list in feedItemsList) {

					if (feedItem.Title != null) 
					{
						db.InsertOrReplace (feedItem, typeof (FeedItem));
					}

					string sql = "select last_insert_rowid()";
					var scalarValue = db.ExecuteScalar<string> (sql);
					int value = scalarValue == null ? 0 : Convert.ToInt32 (scalarValue);
					return value;
					//}
				}

			} catch (SQLiteException e) {
				Debug.WriteLine (e.Message);

				using (var conn = new SQLite.SQLiteConnection (MovieService.Database)) {
					conn.CreateTable<FeedItem> (CreateFlags.ImplicitPK | CreateFlags.AutoIncPK);

				}

				using (var db = new SQLiteConnection (MovieService.Database)) 
				{
				//	foreach (var list in feedItemsList) {
						if (feedItem.Title != null) 
						{
							db.InsertOrReplace (feedItem, typeof (FeedItem));
						}
				//	}

					string sql = "select last_insert_rowid()";
					var scalarValue = db.ExecuteScalar<string> (sql);
					int value = scalarValue == null ? 0 : Convert.ToInt32 (scalarValue);
					return value;
				}
			}

		}

	}
}

