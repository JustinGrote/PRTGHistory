using System.IO.Pipelines;
using System.Text;

namespace PrtgDB;

/// <summary>
/// Represents a PRTG device sensor history file.
/// </summary>
public record SensorHistoryFile : IDisposable
{
  /// <summary>
  /// The device ID is inferred from the filename. If this is -1, the filename was not in the expected format.
  /// </summary>
  public int DeviceId { get; init; }
  public string Path { get; init; }
  public SensorHistoryHeader Header { get; init; }
  public IEnumerable<SensorHistoryEntry> Entries { get; init; }
  Stream stream { get; init; }

  public SensorHistoryFile(string path)
  {
    Path = path;

    stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

    string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

    DeviceId = int.TryParse(fileName.AsSpan(fileName.LastIndexOf(' ') + 1), out int deviceId)
      ? deviceId
      : -1;

    using (PrtgPrdReader reader = new(stream, true))
    {
      Header = reader.ReadHeader();
    }

    // This assumes we are at the point of the stream where the entries begin
    Entries = EnumerateSensorHistoryEntries(stream).Cached();
  }

  static IEnumerable<SensorHistoryEntry> EnumerateSensorHistoryEntries(Stream content)
  {
    // Only one enumerator per stream at a time allowed to avoid seeks getting mixed up
    lock (content)
    {
      // Instantiate Pipelines for buffer management.
      Stream stream = PipeReader.Create(content).AsStream();

      // Read the stream into a buffer

      using (PrtgPrdReader reader = new(stream))
      {
        while (content.Position < content.Length)
        {
          yield return new SensorHistoryEntry(reader);
        }
      }
    }
  }

  public void Dispose()
  {
    stream.Dispose();
  }
}

#pragma warning disable CS9107 // BinaryReader is fairly stateless so this is OK
class PrtgPrdReader(Stream stream, bool leaveOpen = false) : BinaryReader(stream, Encoding.UTF8, leaveOpen)
#pragma warning restore CS9107
{
  public SensorHistoryHeader ReadHeader()
  {
    if (stream.Position != 0)
      throw new InvalidOperationException("Cannot read header from a stream that is not at the beginning");

    try
    {
      return new SensorHistoryHeader(
        ReadInt64(),
        ReadDate(),
        ReadDate(),
        ReadInt32(),
        ReadBytes(20)
      );
    }
    catch (Exception ex)
    {
      throw new InvalidDataException($"Exception occurred while parsing file header: {ex.Message}", ex);
    }
  }
  public DateTime ReadDate()
  {
    var decimalValue = ReadDouble();
    var dateValue = DateTime.FromOADate(decimalValue).ToLocalTime();
    return dateValue;
  }
}

public record SensorHistoryHeader(
  long Version,
  DateTime FirstDate,
  DateTime LastDate,
  int TotalRecords,
  byte[] Unknown
);
