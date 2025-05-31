using System.Collections.ObjectModel;

namespace Dis_1;

public partial class EditProfile : ContentPage
{

    private ObservableCollection<string> cars = new ObservableCollection<string>();
    public EditProfile()
	{
		InitializeComponent();
	}


    private void OnAddCarButtonClicked(object sender, EventArgs e)
    { }
    private void OnSaveChanges(object sender, EventArgs e)
    {
        Application.Current.MainPage = new Profile1();
    }
    private async void OnBackStepClicked(object sender, EventArgs e)
    {
        Application.Current.MainPage = new AppShell();           // :contentReference[oaicite:1]{index=1}

        // 2) переходим по абсолютному маршруту в нужную вкладку/страницу
        //    "profile" — это значение атрибута Route у ShellContent/Tab
        await Shell.Current.GoToAsync("//profile");
    }
    private void OnBackButtonClicked(object sender, EventArgs e)
    {
        Application.Current.MainPage = new AppShell();
    }

}
