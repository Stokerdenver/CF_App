
namespace Dis_1;
using Dis_1.Model;
using Newtonsoft.Json;

public partial class Profile1 : ContentPage
{
    public string UserName { get; set; }
    public UserC user;

    public Profile1() 
    {
        InitializeComponent();

        UserName = Preferences.Get("UserLogin", string.Empty);

        LoadUserData();
        
    }
    // �������� ��������� ������
    public void SetDefaultCar()
    {
        var defaultCar = user.Cars.FirstOrDefault();
        if (defaultCar != null)
        {
            TestLabel.Text = "������ �� �������";
        }
        TestLabel.Text = $"���. �����: {defaultCar.reg_number}\n" +
                            $"������: {defaultCar.model}\n" +
                            $"��� �������: {defaultCar.release_year}"; 
    }

    public async void LoadUserData()
    {
        
        user = await GetUserDataFromServer(UserName);


        // ��������, ���� �� ���������� � ���������
        if (user.Cars != null && user.Cars.Count > 0)
        {
            CarPicker.Items.Clear();
            // ��������� Picker ������������ ������������
            foreach (var car in user.Cars)
            {               
                CarPicker.Items.Add(car.model);
            }
            SetDefaultCar();
        }
        else
        {
            await DisplayAlert("������", "������ �� �������", "��");
        }


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

    private void OnCarSelected(object sender, EventArgs e)
    {   


        int selectedIndex = CarPicker.SelectedIndex;

        if (selectedIndex != -1) // ���������, ��� ����� ��� ������
        {
            // �������� ��������� ���������� �� �������
            var selectedCar = user.Cars[selectedIndex];

            // ��������� ��������� ���������� � ����������� ������
            CurrentCar.SelectedCar = selectedCar;

            // ������������� BindingContext ��� ��������� ����������� ������ ����������
            BindingContext = selectedCar;

            // ���������� ������ � ��������� ������
            TestLabel.Text = $"���. �����: {selectedCar.reg_number}\n" +
                            $"������: {selectedCar.model}\n" +
                            $"��� �������: {selectedCar.release_year}";
        }
    }


    public async Task<UserC> GetUserDataFromServer(string userName)
    {
        var client = new HttpClient();
        var response = await client.GetStringAsync($"http://10.0.2.2:5000/api/User/{userName}");
        var user = JsonConvert.DeserializeObject<UserC>(response);
        return user;
        
    }

    private void EditProfileButton(object sender, EventArgs e)
    {
        Application.Current.MainPage = new EditProfile();
    }

    private void AddCarButton(object sender, EventArgs e)
    {
        Application.Current.MainPage = new AddCarPage();
    }
}