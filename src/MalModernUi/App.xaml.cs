using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace MalModernUi
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            navigationPage = new NavigationPage(new MainPage());
            MainPage = navigationPage;
        }

        private static NavigationPage navigationPage;

        public static NavigationPage GetNavigationPage()
        {
            return navigationPage;
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }

    }
}
