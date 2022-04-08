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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.IO;
using System.Windows.Threading;

namespace PDFCleanupTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<WorkItem> _workItems;
        Task _bgTask;
        public MainWindow()
        {
            InitializeComponent();

            _workItems = new List<WorkItem>();
        }

        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            if(e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var droppedFiles = e.Data.GetData(DataFormats.FileDrop) as string[];
                string outputFodler = null;
                bool isSameFolderSelected = false;
                using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    dialog.Description = "Select Output Folder";
                    dialog.ShowNewFolderButton = true;
                    if(droppedFiles.Length > 0)
                    {
                        dialog.SelectedPath = System.IO.Path.GetDirectoryName(droppedFiles[0]); ;
                    }
                    do
                    {
                        string selectedPath = dialog.SelectedPath;
                        System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                        if (result == System.Windows.Forms.DialogResult.OK)
                        {
                            outputFodler = dialog.SelectedPath;
                            isSameFolderSelected = dialog.SelectedPath == selectedPath;
                            break;
                        }
                    } while (true);
                }
                FillWorkItems(droppedFiles,outputFodler, isSameFolderSelected);
                wrkProgress.Minimum = 0;
                wrkProgress.Maximum = _workItems.Count + 1;
                wrkProgress.Value = wrkProgress.Minimum;
                progressLbl.Content = "";
                userHintLabel.Visibility = Visibility.Collapsed;
                fileListBox.ItemsSource = null;
                fileListBox.ItemsSource = _workItems;
            }
        }

        private void FillWorkItems(string[] listOfFiles, string outputFolder,bool isSameFolderSelected)
        {
            string outFilePathFmt = isSameFolderSelected ? "{0}\\{1}_Processed.pdf" : "{0}\\{1}.pdf";
            _workItems.AddRange((from t in listOfFiles where System.IO.Path.GetExtension(t).ToUpper() == ".PDF" select new WorkItem { FileName = System.IO.Path.GetFileName(t), Action = PDFFileAction.None, InputFilePath = t, OutputFilePath = string.Format(outFilePathFmt, outputFolder, System.IO.Path.GetFileNameWithoutExtension(t)), Status = "Yet To Process" }).ToList());
        }
        private void bttnRemoveSign_Click(object sender, RoutedEventArgs e)
        {
            if (_workItems.Count > 0)
            {
                foreach (WorkItem item in _workItems)
                {
                    item.Action = PDFFileAction.RemoveSign;
                    if (chkPasswordFile.IsChecked ?? false)
                    {
                        InputPassword passwordForm = new InputPassword();

                        passwordForm.ShowDialog();

                        if (!string.IsNullOrWhiteSpace(passwordForm.Password))
                        {
                            item.Password = passwordForm.Password;
                        }
                    }
                }
            }

            _bgTask = Task.Factory.StartNew(() =>
            {
                if (_workItems.Count > 0)
                {
                    ProcessPDFFiles();
                }
            });
        }

        private void ProcessPDFFiles()
        {
            int itemCount = 0;
            foreach (WorkItem item in _workItems)
            {
                ++itemCount;
                this.Dispatcher.Invoke(new Action(() => {
                    wrkProgress.Value = itemCount;
                    progressLbl.Content = "Started";
                    progressLbl.Foreground = Brushes.Red;
                }), DispatcherPriority.Normal);

                var task = Task.Factory.StartNew(() =>
                {
                    switch (item.Action)
                    {
                        case PDFFileAction.RemoveSign:
                            RemoveSign(item);
                            break;
                        case PDFFileAction.RemovePassword:
                            RemovePassword(item);
                            break;
                    }
                });

                task.Wait();
            }

            Application.Current.Dispatcher.Invoke(new Action(() => {
                progressLbl.Content = "Completed";
                progressLbl.Foreground = Brushes.Blue;
                wrkProgress.Value = wrkProgress.Maximum;
            }));
        }

        private void RemoveSign(WorkItem item)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(item.Password))
                {
                    using (PdfStamper pdfDocument = new PdfStamper(new PdfReader(item.InputFilePath), new FileStream(item.OutputFilePath, FileMode.Create)))
                    {
                        pdfDocument.FormFlattening = true;
                    }
                }
                else
                {
                    // Convert the string into a byte array.
                    byte[] unicodeBytes = Encoding.Unicode.GetBytes(item.Password);

                    // Perform the conversion from one encoding to the other.
                    byte[] asciiBytes = Encoding.Convert(Encoding.Unicode, Encoding.ASCII, unicodeBytes);

                    using (PdfStamper pdfDocument = new PdfStamper(new PdfReader(item.InputFilePath, asciiBytes), new FileStream(item.OutputFilePath, FileMode.Create)))
                    {
                        pdfDocument.FormFlattening = true;
                    }
                }

                item.Status = "Processed";
            }
            catch(Exception ex)
            {
                item.Status = string.Format("Error: {0}", ex.Message);
            }
        }

        private void RemovePassword(WorkItem item)
        {
            try
            {

                using (Document pdfDocument = new Document())
                using (PdfWriter writer = PdfWriter.GetInstance(pdfDocument, new FileStream(item.OutputFilePath, FileMode.Create)))
                {
                    pdfDocument.Open();
                    PdfContentByte pdfContentBytes = writer.DirectContent;
                    if (string.IsNullOrWhiteSpace(item.Password))
                    {
                        using (PdfReader inputPdf = new PdfReader(item.InputFilePath))
                        {

                            int noOfPages = inputPdf.NumberOfPages;
                            PdfImportedPage page;
                            for (int pageNum = 1; pageNum <= noOfPages; pageNum++)
                            {
                                //var pageSize = inputPdf.GetPageSize(pageNum);
                                //pdfDocument.SetPageSize(pageSize);
                                page = writer.GetImportedPage(inputPdf, pageNum);
                                pdfDocument.SetPageSize(new iTextSharp.text.Rectangle(page.Width, page.Height));
                                pdfDocument.NewPage();
                                pdfContentBytes.AddTemplate(page, 0, 0);
                            }
                            writer.Flush();
                            pdfDocument.Close();
                        }
                    }
                    else
                    {
                        // Convert the string into a byte array.
                        byte[] unicodeBytes = Encoding.Unicode.GetBytes(item.Password);

                        // Perform the conversion from one encoding to the other.
                        byte[] asciiBytes = Encoding.Convert(Encoding.Unicode, Encoding.ASCII, unicodeBytes);

                        using (PdfReader inputPdf = new PdfReader(item.InputFilePath, asciiBytes))
                        {

                            int noOfPages = inputPdf.NumberOfPages;
                            PdfImportedPage page;
                            for (int pageNum = 1; pageNum <= noOfPages; pageNum++)
                            {
                                //var pageSize = inputPdf.GetPageSize(pageNum);
                                //pdfDocument.SetPageSize(pageSize);
                                page = writer.GetImportedPage(inputPdf, pageNum);
                                pdfDocument.SetPageSize(new iTextSharp.text.Rectangle(page.Width, page.Height));
                                pdfDocument.NewPage();
                                pdfContentBytes.AddTemplate(page, 0, 0);
                            }
                            writer.Flush();
                            pdfDocument.Close();
                        }
                    }
                }

                item.Status = "Processed";
            }
            catch (Exception ex)
            {
                item.Status = string.Format("Error: {0}", ex.Message);
            }
        }

        private void bttnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void bttnRemovePassword_Click(object sender, RoutedEventArgs e)
        {
            foreach (WorkItem item in _workItems)
            {
                item.Action = PDFFileAction.RemovePassword;
                if (chkPasswordFile.IsChecked ?? false)
                {
                    InputPassword passwordForm = new InputPassword();

                    passwordForm.ShowDialog();

                    if (!string.IsNullOrWhiteSpace(passwordForm.Password))
                    {
                        item.Password = passwordForm.Password;
                    }
                }
            }

            _bgTask = Task.Factory.StartNew(() =>
            {
                if (_workItems.Count > 0)
                {
                    ProcessPDFFiles();
                }
            });
        }

        private void bttnClear_Click(object sender, RoutedEventArgs e)
        {
            _workItems.Clear();
        }
    }
}
