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
        var client = new HttpClient();
        var token = Environment.GetEnvironmentVariable("OPENAI_KEY");
        Debug.WriteLine(token);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // root project dir
        // create a markdown file in the project and write this text and open it
        var newFileName = "README_generated.md";
        var activeProject = await VS.Solutions.GetActiveProjectAsync();
        var activeProjectPath = new DirectoryInfo(activeProject.FullPath).Parent.FullName;
        var finalFileName = Path.Combine(activeProjectPath, newFileName);

        await VS.StatusBar.ShowProgressAsync("Inspecting project for context...", 1, 4);
        // TODO: get from the project files or nuspec if exists
        // TODO: Open the CSPROJ file        
        var projContents = File.ReadAllText(activeProject.FullPath);

        // TODO: Fille the prompt: $"Using the project information here, write a README file for a NuGet library with installation instructions and usage example if possible:\n{prompt}"
        //var prompt = "Write a README for the Humanizer library";
        var prompt = $"Using the project information here, write a README file for a NuGet library with installation instructions and usage example if possible:\n{projContents}";

        await VS.StatusBar.ShowProgressAsync("Building prompt for the robots...", 2, 4);
        // build the request payload
        // parameters
        var parameters = new { model = "text-davinci-003", prompt = prompt, max_tokens = 2048, temperature = 0 };

        // convert to json
        var json = JsonConvert.SerializeObject(parameters);

        // convert to stringcontent
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // ask the robots
        // show some progress indicator?
        await VS.StatusBar.ShowProgressAsync("Communicating with the robots...", 3, 4);
        var response = await client.PostAsync("https://api.openai.com/v1/completions", content);

        await VS.StatusBar.ShowProgressAsync("Generating README in project...", 4, 4);
        // extract the text result
        var result = await response.Content.ReadAsStringAsync();
        var doc = JObject.Parse(result);
        var resultText = doc["choices"][0]["text"].ToString();

        Debug.WriteLine(resultText);

        // create the file on disk at project location
        File.WriteAllText(finalFileName, resultText);

        // add the new file on disk to the project
        await activeProject.AddExistingFilesAsync(finalFileName);

        // open the new file
        await VS.Documents.OpenInPreviewTabAsync(finalFileName);

    }
}
