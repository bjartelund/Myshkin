using Microsoft.SemanticKernel.Agents;

namespace Myshkin;

public static class AgentLoop
{
    public static async Task Loop(ChatCompletionAgent planAgent, ChatCompletionAgent codingAgent, ChatHistoryAgentThread thread)
    {
        while (true)
        {
            PrintSeparator();

            Console.WriteLine("Enter your prompt (or '/exit' to quit): ");

            var userInput = Console.ReadLine() ?? string.Empty;

            PrintSeparator();
            if (userInput.Equals("/exit", StringComparison.CurrentCultureIgnoreCase))
                break;

            var planResponse = planAgent.InvokeAsync(userInput, thread);
            Console.WriteLine("Planning Agent Response:");
            var planContent = await ReadContentAsync(planResponse, chunk => chunk.Message.Content);

            PrintSeparator();

            var planText = string.Concat(planContent, "Use this plan to implement the requested changes: ", userInput);

            Console.WriteLine("Coding Agent Response:");
            var codeResponse = codingAgent.InvokeAsync(planText, thread);


            var codeContent = await ReadContentAsync(codeResponse, chunk => chunk.Message.Content);
            Console.WriteLine(codeContent);
            
            PrintSeparator();

            continue;

            static void PrintSeparator()
            {
                Console.WriteLine(new string('-', 25));
            }

            // local helper to accumulate async stream into a single string
            static async Task<string> ReadContentAsync<T>(IAsyncEnumerable<T> source, Func<T, string?> selector)
            {
                var sb = new System.Text.StringBuilder();
                await foreach (var item in source)
                {
                    sb.Append(selector(item));
                }
                return sb.ToString();
            }
        }

    }
}