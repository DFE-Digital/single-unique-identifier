#!/usr/bin/env dotnet
#:package Microsoft.IdentityModel.JsonWebTokens@8.20.0
#:package System.CommandLine@2.0.15
#:property PublishAot=false

// Creates, lists and removes the NHS Multicast Notification Service (MNS) subscriptions that deliver
// pds-record-change-2 events to our MESH mailbox. See README.md in this folder (the runbook).
//
// Usage (run with --help for details):
//   dotnet run scripts/mns_subscribe/subscribe.cs -- list   --env int
//   dotnet run scripts/mns_subscribe/subscribe.cs -- create --env int --mailbox <MAILBOX_ID> --workflow-id <ID> [--dry-run]
//   dotnet run scripts/mns_subscribe/subscribe.cs -- remove --env int --mailbox <MAILBOX_ID> [--id <ID> ...]
//
// Secrets are read from environment variables only (never pass them as arguments):
//   NHS_DIGITAL_CLIENT_ID, NHS_DIGITAL_KID, NHS_DIGITAL_PRIVATE_KEY (PEM)
// The sandbox environment (--env sandbox) is open access and needs no credentials.

using System.CommandLine;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

// The subscriptions to create: (short name used in the reason, MNS criteria).
// String values in criteria must be single-quoted; filters are joined with uppercase AND.
(string Name, string Criteria)[] subscriptions =
[
    ("GP change", "eventType='pds-record-change-2' AND changed_gp=True"),
    ("NHS number change", "eventType='pds-record-change-2' AND changed_nhsnumber=True"),
];

// TODO: We only want events for under-18s. MNS will add an age filter later. When it is available, add it to every criteria above (all
// environments) and replace the existing subscriptions (see "Under-18 filter" in README.md).

// Keep quotes and '+' readable in the JSON rather than \u-escaped (e.g. ').
var jsonOptions = new JsonSerializerOptions
{
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
};
var indentedJsonOptions = new JsonSerializerOptions(jsonOptions) { WriteIndented = true };

// ---------------------------------------------------------------------------
// 1. Command-line options (System.CommandLine handles validation and --help)
// ---------------------------------------------------------------------------

var envOption = new Option<string>("--env")
{
    Description = "NHS API environment (sandbox needs no credentials)",
    Required = true,
};
envOption.AcceptOnlyFromAmong("int", "prod", "sandbox");

var mailboxOption = new Option<string>("--mailbox")
{
    Description = "MESH mailbox ID that MNS delivers events to",
    Required = true,
};

var workflowIdOption = new Option<string>("--workflow-id")
{
    Description =
        "MESH workflow ID added to the endpoint (mesh://<MAILBOX>?workflow_id=<ID>). MNS accepts any value",
    Required = true,
};

var dryRunOption = new Option<bool>("--dry-run")
{
    Description =
        "Check existing subscriptions and show which would be created or skipped, without creating anything (needs credentials, except sandbox)",
};

var createCommand = new Command(
    "create",
    "Create the MNS pds-record-change-2 subscriptions for a MESH mailbox, skipping any that already exist"
)
{
    envOption,
    mailboxOption,
    workflowIdOption,
    dryRunOption,
};
createCommand.SetAction(
    (parseResult, cancellationToken) =>
        CreateAsync(
            parseResult.GetValue(envOption)!,
            parseResult.GetValue(mailboxOption)!,
            parseResult.GetValue(workflowIdOption)!,
            parseResult.GetValue(dryRunOption),
            cancellationToken
        )
);

var listCommand = new Command("list", "List all MNS subscriptions owned by our application")
{
    envOption,
};
listCommand.SetAction(
    (parseResult, cancellationToken) =>
        ListAsync(parseResult.GetValue(envOption)!, cancellationToken)
);

// Required so 'remove' can never delete every subscription the application owns across mailboxes.
var removeMailboxOption = new Option<string>("--mailbox")
{
    Description = "MESH mailbox ID whose subscriptions to remove",
    Required = true,
};

var idOption = new Option<string[]>("--id")
{
    Description =
        "Only remove the subscription with this ID (repeat for several); it must deliver to --mailbox",
};

