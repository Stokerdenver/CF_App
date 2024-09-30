namespace Dis_1
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            // Очистка данных пользователя (например, удаление токена или информации)
            // Preferences.Remove("IsLoggedIn");

            // Перенаправление пользователя на страницу входа или регистрации
            Application.Current.MainPage = new HelloPage();
        }
    }
}
