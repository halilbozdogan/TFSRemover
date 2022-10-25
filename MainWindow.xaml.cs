using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Navigation;

namespace InstanceFactory.SccRemover
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    #region Public Delegates

    /// <summary>
    /// Logging delegate.
    /// </summary>
    /// <param name="strFormat">Format string</param>
    /// <param name="args">Output arguments.</param>
    public delegate void Logger
      (
      string strFormat,
      params object[] args
      );

    #endregion Public Delegates


    #region Public Properties

    /// <summary>
    /// Returns the name of the selected working directory.
    /// </summary>
    public string WorkingDir { get { return (WorkingDirectoryTextBox.Text); } }

    /// <summary>
    /// Returns the names of the user-defined subdirectories to be removed additionally.
    /// </summary>
    public string UserDefinedDirectories { get { return (UserDefinedDirectoriesTextBox.Text); } }

    #endregion Public Properties


    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of <see cref="MainWindow"/>.
    /// </summary>
    public MainWindow()
    {
      InitializeComponent();

      WorkingDirectoryTextBox.Text = Configuration.WorkingDir;

      UserDefinedDirectoriesTextBox.Text = Configuration.UserDefinedDirectories;
    }

    #endregion Public Constructors


    #region Protected Methods

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
      // Save the settings
      Configuration.SaveSettings(WorkingDir, UserDefinedDirectories);

      base.OnClosing(e);
    }

    #endregion Protected Methods


    #region Private Methods

    /// <summary>
    /// Invoked whe the user clicks on a hyperlink.
    /// </summary>
    /// <param name="sender">Event's sender.</param>
    /// <param name="args">Event arguments.</param>
    private void OnRequestNavigate
      (
      object sender,
      RequestNavigateEventArgs args
      )
    {
      // Launch the browser.
      Process.Start(args.Uri.ToString());
    }

    /// <summary>
    /// Invoked when the button to select a source directory is pressed.
    /// </summary>
    /// <param name="sender">Event's sender.</param>
    /// <param name="args">Event arguments.</param>
    private void OnSelectSourceDirectory
      (
      object sender,
      RoutedEventArgs args
      )
    {
      FolderBrowserDialog folderDialog = new FolderBrowserDialog();

      folderDialog.ShowNewFolderButton = true;
      folderDialog.SelectedPath = WorkingDir;

      if (folderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
      {
        return;
      }

      WorkingDirectoryTextBox.Text = folderDialog.SelectedPath;
    }

    /// <summary>
    /// Invoked when the button to select a source directory is pressed.
    /// </summary>
    /// <param name="sender">Event's sender.</param>
    /// <param name="args">Event arguments.</param>
    private async void OnStartRemove
      (
      object sender,
      RoutedEventArgs args
      )
    {
      // Yes, another "are you sure that" dialog.
      if (System.Windows.MessageBox.Show(this, "Please confirm the SCC removal by pressing the OK button.", "SCC Remover", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation)
        != MessageBoxResult.OK)
      {
        return;
      }

      OutputTextBox.Text = String.Empty;

      RemoveButton.IsEnabled = false;

      WriteLine("Start removing SCC content.");

      // Get values while process control is in main thread. Worker threads cannot access UI elements.
      string workingDir = WorkingDir;
      string directoriesToRemove = UserDefinedDirectories;

      // Start processing in a worker thread.
      await Task.Run(() => Remover.Remove(workingDir, directoriesToRemove, WriteLine));

      RemoveButton.IsEnabled = true;

      WriteLine("Removing SCC content finished");
    }

    /// <summary>
    /// Writes one line to the output control.
    /// </summary>
    /// <param name="strFormat">The format string.</param>
    /// <param name="args">The format string parameters.</param>
    private void WriteLine
      (
      string strFormat,
      params object[] args
      )
    {
      string formatString = strFormat;

      // Replace brackets when no parameter is passed.
      if (args.Length == 0)
      {
        formatString = formatString.Replace("{", "{{");
        formatString = formatString.Replace("}", "}}");
      }

      // Write output using dispatcher to be sure UI control can be accessed.
      Dispatcher.BeginInvoke(new Action(() => 
        {
          OutputTextBox.AppendText(String.Format("{0:HH:mm:ss.fffff} - {1}{2}", DateTime.Now, String.Format(formatString, args), Environment.NewLine));

          OutputTextBox.ScrollToEnd();
        }
        ));
    }

    #endregion Private Methods
  }
}
