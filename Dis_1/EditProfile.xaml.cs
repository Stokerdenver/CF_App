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
       
}
