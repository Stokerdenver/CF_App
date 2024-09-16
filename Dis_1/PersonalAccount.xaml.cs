using System.Collections.ObjectModel;

namespace Dis_1;

public partial class PersonalAccount : ContentPage
{

    private ObservableCollection<string> cars = new ObservableCollection<string>();
    public PersonalAccount()
	{
		InitializeComponent();
	}


    private void OnAddCarButtonClicked(object sender, EventArgs e)
    {
        string carRegistration = CarRegistrationEntry.Text;

        // Проверка на пустоту
        if (!string.IsNullOrWhiteSpace(carRegistration))
        {
            // Добавляем автомобиль в список
            cars.Add(carRegistration);

            // Очищаем поле ввода
            CarRegistrationEntry.Text = string.Empty;
        }
        else
        {
            DisplayAlert("Ошибка", "Введите регистрационный номер автомобиля", "OK");
        }
    }
}
