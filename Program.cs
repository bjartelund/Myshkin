
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Myshkin;



var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromMinutes(5),     // <— extend timeout here
    BaseAddress = new Uri("http://localhost:11434")
};


var builder = Kernel.CreateBuilder();
builder.AddOllamaChatCompletion(
"qwen3:8b", httpClient);

// Create CodeAgentTools with the current working directory as base

var currentDirectory = Directory.GetCurrentDirectory();
Console.WriteLine($"Current working directory: {currentDirectory}");

var planAgentTools = new PlanAgentTools(currentDirectory);
var codeAgentTools = new CodeAgentTools(currentDirectory);
builder.Plugins.AddFromObject(planAgentTools);

OllamaPromptExecutionSettings settings = new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(), Temperature = 0 };

var planKernel = builder.Build();

ChatCompletionAgent planningAgent =
    new()
    {
        Name = "Planning-Agent",
        Instructions = """
                       You are a planning agent designed to break down complex tasks into manageable steps. 
                       When given a task, analyze it thoroughly and create a detailed plan outlining the necessary actions to achieve the goal. 
                       Consider potential challenges and dependencies, and suggest strategies to address them. 
                       Provide clear, step-by-step instructions that can be easily followed. 
                        The planning output will go to a coding agent for implementation, so be precise and thorough in your planning.
                       """,
        Kernel = planKernel,
        Arguments =
        new(settings)
    };

var coderKernel = planKernel.Clone();
coderKernel.ImportPluginFromObject(codeAgentTools);

ChatCompletionAgent codingAgent =
    new()
    {
        Name = "Coding-Agent",
        Instructions = """
                       You are a coding agent designed to assist with software development tasks. 
                       You have access to tools for file manipulation, including listing, reading, and writing files. 
                       Use these tools to interact with the file system as needed. 
                       If a user's request is unclear, ask for clarification. 
                       Provide detailed, step-by-step explanations for your actions. 
                       Use code blocks when presenting code. 
                       Handle errors gracefully and inform the user if a task cannot be completed. 
                       Always aim to deliver accurate and helpful responses.
                       
                       If calling a tool fails , and attempting to call it again with modified input is unlikely to succeed, do not retry the call. Instead, inform the user about the failure and suggest alternative approaches or request additional information.
                       
                       """,
        Kernel = coderKernel,
        Arguments =
        new(settings)
    };


ChatHistoryAgentThread thread = new();

await AgentLoop.Loop(planningAgent,codingAgent,thread);
