using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PDFCleanupTool
{
    public class WorkItem : INotifyPropertyChanged
    {
        public string FileName { get; set; }
        string _status;
        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;

                if(value.IndexOf("Yet To Process") != -1)
                {
                    Color = Brushes.Turquoise;
                }
                else if (value.IndexOf("Error") != -1)
                {
                    Color = Brushes.Red;
                }
                else if (value.IndexOf("Processed") != -1)
                {
                    Color = Brushes.Green;
                }
                else
                {
                    Color = Brushes.Black;
                }

                NotifyPropertyChanged("Status");
            }
        }
        Brush _brush;
        public Brush Color
        {
            get { return _brush; }
            set
            {
                _brush = value;
                NotifyPropertyChanged("Color");
            }
        }
        public string InputFilePath { get; set; }
        public string OutputFilePath { get; set; }
        public PDFFileAction Action { get; set; }
        public string Password { get; set; } = "";

        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
