using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Web; 
using Serilog;
using FluentEmail.MailKitSmtp;
using MudBlazor.Services;
using PadelMatcherNet.Components;
using PadelMatcherNet.Components.Account;
using PadelMatcherNet.Data;
using PadelMatcherNet.Services;

var builder = WebApplication.CreateBuilder(args);

// Add MudBlazor services
builder.Services.AddMudServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// Theme Service Unificato
builder.Services.AddScoped<IUnifiedThemeService, UnifiedThemeService>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

// Configurazione database per Identity
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configurazione database per Tournament (stesso database, contesti separati)
builder.Services.AddDbContext<TournamentDbContext>(options =>
    options.UseSqlite(connectionString));

// Registrazione servizi personalizzati
builder.Services.AddScoped<PadelMatcherNet.Services.ISettingsService, PadelMatcherNet.Services.SettingsService>();
builder.Services.AddScoped<PadelMatcherNet.Services.IPlayerService, PadelMatcherNet.Services.PlayerService>();
builder.Services.AddScoped<PadelMatcherNet.Services.ITournamentService, PadelMatcherNet.Services.TournamentService>();
builder.Services.AddScoped<PadelMatcherNet.Services.IMatchService, PadelMatcherNet.Services.MatchService>();
builder.Services.AddScoped<PadelMatcherNet.Services.IStatsService, PadelMatcherNet.Services.StatsService>();
builder.Services.AddScoped<PadelMatcherNet.Services.ICleanupService, PadelMatcherNet.Services.CleanupService>();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

//builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<HtmlRenderer>(); 

// Solo questo, basta e avanza
builder.Services.AddScoped<RazorComponentRenderer>();
// Registra i servizi di Identity per l'invio di email, utilizzando un'implementazione fittizia (NoOp) per evitare errori di invio email.
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
// Configura la sezione EmailSettings dalla configurazione dell'applicazione (ad esempio, appsettings.json) 
// e la associa alla classe EmailSettings per l'iniezione delle dipendenze.
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

var emailSettings = builder.Configuration
    .GetSection("EmailSettings")
    .Get<EmailSettings>() ?? throw new InvalidOperationException("Le impostazioni email non sono state caricate correttamente da appsettings.json");

/*Registra il servizio FluentEmail per l'invio di email, utilizzando le impostazioni SMTP specificate nella configurazione dell'applicazione.
  FluentEmail è una libreria per semplificare l'invio di email in .NET.
  In questo caso, viene utilizzato il client SMTP di MailKit per inviare le email.
  Viene configurato il server SMTP, la porta, se utilizzare SSL, e le credenziali di autenticazione (username e password).
  Queste informazioni vengono lette dalla sezione EmailSettings della configurazione dell'applicazione.
  Viene anche specificato l'indirizzo email e il nome del mittente (FromEmail e FromName) che appariranno come mittente delle email inviate.*/
builder.Services
    .AddFluentEmail(emailSettings.FromEmail, emailSettings.FromName)
    .AddMailKitSender(new SmtpClientOptions
    {
        Server = emailSettings.SmtpServer,
        Port = emailSettings.SmtpPort,
        UseSsl = emailSettings.UseSSL,
        RequiresAuthentication = true,
        User = emailSettings.SmtpUser,
        Password = emailSettings.SmtpPass
    });

// Registra il servizio di invio email personalizzato IEmailService, che utilizza FluentEmail per inviare email.
builder.Services.AddScoped<IEmailService, EmailService>();

// Configura Serilog come sistema di logging per l'applicazione, leggendo la configurazione dall'appsettings.json,
// utilizzando i servizi registrati per arricchire i log e aggiungendo informazioni contestuali ai log.
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();