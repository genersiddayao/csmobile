using CSharpMobile;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
var host = builder.Build();

Bridge.JS = (IJSInProcessRuntime)host.Services.GetRequiredService<IJSRuntime>();
await Bridge.JS.InvokeVoidAsync("csm.dotnetReady");

await host.RunAsync();
