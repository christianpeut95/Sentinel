# Production deployment security baseline

## Docker HTTPS deployment

The supported public Docker arrangement is:

```
Internet ── HTTPS :443 ── Caddy ── private Docker network ── Sentinel :8080
                                                     └─────── SQL Server :1433
```

Only Caddy publishes host ports 80 and 443. Sentinel and SQL Server have no host port mapping. Caddy performs HTTP-to-HTTPS redirection and certificate management, while Sentinel trusts forwarded request scheme information only when `ReverseProxy__UseForwardedHeaders=true` is set by the private Compose deployment.

## First public deployment

1. Create a DNS A/AAAA record for `SENTINEL_HOSTNAME` pointing to the Docker host.
2. Allow inbound TCP 80 and TCP/UDP 443 through the host/network firewall.
3. Copy `Sentinel/.env.example` to `Sentinel/.env` and set `DOCKERHUB_USERNAME`, `SQL_SA_PASSWORD`, `SQL_APP_PASSWORD`, `SENTINEL_HOSTNAME` and `ACME_EMAIL`. Use two distinct generated passwords. The `sa` password is used only by SQL Server and the short-lived bootstrap container; Sentinel connects with the database-scoped `sentinel_app` login.
4. Restrict the `.env` file: `chmod 600 .env`.
5. Start the stack with `docker compose up -d` from the `Sentinel` directory.
6. Retrieve the generated, one-time setup token only from the Docker host:

   ```bash
   docker compose exec sentinel-web cat /var/lib/sentinel/setup/setup-token.txt
   ```

   Enter it at `https://<SENTINEL_HOSTNAME>/Setup` to create the initial
   administrator. The token is not printed to application logs, expires after
   48 hours, and Sentinel deletes the protected file after setup succeeds.
7. Verify Caddy obtains a certificate, HTTP redirects to HTTPS, and Sentinel is reachable only at `https://<SENTINEL_HOSTNAME>`.

Do not add a host `ports:` mapping for Sentinel or SQL Server to a public deployment. Use an authenticated administration channel (for example SSH/VPN) for host and database maintenance.

The bootstrap job grants `sentinel_app` data read/write and schema-management roles only in `SentinelDb`, because Sentinel applies EF Core migrations at startup. It has no SQL Server administrative/server-wide role.

## Persistent data

The Compose stack retains the following named volumes:

- SQL Server database data;
- Sentinel protected files;
- a short-lived protected setup-token volume, automatically cleared after first setup;
- ASP.NET Core Data Protection keys (required to retain encrypted settings and valid cookies across app replacement);
- Caddy certificate/account data and configuration.

Back up volumes using the organisation's approved encrypted backup process. Test restoration before relying on backups. The setup-token volume holds a bootstrap credential only while setup is incomplete; do not copy it, `.env`, setup tokens or logs into public issue trackers.

## Production verification

- Confirm only ports 80/443 are publicly reachable.
- Inspect the certificate chain, expiry and automatic HTTPS redirect.
- Verify `Strict-Transport-Security` after the site is stable on HTTPS.
- Verify Sentinel cookies have the `__Host-` prefix, Secure, HttpOnly, SameSite=Lax and Path=/ attributes.
- Confirm response `Content-Type` matches HTML, JSON, CSV and attachment bodies.
- Confirm WebSocket/SignalR traffic uses `wss://` if enabled.
- Run a TLS scan and permit TLS 1.2/1.3 only.

## Maintenance

Apply operating-system, Docker, reverse-proxy and SQL Server security updates on the organisation's maintenance schedule. Review reverse-proxy configuration after changing domains, adding another proxy, exposing a port or enabling a CDN/WAF.
