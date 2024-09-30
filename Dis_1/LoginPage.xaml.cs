namespace Dis_1;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
	}

    // Метод, который вызывается при нажатии на кнопку "Подтвердить"
    private async void OnLoginConfirmClicked(object sender, EventArgs e)
    {
        
        
        var username = UsernameEntry.Text;

        // Проверяем, пустое ли имя
        if (string.IsNullOrWhiteSpace(username))
        {
            await DisplayAlert("Ошибка", "Имя пользователя не может быть пустым", "ОК");
            return;
        }

        // Здесь добавляем запрос к API для проверки наличия пользователя в базе данных
        bool userExists = await CheckUserExists(username);

        if (userExists)
        {
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
            var response = await client.GetAsync($"http://10.0.2.2:5000/api/User/{username}");
            return response.IsSuccessStatusCode; // Если 200, пользователь существует
        }
    }
}