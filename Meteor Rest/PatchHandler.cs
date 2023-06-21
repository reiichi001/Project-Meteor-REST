using System.Net;

namespace Meteor_Rest
{
    public class PatchHandler
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        public PatchHandler(ILogger logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
    
        public void CheckBootVersion(HttpResponse response, string version)
        {
            _logger.LogInformation("Checking boot version");
            if (version != _configuration["PatchHTTP:ffxiv_bootversion"])
            {
                _logger.LogInformation("Boot version " + version + " didn't match. Patch your shit.");
                //TODO: replace with version number lookup for stuff later
                string? newpatchversion;

                if (!PatchList.bootpatches.TryGetValue(version, out newpatchversion))
                {
                    _logger.LogInformation("Invalid boot patch requested: " + version);
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    //response.Headers.Add("HTTP/1.0 404 NOT FOUND");
                    return;
                }

                response.StatusCode = (int)HttpStatusCode.OK;
                response.Headers.Add("Content-Location", $"ffxiv/{_configuration["PatchHTTP:ffxiv_boothash"]}/vercheck.dat");
                response.Headers.Add("Content-Type", "multipart/mixed; boundary=477D80B1_38BC_41d4_8B48_5273ADB89CAC");
                response.Headers.Add("X-Repository", $"ffxiv/win32/release/boot");
                response.Headers.Add("X-Patch-Module", "ZiPatch");
                response.Headers.Add("X-Protocol", "torrent");
                response.Headers.Add("X-Info-Url", "http://example.com");
                response.Headers.Add("X-Latest-Version", _configuration["PatchHTTP:ffxiv_bootversion"]);
                response.Headers.Add("Connection", "keep-alive");

                SendBootPatches(response, version, _configuration["PatchHTTP:ffxiv_bootversion"]);
            }
            else
            {
                _logger.LogInformation("Boot version matched. Proceed to game version check");
                response.StatusCode = (int)HttpStatusCode.NoContent;
                //response.Headers.Add("HTTP/1.0 204 No Content");
                response.Headers.Add("Content-Location", $"ffxiv/{_configuration["PatchHTTP:ffxiv_boothash"]}/vercheck.dat");
                response.Headers.Add("X-Repository", $"ffxiv/win32/release/boot");
                response.Headers.Add("X-Patch-Module", "ZiPatch");
                response.Headers.Add("X-Protocol", "torrent");
                response.Headers.Add("X-Info-Url", "http://www.example.com");
                response.Headers.Add("X-Latest-Version", _configuration["PatchHTTP:ffxiv_bootversion"]);
            }

        }

        private void SendBootPatches(HttpResponse response, string startversion, string finalpatchversion)
        {
            //StringBuilder sb = new StringBuilder();
            using StreamWriter sb = new StreamWriter(response.Body);
            using BinaryWriter bw = new BinaryWriter(response.Body);
            string? nextpatchversion = startversion;

            while (true)
            {

                if (!PatchList.bootpatches.TryGetValue(nextpatchversion, out nextpatchversion))
                    break;

                string rootPath = "";// app.Environment.WebRootPath;
                string filepath = "";

                if (File.Exists(Path.Combine(rootPath, $"ffxiv/{_configuration["PatchHTTP:ffxiv_boothash"]}/metainfo/D{nextpatchversion}.torrent")) &&
                    File.Exists(Path.Combine(rootPath, $"ffxiv/{_configuration["PatchHTTP:ffxiv_boothash"]}/patch/D{nextpatchversion}.patch")))
                {
                    filepath = $"D{nextpatchversion}";
                }
                else if (File.Exists(Path.Combine(rootPath, $"ffxiv/{_configuration["PatchHTTP:ffxiv_boothash"]}/metainfo/H{nextpatchversion}.torrent")) &&
                    File.Exists(Path.Combine(rootPath, $"ffxiv/{_configuration["PatchHTTP:ffxiv_boothash"]}/patch/H{nextpatchversion}.patch")))
                {
                    filepath = $"H{nextpatchversion}";
                }

                _logger.LogInformation("Sending patch info for " + filepath);

                sb.Write("--477D80B1_38BC_41d4_8B48_5273ADB89CAC\r\n");

                sb.Write("Content-Type: application/octet-stream\r\n");
                sb.Write($"Content-Location: ffxiv/{_configuration["PatchHTTP:ffxiv_boothash"]}/metainfo/{filepath}.torrent\r\n");
                sb.Write($"X-Patch-Length: {new FileInfo($"{rootPath}/ffxiv/{_configuration["PatchHTTP:ffxiv_boothash"]}/metainfo/{filepath}.torrent").Length}\r\n");
                sb.Write("X-Signature: jqxmt9WQH1aXptNju6CmCdztFdaKbyOAVjdGw_DJvRiBJhnQL6UlDUcqxg2DeiIKhVzkjUm3hFXOVUFjygxCoPUmCwnbCaryNqVk_oTk_aZE4HGWNOEcAdBwf0Gb2SzwAtk69zs_5dLAtZ0mPpMuxWJiaNSvWjEmQ925BFwd7Vk=\r\n");

                sb.Write("\r\n");
                sb.Flush();
                //sb.Write(File.ReadAllBytes(Path.Combine(rootPath, $"{GAME_NAME}/{BOOT_HASH}/metainfo/{filepath}.torrent")));

                bw.Write(File.ReadAllBytes(Path.Combine(rootPath, $"ffxiv/{_configuration["PatchHTTP:ffxiv_boothash"]}/metainfo/{filepath}.torrent")));
                bw.Flush();


                sb.Write("\r\n");

                if (nextpatchversion == finalpatchversion)
                    break;

            }



            sb.Write("--477D80B1_38BC_41d4_8B48_5273ADB89CAC--\r\n\r\n");
        }

        public void CheckGameVersion(HttpResponse response, string version)
        {
            _logger.LogInformation("Checking game version");
            if (version != _configuration["PatchHTTP:ffxiv_gameversion"])
            {
                _logger.LogInformation("Game version didn't match. Patch your shit.");
                //TODO: replace with version number lookup for stuff later
                if (version == "")
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }


                response.StatusCode = (int)HttpStatusCode.OK;
                response.Headers.Add("Content-Location", $"ffxiv/{_configuration["PatchHTTP:ffxiv_gamehash"]}/vercheck.dat");
                response.Headers.Add("Content-Type", "multipart/mixed; boundary=477D80B1_38BC_41d4_8B48_5273ADB89CAC");
                response.Headers.Add("X-Repository", $"ffxiv/win32/release/game");
                response.Headers.Add("X-Patch-Module", "ZiPatch");
                response.Headers.Add("X-Protocol", "torrent");
                response.Headers.Add("X-Info-Url", "http://example.com");
                response.Headers.Add("X-Latest-Version", _configuration["PatchHTTP:ffxiv_gameversion"]);
                response.Headers.Add("Connection", "keep-alive");

                SendGamePatches(response, version, _configuration["PatchHTTP:ffxiv_gameversion"]);
            }
            else
            {
                _logger.LogInformation("Game version matched. Proceed to login");
                response.StatusCode = (int)HttpStatusCode.NoContent;
                //response.Headers.Add("HTTP/1.0 204 No Content");
                response.Headers.Add("Content-Location", $"ffxiv/{_configuration["PatchHTTP:ffxiv_gamehash"]}/vercheck.dat");
                response.Headers.Add("X-Repository", $"ffxiv/win32/release/game");
                response.Headers.Add("X-Patch-Module", "ZiPatch");
                response.Headers.Add("X-Protocol", "torrent");
                response.Headers.Add("X-Info-Url", "http://www.example.com");
                response.Headers.Add("X-Latest-Version", _configuration["PatchHTTP:ffxiv_gameversion"]);
            }
        }

