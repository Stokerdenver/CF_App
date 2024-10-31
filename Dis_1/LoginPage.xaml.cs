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
        using (var client = new HttpClient())
        {
            try
            {
                var response = await client.GetAsync($"http://10.0.2.2:5000/api/User/{username}");

                // ���� ����� �������� (��� 200), ���������� true
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                // ���� ������� ��� 404, ���������� false (������������ �� ������)
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return false;
                }

                // ���� ��� ������ ���������� �� 200 � 404, ����������� ����������
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                await DisplayAlert("������", $"�� ������� ��������� ������: {ex.Message}", "��");
            }
            catch (Exception ex)
            {
                await DisplayAlert("������", $"��������� ������: {ex.Message}", "��");
            }

            return false;
        }
    }
}