var removeCommand = new Command(
    "remove",
    "Delete the MNS subscriptions that deliver to a MESH mailbox, after confirmation"
)
{
    envOption,
    removeMailboxOption,
    idOption,
};
removeCommand.SetAction(
    (parseResult, cancellationToken) =>
        RemoveAsync(
            parseResult.GetValue(envOption)!,
            parseResult.GetValue(removeMailboxOption)!,
            parseResult.GetValue(idOption) ?? [],
            cancellationToken
        )
);

var rootCommand = new RootCommand(
    "Create, list and remove the MNS subscriptions for a MESH mailbox"
)
{
    createCommand,
    listCommand,
    removeCommand,
};

return await rootCommand.Parse(args).InvokeAsync();

// ---------------------------------------------------------------------------
// 2. create: build and send the FHIR Subscription requests
// ---------------------------------------------------------------------------

// Creates the subscriptions that do not already exist. Returns the process exit code (0 = success).
async Task<int> CreateAsync(
    string environment,
    string mailbox,
    string workflowId,
    bool dryRun,
    CancellationToken cancellationToken
)
{
    var baseUrl = BaseUrl(environment);
    var tokenUrl = $"{baseUrl}/oauth2/token";
    var subscriptionsUrl = $"{baseUrl}/multicast-notification-service/subscriptions";

    // MNS delivers to MESH using this endpoint format. MNS needs a workflow ID but does not require
    // a specific value; MESH messages arrive tagged with it.
    var meshEndpoint = $"mesh://{mailbox}?workflow_id={workflowId}";

    var requests = subscriptions
        .Select(s => new
        {
            s.Criteria,
            Body = new JsonObject
            {
                ["resourceType"] = "Subscription",
                ["status"] = "requested",
                ["reason"] = $"SUI NotificationService - PDS record change ({s.Name})",
                ["criteria"] = s.Criteria,
                ["channel"] = new JsonObject
                {
                    ["type"] = "message",
                    ["endpoint"] = meshEndpoint,
                    // NotificationService parses FHIR Bundles, so ask for FHIR JSON.
                    ["payload"] = "application/fhir+json",
                },
            },
        })
        .ToList();

    using var http = CreateHttpClient();

    var (authenticated, accessToken) = await AuthenticateAsync(
        http,
        environment,
        tokenUrl,
        cancellationToken
    );
    if (!authenticated)
    {
        return 1;
    }

    // Skip any subscription that already exists, so the script is safe to re-run after a
    // partial failure. Duplicates could mean duplicate MESH messages.
    var existing = await GetSubscriptionsAsync(
        http,
        subscriptionsUrl,
        accessToken,
        cancellationToken
    );
    if (existing is null)
    {
        return 1;
    }

    // Set when a skipped subscription is not active, so the run does not report success while
    // no events are being delivered for that criteria.
    var skippedInactive = false;

    // Stop at the first failure so we know exactly what was created.
    foreach (var request in requests)
    {
        var match = existing.FirstOrDefault(s => IsSameSubscription(s, request.Criteria, mailbox));
        if (match is not null)
        {
            var matchStatus = match["status"]?.ToString();
            var matchEndpoint = (match["channel"] as JsonObject)?["endpoint"]?.ToString();

            Console.WriteLine(
                $"{(dryRun ? "WOULD SKIP" : "SKIPPED")} (already exists): {request.Criteria}"
            );
            Console.WriteLine($"  Subscription ID: {match["id"]}  {matchStatus}");
            Console.WriteLine($"  Endpoint:        {matchEndpoint}");
            if (!string.Equals(matchEndpoint, meshEndpoint, StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine(
                    $"  WARNING: the existing endpoint differs from the requested {meshEndpoint}. "
                        + "Existing subscriptions keep their workflow ID; to change it, delete this subscription and re-run."
                );
            }
            if (matchStatus != "active")
            {
                Console.Error.WriteLine(
                    $"  WARNING: this subscription is '{matchStatus}', not 'active', so it may not be delivering events. "
                        + "Run 'list' for details."
                );
                skippedInactive = true;
            }
            continue;
        }

        if (dryRun)
        {
            Console.WriteLine($"WOULD CREATE: {request.Criteria}");
            Console.WriteLine($"  POST {subscriptionsUrl}");
            Console.WriteLine(request.Body.ToJsonString(indentedJsonOptions));
            continue;
        }

        var correlationId = Guid.NewGuid().ToString();
        using var message = new HttpRequestMessage(HttpMethod.Post, subscriptionsUrl)
        {
            Content = new StringContent(
                request.Body.ToJsonString(jsonOptions),
                System.Text.Encoding.UTF8,
                "application/fhir+json"
            ),
        };

        HttpResponseMessage response;
        try
        {
            response = await SendAsync(
                http,
                message,
                accessToken,
                correlationId,
                cancellationToken
            );
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            // No response (timeout, dropped connection or Ctrl+C), but the request may still have
            // reached MNS, so we cannot say whether the subscription was created.
            Console.Error.WriteLine($"OUTCOME UNKNOWN: {request.Criteria}");
            Console.Error.WriteLine($"  Correlation ID: {correlationId}");
            Console.Error.WriteLine($"  Error:          {ex.GetType().Name}: {ex.Message}");
            Console.Error.WriteLine(
                "  The subscription may have been created. Run 'list' to check; re-running 'create' skips any that exist."
            );
            return 1;
        }

        using (response)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            // MNS documents 201 Created, but accept any 2xx: treating an unexpected success as a
            // failure could lead someone to re-run the script and create a duplicate subscription.
            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"FAILED:  {request.Criteria}");
                WriteFailure(response, correlationId, responseBody);
                return 1;
            }

            Console.WriteLine($"CREATED ({(int)response.StatusCode}): {request.Criteria}");
            Console.WriteLine($"  Subscription ID: {TryGetId(responseBody) ?? "(not returned)"}");
            Console.WriteLine($"  Correlation ID:  {correlationId}");
            if ((int)response.StatusCode != 201)
            {
                // Not the documented status, so show the full response for the ticket.
                Console.WriteLine($"  Response:        {responseBody}");
            }
        }
    }

    if (dryRun)
    {
        Console.WriteLine("[dry run] Nothing was created.");
    }

    return skippedInactive ? 1 : 0;
}

