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
    private HashSet<string> KnownDestinationDirs = new HashSet<string>();

    public CommandLineOptions Options { get; set; }

    public FileSorter(CommandLineOptions options)
    {
      Options = options;
    }

    public void Sort()
    {
      ProcessChildren(Options.Source);
    }

    public void ProcessChildren(string sourceDir)
    {
      // foreach (var dir in Directory.EnumerateDirectories(sourceDir))
      // {
      //   ProcessChildren(dir);
      // }
      foreach (var file in System.IO.Directory.EnumerateFiles(sourceDir))
      {
        var info = new FileInfo(file);
        Console.Write($"{Options.Mode} '{file}'");

        if (SkipFile(info)) continue;

        // Get date from photo EXIF data
        var directories = ImageMetadataReader.ReadMetadata(file);
        var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
        var exifDateTime = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTime) ??
          subIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);

        var photoDate = ParseExifDate(exifDateTime);
        if (photoDate < DateTime.Parse("1980-01-01"))
        {
          Console.WriteLine("... Skipped: File date too old");
          continue;
        }

        var destinationYearMonth = EnsurePath(photoDate);
        var destinationFile = Path.Combine(destinationYearMonth, info.Name);
        Console.Write($" to '{destinationFile}'... ");
        if (File.Exists(destinationFile))
        {
          Console.WriteLine("Skipped: already exists.");
        }
        else
        {
          info.CopyTo(destinationFile);
          Console.WriteLine("Done.");
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

    public DateTime ParseExifDate(string dateStr)
    {
      var segments = dateStr.Split(" ");
      return DateTime.Parse($"{segments[0].Replace(":", "-")} {segments[1]}");
    }

    public bool SkipFile(FileInfo info)
    {
      if (!ACCEPTED_EXTENSIONS.Contains(info.Extension, StringComparer.OrdinalIgnoreCase))
      {
        Console.WriteLine($"... Skipped: Unsupported extension: {info.Extension}");
        return true;
      }
      return false;
    }
  }
}