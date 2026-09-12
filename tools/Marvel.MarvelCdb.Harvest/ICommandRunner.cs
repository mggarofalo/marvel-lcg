using System.ComponentModel;
using System.Diagnostics;

namespace Marvel.MarvelCdb.Harvest;

public interface ICommandRunner
{
    CommandResult Run(IReadOnlyList<string> arguments);
}