// ---------------------------------------------------------------------------
// 3. list: print every subscription owned by our application
// ---------------------------------------------------------------------------

// Lists every subscription owned by the calling application. Returns the process exit code.
async Task<int> ListAsync(string environment, CancellationToken cancellationToken)
{
    var baseUrl = BaseUrl(environment);
    var tokenUrl = $"{baseUrl}/oauth2/token";
    var subscriptionsUrl = $"{baseUrl}/multicast-notification-service/subscriptions";

    using var http = CreateHttpClient();

    var (authenticated, accessToken) = await AuthenticateAsync(
        http,
        environment,
        tokenUrl,
        cancellationToken
    );
    if (!authenticated)
    {
        return 1;
    }

    var existing = await GetSubscriptionsAsync(
        http,
        subscriptionsUrl,
        accessToken,
        cancellationToken
    );
    if (existing is null)
    {
        return 1;
    }

    foreach (var subscription in existing)
    {
        WriteSubscription(subscription, jsonOptions);
    }

    Console.WriteLine(
        existing.Count == 0 ? "No subscriptions." : $"{existing.Count} subscription(s)."
    );
    return 0;
}

// ---------------------------------------------------------------------------
// 4. remove: delete a mailbox's subscriptions, after confirmation
// ---------------------------------------------------------------------------

