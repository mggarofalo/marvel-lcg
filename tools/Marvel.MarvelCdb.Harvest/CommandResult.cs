using System.ComponentModel;
using System.Diagnostics;

namespace Marvel.MarvelCdb.Harvest;

public readonly record struct CommandResult(int ExitCode, string Output, string Error);
