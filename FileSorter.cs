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

    private readonly string[] ACCEPTED_EXTENSIONS = { ".jpg", ".jpeg" };

    // Possibly unnecessary optimization to avoid IO for ensuring folders
    private HashSet<string> KnownDestinationDirs = new HashSet<string>();
    public string SessionId { get; set; }

    public CommandLineOptions Options { get; set; }

    public FileSorter(CommandLineOptions options)
    {
      Options = options;
    }

    public void Sort()
    {
      SessionId = Guid.NewGuid().ToString("N");
      ProcessChildren(Options.Source);
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
        WriteLog($"{Options.Mode} '{file}'");

        if (SkipFile(info)) continue;

        // Get date from photo EXIF data
        var directories = ImageMetadataReader.ReadMetadata(file);
        var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
        var exifDateTime = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTime) ??
          subIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);

        var photoDate = ParseExifDate(exifDateTime);
        if (photoDate == null)
        {
          WriteLog("... Skipped: No EXIF date", true);
          continue;
        }

        if (photoDate < DateTime.Parse("1980-01-01"))
        {
          WriteLog("... Skipped: File date too old", true);
          continue;
        }

        var destinationYearMonth = EnsurePath(photoDate.Value);
        var destinationFile = Path.Combine(destinationYearMonth, info.Name);
        WriteLog($" to '{destinationFile}'... ");
        if (File.Exists(destinationFile))
        {
          WriteLog("Skipped: already exists.", true);
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
          WriteLog("Done.", true);
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

    public bool SkipFile(FileInfo info)
    {
      if (!ACCEPTED_EXTENSIONS.Contains(info.Extension, StringComparer.OrdinalIgnoreCase))
      {
        WriteLog($"... Skipped: Unsupported extension: {info.Extension}", true);
        return true;
      }
      return false;
    }

    public void WriteLog(string text, bool endLine = false)
    {
      var logFile = Path.Combine(Options.Destination, $"log-{DateTime.Now.ToString("yyyyMMdd_hhmm")}-{SessionId}.log");
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
}