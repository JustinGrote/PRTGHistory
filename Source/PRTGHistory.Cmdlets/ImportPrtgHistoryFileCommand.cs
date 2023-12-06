using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;

using PrtgDB;

namespace PRTGPrd.Commands;

[Cmdlet(VerbsData.Import, "PrtgHistoryFile")]
public class ImportPrtgHistoryFileCommand : PSCmdlet
{
  [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
  [NotNull()]
  public string Path { get; set; } = null!;

  protected override void ProcessRecord()
  {
    try
    {
      WriteObject(new SensorHistoryFile(Path));
    }
    catch
    {
      WriteError(new ErrorRecord(new FileNotFoundException(), "FileNotFound", ErrorCategory.ObjectNotFound, Path));
    }
  }
}
