using System.Collections;
using static PrtgDB.SensorHistoryHeader;
using System.IO.Pipelines;
using System.Text;

namespace PrtgDB;

public record SensorHistoryHeader(
  long Version,
  DateTime FirstDate,
  DateTime LastDate,
  int TotalRecords,
  byte[] Unknown
)
{
  public static SensorHistoryHeader ReadHeader(BinaryReader reader)
  {
    try
    {
      return new SensorHistoryHeader(
        reader.ReadInt64(),
        reader.ReadDate(),
        reader.ReadDate(),
        reader.ReadInt32(),
        reader.ReadBytes(20)
      );
    }
    catch (Exception ex)
    {
      throw new NotImplementedException($"Exception occurred while parsing file header: {ex.Message}", ex);
    }
  }
}

public record SensorHistoryFile : IDisposable
{
  public string Path { get; init; }
  public SensorHistoryHeader Header { get; init; }
  public IEnumerable<SensorHistoryEntry> Entries { get; init; }
  Stream stream { get; init; }

  public SensorHistoryFile(string path)
  {
    Path = path;
    stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

    using (BinaryReader reader = new(stream, Encoding.UTF8, leaveOpen: true))
    {
      Header = ReadHeader(reader);
    }

    // This assumes we are at the point of the stream where the entries begin
    Entries = EnumerateSensorHistoryEntries(stream).Cached();
  }

  static IEnumerable<SensorHistoryEntry> EnumerateSensorHistoryEntries(Stream content)
  {
    // Only one enumerator per stream at a time allowed to avoid seeks getting mixed up
    lock (content)
    {
      long startPosition = content.Position;

      // Instantiate Pipelines for buffer management.
      Stream stream = PipeReader.Create(content).AsStream();

      // Read the stream into a buffer
      using (BinaryReader reader = new(content, Encoding.UTF8, leaveOpen: true))
      {
        while (content.Position < content.Length)
        {
          yield return new SensorHistoryEntry(reader);
        }
      }

      // Rewind the stream for subsequent requests (however there shouldn't be any because this should be fronted by a a cache)
      content.Position = startPosition;
    }
  }

  public void Dispose()
  {
    stream.Dispose();
  }
}


public static class BinaryReaderExtensions
{
  public static DateTime ReadDate(this BinaryReader reader)
  {
    var decimalValue = reader.ReadDouble();
    var dateValue = DateTime.FromOADate(decimalValue).ToLocalTime();
    return dateValue;
  }
}
//     private void Init(BinaryReader reader)
//     {
//       Version = reader.ReadInt64();
//       FirstDate = ReadDate(reader);
//       LastDate = ReadDate(reader);
//       TotalRecords = reader.ReadInt32();
//       Unknown = reader.ReadBytes(20);

//       var list = new List<SensorHistoryFileChannel>();

//       for (var i = 0; i < TotalRecords; i++)
//       {
//         try
//         {
//           list.Add(new SensorHistoryFileChannel(reader));
//         }
//         catch (Exception ex)
//         {
//           throw new NotImplementedException($"Exception occurred while parsing file '{Path}': {ex.Message}", ex);
//         }
//       }

//   //todo: debuggerdisplay. update original commit
//   public record SensorHistoryFile
//   {
//     public string Path { get; }

//     public long Version { get; private set; }

//     public DateTime FirstDate { get; private set; }

//     public DateTime LastDate { get; private set; }

//     public int TotalRecords { get; private set; }

//     public byte[] Unknown { get; private set; }

//     public ReadOnlyCollection<SensorHistoryFileChannel> Channels { get; set; }

//     #region Deserialize

//     public SensorHistoryFile(string path)
//     {
//       Path = path;

//       using (var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
//       using (var reader = new BinaryReader(file))
//       {
//         Init(reader);
//       }
//     }

//     internal SensorHistoryFile(BinaryReader reader)
//     {
//       Init(reader);
//     }

//     private void Init(BinaryReader reader)
//     {
//       Version = reader.ReadInt64();
//       FirstDate = ReadDate(reader);
//       LastDate = ReadDate(reader);
//       TotalRecords = reader.ReadInt32();
//       Unknown = reader.ReadBytes(20);

//       var list = new List<SensorHistoryFileChannel>();

//       for (var i = 0; i < TotalRecords; i++)
//       {
//         try
//         {
//           list.Add(new SensorHistoryFileChannel(reader));
//         }
//         catch (Exception ex)
//         {
//           throw new NotImplementedException($"Exception occurred while parsing file '{Path}': {ex.Message}", ex);
//         }
//       }

//       Channels = new ReadOnlyCollection<SensorHistoryFileChannel>(list);
//     }



//     #endregion
//     #region Serialize

//     public SensorHistoryFile(
//         string path,
//         SensorHistoryFile example,
//         IEnumerable<SensorHistoryFileChannel> channels,
//         DateTime? firstDate = null,
//         DateTime? lastDate = null)
//     {
//       if (path == null)
//         throw new ArgumentNullException(nameof(path));

//       if (example == null)
//         throw new ArgumentNullException(nameof(example));

//       if (channels == null)
//         throw new ArgumentNullException(nameof(channels));

//       Path = path;
//       Version = example.Version;
//       Unknown = example.Unknown;

//       Channels = new ReadOnlyCollection<SensorHistoryFileChannel>(channels.ToList());

//       if (Channels.Count == 0)
//         throw new ArgumentException("At least one channel must be specified.", nameof(channels));

//       var sorted = Channels.OrderBy(c => c.DateTime);

//       FirstDate = firstDate ?? sorted.First().DateTime;
//       LastDate = lastDate ?? sorted.Last().DateTime;
//       TotalRecords = Channels.Count;
//     }

//     public byte[] Serialize()
//     {
//       using (var stream = new MemoryStream())
//       using (var writer = new BinaryWriter(stream))
//       {
//         writer.Write(Version);
//         writer.Write(FirstDate.ToUniversalTime().ToOADate());
//         writer.Write(LastDate.ToUniversalTime().ToOADate());
//         writer.Write(TotalRecords);
//         writer.Write(Unknown);

//         foreach (var channel in Channels)
//           channel.Serialize(writer);

//         return stream.ToArray();
//       }
//     }

//     #endregion
//   }
// }
