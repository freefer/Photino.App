using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using PhotinoNET;
using PhotinoNET.Server;
using System.Net.NetworkInformation;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Photino.HelloPhotino.Angular
{
    internal class Program
    {
        public static WebApplication CreateStaticFileServer(string[] args, int startPort, int portRange, string webRootFolder, out string baseUrl)
        {
            WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = args,
                WebRootPath = webRootFolder
            });

            webApplicationBuilder.Environment.ContentRootPath = webRootFolder;
            int port;
            DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
            for (port = startPort; IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any((IPEndPoint x) => x.Port == port); port++)
            {
                if (port > port + portRange)
                {
                    defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(41, 2);
                    defaultInterpolatedStringHandler.AppendLiteral("Couldn't find open port within range ");
                    defaultInterpolatedStringHandler.AppendFormatted(port - portRange);
                    defaultInterpolatedStringHandler.AppendLiteral(" - ");
                    defaultInterpolatedStringHandler.AppendFormatted(port);
                    defaultInterpolatedStringHandler.AppendLiteral(".");
                    throw new SystemException(defaultInterpolatedStringHandler.ToStringAndClear());
                }
            }

            defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(17, 1);
            defaultInterpolatedStringHandler.AppendLiteral("http://localhost:");
            defaultInterpolatedStringHandler.AppendFormatted(port);
            baseUrl = defaultInterpolatedStringHandler.ToStringAndClear();
            webApplicationBuilder.WebHost.UseUrls(baseUrl);
            WebApplication webApplication = webApplicationBuilder.Build();

            return webApplication;
        }
        [STAThread]
        static void Main(string[] args)
        {

            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".*"] = "application/octet-stream";//配置添加新的映射关系

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var indexPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "index.html");
            var server = Program.CreateStaticFileServer(args, 8000, 100, "wwwroot", out string baseUrl);

            DefaultFilesOptions defaultFilesOptions = new DefaultFilesOptions();
            defaultFilesOptions.DefaultFileNames.Clear(); // 清除默认的文件名
            defaultFilesOptions.DefaultFileNames.Add("index.html"); // 设置新的默认文件名

            server.UseDefaultFiles(defaultFilesOptions);
            server.UseStaticFiles();

            server.RunAsync();

            // Window title declared here for visibility
            string windowTitle = "Photino.Angular Demo App";

            // Creating a new PhotinoWindow instance with the fluent API
            var window = new PhotinoWindow()
                //.SetTitle(windowTitle)

                // .SetFullScreen(true)
                // Resize to a percentage of the main monitor work area
                //.Resize(50, 50, "%")
                // Center window in the middle of the screen
                .Center()
                // Users can resize windows by default.
                // Let's make this one fixed instead.
                .SetResizable(true)
                .RegisterCustomSchemeHandler("app", (object sender, string scheme, string url, out string contentType) =>
                {
                    contentType = "text/javascript";
                    return new MemoryStream(Encoding.UTF8.GetBytes(@"
                        (() =>{
                            window.setTimeout(() => {
                                alert(`🎉 Dynamically inserted JavaScript.`);
                            }, 1000);
                        })();
                    "));
                })
                // Most event handlers can be registered after the
                // PhotinoWindow was instantiated by calling a registration 
                // method like the following RegisterWebMessageReceivedHandler.
                // This could be added in the PhotinoWindowOptions if preferred.
                .RegisterWebMessageReceivedHandler((object sender, string message) =>
                {
                    var window = (PhotinoWindow)sender;

                    // The message argument is coming in from sendMessage.
                    // "window.external.sendMessage(message: string)"
                    string response = $"Received message: \"{message}\"";

                    // Send a message back the to JavaScript event handler.
                    // "window.external.receiveMessage(callback: Function)"
                    window.SendWebMessage(response);
                })
                .Load($"{baseUrl}"); // Can be used with relative path strings or "new URI()" instance to load a website.

            window.WaitForClose(); // Starts the application event loop
        }
    }
}