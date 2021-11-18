using CommandLine;

namespace PhotoSort
{
    public class CommandLineOptions
    {
        [Option(shortName: 's', longName: "source", Required = true, HelpText = "Source directory to process")]
        public string Source { get; set; }

        [Option(shortName: 'd', longName: "destination", Required = true, HelpText = "Destination directory for the sorted files")]
        public string Destination { get; set; }

        [Option(shortName: 'm', longName: "mode", Required = false, HelpText = "Mode: 'copy' or 'move' files to destination", Default ="copy")]
        public string Mode { get; set; }
    }
}