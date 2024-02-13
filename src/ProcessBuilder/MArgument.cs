using CmlLib.Core.Rules;

namespace CmlLib.Core.ProcessBuilder;

public class MArgument
{
    public MArgument()
    {

    }

    public MArgument(string arg)
    {
        Values = new string[] { arg };
    }

    public string[]? Values { get; set; }
    public LauncherRule[]? Rules { get; set; }
}