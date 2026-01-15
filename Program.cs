
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
        Instructions = """
                       You are a coding agent designed to assist with software development tasks. You have access to tools for file manipulation, including listing, reading, and writing files. Use these tools to interact with the file system as needed. If a user's request is unclear, ask for clarification. Provide detailed, step-by-step explanations for your actions. Use code blocks when presenting code. Handle errors gracefully and inform the user if a task cannot be completed. Always aim to deliver accurate and helpful responses.
                       
                       If calling a tool fails , and attempting to call it again with modified input is unlikely to succeed, do not retry the call. Instead, inform the user about the failure and suggest alternative approaches or request additional information.
                       
                                Example of unified diff format:
                                --- a/original.txt
                                +++ b/modified.txt
                                @@ -1,3 +1,4 @@
                                -Line 1
                                +Line 1 modified
                       """,
        Kernel = kernel,
        Arguments = 
        new (settings)
    };

var thread = new ChatHistoryAgentThread();

while (true)
{
    Console.Write("Enter your prompt (or '/exit' to quit): ");    var userInput = Console.ReadLine() ?? string.Empty;
    
    if (userInput.Equals("/exit", StringComparison.CurrentCultureIgnoreCase))
        break;

    var agentResponse = agent.InvokeAsync(userInput, thread);
    await foreach (var message in agentResponse)
    {
        Console.WriteLine(message.Message);
    }
}

