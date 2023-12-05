using System.Diagnostics;

namespace PrtgDB
{
  [DebuggerDisplay("DateTime = {DateTime}, SensorId = {SensorId}, ChannelId = {ChannelId}, Value = {Value}")]
  public record SensorHistoryEntry
  {
    public DateTime DateTime { get; }

    public int SensorId { get; }

    public int ChannelId { get; }

    public byte[] Unknown1 { get; }

    public short Type { get; }

    //public byte Unknown2 { get; }

    public double Value { get; }

    public SensorHistoryEntry(BinaryReader reader)
    {
      DateTime = reader.ReadDate();
      SensorId = reader.ReadInt32();
      ChannelId = reader.ReadInt32();
      Unknown1 = reader.ReadBytes(2);
      Type = reader.ReadInt16();
      //Unknown2 = reader.ReadByte();
      Value = ReadValue(reader);
    }

    private double ReadValue(BinaryReader reader)
    {
      double val;

      switch (Type)
      {
        case 0xCA:
        case 0xC9:
        case 0x66:
        case 0x68:
          val = reader.ReadInt64();
          break;
        case 0xCB:
          val = reader.ReadDouble();
          break;
        case 0x2: //todo: dont know what the rules are for this exactly
        case 0x3:
          val = reader.ReadInt64();
          Debug.Assert(val == 0, $"Val was not 0 with file type '{Type}'");
          break;
        default:
          val = reader.ReadInt64();

          if (val == 0)
            return val;

          throw new NotImplementedException($"Don't know how to handle type '0x{Type.ToString("X")}' for use with value '{val}' in sensor/channel {SensorId}/{ChannelId} at time {DateTime} at position 0x{reader.BaseStream.Position.ToString("X")}");
      }

      return val;
    }

    private byte GetType(double value)
    {
      if (value == 0 && ChannelId == -1)
        return 0x2;

      if (value == (long)value)
        return 0xCA;
      else
        return 0xCB;
    }

    internal void Serialize(BinaryWriter writer)
    {
      var oaDate = DateTime.ToUniversalTime().ToOADate();

#if DEBUG1
    if (writer.BaseStream is MemoryStream)
    {
        Write(w => w.Write(oaDate), writer, DateTimeBytes, nameof(DateTime));
        Write(w => w.Write(SensorId), writer, SensorIdBytes, nameof(SensorId));
        Write(w => w.Write(ChannelId), writer, ChannelIdBytes, nameof(ChannelId));
        Write(w => w.Write(Unknown1), writer, Unknown1Bytes, nameof(Unknown1));
        Write(w => w.Write(Type), writer, TypeBytes, nameof(Type));
        Write(w => w.Write(Unknown2), writer, Unknown2Bytes, nameof(Unknown2));
        Write(w => WriteValue(writer), writer, ValueBytes, nameof(Value));
    }
#else
      writer.Write(oaDate);
      writer.Write(SensorId);
      writer.Write(ChannelId);
      writer.Write(Unknown1);
      writer.Write(Type);
      //writer.Write(Unknown2);
      WriteValue(writer);
#endif
    }

    private void WriteValue(BinaryWriter writer)
    {
      switch (Type)
      {
        case 0xCA:
        case 0xC9:
          writer.Write((long)Value);
          break;
        case 0xCB:
          writer.Write((double)Value);
          break;
        case 0x2: //todo: dont know what the rules are for this exactly
        case 0x3:
          writer.Write((long)Value);
          break;
        default:
          if (Value == 0)
          {
            writer.Write((long)Value);
            break;
          }
          else
            throw new NotImplementedException($"Don't know how to handle type '{Type}'.");
      }
    }

#if DEBUG1
        private void Write(Action<BinaryWriter> write, BinaryWriter writer, byte[] expectedBytes, string property)
        {
            var position = writer.BaseStream.Position;

            write(writer);

            var newPosition = writer.BaseStream.Position;

            var bytes = ((MemoryStream) writer.BaseStream).GetBuffer().Skip((int) position).Take((int) (newPosition - position)).ToArray();

            for (var i = 0; i < expectedBytes.Length; i++)
                Debug.Assert(expectedBytes[i] == bytes[i], $""); //todo: add description

            for (var i = position; i < newPosition; i++)
            {
                var ourByte = ((MemoryStream) writer.BaseStream).GetBuffer()[i];
                var expectedByte = SensorHistoryFileTests.testFile[i];

                if (ourByte != expectedByte)
                    Debug.Assert(ourByte == expectedByte, $"Invalid value at position {writer.BaseStream.Position}. Expected: {expectedByte}. Actual: {ourByte}");
            }
        }
#endif
  }
}
