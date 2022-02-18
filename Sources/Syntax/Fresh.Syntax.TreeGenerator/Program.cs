
using System;
using System.IO;
using System.Text.Json;
using Fresh.Syntax.TreeGenerator;

internal sealed class Program
{
    internal static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Please pass in the path to the JSON as a command line argument!");
            Environment.Exit(1);
            return;
        }

        var json = File.ReadAllText(args[0]);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var treeModel = JsonSerializer.Deserialize<TreeModel>(json, options)
                     ?? throw new InvalidOperationException("Deserializer returned null");
        var code = Generator.Generate(treeModel);
        Console.WriteLine(code);
    }
}
