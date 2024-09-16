using Dis_1.Controller;

namespace Dis_1.View;

public partial class GPS_page : ContentPage
{
    private readonly LocationController _locationController;
   
    public GPS_page(LocationController locationController)
	{
		InitializeComponent();
        _locationController = locationController;
    }
   public GPS_page()
    {
        InitializeComponent();
    }
   
    private async void OnStartTrackingClicked(object sender, EventArgs e)
    {
        await _locationController.UpdateLocationAsync();
    }

}