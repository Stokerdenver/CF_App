namespace Dis_1;

public partial class LoginPage : ContentPage
{
    public string username;

    public LoginPage()
	{
		InitializeComponent();
	}

    private async void OnBackStepClicked(object sender, EventArgs e)
    {
        Application.Current.MainPage = new HelloPage();
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
        using (var client = new HttpClient())
        {
            try
            {
                var response = await client.GetAsync($"http://10.0.2.2:5000/api/User/{username}");

                // Если ответ успешный (код 200), возвращаем true
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                // Если получен код 404, возвращаем false (пользователь не найден)
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return false;
                }

                // Если код ответа отличается от 200 и 404, выбрасываем исключение
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось выполнить запрос: {ex.Message}", "ОК");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Произошла ошибка: {ex.Message}", "ОК");
            }

            return false;
        }
    }
}