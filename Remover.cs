#region Using

using System;
using System.IO;
using System.Linq;

#endregion Using


namespace InstanceFactory.SccRemover
{
  /// <summary>
  /// Class to do the SCC removement.
  /// </summary>
  public class Remover
  {
    #region Private Properties

    /// <summary>
    /// Gets / sets the working directory.
    /// </summary>
    private string WorkingDir { get; set; }

    /// <summary>
    /// Gets / sets the user-defined subdirectories to be removed.
    /// </summary>
    private string UserDefinedDirectories { get; set; }

    /// <summary>
    /// Gets / sets the logger to be used for output messages.
    /// </summary>
    private MainWindow.Logger Logger { get; set; }

    #endregion Private Properties


    #region Private Constructors

    /// <summary>
    /// Initialisiert eine neue Instanz von <see cref="Remover"/>.
    /// </summary>
    /// <param name="workingDir">The path pf the working directory, containing the VS solution to be processed.</param>
    /// <param name="userDefinedDirectories">The list of the user-defined subdirectories to be deleted.</param>
    /// <param name="logger">Logger for trace output.</param>
    private Remover
      (
      string workingDir,
      string userDefinedDirectories,
      MainWindow.Logger logger
      )
    {
      WorkingDir = workingDir;

      UserDefinedDirectories = userDefinedDirectories;

      Logger = logger;
    }

    #endregion Private Constructors


    #region Private Methods

    /// <summary>
    /// Controls the removal process.
    /// </summary>
    private void Process()
    {
      try
      {
        Logger("Starting processing");

        // Delete the files
        DeleteFiles();

        // Delete the default subdirectories
        DeleteDirectories(Configuration.DefaultDirectories);

        // Delete the user-defined subdirectories
        DeleteDirectories(UserDefinedDirectories);

        // Remove SCC footprint from project files
        RemoveSccFromProjects();

        // Remove SCC footprint from solution files
        RemoveSccFromSolutions();

        Logger("Finished processing");
      }
      catch (Exception e)
      {
        Logger("Exception caught: {0}{1}", Environment.NewLine, e);
      }
    }

    /// <summary>
    /// Controls the removal of the SCC footprint from all solution files of the working directory and its subdirectories.
    /// </summary>
    private void RemoveSccFromSolutions()
    {
      // Get all solution files (hopefully ;-) )
      string[] solutionFileList = Directory.GetFiles(WorkingDir, "*.sln", SearchOption.AllDirectories);

      Logger("Removing SCC footprint from solution files, {0} file(s) found", solutionFileList.Length);

      foreach (string solutionFile in solutionFileList)
      {
        RemoveSccfromSolution(solutionFile);
      }
    }

    /// <summary>
    /// Removes the SCC footprint from project files.
    /// </summary>
    /// <param name="solutionFile">Name of the solution file to be processed.</param>
    private void RemoveSccfromSolution
      (
      string solutionFile
      )
    {
      Logger("Attempt to remove SCC footprint from solution file >{0}<", solutionFile);

      // Flag to signal if file content was changed
      bool sccFootprintRemoved = false;

      // Flag to signal if we are currently processing the TFS Version Control section.
      bool processingTfsVcSection = false;

      // Keep changed file in memory
      using (MemoryStream memoryStream = new MemoryStream())
      {
        using (StreamWriter writer = new StreamWriter(memoryStream))
        {
          using (StreamReader reader = new StreamReader(solutionFile))
          {
            string line;
            int lineCount = 0;

            while ((line = reader.ReadLine()) != null)
            {
              ++lineCount;

              // Check if current line is the start of the TFS VS section
              if (line.Contains("GlobalSection(TeamFoundationVersionControl)"))
              {
                Logger("SCC footprint found in line {0}: >{1}<", lineCount, line);
                // Set flags
                sccFootprintRemoved = true;
                processingTfsVcSection = true;
                // don't write line to output
                continue;
              }

              // Check if current line is the last of the TFS VC section
              if (processingTfsVcSection
                && line.Contains("EndGlobalSection"))
              {
                Logger("End of SCC footprint found in line {0}: >{1}<", lineCount, line);
                // Reset flag
                processingTfsVcSection = false;
                // but do not pass the line to the output
                continue;
              }

              // If we are processing the TFS VC section, no line is passed to the output
              if (processingTfsVcSection)
              {
                Logger("Removing SCC footpring line {0}: >{1}<", lineCount, line);
                // read the next line
                continue;
              }

              // Currentline does not contain SCC footprint, so write it to output
              writer.WriteLine(line);
            }

            reader.Close();
          }

          // We're finished with scanning the file; write it to disk if it was changed
          WriteProcessedFile(solutionFile, sccFootprintRemoved, writer);
        }
      }
    }

    /// <summary>
    /// Writes the process file to disk, in case any SCC footprint was removed.
    /// </summary>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="sccFootprintRemoved">Flag indicating if footprint was removed (<c>true</c>) or not (<c>false</c>).</param>
    /// <param name="outputStream">Memorystream containg the data to be written to disk.</param>
    private void WriteProcessedFile
      (
      string fileName,
      bool sccFootprintRemoved,
      StreamWriter outputStream
      )
    {
      if (!sccFootprintRemoved)
      {
        Logger("File >{0}< does not contain SCC footprint.", fileName);
        return;
      }

      // Flush the output to make sure it will be writte to disk.
      outputStream.Flush();

      // The file contained SCC footrpint, so override the exisiting file with the changed content
      using (FileStream fileStream = new FileStream(fileName, FileMode.Truncate))
      {
        fileStream.Write(((MemoryStream) outputStream.BaseStream).ToArray(), 0, (int)outputStream.BaseStream.Length);
      }

      Logger("SCC footprint removed from file >{0}<", fileName);
    }

