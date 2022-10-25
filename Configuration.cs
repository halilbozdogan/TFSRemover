#region Using

using System;
using System.Configuration;
using Config = System.Configuration;

#endregion Using


namespace InstanceFactory.SccRemover
{
  /// <summary>
  /// Handels all configuration access.
  /// </summary>
  public static class Configuration
  {
    #region Private Const Data Member

    /// <summary>
    /// The key of the appSettings working directory value.
    /// </summary>
    private const string KeyWorkingDir = "WorkingDir";

    /// <summary>
    /// The key of the appSettings of the user-defined subdirectories to be removed.
    /// </summary>
    private const string KeyUserDefinedDirectories = "UserDefinedDirectories";

    /// <summary>
    /// The key of the appSettings value containing the default file types to be deleted.
    /// </summary>
    private const string KeyDefaultFileTypes = "DefaultFileTypes";

    /// <summary>
    /// The key of the appSettings value containing the default directories to be deleted.
    /// </summary>
    private const string KeyDefaultDirectories = "DefaultDirectories";

    #endregion Private Const Data Member


    #region Public Const Data Member

    /// <summary>
    /// The separator charactar of lists in strings.
    /// </summary>
    public const char ListSeparator = ';';

    #endregion Public Const Data Member


    #region Public Static Properties

    /// <summary>
    /// Returns the configured name of the working directory.
    /// </summary>
    public static string WorkingDir { get { return (ConfigurationManager.AppSettings[Configuration.KeyWorkingDir]); } }

    /// <summary>
    /// Returns the configured list of the user-defined subdirectories of the working directory to be removed, seperated by semicolon.
    /// </summary>
    public static string UserDefinedDirectories { get { return (ConfigurationManager.AppSettings[Configuration.KeyUserDefinedDirectories]); } }

    /// <summary>
    /// Returns the configured list of the default subdirectories of the working directory to be removed, seperated by semicolon.
    /// </summary>
    public static string DefaultDirectories { get { return (ConfigurationManager.AppSettings[Configuration.KeyDefaultDirectories]); } }

    /// <summary>
    /// Returns the configured list of the default filetypes of the working directory to be removed, seperated by semicolon.
    /// </summary>
    public static string DefaultFileTypes { get { return (ConfigurationManager.AppSettings[Configuration.KeyDefaultFileTypes]); } }

    #endregion Public Static Properties


    #region Public Static Methods

    /// <summary>
    /// Saves the configuration.
    /// </summary>
    /// <param name="workingDir">The working directory to be saved.</param>
    /// <param name="userDefinedDirectories">The list of the user-defined subdirectories that should be removed too, to be saved.</param>
    public static void SaveSettings
      (
      string workingDir,
      string userDefinedDirectories
      )
    {
      Config.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

      // Remove old values
      config.AppSettings.Settings.Remove(Configuration.KeyWorkingDir);
      config.AppSettings.Settings.Remove(Configuration.KeyUserDefinedDirectories);

      // Set new value
      config.AppSettings.Settings.Add(Configuration.KeyWorkingDir, workingDir);
      config.AppSettings.Settings.Add(Configuration.KeyUserDefinedDirectories, userDefinedDirectories);

      // Save the changes.
      config.Save(ConfigurationSaveMode.Modified);
    }

    #endregion Public Static Methods

  }
}
