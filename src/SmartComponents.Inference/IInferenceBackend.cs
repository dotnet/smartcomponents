using System.Threading.Tasks;

namespace SmartComponents.StaticAssets.Inference;

public interface IInferenceBackend
{
    Task<string> GetChatResponseAsync(ChatOptions options);
}