    /// <summary>
    /// Controls the removal of the SCC footprint from all project files of the working directory and its subdirectories.
    /// </summary>
    private void RemoveSccFromProjects()
    {
      // Get all project files (hopefully ;-), don't know all project file type extensions )
      string[] projectFileList = Directory.GetFiles(WorkingDir, "*.*proj", SearchOption.AllDirectories);

      Logger("Removing SCC footprint from project files, {0} file(s) found", projectFileList.Length);

      foreach(string projectFile in projectFileList)
      {
        RemoveSccFromProject(projectFile);
      }
    }

    /// <summary>
    /// Removes the SCC footprint from project files.
    /// </summary>
    /// <param name="projectFile">Name of the project file to be processed.</param>
    private void RemoveSccFromProject
      (
      string projectFile
      )
    {
      Logger("Attempt to remove SCC footprint from project file >{0}<", projectFile);
      
      // Flag to signal if file content was changed
      bool sccFootprintRemoved = false;

      // Keep changed file in memory
      using (MemoryStream memoryStream = new MemoryStream())
      {
        using (StreamWriter writer = new StreamWriter(memoryStream))
        {
          using (StreamReader reader = new StreamReader(projectFile))
          {
            string line;
            int lineCount = 0;

            while ((line = reader.ReadLine()) != null)
            {
              ++lineCount;
              // Check if current line is SCC footprint
              if (line.TrimStart().ToLower().StartsWith("<scc"))
              {
                Logger("SCC footprint found in line {0}: >{1}<", lineCount, line);
                sccFootprintRemoved = true;
                // don't write line to output
                continue;
              }
              // Currentline does not contain SCC footprint, so write it to output
              writer.WriteLine(line);
            }

            reader.Close();
          }

          // We're finished with scanning the file; write it to disk if it was changed
          WriteProcessedFile(projectFile, sccFootprintRemoved, writer);
        }
      }
    }

    /// <summary>
    /// Deletes the files.
    /// </summary>
    private void DeleteFiles()
    {
      // Delete the default filetypes
      string[] fileTypeArray = Configuration.DefaultFileTypes.Split(Configuration.ListSeparator);

      Logger("Deleting files from list >{0}<, containing {1} entries", Configuration.DefaultFileTypes, fileTypeArray.Length);

      foreach (string fileType in fileTypeArray)
      {
        DeleteFilesOfType(fileType);
      }
    }

    /// <summary>
    /// Deletes all subdirectories contained in the list.
    /// </summary>
    /// <param name="directoryList">List of directory names to be deleted; seperated by semicolon.</param>
    private void DeleteDirectories
      (
      string directoryList
      )
    {
      // Delete the subdirectories
      string[] directoryArray = directoryList.Split(Configuration.ListSeparator);

      Logger("Deleting subdirectories from list >{0}<, containing {1} entries", directoryList, directoryArray.Length);

      foreach (string directory in directoryArray)
      {
        DeleteDirectory(directory);
      }
    }

    /// <summary>
    /// Deletes all files of a specific type from the working directory.
    /// </summary>
    /// <param name="fileType">Type of Files to be removed.</param>
    private void DeleteFilesOfType
      (
      string fileType
      )
    {
      Logger("Attempt to delete files of type >{0}<", fileType);

      string[] fileList = Directory.GetFiles(WorkingDir, String.Format("*.{0}", fileType), SearchOption.AllDirectories);

      Logger("{0} file(s) of type >{1}< found", fileList.Length, fileType);

      foreach (string file in fileList)
      {
        Logger("Deleting file >{0}<", file);
        File.Delete(file);
      }
    }

    /// <summary>
    /// Deletes a directory with all its content.
    /// </summary>
    /// <param name="directoryName">Name of the directory to be deleted.</param>
    private void DeleteDirectory
      (
      string directoryName
      )
    {
      Logger("Attempt to delete directories named >{0}<", directoryName);

      // Make sure to rmove white space!
      string localName = directoryName.Trim();

      string[] directoryList = Directory.GetDirectories(WorkingDir, localName, SearchOption.AllDirectories);

      Logger("{0} directorie(s) named >{1}< found", directoryList.Length, localName);

      foreach (string directory in directoryList)
      {
        Logger("Deleting directory >{0}<, including subdirectories", directory);

        // The subdirectory might be deleted already by deleting its parent.
        if (Directory.Exists(directory))
        {
          Directory.Delete(directory, true);
        }
      }
    }

    #endregion Private Methods


    #region Public Static Methods

    /// <summary>
    /// Starts to remove the SCC footprint from working directory.
    /// </summary>
    /// <param name="workingDir">Working directory.</param>
    /// <param name="directoriesToRemove">Additions sub-directories to remove.</param>
    public static void Remove
      (
      string workingDir,
      string directoriesToRemove,
      MainWindow.Logger logger
      )
    {
      Remover remover = new Remover(workingDir, directoriesToRemove, logger);

      remover.Process();
    }

    #endregion Public Static Methods
  }
}
