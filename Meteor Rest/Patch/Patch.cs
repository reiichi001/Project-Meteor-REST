using System.Net;

namespace Meteor_Rest.Patch
{
    public class Patch
    {
        public static void checkBootVersion(WebApplication app, HttpResponse response, string version)
        {
            app.Logger.LogInformation("Checking boot version");
            if (version != ConfigConstants.BOOT_VERSION)
            {
                app.Logger.LogInformation("Boot version " + version + " didn't match. Patch your shit.");
                //TODO: replace with version number lookup for stuff later
                String newpatchversion;

                if (!PatchList.bootpatches.TryGetValue(version, out newpatchversion))
                {
                    app.Logger.LogInformation("Invalid boot patch requested: " + version);
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    //response.Headers.Add("HTTP/1.0 404 NOT FOUND");
                    return;
                }

                response.StatusCode = (int)HttpStatusCode.OK;
                response.Headers.Add("Content-Location", $"{Constant.GAME_NAME}/{Constant.BOOT_HASH}/vercheck.dat");
                response.Headers.Add("Content-Type", "multipart/mixed; boundary=477D80B1_38BC_41d4_8B48_5273ADB89CAC");
                response.Headers.Add("X-Repository", $"{Constant.GAME_NAME}/win32/release/boot");
                response.Headers.Add("X-Patch-Module", "ZiPatch");
                response.Headers.Add("X-Protocol", "torrent");
                response.Headers.Add("X-Info-Url", "http://example.com");
                response.Headers.Add("X-Latest-Version", ConfigConstants.BOOT_VERSION);
                response.Headers.Add("Connection", "keep-alive");

                sendBootPatches(app, response, version, ConfigConstants.BOOT_VERSION);
            }
            else
            {
                app.Logger.LogInformation("Boot version matched. Proceed to game version check");
                response.StatusCode = (int)HttpStatusCode.NoContent;
                //response.Headers.Add("HTTP/1.0 204 No Content");
                response.Headers.Add("Content-Location", $"{Constant.GAME_NAME}/{Constant.BOOT_HASH}/vercheck.dat");
                response.Headers.Add("X-Repository", $"{Constant.GAME_NAME}/win32/release/boot");
                response.Headers.Add("X-Patch-Module", "ZiPatch");
                response.Headers.Add("X-Protocol", "torrent");
                response.Headers.Add("X-Info-Url", "http://www.example.com");
                response.Headers.Add("X-Latest-Version", ConfigConstants.BOOT_VERSION);
            }

        }

        private static void sendBootPatches(WebApplication app, HttpResponse response, string startversion, string finalpatchversion)
        {


            //StringBuilder sb = new StringBuilder();
            StreamWriter sb = new StreamWriter(response.Body);
            BinaryWriter bw = new BinaryWriter(response.Body);
            string nextpatchversion = startversion;

            while (true)
            {

                if (!PatchList.bootpatches.TryGetValue(nextpatchversion, out nextpatchversion))
                    break;

                string rootPath = app.Environment.WebRootPath;
                string filepath = "";

                if (File.Exists(Path.Combine(rootPath, $"{Constant.GAME_NAME}/{Constant.BOOT_HASH}/metainfo/D{nextpatchversion}.torrent")) &&
                    File.Exists(Path.Combine(rootPath, $"{Constant.GAME_NAME}/{Constant.BOOT_HASH}/patch/D{nextpatchversion}.patch")))
                {
                    filepath = $"D{nextpatchversion}";
                }
                else if (File.Exists(Path.Combine(rootPath, $"{Constant.GAME_NAME}/{Constant.BOOT_HASH}/metainfo/H{nextpatchversion}.torrent")) &&
                    File.Exists(Path.Combine(rootPath, $"{Constant.GAME_NAME}/{Constant.BOOT_HASH}/patch/H{nextpatchversion}.patch")))
                {
                    filepath = $"H{nextpatchversion}";
                }

                app.Logger.LogInformation("Sending patch info for " + filepath);

                sb.Write("--477D80B1_38BC_41d4_8B48_5273ADB89CAC\r\n");

                sb.Write("Content-Type: application/octet-stream\r\n");
                sb.Write($"Content-Location: {Constant.GAME_NAME}/{Constant.BOOT_HASH}/metainfo/{filepath}.torrent\r\n");
                sb.Write($"X-Patch-Length: {new FileInfo($"{rootPath}/{Constant.GAME_NAME}/{Constant.BOOT_HASH}/metainfo/{filepath}.torrent").Length}\r\n");
                sb.Write("X-Signature: jqxmt9WQH1aXptNju6CmCdztFdaKbyOAVjdGw_DJvRiBJhnQL6UlDUcqxg2DeiIKhVzkjUm3hFXOVUFjygxCoPUmCwnbCaryNqVk_oTk_aZE4HGWNOEcAdBwf0Gb2SzwAtk69zs_5dLAtZ0mPpMuxWJiaNSvWjEmQ925BFwd7Vk=\r\n");

                sb.Write("\r\n");
                sb.Flush();
                //sb.Write(File.ReadAllBytes(Path.Combine(rootPath, $"{GAME_NAME}/{BOOT_HASH}/metainfo/{filepath}.torrent")));

                bw.Write(File.ReadAllBytes(Path.Combine(rootPath, $"{Constant.GAME_NAME}/{Constant.BOOT_HASH}/metainfo/{filepath}.torrent")));
                bw.Flush();


                sb.Write("\r\n");

                if (nextpatchversion == finalpatchversion)
                    break;

            }



            sb.Write("--477D80B1_38BC_41d4_8B48_5273ADB89CAC--\r\n\r\n");

            sb.Flush();
            bw.Close();
            bw.Dispose();
            sb.Close();
            sb.Dispose();
        }

