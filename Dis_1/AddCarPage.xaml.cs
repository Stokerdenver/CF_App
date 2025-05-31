using System.Text;
using System.Xml.Linq;
using Dis_1.Model;
using Newtonsoft.Json;
namespace Dis_1;

public partial class AddCarPage : ContentPage
{
    public string UserName { get; set; }
    public AddCarPage()
	{
		InitializeComponent();
        UserName = Preferences.Get("UserLogin", string.Empty);
        

    }

    private async void OnGoToShellClicked(object sender, EventArgs e)
    {
        Application.Current.MainPage = new AppShell();           // :contentReference[oaicite:1]{index=1}

        // 2) ��������� �� ����������� �������� � ������ �������/��������
        //    "profile" � ��� �������� �������� Route � ShellContent/Tab
        await Shell.Current.GoToAsync("//profile");
    }

        private async void OnAddCarClicked(object sender, EventArgs e)
    {
        var regNumber = RegNumberEntry.Text;
        var model = ModelEntry.Text;
        var rel_year = YearEntry.Text;

        if (
            string.IsNullOrWhiteSpace(regNumber) ||
            string.IsNullOrWhiteSpace(model) ||
            string.IsNullOrWhiteSpace(rel_year) 
           )
        {
            await DisplayAlert("������", "��� ���� ������ ���� ���������.", "��");
            return;
        }

        // �������� ������ ������������ � �������, ����� �������� user_id
        var user = await GetUserDataFromServer(UserName);
        if (user == null)
        {
            await DisplayAlert("������", "������������ �� ������.", "��");
            return;
        }

        var carData = new
        {
            reg_number = regNumber,
            model = model,
            release_year = rel_year,
            user_id = user.id
            
        };

        await AddCarAsync(carData);

        await DisplayAlert("Info", "���� ��������� !", "��");
    }

    public async Task AddCarAsync(object carData)
    {
        var client = new HttpClient();
        var json = System.Text.Json.JsonSerializer.Serialize(carData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"{AppSettings.ServerUrl}/api/CarData", content);
    }

    public async Task<UserC> GetUserDataFromServer(string userName)
    {
        var client = new HttpClient();
        var response = await client.GetStringAsync($"{AppSettings.ServerUrl}/api/User/{userName}");
        var user = JsonConvert.DeserializeObject<UserC>(response);
        return user;

    }

}