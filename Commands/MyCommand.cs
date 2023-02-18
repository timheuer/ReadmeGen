using Microsoft.Build.Construction;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;

namespace ReadmeGen;

[Command(PackageIds.MyCommand)]
internal sealed class MyCommand : BaseCommand<MyCommand> {
    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e) {
        // create a web request with the bearer token
        var token = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var model = Environment.GetEnvironmentVariable("AZURE_MODEL_DEPLOYMENT");

        OutputWindowPane pane;

        pane = await VS.Windows.GetOutputWindowPaneAsync(Community.VisualStudio.Toolkit.Windows.VSOutputWindowPane.General);

        await pane.ActivateAsync();
        await pane.WriteLineAsync($"OpenAI Token {token}");

        // root project dir
        // create a markdown file in the project and write this text and open it
        var newFileName = "README_generated.md";
        var activeProject = await VS.Solutions.GetActiveProjectAsync();
        var activeProjectPath = new DirectoryInfo(activeProject.FullPath).Parent.FullName;
        var finalFileName = Path.Combine(activeProjectPath, newFileName);

        await VS.StatusBar.StartAnimationAsync(StatusAnimation.Find);
        await VS.StatusBar.ShowProgressAsync("Inspecting project for context...", 1, 4);
        await pane.WriteLineAsync($"Gathering all data from project file: {activeProject.FullPath}");
        // TODO: get from the project files or nuspec if exists
        // TODO: Open the CSPROJ file        
        var projContents = File.ReadAllText(activeProject.FullPath);

        // TODO: Fille the prompt: $"Using the project information here, write a README file for a NuGet library with installation instructions and usage example if possible:\n{prompt}"
        //var prompt = "Write a README for the Humanizer library";
        var prompt = $"Using the project information here, write a README file for a NuGet library with installation instructions and usage example if possible:\n{projContents}";

        await VS.StatusBar.ShowProgressAsync("Building prompt for the robots...", 2, 4);
        await pane.WriteLineAsync($"Prompt: {prompt}");

        // ask the robots
        // show some progress indicator?
        await VS.StatusBar.ShowProgressAsync("Communicating with the robots...", 3, 4);

        var options = new Azure.AI.OpenAI.CompletionsOptions() {
            Prompt = { prompt },
            Temperature = 0.3f,
            MaxTokens = 2048,
            Model = "text-davinci-003"
        };        
        
        var oai = new Azure.AI.OpenAI.OpenAIClient(new Uri(endpoint), new Azure.AzureKeyCredential(token));
        var completions = await oai.GetCompletionsAsync(model, options, new System.Threading.CancellationToken());

        await VS.StatusBar.ShowProgressAsync("Generating README in project...", 4, 4);
        // extract the text result
        await pane.WriteLineAsync($"Full OpenAI Result: {completions.GetRawResponse().Content.ToString()}");
        var resultText = completions.Value.Choices[0].Text;

        Debug.WriteLine(resultText);

        // create the file on disk at project location
        File.WriteAllText(finalFileName, resultText);
        await pane.WriteLineAsync($"Written file to: {finalFileName}");
        
        // add the new file on disk to the project
        await activeProject.AddExistingFilesAsync(finalFileName);

        await VS.StatusBar.EndAnimationAsync(StatusAnimation.Find);
        
        // open the new file
        await VS.Documents.OpenInPreviewTabAsync(finalFileName);

    }
}