// Deletes the owned subscriptions that deliver to the mailbox (only the given IDs, if any), after
// the user types 'yes'. Returns the process exit code.
async Task<int> RemoveAsync(
    string environment,
    string mailbox,
    string[] ids,
    CancellationToken cancellationToken
)
{
    var baseUrl = BaseUrl(environment);
    var tokenUrl = $"{baseUrl}/oauth2/token";
    var subscriptionsUrl = $"{baseUrl}/multicast-notification-service/subscriptions";

    using var http = CreateHttpClient();

    var (authenticated, accessToken) = await AuthenticateAsync(
        http,
        environment,
        tokenUrl,
        cancellationToken
    );
    if (!authenticated)
    {
        return 1;
    }

    var existing = await GetSubscriptionsAsync(
        http,
        subscriptionsUrl,
        accessToken,
        cancellationToken
    );
    if (existing is null)
    {
        return 1;
    }

    var mailboxSubscriptions = existing
        .Where(s =>
            string.Equals(
                MeshMailbox((s["channel"] as JsonObject)?["endpoint"]?.ToString()),
                mailbox,
                StringComparison.OrdinalIgnoreCase
            )
        )
        .ToList();

    // Fail before prompting if an --id does not deliver to this mailbox (a typo, or another
    // mailbox's subscription), so it cannot silently remove a smaller set than intended.
    var unknownIds = ids.Where(id => mailboxSubscriptions.All(s => s["id"]?.ToString() != id))
        .ToList();
    if (unknownIds.Count > 0)
    {
        Console.Error.WriteLine(
            $"No subscription delivering to mailbox {mailbox} has ID: {string.Join(", ", unknownIds)}"
        );
        Console.Error.WriteLine("Run 'list' to see the IDs. Nothing was removed.");
        return 1;
    }

    var targets = mailboxSubscriptions
        .Where(s => ids.Length == 0 || ids.Contains(s["id"]?.ToString()))
        .ToList();

    if (targets.Count == 0)
    {
        Console.WriteLine($"No subscriptions deliver to mailbox {mailbox}. Nothing to remove.");
        return 0;
    }

    foreach (var subscription in targets)
    {
        WriteSubscription(subscription, jsonOptions);
    }

    Console.WriteLine();
    Console.WriteLine(
        $"About to DELETE {targets.Count} subscription(s) in '{environment}'. "
            + "MNS stops delivering their events to MESH."
    );

    if (Console.IsInputRedirected)
    {
        Console.Error.WriteLine(
            "remove needs an interactive terminal to confirm. Nothing was removed."
        );
        return 1;
    }

    Console.Write("Type 'yes' to remove them: ");
    if (Console.ReadLine()?.Trim() != "yes")
    {
        Console.WriteLine("Aborted. Nothing was removed.");
        return 1;
    }

    // Stop at the first failure so we know exactly what was removed.
    foreach (var subscription in targets)
    {
        var id = subscription["id"]?.ToString();
        var criteria = subscription["criteria"]?.ToString();
        var correlationId = Guid.NewGuid().ToString();
        using var message = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{subscriptionsUrl}/{Uri.EscapeDataString(id ?? "")}"
        );

        HttpResponseMessage response;
        try
        {
            response = await SendAsync(
                http,
                message,
                accessToken,
                correlationId,
                cancellationToken
            );
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            // No response, but the request may still have reached MNS.
            Console.Error.WriteLine($"OUTCOME UNKNOWN: {id}  {criteria}");
            Console.Error.WriteLine($"  Correlation ID: {correlationId}");
            Console.Error.WriteLine($"  Error:          {ex.GetType().Name}: {ex.Message}");
            Console.Error.WriteLine(
                "  The subscription may have been removed. Run 'list' to check."
            );
            return 1;
        }

        using (response)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"FAILED:  {id}  {criteria}");
                WriteFailure(response, correlationId, responseBody);
                return 1;
            }

            Console.WriteLine($"REMOVED ({(int)response.StatusCode}): {id}  {criteria}");
            Console.WriteLine($"  Correlation ID:  {correlationId}");
        }
    }

    return 0;
}

// ---------------------------------------------------------------------------
// 5. Shared helpers
// ---------------------------------------------------------------------------

// Prints one subscription. Shows the raw JSON when it is not active, for the ticket.
static void WriteSubscription(JsonObject subscription, JsonSerializerOptions jsonOptions)
{
    var status = subscription["status"]?.ToString();
    var channel = subscription["channel"] as JsonObject;

    Console.WriteLine($"{subscription["id"]}  {status}");
    Console.WriteLine($"  Criteria: {subscription["criteria"]}");
    Console.WriteLine($"  Endpoint: {channel?["endpoint"]}  ({channel?["payload"]})");
    Console.WriteLine($"  Reason:   {subscription["reason"]}");
    if (status != "active")
    {
        // Not delivering events, so show everything MNS returned for the ticket.
        Console.WriteLine($"  Raw:      {subscription.ToJsonString(jsonOptions)}");
    }
}

// NHS API Platform returns 403 without a User-Agent, and HttpClient sends none by default.
static HttpClient CreateHttpClient()
{
    var http = new HttpClient();
    http.DefaultRequestHeaders.UserAgent.Add(
        new ProductInfoHeaderValue("dfe-sui-subscribe", "1.0")
    );
    return http;
}

static string BaseUrl(string environment) =>
    environment switch
    {
        "prod" => "https://api.service.nhs.uk",
        "sandbox" => "https://sandbox.api.service.nhs.uk",
        _ => "https://int.api.service.nhs.uk",
    };

