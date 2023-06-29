﻿using HtmlAgilityPack;

namespace CmlLib.Core.Installer.Forge;

public class ForgeVersionLoader
{
    private readonly HttpClient _httpClient;

    public ForgeVersionLoader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<ForgeVersion>> GetForgeVersions(string mcVersion)
    {
        var html = await _httpClient.GetStringAsync($"https://files.minecraftforge.net/net/minecraftforge/forge/index_{mcVersion}.html");
        File.WriteAllText("html.html", html);
        //var html2 = File.ReadAllText("html.html");
        return findForgeVersionsInHtml(html, mcVersion);
    }

    private IEnumerable<ForgeVersion> findForgeVersionsInHtml(string html, string mcVersion)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);
        return document.DocumentNode
            .SelectNodes("//html[1]//body[1]//main[1]//div[2]//div[2]//div[2]//table[1]//tbody[1]//tr")
            .Select(node => getForgeVersion(node, mcVersion));
    }

    private ForgeVersion getForgeVersion(HtmlNode node, string mcVersion)
    {
        var version = new ForgeVersion();
        version.MinecraftVersionName = mcVersion;
        var tds = node.Descendants("td");
        foreach (var td in tds)
        {
            if (td.HasClass("download-version"))
                version.ForgeVersionName = td.InnerText.Trim();
            if (td.HasClass("download-time"))
                version.Time = td.InnerText.Trim();
            if (td.HasClass("download-files"))
                version.Files = getForgeVersionFiles(td);
        }
        return version;
    }

    private IEnumerable<ForgeVersionFile> getForgeVersionFiles(HtmlNode node)
    {
        var lis = node.SelectNodes("ul[1]/li");
        if (lis == null)
            return Enumerable.Empty<ForgeVersionFile>();

        var files = new List<ForgeVersionFile>();
        foreach (var li in lis)
        {
            var forgeVersionFile = new ForgeVersionFile();
            string? firstLink = null, secondLink = null;

            var firstANode = li.SelectSingleNode("a[1]");
            if (firstANode != null)
            {
                firstLink = firstANode.GetAttributeValue("href", "").Trim();
                forgeVersionFile.Type = firstANode.InnerText.Trim();
            }

            var infoTooltip = li.Descendants().FirstOrDefault(node => node.HasClass("info-tooltip"));
            if (infoTooltip != default)
            {
                forgeVersionFile.MD5 = infoTooltip.ChildNodes[2].InnerText.Trim();
                forgeVersionFile.SHA1 = infoTooltip.ChildNodes[6].InnerText.Trim();
                secondLink = infoTooltip
                    .Descendants("a")
                    .FirstOrDefault()?
                    .GetAttributeValue("href", "")?
                    .Trim();
            }

            if (string.IsNullOrEmpty(secondLink))
            {
                forgeVersionFile.DirectUrl = firstLink;
            }
            else
            {
                forgeVersionFile.AdUrl = firstLink;
                forgeVersionFile.DirectUrl = secondLink;
            }

            files.Add(forgeVersionFile);
        }

        return files;
    }
}
