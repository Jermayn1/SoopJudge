using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using SoopWorkshop.Backend.API.Configuration;
using SoopWorkshop.Backend.API.Middleware;
using SoopWorkshop.Backend.Application;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Infrastructure;
using SoopWorkshop.Backend.Infrastructure.Configuration;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// In der Entwicklung ist die .env im Repository-Wurzelverzeichnis die eine Wahrheit —
// dieselbe Datei, aus der docker-compose die Datenbank aufsetzt. Sie wird zuletzt
// hinzugefügt und schlägt damit auch Umgebungsvariablen, damit eine vergessene
// Variable in der Shell nicht still etwas anderes bewirkt als in der Datei steht.
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddDotEnv(builder.Environment.ContentRootPath);
}

// Add services to the container.

// Enums gehen als Zeichenkette über die Leitung, nicht als Zahl — sonst liest ein
// Frontend außerhalb von .NET "difficulty": 0 und muss die Bedeutung raten.
// Der Konverter steht an den Enums selbst (SoopWorkshop.Shared/Enums), nicht hier:
// eine Registrierung über AddJsonOptions wirkt nur zur Laufzeit, der OpenAPI-Erzeuger
// liest den Typ. Beides getrennt zu pflegen hieße, zwei Wahrheiten zu haben.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Application und Infrastructure einbinden
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Zugangsschutz für api/admin/*. Bricht den Start ab, wenn kein Passwort
// gesetzt ist — siehe AdminAuthenticationExtensions.
builder.Services.AddAdminAuthentication(builder.Configuration, builder.Environment);

// Hinter dem Reverse Proxy endet TLS bei nginx; das Backend selbst spricht nur
// http. Ohne diese Köpfe sähe es dauerhaft "http" und würde das Anmelde-Cookie
// als nicht-Secure ausstellen, obwohl der Browser über https spricht.
//
// KnownIPNetworks und KnownProxies werden geleert, weil der Proxy ein anderer
// Container mit wechselnder Adresse ist - die Voreinstellung vertraut nur
// Loopback und verwirft die Köpfe still. Das ist nur vertretbar, WEIL das
// Backend im Compose-Aufbau in einem internen Netz ohne veröffentlichten Port
// liegt und ausschließlich über den Proxy erreichbar ist. Wird es jemals
// direkt veröffentlicht, muss hier ein bekanntes Netz eingetragen werden -
// sonst kann jeder Aufrufer das Schema fälschen.
if (!builder.Environment.IsDevelopment())
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

// CORS (Cross-Origin Resource Sharing), erlaubt es dem Frontend Requests an die API zu senden.
// Die erlaubten Origins stehen in der Konfiguration, damit sie im Betrieb über
// Umgebungsvariablen gesetzt werden können.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            // Ohne das schickt der Browser das Anmelde-Cookie nicht mit, weil
            // Frontend und API verschiedene Origins sind. Erlaubt ist die
            // Kombination nur, weil die Origins oben namentlich gelistet sind —
            // zusammen mit AllowAnyOrigin lehnt ASP.NET sie ab, und zwar zu Recht.
            .AllowCredentials();
    });
});

var app = builder.Build();

// Welche Werte wirklich gelten, gehört in die erste Logzeile und nicht in den
// Stacktrace einer fehlgeschlagenen Abfrage. Das Passwort bleibt außen vor.
var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("SoopWorkshop.Start");
var evaluationOptions = app.Services.GetRequiredService<IOptions<EvaluationOptions>>().Value;

startupLogger.LogInformation(
    "Konfiguration: Datenbank {Database}, Auswertung {MaxConcurrency} gleichzeitig, " +
    "Zeitgrenzen {CompileTimeout}s kompilieren / {RunTimeout}s ausfuehren.",
    ConnectionStringSummary.Describe(app.Configuration.GetConnectionString("DefaultConnection")),
    evaluationOptions.MaxConcurrency,
    evaluationOptions.CompileTimeoutSeconds,
    evaluationOptions.RunTimeoutSeconds);

// Muss vor allem stehen, was Schema oder Aufrufer-Adresse auswertet — sonst
// arbeitet die Kette mit der Adresse des Proxys statt der des Browsers.
if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders();
}

// Exception Middleware einbinden
app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Zum testen der Endpunkte und Datenbank befüllen zum test
    app.MapScalarApiReference();

    // Nur in der Entwicklung, wo das https-Startprofil auf 7212 horcht. Im
    // Container horcht das Backend ausschließlich auf http, die Umleitung
    // macht dort der Reverse Proxy — eine Umleitung hier würde ins Leere
    // zeigen, weil das Backend keinen https-Port kennt.
    app.UseHttpsRedirection();
}

// CORS einbinden
app.UseCors("AllowFrontend");

// Reihenfolge ist Pflicht: erst feststellen, wer da ist, dann entscheiden, ob
// er darf. Umgedreht liest UseAuthorization eine noch leere Identität und
// weist jeden Aufruf ab, auch den angemeldeten.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Bewusst ohne Anmeldung: der Healthcheck läuft im Compose-Aufbau als
// Container-Befehl und hat kein Cookie. Er gibt nur "Healthy" oder "Unhealthy"
// zurück, keine Innenansicht.
app.MapHealthChecks("/health");

app.Run();

// Nur für die Integrationstests. Top-Level-Statements erzeugen zwar eine
// Program-Klasse, aber eine interne - WebApplicationFactory<Program> braucht
// sie öffentlich. Zur Laufzeit ändert das nichts.
public partial class Program;
