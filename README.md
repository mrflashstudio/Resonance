# Resonance

Resonance is a work-in-progress private server implementation for [Rhythia](https://www.rhythia.com/), built with .NET 10.

> [!WARNING]
> The server is currently in development and is not yet ready for public use. The implementation and architecture are not stable. APIs, configuration, and database schemas may change without backward compatibility.


## Feature roadmap

- [x] Authentication (login, logout, registration and persistent sessions)
- [x] Account privileges and badges
- [x] Online-player list and join/leave notifications
- [x] Player activity (idle, playing, etc.)
- [ ] Beatmap collections, search, details, and downloads
- [ ] Skin browsing and downloads
- [x] Player profiles (basic profile reads, avatars and banners)
- [ ] Score submission
- [ ] Leaderboards, player statistics, and RP calculation
- [ ] Replay downloads
- [ ] Friends
- [x] Chat (channels, direct messages and commands)
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
    127.0.0.1 static.resonance.local
    ```
    (use your `RESONANCE_DOMAIN` if you changed it)

## Additional setup (production installation)

1. Set `RESONANCE_DOMAIN` to a public domain you control, without a scheme or port.
2. Create DNS `A` records pointing the domain and its `www`, `production`, `socket`, `socketdev`, and `static` subdomains to your server's public IPv4 address. Add `AAAA` records only if the server is also reachable over IPv6.
3. Allow inbound TCP ports **80** and **443** through the firewall. UDP port **443** is optional for HTTP/3.

Caddy obtains and renews public TLS certificates automatically. You do not need to export or manually trust a certificate in production.

Both configurations apply database migrations before starting Resonance and keep database, custom images, and certificate data in Docker volumes. Back up the database and custom images before upgrading.

## Account registration

There is no registration front-end yet. Register through the API:

```sh
curl.exe --ssl-revoke-best-effort https://production.resonance.local/api/registerAccount -H "Content-Type: application/json" --data "{\"username\":\"ResonanceUser\",\"password\":\"SecurePassword123\"}"
```

Replace `resonance.local` with your `RESONANCE_DOMAIN` and choose your own credentials. Successful registration returns HTTP `201`.

## Privileges

After registration, connect to the server and send `/claimsuperuser <code>` in chat, using the single-use code from the server logs. The code is available while no superuser exists and changes on restart. The command and reply are visible only to you.

With superuser access or Developer privileges, use `/giveprivilege` to grant privileges to yourself or another user by username or account ID:

```text
/giveprivilege ResonanceUser Developer
/giveprivilege "Another User" Moderator
/giveprivilege 3 Supporter
```

Available privileges are `Developer`, `Moderator`, `TeamAssistant`, `TeamRanked`, and `Supporter`. Use `/takeprivilege` to remove them. Superuser access remains independent of these privileges.

Use `/help` for other available commands and `/help <command>` for usage and examples.

## Avatars and banners

Default PNG images are in `Resonance/Assets/defaults/avatars` and `banners`, named `user.png` and `bot.png`. Replace them before building to use your own defaults.

For custom images, copy `<userId>.png` into `/data/assets/avatars` or `/data/assets/banners` in the application container:

```sh
docker compose -f docker-compose.dev.yml cp ./avatar.png resonance:/data/assets/avatars/42.png
```

Use `docker-compose.yml` for production. Custom images override defaults and persist in a Docker volume.

## Connecting

Follow the [Retune setup instructions](https://github.com/mrflashstudio/Retune#using-retune), using your `RESONANCE_DOMAIN` as the server domain (`resonance.local` for the default local setup).