// Gets an access token, except in the open-access sandbox. Authenticated is false if it failed
// (the error has already been printed).
static async Task<(bool Authenticated, string? AccessToken)> AuthenticateAsync(
    HttpClient http,
    string environment,
    string tokenUrl,
    CancellationToken cancellationToken
)
{
    if (environment == "sandbox")
    {
        return (true, null);
    }

    var accessToken = await GetAccessTokenAsync(http, tokenUrl, cancellationToken);
    return (accessToken is not null, accessToken);
}

// NHS application-restricted signed JWT auth. Mirrors FhirAuthTokenService in GetAnIdentifier.
// Returns null (after printing why) if the credentials are missing or the token request fails.
static async Task<string?> GetAccessTokenAsync(
    HttpClient http,
    string tokenUrl,
    CancellationToken cancellationToken
)
{
    var clientId = Environment.GetEnvironmentVariable("NHS_DIGITAL_CLIENT_ID");
    var kid = Environment.GetEnvironmentVariable("NHS_DIGITAL_KID");
    var privateKey = Environment.GetEnvironmentVariable("NHS_DIGITAL_PRIVATE_KEY");

    if (
        string.IsNullOrWhiteSpace(clientId)
        || string.IsNullOrWhiteSpace(kid)
        || string.IsNullOrWhiteSpace(privateKey)
    )
    {
        Console.Error.WriteLine(
            "Set NHS_DIGITAL_CLIENT_ID, NHS_DIGITAL_KID and NHS_DIGITAL_PRIVATE_KEY first (see README.md)."
        );
        return null;
    }

    using var rsa = RSA.Create();
    rsa.ImportFromPem(privateKey);

    var clientAssertion = new JsonWebTokenHandler().CreateToken(
        new SecurityTokenDescriptor
        {
            Issuer = clientId,
            Audience = tokenUrl,
            Subject = new ClaimsIdentity([
                new Claim(JwtRegisteredClaimNames.Sub, clientId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ]),
            // NHS rejects exp more than 5 minutes ahead of its clock, so leave a margin for skew.
            Expires = DateTime.UtcNow.AddMinutes(4),
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(rsa) { KeyId = kid },
                SecurityAlgorithms.RsaSha512
            ),
        }
    );

    using var tokenResponse = await http.PostAsync(
        tokenUrl,
        new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_assertion_type"] =
                    "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                ["client_assertion"] = clientAssertion,
            }
        ),
        cancellationToken
    );

    if (!tokenResponse.IsSuccessStatusCode)
    {
        Console.Error.WriteLine($"Token request failed: {(int)tokenResponse.StatusCode}");
        Console.Error.WriteLine(await tokenResponse.Content.ReadAsStringAsync(cancellationToken));
        return null;
    }

    var accessToken = (
        await tokenResponse.Content.ReadFromJsonAsync<JsonObject>(cancellationToken)
    )!["access_token"]!.GetValue<string>();
    Console.WriteLine("Access token obtained.");
    return accessToken;
}

// Pages through GET /subscriptions (a FHIR searchset Bundle) and returns every subscription owned
// by the calling application, or null (after printing why) if a request fails.
static async Task<List<JsonObject>?> GetSubscriptionsAsync(
    HttpClient http,
    string subscriptionsUrl,
    string? accessToken,
    CancellationToken cancellationToken
)
{
    var subscriptions = new List<JsonObject>();
    string? continueFrom = null;

    while (true)
    {
        var url = continueFrom is null
            ? subscriptionsUrl
            : $"{subscriptionsUrl}?continue_from={Uri.EscapeDataString(continueFrom)}";

        var correlationId = Guid.NewGuid().ToString();
        using var message = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await SendAsync(
            http,
            message,
            accessToken,
            correlationId,
            cancellationToken
        );
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Console.Error.WriteLine("FAILED:  list subscriptions");
            WriteFailure(response, correlationId, responseBody);
            return null;
        }

        var bundle = JsonNode.Parse(responseBody)!.AsObject();

        // MNS returns each Subscription directly in entry[], not wrapped in "resource".
        subscriptions.AddRange(bundle["entry"]?.AsArray().OfType<JsonObject>() ?? []);

        // Follow the "next" link by its continue_from cursor, not its URL: MNS may return
        // relative links without the /multicast-notification-service prefix.
        var nextUrl = bundle["link"]
            ?.AsArray()
            .OfType<JsonObject>()
            .FirstOrDefault(l => l["relation"]?.ToString() == "next")
            ?["url"]?.ToString();
        if (nextUrl is null)
        {
            return subscriptions;
        }

        // A "next" link we cannot follow means there are more pages we have not read. Fail rather
        // than return a partial list, which could make 'create' miss an existing subscription.
        var nextCursor = ContinueFrom(nextUrl);
        if (string.IsNullOrEmpty(nextCursor) || nextCursor == continueFrom)
        {
            Console.Error.WriteLine("FAILED:  list subscriptions");
            Console.Error.WriteLine($"  Correlation ID: {correlationId}");
            Console.Error.WriteLine(
                $"  Cannot follow the 'next' link (no new continue_from cursor): {nextUrl}"
            );
            return null;
        }
        continueFrom = nextCursor;
    }
}

