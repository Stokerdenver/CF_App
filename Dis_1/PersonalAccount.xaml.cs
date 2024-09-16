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

        // �������� �� �������
        if (!string.IsNullOrWhiteSpace(carRegistration))
        {
            // ��������� ���������� � ������
            cars.Add(carRegistration);

            // ������� ���� �����
            CarRegistrationEntry.Text = string.Empty;
        }
        else
        {
            DisplayAlert("������", "������� ��������������� ����� ����������", "OK");
        }
    }
}
