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

        // ���������, ������ �� ���
        if (string.IsNullOrWhiteSpace(username))
        {
            await DisplayAlert("������", "��� ������������ �� ����� ���� ������", "��");
            return;
        }

        // ������ � API ��� �������� ������� ������������ � ���� ������
        bool userExists = await CheckUserExists(username);

        if (userExists)
        {

            // ���������� ���������
            //  MessagingCenter.Send(this, "UserLoggedIn");
          /*  var profilePage = new Profile1(username);
            profilePage.LoadUserData();
          */ 
            // ���� ������������ ������, ��������� �� ������� ��������
            Application.Current.MainPage = new AppShell();

          

        }
        else
        {
            // ���� ������������ �� ������, ������� ������
            await DisplayAlert("������", "������������ �� ������", "��");
        }

        
    }

    // ����� ��� �������� ������� ������������ ����� ������ � API
    private async Task<bool> CheckUserExists(string username)
    {
        // ������ ������� � ������ WebAPI
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