// Sends an MNS request with the bearer token (if any) and the given correlation ID. The caller
// creates the correlation ID so it can still report it if the request throws.
// accessToken is null for the open-access sandbox, which does not require authentication.
static Task<HttpResponseMessage> SendAsync(
    HttpClient http,
    HttpRequestMessage message,
    string? accessToken,
    string correlationId,
    CancellationToken cancellationToken
)
{
    if (accessToken is not null)
    {
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }
    message.Headers.Add("X-Correlation-ID", correlationId);

    return http.SendAsync(message, cancellationToken);
}

// True if an existing subscription has the same criteria and delivers to the same MESH mailbox.
// Criteria are compared ignoring quotes, whitespace and case, because MNS may return them
// normalized. Only the mailbox is compared, not the workflow ID: a subscription to the same
// mailbox with a different workflow ID would still deliver duplicate messages.
static bool IsSameSubscription(JsonObject existing, string criteria, string mailbox) =>
    NormaliseCriteria(existing["criteria"]?.ToString()) == NormaliseCriteria(criteria)
    && string.Equals(
        MeshMailbox((existing["channel"] as JsonObject)?["endpoint"]?.ToString()),
        mailbox,
        StringComparison.OrdinalIgnoreCase
    );

static string? NormaliseCriteria(string? criteria) =>
    criteria is null
        ? null
        : string.Concat(criteria.Where(c => c != '\'' && !char.IsWhiteSpace(c))).ToLowerInvariant();

// The mailbox ID from a mesh://<MAILBOX>[?workflow_id=<ID>] endpoint, or null for any other endpoint.
static string? MeshMailbox(string? endpoint)
{
    const string scheme = "mesh://";
    if (endpoint is null || !endpoint.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
    {
        return null;
    }

    var mailbox = endpoint[scheme.Length..];
    var queryStart = mailbox.IndexOf('?');
    return queryStart < 0 ? mailbox : mailbox[..queryStart];
}

static void WriteFailure(HttpResponseMessage response, string correlationId, string responseBody)
{
    Console.Error.WriteLine($"  Status:         {(int)response.StatusCode}");
    Console.Error.WriteLine($"  Correlation ID: {correlationId}");
    Console.Error.WriteLine($"  Response:       {responseBody}");
}

// Reads the continue_from cursor from a "next" link, which may be absolute or relative.
static string? ContinueFrom(string linkUrl)
{
    var queryStart = linkUrl.IndexOf('?');
    if (queryStart < 0)
    {
        return null;
    }

    // Parsed by hand because HttpUtility.ParseQueryString decodes '+' as a space, which would
    // corrupt a cursor containing a literal '+' (e.g. base64).
    foreach (var pair in linkUrl[(queryStart + 1)..].Split('&'))
    {
        var parts = pair.Split('=', 2);
        if (parts.Length == 2 && Uri.UnescapeDataString(parts[0]) == "continue_from")
        {
            return Uri.UnescapeDataString(parts[1]);
        }
    }

    return null;
}

// Reads "id" from the create response, or null if the body is empty or not the expected JSON.
static string? TryGetId(string responseBody)
{
    try
    {
        return (JsonNode.Parse(responseBody) as JsonObject)?["id"]?.ToString();
    }
    catch (JsonException)
    {
        return null;
    }
}
