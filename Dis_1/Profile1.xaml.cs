
namespace Dis_1;
using Dis_1.Model;
using Newtonsoft.Json;

public partial class Profile1 : ContentPage
{
    public string UserName { get; set; }
    public Profile1()
	{
		InitializeComponent();
        
        UserName = "stoker";
        LoadUserData();
    }

    public async void LoadUserData()
    {
        // ������ ������� � ������ Web API
        var user = await GetUserDataFromServer(UserName);
        

        if (user != null)
        {
            // �������� ������ � ����������
            this.BindingContext = user;
        }
        else
        {
            await DisplayAlert("������", "������ ������������ �� �������", "��");
        }
    }

    private async Task<UserC> GetUserDataFromServer(string userName)
    {
        var client = new HttpClient();
        var response = await client.GetStringAsync($"http://10.0.2.2:5000/api/User/{userName}");
        var user = JsonConvert.DeserializeObject<UserC>(response);
        return user;       
    }
}