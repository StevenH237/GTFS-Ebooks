namespace Nixill.GTFSBooks;

public class GTFSBooksMain
{
  static void Main(string[] args)
  {
    // First get the GTFS file to open.
    string file;

    if (args.Length > 0)
    {
      file = args[0];
    }
    else
    {
      Console.Write("Please select a GTFS file: ");
      file = (string)Console.ReadLine();
    }
  }
}