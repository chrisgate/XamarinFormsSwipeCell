using Xamarin.Forms;

namespace CustomCell
{
    public partial class CustomCellPage : ContentPage
    {
        public CustomCellPage()
        {
            InitializeComponent();

            listView.ItemsSource = DataSource.GetList();
        }
    }
}
