# SocialWebIntelligence

**SocialWebIntelligence** is a .NET-based website intelligence and
monitoring application. It is being developed as a portfolio project to
collect publicly accessible information from registered company
websites, store structured snapshots, and identify changes between
collections.

The current development focus is **Website Contact Intelligence** and
**About-page history/change detection**.

> **Project status:** In development. Some capabilities described below
> are implemented or under development; see the feature status section.
> Do not treat planned capabilities as production-ready.

## Table of contents

-   [Goals](#goals)
-   [Current capabilities](#current-capabilities)
-   [Technology stack](#technology-stack)
-   [Solution structure](#solution-structure)
-   [How collection works](#how-collection-works)
-   [Database overview](#database-overview)
-   [Getting started](#getting-started)
-   [Configuration](#configuration)
-   [Build and test](#build-and-test)
-   [Development notes](#development-notes)
-   [Responsible collection](#responsible-collection)
-   [Roadmap](#roadmap)

## Goals

The application is intended to help users:

1.  Register company websites for monitoring.
2.  Discover relevant pages such as Contact Us and About Us.
3.  Extract publicly available business contact details and page
    information.
4.  Store collection snapshots in MariaDB.
5.  compare successive snapshots and record detected changes.
6.  Review historical information through an ASP.NET Core MVC interface.
7.  Export collected information for further analysis.

## Current capabilities

The project is being built incrementally. The following describes the
current design and work completed so far.

  -----------------------------------------------------------------------
  Capability                          Status
  ----------------------------------- -----------------------------------
  Register and manage monitored       Implemented
  sources                             

  Website collection using HTTP       Implemented / under active
  requests                            development

  Contact-page discovery and          Implemented / under active
  extraction                          development

  Store Contact information as JSON   Implemented
  snapshots                           

  About-page collection and           Implemented / under active
  persistence                         development

  About-page snapshot history and     Implemented
  details                             

  Compare snapshots and identify      Implemented in persistence
  field changes                       services; integration testing
                                      ongoing

  Persist detected changes in         Implemented in About persistence;
  `WebsiteCollectionChanges`          verify Contact integration in the
                                      current branch

  Change History dashboard            Under development

  CSV, Excel, and JSON exports        Planned

  Background/Worker-based scheduled   Planned
  collection                          

  Authorized social-platform          Future scope; dependent on official
  integrations                        API access and permissions
  -----------------------------------------------------------------------

## Technology stack

-   **Language:** C#
-   **Framework:** .NET 9
-   **Web application:** ASP.NET Core MVC
-   **Data access:** Entity Framework Core
-   **Database:** MariaDB
-   **Database provider:** Pomelo.EntityFrameworkCore.MySql
-   **Website parsing:** HtmlAgilityPack
-   **Testing:** xUnit and EF Core InMemory provider
-   **Development IDE:** Visual Studio 2022

## Solution structure

The solution contains five projects:

``` text
SocialWebIntelligence/
│
├── SocialWebIntelligence.sln
│
├── SocialWebIntelligence.Web/
│   ├── Controllers/
│   ├── Views/
│   ├── wwwroot/
│   └── Program.cs
│
├── SocialWebIntelligence.Core/
│   ├── DTOs/
│   └── Entities/
│
├── SocialWebIntelligence.Infrastructure/
│   ├── Collectors/
│   │   └── Websites/
│   ├── Persistence/
│   └── Migrations/
│
├── SocialWebIntelligence.Worker/
│
└── SocialWebIntelligence.Tests/
```

Responsibilities:

-   **Web:** MVC interface, controllers, and application entry point.
-   **Core:** Domain entities and data-transfer objects.
-   **Infrastructure:** Website collection, parsing, persistence, and
    database integration.
-   **Worker:** Intended location for background collection processes.
-   **Tests:** Unit and integration-style tests for application
    services.

The exact subfolder layout may evolve as development continues.

## How collection works

The website collection workflow is designed around registered
`MonitoredSource` records.

1.  A user registers a company website and selects the Website platform.
2.  The application discovers relevant pages from the registered
    website.
3.  The Contact collector requests the selected Contact page and
    extracts available contact information.
4.  The About collector requests the discovered About page and extracts
    available About information.
5.  The persistence services serialize extracted information and save
    successful collection snapshots.
6.  The application compares the latest extraction with the previous
    successful snapshot.
7.  When differences are detected, change records are associated with
    the newly created snapshot.
8.  The MVC interface provides access to saved history and collection
    details.

A repeated collection with an unchanged content hash can be treated as
unchanged and does not need to create another snapshot. The first
snapshot is the baseline for About-page comparison; subsequent
collections can be compared against it.

## Database overview

The main entities currently used by the collection workflow include:

### `MonitoredSources`

Stores websites or other sources registered for monitoring. Fields
include company name, platform, URL, active status, creation time, and
last collection time.

### `WebsiteCollections`

Stores collection snapshots, including:

-   Monitored source ID
-   Page URL and title
-   Page type (for example, `Contact` or `About`)
-   Extracted data as JSON
-   Content hash
-   HTTP status code
-   Success status and error message
-   Collection timestamp

### `WebsiteCollectionChanges`

Stores detected differences associated with a saved collection snapshot.
Fields include:

-   Website collection ID
-   Changed field
-   Change type (such as `Added`, `Removed`, or `Modified`)
-   Old value and new value
-   Detection timestamp

The change-history view joins change records to their collection
snapshots and monitored sources to display the company, page type,
changed field, values, and detection time.

## Getting started

### Prerequisites

Install the following on your development machine:

-   Windows
-   Visual Studio 2022
-   .NET 9 SDK
-   MariaDB Server
-   Git

Confirm the .NET SDK is available:

``` powershell
dotnet --version
```

### Clone the repository

``` powershell
git clone <YOUR_REPOSITORY_URL>
cd "Social Media & Website Intelligence Platform"
```

Replace `<YOUR_REPOSITORY_URL>` with your repository's actual Git URL.

If you already have the project locally, open
`SocialWebIntelligence.sln` in Visual Studio 2022 or navigate to the
solution directory in PowerShell.

### Create the database

Create a MariaDB database for local development:

``` sql
CREATE DATABASE SocialWebIntelligenceDb;
```

Use the database name expected by your application configuration.

### Configure the connection string

Configure the MariaDB connection string for the Web startup project. Do
not commit database passwords or other secrets to source control.

For local development, .NET User Secrets can be used:

``` powershell
dotnet user-secrets init --project SocialWebIntelligence.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_CONNECTION_STRING" --project SocialWebIntelligence.Web
```

The key `DefaultConnection` is an example. If the application uses a
different connection-string key, use the exact key referenced by its
`Program.cs` or configuration code.

### Restore dependencies and apply migrations

Run these commands from the solution directory:

``` powershell
dotnet restore SocialWebIntelligence.sln
```

Apply the Infrastructure project's EF Core migrations using the Web
project as the startup project:

``` powershell
dotnet ef database update --project SocialWebIntelligence.Infrastructure --startup-project SocialWebIntelligence.Web
```

If the `dotnet ef` command is unavailable, install the EF Core
command-line tool:

``` powershell
dotnet tool install --global dotnet-ef --version 9.0.0
```

If it is already installed, update it when needed:

``` powershell
dotnet tool update --global dotnet-ef --version 9.0.0
```

### Run the application

``` powershell
dotnet run --project SocialWebIntelligence.Web
```

Use the local URL printed in the terminal to open the application in
your browser.

## Configuration

Check the Web project's configuration and startup code for the actual
names of:

-   MariaDB connection-string key
-   Database server, port, database, and credentials
-   Any environment-specific settings

Keep secrets outside committed files. Before deployment, use the hosting
environment's secret/configuration mechanism and review database access,
HTTPS, logging, and error handling.

## Build and test

Build the complete solution:

``` powershell
dotnet build SocialWebIntelligence.sln
```

Run the automated tests:

``` powershell
dotnet test SocialWebIntelligence.Tests
```

The test project uses xUnit and EF Core InMemory for persistence-service
tests. An InMemory test passing does **not** prove that MariaDB
migrations have been applied successfully; verify the development
database separately.

## Development notes

-   Keep changes incremental and focused.
-   Preserve existing collection history and database records while
    debugging.
-   Add or update tests when changing extraction, comparison, or
    persistence behavior.
-   Treat the first successful About snapshot as the baseline; a later
    collection is needed to test detected changes.
-   When testing change detection, use a local test website or another
    website for which you have permission to test.
-   A changed page does not guarantee a change record: the relevant
    content must be successfully extracted, its hash/comparison logic
    must detect a difference, and the persistence path must save the
    change record.

### Local test website

A separate `TestWebsite` folder can be used as a local test fixture with
`index.html`, `about.html`, and `contact.html`.

Start it from that folder with:

``` powershell
py -3.11 -m http.server 8000 --bind 127.0.0.1
```

Register this URL in the application:

``` text
http://127.0.0.1:8000/
```

The Web application/collector must run on the same computer as the test
server for this loopback address to work. Keep the Python server running
while collecting. Saving an HTML file makes the new content available on
the next HTTP request, but it does not trigger collection automatically.

## Responsible collection

SocialWebIntelligence is intended for responsible collection of publicly
accessible information.

-   Respect each website's terms of service and applicable robots
    policies.
-   Use reasonable request rates and avoid unnecessary repeated
    requests.
-   Do not bypass authentication, paywalls, CAPTCHAs, access controls,
    or HTTP 403 restrictions.
-   Collect only information that is publicly accessible and appropriate
    for the project's purpose.
-   For social platforms, use official APIs and only the permissions and
    access levels granted to the application.
-   Do not use session cookies, fake accounts, browser automation, or
    other methods to circumvent platform restrictions.

## Roadmap

Planned development areas include:

1.  Complete and validate Contact and About collection workflows.
2.  Complete the Website Change History dashboard.
3.  Improve page discovery and extraction reliability.
4.  Add historical snapshot browsing and change details.
5.  Add CSV, Excel, and JSON exports.
6.  Add background collection through the Worker project.
7.  Consider additional website pages and authorized social-platform API
    integrations.

------------------------------------------------------------------------

**SocialWebIntelligence** is an evolving portfolio project. Features,
architecture, and setup instructions may change as implementation
progresses.
