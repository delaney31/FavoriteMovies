﻿using System;
using System.Drawing;
using UIKit;
using Foundation;
using CoreGraphics;
using SidebarNavigation;
using System.Collections.ObjectModel;
using FavoriteMoviesPCL;
using System.Threading.Tasks;
using System.Diagnostics;

namespace FavoriteMovies
{
	public partial class RootViewController : UIViewController
	{
		// the sidebar controller for the app
		public SidebarController SidebarController { get; private set; }

		//// the navigation controller
		public NavController NavController { get; private set; }
		//// the tab controller
		public MovieTabBarController TabController { get; private set; }
		ObservableCollection<Movie> NowPlaying;
		ObservableCollection<Movie> TopRated;
		ObservableCollection<Movie> Popular;
		ObservableCollection<Movie> MovieLatest;

		int Page;

		public static int RandomNumber (int min, int max)
		{
			Random random = new Random (); return random.Next (min, max);

		}
		//public RootViewController() : base(null, null)
		public RootViewController ()
		{
			
			try {
				Page = RandomNumber (1, 50);

				var task = Task.Run (async () => {
					NowPlaying = await MovieService.GetMoviesAsync (MovieService.MovieType.NowPlaying, 1);
					TopRated = await MovieService.GetMoviesAsync (MovieService.MovieType.TopRated, Page);
					Popular = await MovieService.GetMoviesAsync (MovieService.MovieType.Popular, Page);
					MovieLatest = await MovieService.GetMoviesAsync (MovieService.MovieType.Upcoming, Page);
					//TVNowAiring = await MovieService.GetMoviesAsync (MovieService.MovieType.TVLatest, Page);
				});
				TimeSpan ts = TimeSpan.FromMilliseconds (4000);
				task.Wait (ts);
				if (!task.Wait (ts))
					Console.WriteLine ("The timeout interval elapsed.");
			} catch (Exception e) {
				Debug.WriteLine (e.Message);

			}
		}
		public override bool ShouldAutorotate ()
		{
			return base.ShouldAutorotate ();
		}

		public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations ()
		{
			return UIInterfaceOrientationMask.Portrait;
		}
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
				
			var mainView = new MainViewController (TopRated, NowPlaying, Popular, MovieLatest, Page);
			mainView.Title = "Home";
			mainView.TabBarItem.SetFinishedImages(UIImage.FromBundle ("home-7.png"),UIImage.FromBundle ("home-7.png"));

			var uinc1 = new UINavigationController (mainView);


			var tab2 = new NewsFeedViewController ();
			tab2.Title = "Updates";
			tab2.View.BackgroundColor = UIColor.Clear.FromHexString (UIColorExtensions.TAB_BACKGROUND_COLOR, 1.0f);
			tab2.TabBarItem.SetFinishedImages (UIImage.FromBundle ("newspaper-7.png"), UIImage.FromBundle ("newspaper-7.png"));
			var uinc2 = new UINavigationController (tab2);
			//uinc2.PushViewController (tab2,true);
			//var controllers = new UIViewController [] { tab2 };
			//uinc2.SetViewControllers (controllers, true);
			Console.WriteLine (uinc2.ViewControllers.Length);

			var tab3 = new UIViewController ();
			tab3.Title = "Messaging";
			tab3.View.BackgroundColor = UIColor.Clear.FromHexString (UIColorExtensions.TAB_BACKGROUND_COLOR, 1.0f);
			tab3.TabBarItem.SetFinishedImages (UIImage.FromBundle ("message-7.png"), UIImage.FromBundle ("message-7.png"));

			var uinc3 = new UINavigationController (tab3);
			//uinc3.PushViewController (tab3, true);


			var tab4 = new UIViewController ();
			tab4.Title = "Invite";
			tab4.View.BackgroundColor = UIColor.Clear.FromHexString (UIColorExtensions.TAB_BACKGROUND_COLOR, 1.0f);
			tab4.TabBarItem.SetFinishedImages (UIImage.FromBundle ("email-7.png"), UIImage.FromBundle ("email-7.png"));

			var uinc4 = new UINavigationController (tab4);
			//uinc4.PushViewController (tab4, true);


			var tabs = new UINavigationController [] {
				uinc1, uinc2, uinc3, uinc4};
			TabController = new MovieTabBarController ();


			TabController.NavigationItem.SetRightBarButtonItem (
				new UIBarButtonItem (UIImage.FromBundle ("threelines")
					, UIBarButtonItemStyle.Plain
					, (sender, args) => {
						SidebarController.ToggleMenu ();

					}), true);
			try 
			{
				TabController.SetViewControllers (tabs, true);
			} catch (Exception ex) 
			{
				Debug.Write (ex.Message);
			}

			// create a slideout navigation controller with the top navigation controller and the menu view controller
			NavController = new NavController ();
			NavController.NavigationBar.BarTintColor = UIColor.Clear.FromHexString (UIColorExtensions.NAV_BAR_COLOR, 1.0f);
			NavController.NavigationBar.TintColor = UIColor.White;
			NavController.NavigationBar.Translucent = true;
			NavController.NavigationBar.TitleTextAttributes = new UIStringAttributes () {
				ForegroundColor = UIColor.White
			};

			SidebarController = new SidebarController (this, NavController, new SideMenuController ());
			SidebarController.MenuWidth = 220;
			SidebarController.MenuLocation = SidebarController.MenuLocations.Right;
			SidebarController.ReopenOnRotate = false;
			SidebarController.HasShadowing = true;

			NavController.PushViewController (TabController, true);


		}
	}
}
