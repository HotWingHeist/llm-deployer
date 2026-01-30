using LLMDeployer.Core.Services;

namespace LLMDeployer.UI;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════╗");
        Console.WriteLine("║     🤖 LLM Deployer - Chat UI    ║");
        Console.WriteLine("╚════════════════════════════════════╝\n");

        var modelManager = new ModelManager();
        var chatService = new ChatService(modelManager);

        // Load a model
        Console.WriteLine("📦 Loading model...");
        var model = await modelManager.LoadModelAsync("C:\\models\\default-model.bin");
        Console.WriteLine($"✓ Model loaded: {model.Name}\n");

        // Start chat session
        Console.WriteLine("💬 Starting chat session...");
        var session = chatService.StartSession(model.Id);
        Console.WriteLine($"✓ Chat session started (ID: {session.Id})\n");

        Console.WriteLine("════════════════════════════════════");
        Console.WriteLine("Type 'help' for commands, 'exit' to quit");
        Console.WriteLine("════════════════════════════════════\n");

        // Interactive chat loop
        bool running = true;
        while (running)
        {
            try
            {
                Console.Write("You: ");
                var userInput = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(userInput))
                    continue;

                // Handle commands
                switch (userInput.ToLower().Trim())
                {
                    case "exit":
                    case "quit":
                        running = false;
                        break;

                    case "help":
                        DisplayHelp();
                        break;

                    case "history":
                        DisplayHistory(chatService, session.Id);
                        break;

                    case "clear":
                        chatService.ClearHistory(session.Id);
                        Console.WriteLine("✓ Chat history cleared\n");
                        break;

                    default:
                        // Send message to model
                        Console.WriteLine("⏳ Processing...");
                        var response = await chatService.SendMessageAsync(session.Id, userInput);
                        Console.WriteLine($"🤖 Assistant: {response}\n");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}\n");
            }
        }

        // End session
        chatService.EndSession(session.Id);
        Console.WriteLine("\n✓ Chat session ended");
        Console.WriteLine("Thank you for using LLM Deployer!");
    }

    static void DisplayHelp()
    {
        Console.WriteLine("\n════════════════════════════════════");
        Console.WriteLine("📖 Available Commands:");
        Console.WriteLine("════════════════════════════════════");
        Console.WriteLine("  history - Show chat history");
        Console.WriteLine("  clear   - Clear chat history");
        Console.WriteLine("  help    - Show this help message");
        Console.WriteLine("  exit    - Exit the application");
        Console.WriteLine("════════════════════════════════════\n");
    }

    static void DisplayHistory(ChatService chatService, string sessionId)
    {
        var history = chatService.GetChatHistory(sessionId);
        
        if (!history.Any())
        {
            Console.WriteLine("\n📝 No chat history yet\n");
            return;
        }

        Console.WriteLine("\n════════════════════════════════════");
        Console.WriteLine("📜 Chat History:");
        Console.WriteLine("════════════════════════════════════");
        
        foreach (var msg in history)
        {
            var role = msg.Role == "user" ? "You" : "Assistant";
            Console.WriteLine($"[{role}]: {msg.Content}");
        }
        
        Console.WriteLine("════════════════════════════════════\n");
    }
}
