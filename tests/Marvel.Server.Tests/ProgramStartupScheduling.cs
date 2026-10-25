using Xunit;

namespace Marvel.Server.Tests;

// The startup deadline measures host readiness without competing dataset loads
// from other test collections in this assembly. Other collections stay parallel.
[CollectionDefinition(nameof(ProgramStartupScheduling), DisableParallelization = true)]
public sealed class ProgramStartupScheduling;
