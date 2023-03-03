using System.Collections;
using Nixill.GTFS.Entities;
using Nixill.GTFS.Feeds;
using NodaTime;

namespace Nixill.GTFSBooks.Entities;

public class ServiceCollection : IEnumerable<LocalDate>
{
  public readonly bool Monday;
  public readonly bool Tuesday;
  public readonly bool Wednesday;
  public readonly bool Thursday;
  public readonly bool Friday;
  public readonly bool Saturday;
  public readonly bool Sunday;

  public readonly LocalDate StartDate;
  public readonly LocalDate EndDate;

  public readonly IReadOnlyList<LocalDate> Additions;
  public readonly IReadOnlyList<LocalDate> Exceptions;

  private static IReadOnlyList<LocalDate> EmptyDateList = new List<LocalDate>().AsReadOnly();

  private Period OneDay = Period.FromDays(1);

  public bool this[IsoDayOfWeek dow]
  {
    get
    {
      switch (dow)
      {
        case IsoDayOfWeek.Monday: return Monday;
        case IsoDayOfWeek.Tuesday: return Tuesday;
        case IsoDayOfWeek.Wednesday: return Wednesday;
        case IsoDayOfWeek.Thursday: return Thursday;
        case IsoDayOfWeek.Friday: return Friday;
        case IsoDayOfWeek.Saturday: return Saturday;
        case IsoDayOfWeek.Sunday: return Sunday;
        default: return false;
      }
    }
  }

  public bool this[LocalDate date]
  {
    get
    {
      if (Additions.Contains(date)) return true;
      if (Exceptions.Contains(date)) return false;
      if (date < StartDate || date > EndDate) return false;
      return this[date.DayOfWeek];
    }
  }

  public byte Mask => (byte)(
    (Monday ? 1 : 0) +
    (Tuesday ? 2 : 0) +
    (Wednesday ? 4 : 0) +
    (Thursday ? 8 : 0) +
    (Friday ? 16 : 0) +
    (Saturday ? 32 : 0) +
    (Sunday ? 64 : 0)
  );

  public ServiceCollection(bool mon, bool tue, bool wed, bool thu, bool fri, bool sat, bool sun, LocalDate start, LocalDate end, IEnumerable<LocalDate>? plus = null, IEnumerable<LocalDate>? minus = null)
  {
    Monday = mon;
    Tuesday = tue;
    Wednesday = wed;
    Thursday = thu;
    Friday = fri;
    Saturday = sat;
    Sunday = sun;
    StartDate = start;
    EndDate = end;

    if (plus != null) Additions = new List<LocalDate>(plus).AsReadOnly();
    else Additions = EmptyDateList;

    if (minus != null) Exceptions = new List<LocalDate>(minus).AsReadOnly();
    else Exceptions = EmptyDateList;
  }

  public ServiceCollection(Calendar cal, IEnumerable<CalendarDate>? dates = null)
  {
    Monday = cal.Monday;
    Tuesday = cal.Tuesday;
    Wednesday = cal.Wednesday;
    Thursday = cal.Thursday;
    Friday = cal.Friday;
    Saturday = cal.Saturday;
    Sunday = cal.Sunday;
    StartDate = cal.StartDate;
    EndDate = cal.EndDate;

    if (dates != null)
    {
      var plus = new List<LocalDate>();
      var minus = new List<LocalDate>();

      foreach (var date in dates)
      {
        if (date.IsAdded) plus.Add(date.Date);
        if (date.IsRemoved) minus.Add(date.Date);
      }

      Additions = plus.Except(minus).ToList().AsReadOnly();
      Exceptions = minus.Except(plus).ToList().AsReadOnly();
    }
    else
    {
      Additions = EmptyDateList;
      Exceptions = EmptyDateList;
    }
  }

  public ServiceCollection(IEnumerable<CalendarDate> dates)
  {
    Monday = false;
    Tuesday = false;
    Wednesday = false;
    Thursday = false;
    Friday = false;
    Saturday = false;
    Sunday = false;

    Exceptions = EmptyDateList;

    Additions = dates
      .Where(x => x.IsAdded)
      .Select(x => x.Date)
      .Except(dates
        .Where(x => x.IsRemoved)
        .Select(x => x.Date))
      .ToList()
      .AsReadOnly();

    StartDate = Additions.Min();
    EndDate = Additions.Max();
  }

  public static readonly ServiceCollection Empty =
    new ServiceCollection(false, false, false, false, false, false, false, LocalDate.MinIsoValue, LocalDate.MinIsoValue);

  // This represents a new ServiceCollection which has the same properties
  // as this one, except that its StartDate is as early as possible and
  // its EndDate is as late as possible without actually adding any extra
  // days of service.
  public ServiceCollection WideRange()
  {
    var start = StartDate;
    var end = EndDate;

    // If the ServiceCollection is defined only by Additions, then its
    // range can be infinite. However, since we don't have a good way
    // to describe "infinite", we'll just say the start of year 0 to the
    // end of year 9999. If someone's using this code in the year 10,000
    // or later, hi! What's it like living about eight thousand years
    // after I wrote this? Why in the world are you still using it?
    if (Mask == 0)
    {
      start = LocalDate.MinIsoValue;
      end = LocalDate.MaxIsoValue;
    }
    else
    {
      // Start with the day before the start of service.
      // Any day that's either not defined by the calendar, or
      // explicitly added by Additions, is acceptable in the range.
      for (LocalDate date = start - OneDay; !this[date.DayOfWeek] || Additions.Contains(date); date -= OneDay)
      {
        start = date;
      }

      // Start with the day after the end of service.
      // Any day that's either not defined by the calendar, or
      // explicitly added by Additions, is acceptable in the range.
      for (LocalDate date = end + OneDay; !this[date.DayOfWeek] || Additions.Contains(date); date += OneDay)
      {
        end = date;
      }
    }

    return new ServiceCollection(Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday, StartDate, EndDate,
      Additions, Exceptions);
  }

  private ServiceCollection Union(ServiceCollection other)
  {

  }

  private IEnumerable<LocalDate> DatesBetween(LocalDate start, LocalDate end)
  {
    for (LocalDate date = start; date <= end; date += OneDay) yield return date;
  }

  // Enumerates over all the dates in service.
  public IEnumerator<LocalDate> GetEnumerator()
  {
    IEnumerable<LocalDate> dates = Enumerable.Empty<LocalDate>();

    if (Mask != 0)
    {
      dates = DatesBetween(StartDate, EndDate)
        .Where(d => this[d.DayOfWeek])
        .Except(Exceptions);
    }

    dates = dates
      .Union(Additions)
      .Order();

    return dates.GetEnumerator();
  }
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}