        public static void checkGameVersion(WebApplication app, HttpResponse response, string version)
        {
            app.Logger.LogInformation("Checking game version");
            if (version != ConfigConstants.GAME_VERSION)
            {
                app.Logger.LogInformation("Game version didn't match. Patch your shit.");
                //TODO: replace with version number lookup for stuff later
                if (version == "")
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }

                response.StatusCode = (int)HttpStatusCode.OK;
                response.Headers.Add("Content-Location", $"{Constant.GAME_NAME}/{Constant.GAME_HASH}/vercheck.dat");
                response.Headers.Add("Content-Type", "multipart/mixed; boundary=477D80B1_38BC_41d4_8B48_5273ADB89CAC");
                response.Headers.Add("X-Repository", $"{Constant.GAME_NAME}/win32/release/game");
                response.Headers.Add("X-Patch-Module", "ZiPatch");
                response.Headers.Add("X-Protocol", "torrent");
                response.Headers.Add("X-Info-Url", "http://example.com");
                response.Headers.Add("X-Latest-Version", ConfigConstants.GAME_VERSION);
                response.Headers.Add("Connection", "keep-alive");

                sendGamePatches(app, response, version, ConfigConstants.GAME_VERSION);
            }
            else
            {
                app.Logger.LogInformation("Game version matched. Proceed to login");
                response.StatusCode = (int)HttpStatusCode.NoContent;
                //response.Headers.Add("HTTP/1.0 204 No Content");
                response.Headers.Add("Content-Location", $"{Constant.GAME_NAME}/{Constant.GAME_HASH}/vercheck.dat");
                response.Headers.Add("X-Repository", $"{Constant.GAME_NAME}/win32/release/game");
                response.Headers.Add("X-Patch-Module", "ZiPatch");
                response.Headers.Add("X-Protocol", "torrent");
                response.Headers.Add("X-Info-Url", "http://www.example.com");
                response.Headers.Add("X-Latest-Version", ConfigConstants.GAME_VERSION);
            }
        }

        private static void sendGamePatches(WebApplication app, HttpResponse response, string startversion, string finalpatchversion)
        {
            string rootPath = app.Environment.WebRootPath;

            //StringBuilder sb = new StringBuilder();
            StreamWriter sb = new StreamWriter(response.Body);
            BinaryWriter bw = new BinaryWriter(response.Body);
            string nextpatchversion = startversion;

            while (true)
            {

                if (!PatchList.gamepatches.TryGetValue(nextpatchversion, out nextpatchversion))
                    break;

                string filepath = "";

                if (File.Exists(Path.Combine(rootPath, $"{Constant.GAME_NAME}/{Constant.GAME_HASH}/metainfo/D{nextpatchversion}.torrent"))
                    //&& File.Exists(Path.Combine(rootPath, $"{GAME_NAME}/{GAME_HASH}/patch/D{nextpatchversion}.patch"))
                    )
                {
                    filepath = $"D{nextpatchversion}";
                }
                else if (File.Exists(Path.Combine(rootPath, $"{Constant.GAME_NAME}/{Constant.GAME_HASH}/metainfo/H{nextpatchversion}.torrent"))
                    //&& File.Exists(Path.Combine(rootPath, $"{GAME_NAME}/{GAME_HASH}/patch/H{nextpatchversion}.patch"))
                    )
                {
                    filepath = $"H{nextpatchversion}";
                }

                app.Logger.LogInformation("Sending patch info for " + filepath);

                sb.Write("--477D80B1_38BC_41d4_8B48_5273ADB89CAC\r\n");

                sb.Write("Content-Type: application/octet-stream\r\n");
                sb.Write($"Content-Location: {Constant.GAME_NAME}/{Constant.GAME_HASH}/metainfo/{filepath}.torrent\r\n");
                sb.Write($"X-Patch-Length: {new FileInfo($"{rootPath}/{Constant.GAME_NAME}/{Constant.GAME_HASH}/metainfo/{filepath}.torrent").Length}\r\n");
                sb.Write("X-Signature: jqxmt9WQH1aXptNju6CmCdztFdaKbyOAVjdGw_DJvRiBJhnQL6UlDUcqxg2DeiIKhVzkjUm3hFXOVUFjygxCoPUmCwnbCaryNqVk_oTk_aZE4HGWNOEcAdBwf0Gb2SzwAtk69zs_5dLAtZ0mPpMuxWJiaNSvWjEmQ925BFwd7Vk=\r\n");

                sb.Write("\r\n");
                sb.Flush();
                //sb.Write(File.ReadAllBytes(Path.Combine(rootPath, $"{GAME_NAME}/{GAME_HASH}/metainfo/{filepath}.torrent")));

                bw.Write(File.ReadAllBytes(Path.Combine(rootPath, $"{Constant.GAME_NAME}/{Constant.GAME_HASH}/metainfo/{filepath}.torrent")));
                bw.Flush();


                sb.Write("\r\n");

                if (nextpatchversion == finalpatchversion)
                    break;

            }



            sb.Write("--477D80B1_38BC_41d4_8B48_5273ADB89CAC--\r\n\r\n");

            sb.Flush();
            bw.Close();
            bw.Dispose();
            sb.Close();
            sb.Dispose();
        }
    }
}
