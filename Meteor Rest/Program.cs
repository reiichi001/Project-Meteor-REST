using Meteor_Rest;
using Meteor_Rest.Patch;
using System.Net;
using System.Text;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
{
    Args = args,
    ContentRootPath = Directory.GetCurrentDirectory(),
    WebRootPath = "www"
});

var app = builder.Build();

//Load Config
ConfigConstants.Load(app);
ConfigConstants.ApplyLaunchArgs(app, args);
PatchList.Load(app);

//Serve on the launch server port
app.Urls.Add($"http://{ConfigConstants.OPTIONS_BINDIP}:{ConfigConstants.OPTIONS_PORT}");

//Server on the patch server port
app.Urls.Add($"http://{ConfigConstants.OPTIONS_BINDIP}:{ConfigConstants.PATCHER_PORT}");

//Server on the torrent server port
app.Urls.Add($"http://{ConfigConstants.OPTIONS_BINDIP}:8999");
app.Urls.Add($"http://{ConfigConstants.OPTIONS_BINDIP}:54997");

app.UseDefaultFiles();

app.UseStaticFiles();

app.MapGet("/", (HttpResponse response) =>
{
    response.Redirect("account/content/login.html");
    
    //return "Hello from Carter!";
});


app.MapGet($"/announce", (HttpResponse response) =>
{
    ReadOnlyMemory<byte> source = File.ReadAllBytes($"{app.Environment.WebRootPath}/announce");
    response.StatusCode = 200;
    response.Headers.ContentType = "application/octet-stream";
    response.Headers.ContentLength = 0;
    response.Headers.CacheControl = "no-cache, no-store";
    response.Headers.Pragma = "no-cache";
    response.Body.WriteAsync(source);
});

app.MapGet("/patch/vercheck/ffxiv/win32/release/{*path}", PatchCheck);
async Task PatchCheck(HttpResponse response, string path)
{
    app.Logger.LogInformation($"Looking for: {path}");

    string[] paths = path?.Split('/');

    if (paths?[0] == "boot")
    {
        Patch.checkBootVersion(app, response, paths[1]); return;
    }
    else if (paths?[0] == "game")
    {
        Patch.checkGameVersion(app, response, paths[1]); return;
    }
    else
    {
        response.StatusCode = 404;
    }
    //ReadOnlyMemory<byte> source = File.ReadAllBytes("news.html");
    response.ContentType = "text/html";
    //response.ContentLength = source.Length;
    //await response.Body.WriteAsync(source);
    await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Not Implented"));
}

app.MapGet("/account/content/ffxivlogin", (HttpResponse response) => response.Redirect(Constant.loginPage));

app.Run();