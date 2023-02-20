using Nixill.GTFS.Entities;
using Nixill.GTFS.Feeds;
using Nixill.GTFS.Sources;
using NodaTime;
using NodaTime.Text;

namespace Nixill.GTFSBooks;

public class GTFSBooksMain
{
  static void Main(string[] args)
  {
    // First get the GTFS file to open.
    LenientGTFSFeed feed = GetGTFSFeed(args);

    // Now get the current time
    string timestamp = LocalDateTimePattern.CreateWithInvariantCulture("uuuu-MM-dd-HH-mm-ss").Format(
      SystemClock.Instance.GetCurrentInstant().WithOffset(Offset.Zero).LocalDateTime
    );

    // Now create the output
    string dir = $@"out\{timestamp}\";
    Directory.CreateDirectory(dir);

    foreach (Route rt in feed.Routes)
    {

    }
  }

  static LenientGTFSFeed GetGTFSFeed(string[] args)
  {
    string path;
    if (args.Length > 0)
    {
      path = args[0];
    }
    else
    {
      Console.Write("Please select a GTFS file: ");
      path = Console.ReadLine() ?? "";
    }

    if (File.Exists(path)) return new LenientGTFSFeed(new ZipGTFSDataSource(path));
    else if (Directory.Exists(path)) return new LenientGTFSFeed(new DirectoryGTFSDataSource(path));

    throw new FileNotFoundException("Does not exist:", path);
  }
}