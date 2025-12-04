using NutriPlanner.Models;
using NutriPlanner.ViewModels;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NutriPlanner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Конструктор без параметров (для дизайнеров и совместимости)
        public MainWindow()
        {
            InitializeComponent();
            // Можно поставить пустую ViewModel или заглушку
            DataContext = new MainViewModel(null);
        }

        // Конструктор с пользователем
        public MainWindow(User user)
        {
            InitializeComponent();
            DataContext = new MainViewModel(user);
        }
    }
}