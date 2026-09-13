using System.Diagnostics;
using Marvel.Session;

namespace Marvel.Server;

internal sealed class PersistenceFailureException(Exception failure)
    : IOException("session persistence failed", failure);
