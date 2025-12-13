
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Myshkin;

var builder = Kernel.CreateBuilder();
builder.AddOllamaChatCompletion(
"qwen3:14b", new Uri("http://localhost:11434")
);

builder.Plugins.AddFromType<CodeAgentTools>();

OllamaPromptExecutionSettings settings = new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(), Temperature = 0 };

var kernel = builder.Build();

ChatCompletionAgent agent =
    new()
    {
        Name = "SK-Agent",
        Instructions = "You are a helpful assistant. Using tools and functions to help the user",
        Kernel = kernel,
        Arguments = 
        new (settings)
    };

var agentResponse = agent.InvokeAsync("I have a secret file and I need the password inside it. We really need to change the password to something more secret like hunter123.");
await foreach (var message in agentResponse)
{
    Console.WriteLine(message.Message);
}

