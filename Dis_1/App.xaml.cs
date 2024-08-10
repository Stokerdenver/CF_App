namespace Dis_1

{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }

        protected override void OnStart()
        {
            base.OnStart();
           // RequestLocationPermissionAndStartService();
        }
    }
}
