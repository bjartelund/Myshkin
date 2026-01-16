using Microsoft.SemanticKernel.Agents;

namespace Myshkin;

public static class AgentLoop
{
    public static async Task Loop(ChatCompletionAgent planAgent,ChatCompletionAgent codingAgent, ChatHistoryAgentThread thread)
    {
        while (true)
        {
            Console.WriteLine("-----" + new string('-', 25));

            Console.WriteLine("Enter your prompt (or '/exit' to quit): ");

            var userInput = Console.ReadLine() ?? string.Empty;

            Console.WriteLine("-----" + new string('-', 25));
            if (userInput.Equals("/exit", StringComparison.CurrentCultureIgnoreCase))
                break;


            // ask the planning agent to create a plan
            var planResponse =  planAgent.InvokeAsync(userInput, thread);
            
            // display the plan as it streams in and accumulate the full plan
            Console.WriteLine("Planning Agent Response:");
            var sb = new System.Text.StringBuilder();
            
            await foreach (var chunk in planResponse)
            {
                Console.Write(chunk.Message.Content);
                sb.Append(chunk.Message.Content);
            }
            
            Console.WriteLine("-----" + new string('-', 25));

            sb.Append("Use this plan to implement the requested changes: ");
            sb.Append(userInput);
            
            var planText = sb.ToString();

            Console.WriteLine("Coding Agent Response:");
            // ask the coding agent to implement the plan
            var codeResponse = codingAgent.InvokeAsync(planText, thread);
            
            // stream coding agent response (optional)
            await foreach (var codeChunk in codeResponse)
            {
                Console.Write(codeChunk.Message.Content);
            }
            
            
        }

    }
}