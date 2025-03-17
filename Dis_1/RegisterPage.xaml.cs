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
            // �������� ��������� �������� �� ������ �������
            selectedGender = SexPicker.Items[selectedIndex]; // "�" ��� "�"
        }
    }

    

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        
        // �������� ������ �� �����
        var name = NameEntry.Text;
        var experience = ExpEntry.Text;
        var age = AgeEntry.Text;
        var regNumber = RegNumberEntry.Text;
        var model = ModelEntry.Text;
        var rel_year = YearEntry.Text;
       

        // ���������� ������ (��������, ����� ���� �� ���� �������)
        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(experience) ||
            string.IsNullOrWhiteSpace(age) ||
            string.IsNullOrWhiteSpace(regNumber) ||
            string.IsNullOrWhiteSpace(model) ||
            string.IsNullOrWhiteSpace(rel_year) ||
            string.IsNullOrWhiteSpace(selectedGender))
        {
            await DisplayAlert("������", "��� ���� ������ ���� ���������.", "��");
            return;
        }

        var userData = new
        {   
           
            name = name,
            driving_exp = experience,
            age = age,
            sex = selectedGender
        };

        var carData = new
        {
            reg_number = regNumber,
            model = model,
            release_year = rel_year,

        };


        await RegisterUserAsync(userData);

        await AddCarAsync(carData);

        // ����� �������� ����������� ��������� ���������� � Preferences
        Preferences.Set("IsRegistered", true);

        // �������������� ������������ �� ������� ��������
        Application.Current.MainPage = new AppShell();
    }

    public async Task RegisterUserAsync(object userData)
    {  
            var client = new HttpClient();
            var json = System.Text.Json.JsonSerializer.Serialize(userData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{AppSettings.ServerUrl}/api/User", content);

    }

    public async Task AddCarAsync(object carData)
    {
        var client = new HttpClient();
        var json = System.Text.Json.JsonSerializer.Serialize(carData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"{AppSettings.ServerUrl}/api/CarData", content);
    }

    

}