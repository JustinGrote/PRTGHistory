using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace PrtgDB;

[SuppressMessage("Usage", "IDE0049", Justification = "Easier to line up the types to the Reader Methods")]
public enum SensorValueType : Int16
{
  Unknown = 0,
  Byte = 0xCA,
  Byte2 = 0xC9,
  Byte3 = 0x66,
  Byte4 = 0x68,
  Double = 0xCB,
  Neg1ChannelMaybe = 0x2,
  Neg2ChannelMaybe = 0x3
}

[SuppressMessage("Usage", "IDE0049", Justification = "Easier to line up the types to the Reader Methods")]
[DebuggerDisplay("DateTime = {DateTime}, SensorId = {SensorId}, ChannelId = {ChannelId}, Value = {Value}")]
public record SensorHistoryEntry
{
  public DateTime DateTime { get; init; }
  public Int32 SensorId { get; init; }
  public Int32 ChannelId { get; init; }
  public byte[] Unknown1 { get; init; }
  public SensorValueType Type { get; init; }
  byte[] value { get; init; }
  public double Value => Type switch
  {
    SensorValueType.Byte or
    SensorValueType.Byte2 or
    SensorValueType.Byte3 or
    SensorValueType.Byte4 or
    SensorValueType.Neg1ChannelMaybe or
    SensorValueType.Neg2ChannelMaybe
      => BitConverter.ToInt64(value, 0),

    SensorValueType.Double => BitConverter.ToDouble(value, 0),

    _ => value[0] == 0 ? 0 : throw new NotImplementedException($"Don't know how to handle type '{Type}'.")
  };

  internal SensorHistoryEntry(PrtgPrdReader reader)
  {
    DateTime = reader.ReadDate();
    SensorId = reader.ReadInt32();
    ChannelId = reader.ReadInt32();
    Unknown1 = reader.ReadBytes(2);
    Type = (SensorValueType)reader.ReadInt16();
    value = reader.ReadBytes(8);
  }
}
