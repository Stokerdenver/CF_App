namespace Dis_1;
public partial class HelloPage : ContentPage
{
	public HelloPage()
	{
		InitializeComponent();
	}

    private async void OnLoginClicked(object sender, EventArgs e)
    {

        Console.WriteLine("������ ������");
        // ������� �� �������� �����
        Application.Current.MainPage = new LoginPage();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        // ������� �� �������� �����������
        Application.Current.MainPage = new RegisterPage();
    }
}