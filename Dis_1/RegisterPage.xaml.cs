using System.Text.Json;
using System.Text;
using Dis_1.Model;
using Newtonsoft.Json;


namespace Dis_1;

public partial class RegisterPage : ContentPage
{
    private string selectedGender;

    public RegisterPage()
	{
		InitializeComponent();
	}

    private void OnGenderSelected(object sender, EventArgs e)
    {
        
        int selectedIndex = SexPicker.SelectedIndex;

        if (selectedIndex != -1)
        {
            // Получаем выбранное значение на основе индекса
            selectedGender = SexPicker.Items[selectedIndex]; // "М" или "Ж"
        }
    }

    

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        
        // Собираем данные из полей
        var name = NameEntry.Text;
        var experience = ExpEntry.Text;
        var age = AgeEntry.Text;
        var regNumber = RegNumberEntry.Text;
        var model = ModelEntry.Text;
        var rel_year = YearEntry.Text;
       

        // Валидируем данные (например, чтобы поля не были пустыми)
        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(experience) ||
            string.IsNullOrWhiteSpace(age) ||
            string.IsNullOrWhiteSpace(regNumber) ||
            string.IsNullOrWhiteSpace(model) ||
            string.IsNullOrWhiteSpace(rel_year) ||
            string.IsNullOrWhiteSpace(selectedGender))
        {
            await DisplayAlert("Ошибка", "Все поля должны быть заполнены.", "ОК");
            return;
        }

        var userData = new
        {
            name = name,
            driving_exp = experience,
            age = age,
            sex = selectedGender
        };

        // Регистрируем пользователя и получаем его ID
        var userId = await RegisterUserAsync(userData);
        if (userId == null)
        {
            await DisplayAlert("Ошибка", "Не удалось зарегистрировать пользователя.", "ОК");
            return;
        }

        var carData = new
        {
            reg_number = regNumber,
            model = model,
            release_year = rel_year,
            user_id = userId  // Добавляем ID пользователя
        };

        await AddCarAsync(carData);

        Preferences.Set("IsRegistered", true);
        Application.Current.MainPage = new AppShell();
    }

    public async Task<int?> RegisterUserAsync(object userData)
    {
        var client = new HttpClient();
        var json = System.Text.Json.JsonSerializer.Serialize(userData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"{AppSettings.ServerUrl}/api/User", content);

        if (!response.IsSuccessStatusCode)
            return null;

        // Получаем ID пользователя из ответа
        var responseBody = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<int>(responseBody);
    }

    public async Task AddCarAsync(object carData)
    {
        var client = new HttpClient();
        var json = System.Text.Json.JsonSerializer.Serialize(carData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"{AppSettings.ServerUrl}/api/CarData", content);
    }

    

}