// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if DEBUG
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SmartComponents.StaticAssets.Inference;

namespace SmartComponents.Inference.OpenAI;

// This is primarily so that E2E tests running in CI don't have to call OpenAI for real, so that:
// [1] We don't have to make the API keys available to CI
// [2] There's no risk of random failures due to network issues or the nondeterminism of the AI responses
// It will not be used in real apps in production. Its other benefit is reducing OpenAI usage during local development.

internal static class ResponseCache
{
    static bool IsEnabled = Environment.GetEnvironmentVariable("SMARTCOMPONENTS_E2E_TEST") == "true";

    readonly static Lazy<string> CacheDir = new(() =>
    {
        var dir = Path.Combine(GetSolutionDirectory(), "test", "CachedResponses");
        Directory.CreateDirectory(dir);
        return dir;
    });

    public static bool TryGetCachedResponse(ChatParameters request, out string? response)
    {
        if (!IsEnabled)
        {
            response = null;
            return false;
        }

        var filePath = GetCacheFilePath(request);
        if (File.Exists(filePath))
        {
            Console.WriteLine("Using cached response for " + Path.GetFileName(filePath));
            response = File.ReadAllText(filePath);
            return true;
        }
        else
        {
            Console.WriteLine("Did not find cached response for " + Path.GetFileName(filePath));
            response = null;
            return false;
        }
    }

    public static void SetCachedResponse(ChatParameters request, string response)
    {
        if (IsEnabled)
        {
            var filePath = GetCacheFilePath(request);
            File.WriteAllText(filePath, response);
            File.WriteAllText(filePath.Replace(".response.txt", ".request.json"), GetCacheKeyInput(request));
        }
    }

    private static string GetCacheFilePath(ChatParameters request)
        => GetCacheFilePath(request, request.Messages.LastOrDefault()?.Text ?? "no_messages");

    private static string GetCacheFilePath<T>(T request, string summary)
        => Path.Combine(CacheDir.Value, $"{GetCacheKey(request, summary)}.response.txt");

    private static string GetSolutionDirectory()
    {
        const string filename = "SmartComponents.sln";
        var dir = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!);
        while (dir != null)
        {
            if (dir.EnumerateFiles(filename).Any())
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException($"Could not find directory containing {filename}");
    }

    private static string GetCacheKeyInput<T>(T request)
    {
        return JsonSerializer.Serialize(request).Replace("\\r", "");
    }

    private static string GetCacheKey<T>(T request, string summary)
    {
        var json = GetCacheKeyInput(request);
        var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));

        var sb = new StringBuilder();
        for (var i = 0; i < 8; i++)
        {
            sb.Append(hash[i].ToString("x2"));
        }

        sb.Append("_");
        sb.Append(ToShortSafeString(summary));

        return sb.ToString();
    }

    private static string ToShortSafeString(string summary)
    {
        // This is just to make the cache filenames more recognizable. Won't help much if there's a common long prefix.
        var sb = new StringBuilder();
        foreach (var c in summary)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
            else if (c == ' ')
            {
                sb.Append('_');
            }

            if (sb.Length >= 30)
            {
                break;
            }
        }
        return sb.ToString();
    }
}
#endif
