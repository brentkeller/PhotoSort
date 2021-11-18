using System;
using System.Threading.Tasks;
using CommandLine;

namespace PhotoSort
{
  class Program
  {
    static int Main(string[] args)
    {
      return Parser.Default.ParseArguments<CommandLineOptions>(args)
        .MapResult(
            options => RunAndReturnExitCode(options),
            _ => 1
        );
    }

    private static int RunAndReturnExitCode(CommandLineOptions opts)
    {
      Console.WriteLine($"Source: {opts.Source}");
      Console.WriteLine($"Destination: {opts.Destination}");
      Console.WriteLine($"Mode: {opts.Mode}");

      var sorter = new FileSorter(opts);
      sorter.Sort();

      return 0;
    }
  }
}