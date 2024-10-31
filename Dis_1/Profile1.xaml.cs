
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
    // Выбираем дефолтную машину
    public void SetDefaultCar()
    {
        var defaultCar = user.Cars.FirstOrDefault();
        if (defaultCar != null)
        {
            TestLabel.Text = "Машины не найдены";
        }
        TestLabel.Text = $"Рег. номер: {defaultCar.reg_number}\n" +
                            $"Модель: {defaultCar.model}\n" +
                            $"Год выпуска: {defaultCar.release_year}"; 
    }

    public async void LoadUserData()
    {
        
        user = await GetUserDataFromServer(UserName);


        // Проверка, есть ли автомобили в коллекции
        if (user.Cars != null && user.Cars.Count > 0)
        {
            CarPicker.Items.Clear();
            // Заполняем Picker автомобилями пользователя
            foreach (var car in user.Cars)
            {               
                CarPicker.Items.Add(car.model);
            }
            SetDefaultCar();
        }
        else
        {
            await DisplayAlert("Ошибка", "Машины не найдены", "ОК");
        }


        if (user != null)
        {   
            // Привязка данных к интерфейсу
            this.BindingContext = user;
            
        }
        else
        {
            await DisplayAlert("Ошибка", "Данные пользователя не найдены", "ОК");
        }
        
    }

    private void OnCarSelected(object sender, EventArgs e)
    {   


        int selectedIndex = CarPicker.SelectedIndex;

        if (selectedIndex != -1) // Проверяем, что выбор был сделан
        {
            // Получаем выбранный автомобиль по индексу
            var selectedCar = user.Cars[selectedIndex];

            // Обновляем выбранный автомобиль в статическом классе
            CurrentCar.SelectedCar = selectedCar;

            // Устанавливаем BindingContext для элементов отображения данных автомобиля
            BindingContext = selectedCar;

            // Отображаем данные о выбранной машине
            TestLabel.Text = $"Рег. номер: {selectedCar.reg_number}\n" +
                            $"Модель: {selectedCar.model}\n" +
                            $"Год выпуска: {selectedCar.release_year}";
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