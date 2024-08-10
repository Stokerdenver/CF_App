using System.Text.Json;
using System.Text;


namespace Dis_1;

public partial class NewPage1 : ContentPage
{
    private bool _isSending = false;
    public string responseBody;
    public NewPage1()
	{
		InitializeComponent();

	}

    private async void OnStartButtonClicked(object sender, EventArgs e)
    {
        _isSending = !_isSending;
        StatusLabel.Text = _isSending ? "Status: Sending..." : "Status: Not sending";

        if (_isSending)
        {
            await SendDataAsync();
        }
    }

    private static readonly HttpClient _client = new HttpClient();
    
    private async Task SendDataAsync()
    {
  
        while (_isSending)
        {
            var data = new
            {
                Data = "Sample data from mobile app"
            };

            var json = JsonSerializer.Serialize(data);

           Jsonlabel.Text = json;

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                var response = await _client.PostAsync("http://10.0.2.2:5000/api/SensorData", content);
                response.EnsureSuccessStatusCode();
                responseBody = await response.Content.ReadAsStringAsync();

            }
            catch (HttpRequestException httpRequestException)
            {
                StatusLabel.Text = $"Request error: {httpRequestException.Message}";
            }
            catch (Exception ex) 
            {
                StatusLabel.Text = $"Error: {responseBody}";

            }

            await Task.Delay(1000); 
        }
    }


}