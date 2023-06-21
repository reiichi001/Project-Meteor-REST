using Meteor_Rest;
using System.IO;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
/*
var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
{
    Args = args,
    ContentRootPath = Directory.GetCurrentDirectory(),
    WebRootPath = "www"
});
*/

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

//Serve on the launch server port
app.Urls.Add($"http://{app.Configuration["General:rest_server_ip"]}:{app.Configuration["General:rest_server_port"]}");

//Server on the patch server port
app.Urls.Add($"http://{app.Configuration["General:rest_server_ip"]}:{app.Configuration["PatchHTTP:ffxiv_patchserver_port"]}");

//Server on the torrent server port
app.Urls.Add($"http://{app.Configuration["General:rest_server_ip"]}:8999");
app.Urls.Add($"http://{app.Configuration["General:rest_server_ip"]}:54997");

AuthHandler authHandler = new AuthHandler(app.Logger, app.Configuration);
PatchHandler patchHandler = new PatchHandler(app.Logger, app.Configuration);



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

    string[] paths = path.Split('/');

    if (paths?[0] == "boot")
    {
        patchHandler.CheckBootVersion(response, paths[1]); return;
    }
    else if (paths?[0] == "game")
    {
        patchHandler.CheckGameVersion(response, paths[1]); return;
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

app.MapGet("/account/content/ffxivlogin", (HttpResponse response) => response.Redirect(app.Configuration["General:rest_login_page"]));

app.MapPost("/lobby/{*path}", HandleAuth);
async Task HandleAuth(HttpRequest request, HttpResponse response, string path)

{
    app.Logger.LogInformation($"Looking for: {path}");
    string[] paths = path.Split('/');

    if (paths?[0] == "createaccount")
    {
        authHandler.CreateAccount(request, response); return;
    }
    else if (paths?[0] == "login")
    {
        authHandler.LoginAccount(request, response); return;
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
};

app.Run();