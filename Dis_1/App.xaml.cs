namespace Dis_1

{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new HelloPage();

            /*
            // Проверка, зарегистрирован ли пользователь
            if (IsUserRegistered())
            {
                MainPage = new AppShell(); // Главная страница приложения
            }
            else
            {
                MainPage = new RegisterPage(); // Страница регистрации
            }

            */
        }

        protected override void OnStart()
        {
            base.OnStart();
           // RequestLocationPermissionAndStartService();
        }

        private bool IsUserRegistered()
        {
            // Проверяем сохраненный флаг в Preferences
            return Preferences.Get("IsRegistered", false);
        }
    }
}
