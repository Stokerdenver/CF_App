using System.Text.Json;
using System.Text;
using System.Net.NetworkInformation;

namespace Dis_1;

public partial class NewPage2 : ContentPage
{

    private HttpClient _client;
    public NewPage2()
	{
		InitializeComponent();
        _client = new HttpClient();
    }

    private async void OnSendButtonClicked(object sender, EventArgs e)
    {
        await SendDataAsync();
    }

    private async Task SendDataAsync()
    {
        var data = new
        {
            Data = "Sample data from mobile app"
        };

        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            // Проверка доступности сервера с помощью HTTP-запроса
            var response = await _client.GetAsync("http://192.168.0.105:5000");
            response.EnsureSuccessStatusCode();

            // Отправка данных на сервер
            var postResponse = await _client.PostAsync("http://192.168.0.105:5000/api/SensorData", content);
            postResponse.EnsureSuccessStatusCode();

            // Обработка успешного ответа
            var responseBody = await postResponse.Content.ReadAsStringAsync();
            StatusLabel.Text = $"Data sent successfully! Server response: {responseBody}";
        }
        catch (HttpRequestException ex)
        {
            StatusLabel.Text = $"Request error: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
        }
    }

}
   