        private void SendGamePatches(HttpResponse response, string startversion, string finalpatchversion)
        {
            string rootPath = ""; // app.Environment.WebRootPath;

            //StringBuilder sb = new StringBuilder();
            using StreamWriter sb = new StreamWriter(response.Body);
            using BinaryWriter bw = new BinaryWriter(response.Body);
            string? nextpatchversion = startversion;

            while (true)
            {

                if (!PatchList.gamepatches.TryGetValue(nextpatchversion, out nextpatchversion))
                    break;

                string filepath = "";

                if (File.Exists(Path.Combine(rootPath, $"ffxiv/{_configuration["PatchHTTP:ffxiv_gamehash"]}/metainfo/D{nextpatchversion}.torrent"))
                    //&& File.Exists(Path.Combine(rootPath, $"{GAME_NAME}/{GAME_HASH}/patch/D{nextpatchversion}.patch"))
                    )
                {
                    filepath = $"D{nextpatchversion}";
                }
                else if (File.Exists(Path.Combine(rootPath, $"ffxiv/{_configuration["PatchHTTP:ffxiv_gamehash"]}/metainfo/H{nextpatchversion}.torrent"))
                    //&& File.Exists(Path.Combine(rootPath, $"{GAME_NAME}/{GAME_HASH}/patch/H{nextpatchversion}.patch"))
                    )
                {
                    filepath = $"H{nextpatchversion}";
                }

                _logger.LogInformation("Sending patch info for " + filepath);

                sb.Write("--477D80B1_38BC_41d4_8B48_5273ADB89CAC\r\n");

                sb.Write("Content-Type: application/octet-stream\r\n");
                sb.Write($"Content-Location: ffxiv/{_configuration["PatchHTTP:ffxiv_gamehash"]}/metainfo/{filepath}.torrent\r\n");
                sb.Write($"X-Patch-Length: {new FileInfo($"{rootPath}/ffxiv/{_configuration["PatchHTTP:ffxiv_gamehash"]}/metainfo/{filepath}.torrent").Length}\r\n");
                sb.Write("X-Signature: jqxmt9WQH1aXptNju6CmCdztFdaKbyOAVjdGw_DJvRiBJhnQL6UlDUcqxg2DeiIKhVzkjUm3hFXOVUFjygxCoPUmCwnbCaryNqVk_oTk_aZE4HGWNOEcAdBwf0Gb2SzwAtk69zs_5dLAtZ0mPpMuxWJiaNSvWjEmQ925BFwd7Vk=\r\n");

                sb.Write("\r\n");
                sb.Flush();
                //sb.Write(File.ReadAllBytes(Path.Combine(rootPath, $"{GAME_NAME}/{GAME_HASH}/metainfo/{filepath}.torrent")));

                bw.Write(File.ReadAllBytes(Path.Combine(rootPath, $"ffxiv/{_configuration["PatchHTTP:ffxiv_gamehash"]}/metainfo/{filepath}.torrent")));
                bw.Flush();


                sb.Write("\r\n");

                if (nextpatchversion == finalpatchversion)
                    break;

            }

            sb.Write("--477D80B1_38BC_41d4_8B48_5273ADB89CAC--\r\n\r\n");
        }
    }
}
