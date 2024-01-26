using System.Reflection;

public static class RepoSharedConfigUtil
{
    public static void AddRepoSharedConfig(this IHostApplicationBuilder builder)
    {
        // This is only used within this repo to simplify sharing config
        // across multiple projects. For real usage, just add the required
        // config values to your appsettings.json file.

        var envVarPath = Environment.GetEnvironmentVariable("SMARTCOMPONENTS_REPO_CONFIG_FILE_PATH");
        if (!string.IsNullOrEmpty(envVarPath))
        {
            builder.Configuration.AddJsonFile(envVarPath);
            return;
        }

        var dir = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
        while (true)
        {
            var path = Path.Combine(dir, "RepoSharedConfig.json");
            if (File.Exists(path))
            {
                builder.Configuration.AddJsonFile(path);
                return;
            }

            var parent = Directory.GetParent(dir);
            if (parent == null)
            {
                throw new FileNotFoundException("Could not find RepoSharedConfig.json");
            }

            dir = parent.FullName;
        }
    }
}
