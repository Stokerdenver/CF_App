namespace Dis_1;

public partial class LoginPage : ContentPage
{
    public string username;

    public LoginPage()
	{
		InitializeComponent();
	}

    private async void OnLoginConfirmClicked(object sender, EventArgs e)
    {

        username = UsernameEntry.Text;

        Preferences.Set("UserLogin", UsernameEntry.Text);

        // Проверяем, пустое ли имя
        if (string.IsNullOrWhiteSpace(username))
        {
            await DisplayAlert("Ошибка", "Имя пользователя не может быть пустым", "ОК");
            return;
        }

        // Запрос к API для проверки наличия пользователя в базе данных
        bool userExists = await CheckUserExists(username);

        if (userExists)
        {

            // Отправляем сообщение
            //  MessagingCenter.Send(this, "UserLoggedIn");
          /*  var profilePage = new Profile1(username);
            profilePage.LoadUserData();
          */ 
            // Если пользователь найден, переходим на главную страницу
            Application.Current.MainPage = new AppShell();

          

        }
        else
        {
            // Если пользователь не найден, выводим ошибку
            await DisplayAlert("Ошибка", "Пользователь не найден", "ОК");
        }

        
    }

    // Метод для проверки наличия пользователя через запрос к API
    private async Task<bool> CheckUserExists(string username)
    {
        // Пример запроса к вашему WebAPI
        using (var client = new HttpClient())
        {
            var response = await client.GetStringAsync($"http://10.0.2.2:5000/api/User/{username}");
            if (response != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}