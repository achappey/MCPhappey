using System.Text.Json;
using MCPhappey.Common.Constants;
using MCPhappey.Common.Models;
using ModelContextProtocol.Protocol;

namespace MCPhappey.Servers.JSON;

public static class StaticContentLoader
{
    public static IEnumerable<ServerConfig> GetServers(this string basePath)
    {
        var servers = new List<ServerConfig>();

        foreach (var subDir in Directory.GetDirectories(basePath, "*", SearchOption.AllDirectories))
        {
            var serverJsonFiles = Directory.GetFiles(subDir, "*Server.json", SearchOption.TopDirectoryOnly);
            if (serverJsonFiles.Length == 0)
                continue;

            foreach (var file in serverJsonFiles)
            {
                var jsonContent = File.ReadAllText(file);

                var serverObj = JsonSerializer.Deserialize<Server>(jsonContent);
                if (serverObj == null)
                    continue;

                ServerConfig serverConfig = new()
                {
                    Server = serverObj,
                    SourceType = ServerSourceType.Static,
                };

                // Check for Tools.json, Prompts.json, Resources.json in the same subDir
                var promptsFile = Path.Combine(subDir, "Prompts.json");
                var resourcesFile = Path.Combine(subDir, "Resources.json");
                var resourceTemplatesFile = Path.Combine(subDir, "ResourceTemplates.json");

                var updateTimes = new List<DateTime>
                {
                    File.GetLastWriteTime(file)
                };

                if (File.Exists(promptsFile)) updateTimes.Add(File.GetLastWriteTime(promptsFile));
                if (File.Exists(resourcesFile)) updateTimes.Add(File.GetLastWriteTime(resourcesFile));
                if (File.Exists(resourceTemplatesFile)) updateTimes.Add(File.GetLastWriteTime(resourceTemplatesFile));

                var lastUpdate = updateTimes.Max();
                if (File.Exists(promptsFile))
                {
                    serverConfig.PromptList = JsonSerializer.Deserialize<PromptTemplates>(File.ReadAllText(promptsFile));
                    serverObj.Capabilities.Prompts = new(); // not null
                }

                if (serverObj.Plugins?.Any() == true)
                {
                    serverObj.Capabilities.Tools = new();
                }

                // If Resources.json exists, mark as not null
                if (File.Exists(resourcesFile))
                {
                    serverConfig.ResourceList = JsonSerializer.Deserialize<ListResourcesResult>(
                            File.ReadAllText(resourcesFile));

                    serverObj.Capabilities.Resources = new();
                }

                if (File.Exists(resourceTemplatesFile))
                {
                    serverConfig.ResourceTemplateList = JsonSerializer.Deserialize<ListResourceTemplatesResult>(
                            File.ReadAllText(resourceTemplatesFile));
                    serverObj.Capabilities.Resources = new();
                }

                serverConfig.Server.ServerInfo.Version = lastUpdate.ToString("yyyyMMdd");

                servers.Add(serverConfig);
            }
        }

        return servers;
    }
}