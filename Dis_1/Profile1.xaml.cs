
namespace Dis_1;
using Dis_1.Model;
using Newtonsoft.Json;

public partial class Profile1 : ContentPage
{
    public string UserName { get; set; }

    public Profile1() // Конструктор по умолчанию
    {
        InitializeComponent();

        UserName = Preferences.Get("UserLogin", string.Empty);

        LoadUserData();
    }
    
   
    public async void LoadUserData()
    {
        // Пример запроса к вашему Web API
        var user = await GetUserDataFromServer(UserName);

        //await DisplayAlert("Внимание", "Метод отработал", "ОК");

        if (user != null)
        {
            // Привязка данных к интерфейсу
            this.BindingContext = user;
        }
        else
        {
            await DisplayAlert("Ошибка", "Данные пользователя не найдены", "ОК");
        }
    }

    private async Task<UserC> GetUserDataFromServer(string userName)
    {
        var client = new HttpClient();
        var response = await client.GetStringAsync($"http://10.0.2.2:5000/api/User/{userName}");
        var user = JsonConvert.DeserializeObject<UserC>(response);
        return user;       
        
    }
    private void EditProfileButton(object sender, EventArgs e)
    {
        Application.Current.MainPage = new EditProfile();
    }
}