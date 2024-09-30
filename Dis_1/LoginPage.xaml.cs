namespace Dis_1;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
	}

    // �����, ������� ���������� ��� ������� �� ������ "�����������"
    private async void OnLoginConfirmClicked(object sender, EventArgs e)
    {
        
        
        var username = UsernameEntry.Text;

        // ���������, ������ �� ���
        if (string.IsNullOrWhiteSpace(username))
        {
            await DisplayAlert("������", "��� ������������ �� ����� ���� ������", "��");
            return;
        }

        // ����� ��������� ������ � API ��� �������� ������� ������������ � ���� ������
        bool userExists = await CheckUserExists(username);

        if (userExists)
        {
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
            var response = await client.GetAsync($"http://10.0.2.2:5000/api/User/{username}");
            return response.IsSuccessStatusCode; // ���� 200, ������������ ����������
        }
    }
}