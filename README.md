# Resonance

Resonance is a work-in-progress private server implementation for [Rhythia](https://www.rhythia.com/), built with .NET 10.

> [!WARNING]
> The server is currently in development and is not yet ready for public use. The implementation and architecture are not stable. APIs, configuration, and database schemas may change without backward compatibility.


## Feature roadmap

- [x] Authentication (login, logout, registration and persistent sessions)
- [x] Online-player list and join/leave notifications
- [ ] Beatmap collections, search, details, and downloads
- [ ] Skin browsing and downloads
- [ ] Player profiles
- [ ] Score submission
- [ ] Leaderboards, player statistics, and RP calculation
- [ ] Replay downloads
- [ ] Friends
- [ ] Chat
- [ ] Clans
- [ ] Spectating
- [ ] Multiplayer lobbies and matches
- [ ] Server website ([Encore](https://github.com/mrflashstudio/Encore))
- [x] Server switcher ([Retune](https://github.com/mrflashstudio/Retune))


## Installation

Requires Docker with Compose. For production, complete the DNS and port setup below before starting the server.

1. Clone the repository.
2. Open a terminal in the repository directory.
3. Set up the environment:

   - Review `Resonance/appsettings.json`, the defaults are suitable for getting started.
   - Copy `.env.example` to `.env`, then set `RESONANCE_DOMAIN` (or keep the default for local use) and choose a strong `POSTGRES_PASSWORD`.

     ```sh
     cp .env.example .env
     ```
4. Run the server:

    - Production:

      ```sh
      docker compose -f docker-compose.yml up -d --build
      ```

    - Local:

      ```sh
      docker compose -f docker-compose.dev.yml up -d --build
      ```


## Additional setup (local installation)

For a local installation, export Caddy's local CA certificate, trust it, and add hosts entries:

1. Export Caddy's local CA certificate:

    ```sh
    docker compose -f docker-compose.dev.yml cp caddy:/data/caddy/pki/authorities/local/root.crt ./caddy-root.crt
    ```
2. Trust it on your machine. On Windows, run in PowerShell:

    ```powershell
    Import-Certificate -FilePath ./caddy-root.crt -CertStoreLocation Cert:\CurrentUser\Root
    ```
3. Add these entries to your hosts file (`C:\Windows\System32\drivers\etc\hosts` on Windows):

    ```text
    # Resonance Web
    127.0.0.1 resonance.local
    127.0.0.1 www.resonance.local
    # Resonance Server
    127.0.0.1 production.resonance.local
    127.0.0.1 socket.resonance.local
    127.0.0.1 socketdev.resonance.local
    ```
    (use your `RESONANCE_DOMAIN` if you changed it)

## Additional setup (production installation)

1. Set `RESONANCE_DOMAIN` to a public domain you control, without a scheme or port.
2. Create DNS `A` records pointing the domain and its `www`, `production`, `socket`, and `socketdev` subdomains to your server's public IPv4 address. Add `AAAA` records only if the server is also reachable over IPv6.
3. Allow inbound TCP ports **80** and **443** through the firewall. UDP port **443** is optional for HTTP/3.

Caddy obtains and renews public TLS certificates automatically. You do not need to export or manually trust a certificate in production.

Both configurations apply database migrations before starting Resonance and keep database and certificate data in Docker volumes. Back up the database before upgrading.

## Account registration

There are no built-in accounts or registration front-end yet. Create an account through the API:

```sh
curl.exe --ssl-revoke-best-effort https://production.resonance.local/api/registerAccount -H "Content-Type: application/json" --data "{\"username\":\"ResonanceUser\",\"password\":\"SecurePassword123\"}"
```

Replace `resonance.local` with your `RESONANCE_DOMAIN` and choose your own credentials. Successful registration returns HTTP `201`.

## Connecting

Follow the [Retune setup instructions](https://github.com/mrflashstudio/Retune#using-retune), using your `RESONANCE_DOMAIN` as the server domain (`resonance.local` for the default local setup).
