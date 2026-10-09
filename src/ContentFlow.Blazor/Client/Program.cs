// WebAssembly boot entry for the Client bundle (InteractiveAuto shop area).
//
// This host activates when the browser downloads the client bundle after server
// prerendering. Shop components must stay free of server-only dependencies
// (no Infra implementations, no DbContext, no secrets).

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

await builder.Build().RunAsync();
