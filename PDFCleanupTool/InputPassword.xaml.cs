using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PDFCleanupTool
{
    /// <summary>
    /// Interaction logic for InputPassword.xaml
    /// </summary>
    public partial class InputPassword : Window
    {
        public InputPassword()
        {
            InitializeComponent();
        }

        private void bttnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Password = txtPDFPassword.Text;
            this.Close();
        }

        public string Password { get; private set; } = "";

        private void bttnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Password = "";
            this.Close();
        }
    }
}
