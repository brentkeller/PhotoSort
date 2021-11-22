using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace PhotoSort
{

  public class FileSorter
  {

    // TODO: support video files
    private readonly string[] ACCEPTED_EXTENSIONS = { ".jpg", ".jpeg" };

    // Possibly unnecessary optimization to avoid IO for ensuring folders
    private HashSet<string> KnownDestinationDirs = new HashSet<string>();
    public string SessionId { get; private set; }
    public DateTime SortTime { get; private set; }
    private List<ProcessedItem> FailedItems = new List<ProcessedItem>(0);

    // TODO: Track count of files found/successfully processed

    public CommandLineOptions Options { get; set; }

    public FileSorter(CommandLineOptions options)
    {
      Options = options;
    }

    public void Sort()
    {
      SessionId = Guid.NewGuid().ToString("N");
      SortTime = DateTime.Now;
      ProcessChildren(Options.Source);
      LogFailureSummary();
    }

    private void LogFailureSummary()
    {
      WriteLog("These files couldn't be processed:", true);
      foreach (var item in FailedItems.OrderBy(x => x.Outcome).ThenBy(x => x.File.FullName))
      {
        WriteLog(item.GetSummaryMessage(), true);
      }
    }
    public void ProcessChildren(string sourceDir)
    {
      foreach (var dir in System.IO.Directory.EnumerateDirectories(sourceDir))
      {
        ProcessChildren(dir);
      }

      foreach (var file in System.IO.Directory.EnumerateFiles(sourceDir))
      {
        var info = new FileInfo(file);
        var result = new ProcessedItem(info);

        if (!ACCEPTED_EXTENSIONS.Contains(info.Extension, StringComparer.OrdinalIgnoreCase))
        {
          result.Outcome = SortOutcome.UnsupportedExtension;
          FailedItems.Add(result);
          WriteLog($"{Options.Mode} {result.GetLogMessage()}", true);
          continue;
        }

        // Get date from photo EXIF data
        var directories = ImageMetadataReader.ReadMetadata(file);
        var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
        var exifDateTime = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTime) ??
          subIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);

        var photoDate = ParseExifDate(exifDateTime);
        if (photoDate == null)
        {
          result.Outcome = SortOutcome.NoExifDate;
          FailedItems.Add(result);
          WriteLog($"{Options.Mode} {result.GetLogMessage()}", true);
          continue;
        }

        if (photoDate < DateTime.Parse("1980-01-01"))
        {
          result.Outcome = SortOutcome.TooOld;
          FailedItems.Add(result);
          WriteLog($"{Options.Mode} {result.GetLogMessage()}", true);
          continue;
        }

        var destinationYearMonth = EnsurePath(photoDate.Value);
        var destinationFile = Path.Combine(destinationYearMonth, info.Name);
        result.Destination = destinationFile;
        if (File.Exists(destinationFile))
        {
          result.Outcome = SortOutcome.AlreadyExists;
          FailedItems.Add(result);
          WriteLog($"{Options.Mode} {result.GetLogMessage()}", true);
        }
        else
        {
          if (string.Equals(Options.Mode, "copy", StringComparison.OrdinalIgnoreCase))
          {
            info.CopyTo(destinationFile);
          }
          else
          {
            info.MoveTo(destinationFile);
          }
          WriteLog($"{Options.Mode} {result.GetLogMessage()}", true);
        }
      }
    }

    public string EnsurePath(DateTime photoDate)
    {
      var destinationYearMonth = Path.Combine(Options.Destination, photoDate.Year.ToString(), photoDate.Month.ToString("00"));
      if (!KnownDestinationDirs.Contains(destinationYearMonth))
      {
        System.IO.Directory.CreateDirectory(destinationYearMonth);
        KnownDestinationDirs.Add(destinationYearMonth);
      }
      return destinationYearMonth;
    }

    public DateTime? ParseExifDate(string dateStr)
    {
      if (string.IsNullOrWhiteSpace(dateStr))
        return null;
      var segments = dateStr.Split(" ");
      return DateTime.Parse($"{segments[0].Replace(":", "-")} {segments[1]}");
    }

    public void WriteLog(string text, bool endLine = false)
    {
      var logFile = Path.Combine(Options.Destination, $"log-{SortTime.ToString("yyyyMMdd_hhmm")}-{SessionId}.log");
      if (endLine)
      {
        Console.WriteLine(text);
        using (StreamWriter sw = File.AppendText(logFile))
        {
          sw.WriteLine(text);
        }
      }
      else
      {
        Console.Write(text);
        using (StreamWriter sw = File.AppendText(logFile))
        {
          sw.Write(text);
        }
      }
    }
  }

  public enum SortOutcome
  {
    Succeeded,
    UnsupportedExtension,
    NoExifDate,
    TooOld,
    AlreadyExists,
  }

  public class ProcessedItem
  {
    public FileInfo File { get; set; }
    public SortOutcome Outcome { get; set; } = SortOutcome.Succeeded;
    public string Destination { get; set; }

    public ProcessedItem(FileInfo info)
    {
      File = info;
    }

    public string GetLogMessage()
    {
      var outcome = "";
      switch (Outcome)
      {
        case SortOutcome.UnsupportedExtension:
          outcome = $"Skipped: Unsupported extension: {File.Extension}";
          break;
        case SortOutcome.NoExifDate:
          outcome = "Skipped: No EXIF date";
          break;
        case SortOutcome.TooOld:
          outcome = "Skipped: File date too old";
          break;
        case SortOutcome.AlreadyExists:
          outcome = "Skipped: already exists.";
          break;
        case SortOutcome.Succeeded:
          outcome = "Done.";
          break;
      }
      return $"'{File.FullName}' {(string.IsNullOrWhiteSpace(Destination) ? "" : $"to '{Destination}'")}... {outcome}";
    }

    public string GetSummaryMessage()
    {
      return $"{Outcome.ToString()}: {File.FullName}";
    }
  }
}