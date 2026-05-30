using Microsoft.Extensions.AI;

namespace Vence.AI;

public interface IChatClientFactory
{
    IChatClient CreateClient();
}
