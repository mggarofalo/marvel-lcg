using Marvel.Protocol;
using Marvel.Transport;

namespace Marvel.PresentationProbe.InfrastructureAllowed;

public static class InfrastructureAllowed
{
    public static IEngineTransport Connect(IEngineEndpoint endpoint) => new InProcessTransport(endpoint);
}
