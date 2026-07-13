using PersonalFinanceTracker.Infrastructure.DependencyInjection;
using PersonalFinanceTracker.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddPersonalFinanceInfrastructure(builder.Configuration);

var app = builder.Build();

// Dev-friendly: create the SQLite schema on first run so there's no manual
// migration step for anyone cloning the repo. See ServiceCollectionExtensions
// for the trade-off note on EnsureCreated vs real migrations.
app.Services.EnsurePersonalFinanceDatabaseCreated();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
