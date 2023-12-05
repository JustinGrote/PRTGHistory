namespace PrtgDB.Test;

public class UnitTest1
{
  [Fact]
  public void LoadsHistoryFile()
  {

    SensorHistoryFile file = new("Sample.prd");
    file.Should().NotBeNull();
  }
}
