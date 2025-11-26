using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace OpenApi;

public sealed class FindOpenApiOptions : DefaultOpenApiConfigurationOptions
{
    public FindOpenApiOptions()
    {
        DocumentFilters.Add(new FindDocumentFilter());
    }

    public override OpenApiInfo Info { get; set; } = new OpenApiInfo
    {
        Title = "Find a Record API",
        Version = "1",
        Description = @"
## How organisations join the platform

Before any searches can take place, every organisation that holds records — such as Local Authorities, Police forces, or health services — must **enrol with the platform**.

During enrolment:

- The platform generates a **secret encryption key** for that organisation (the key never leaves the platform and is never shared).
- The organisation then takes **every person in their existing database**, for example all children they are responsible for.
- For each person, they provide the NHS number to the platform.
- The platform **encrypts those NHS numbers** using the organisation’s own key and returns an encrypted identifier for each person.
- The organisation stores these encrypted identifiers alongside their own records.

Every organisation begins with the same real-world identity — the NHS number — but each sees it in **their own encrypted language**.

## Example: searching for Josh (MATCH demo)

To initiate discovery for **Josh Parker**, the demo MATCH service allows a simple demographic lookup:

    {
      ""given"": ""Josh"",
      ""family"": ""Parker""
    }

This identifies Josh in PDS and retrieves his NHS number **4857771234** (used only internally).

## Understanding encrypted identities — using Josh Parker as an example

Inside the platform we know Josh’s NHS number is **4857771234**, along with rich historic information.  
No organisation searching for Josh is ever shown his NHS number.

When the Local Authority searches, the platform returns:

**9D8EHKuYWQVYGYVbSb0JZg**

When the Police search, they see:

**s-gImLeTDEUJoKyE7_aVjQ**

Different encrypted IDs — **same person**.

This means nobody outside the platform can join records between organisations.

## How a search for other records happens

The Police take their encrypted ID:

**s-gImLeTDEUJoKyE7_aVjQ**

They call the **Find** endpoint to ask:

> Do any other organisations know this person?

The platform:

1️⃣ **Decrypts** that identifier internally to recover **4857771234**  
2️⃣ Contacts each custodian in the network  
3️⃣ **Re-encrypts** the NHS number using each custodian’s key before asking

So the Local Authority receives the question in their format:

**9D8EHKuYWQVYGYVbSb0JZg**

No custodian ever sees another organisation’s encrypted ID.

## What the Police get back

If the Data Sharing Agreement permits disclosure:

- Which organisation holds a record  
- The **type** of record  
- A **secure, time-limited link** to request the content (via Fetch)

These links:

- Only work for the searcher
- Expire automatically
- Are mediated by **Fetch**, which re-checks permissions

## In short

We enable discovery across organisations **without revealing NHS numbers** and only where permitted.

The platform matches identities **securely behind the scenes**.

## API capabilities

- Start a search for an encrypted identity
- Check job status
- Retrieve permitted results
- Cancel a search job

## Demo custodian interaction and future flexibility

In this demo, we have set up each custodian slightly differently — using different endpoint URLs and different authentication patterns — to show how the platform can communicate with a range of systems.

The same approach could be expanded in future to support:

REST, SOAP, RPC-style services, GraphQL, etc.

Various authentication models including OAuth2, mTLS, signed requests and API keys

This keeps the platform flexible, allowing custodians to use technology that suits their own environment while still being reachable through a common discovery process.
"
    };

    public override OpenApiVersionType OpenApiVersion { get; set; } = OpenApiVersionType.V3;
}
