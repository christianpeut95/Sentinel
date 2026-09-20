# Contributing to Sentinel

Thank you for helping improve Sentinel. Please use only synthetic or
appropriately de-identified data in issues, pull requests, screenshots and
test fixtures.

## Before opening a pull request

1. Create a focused branch from the repository's default branch.
2. Keep secrets, locally generated files and production data out of Git.
   In particular, do not commit `.env`, local `appsettings` files, uploads,
   `App_Data`, `bin_temp`, test results or database backups.
3. Run the local checks from the repository root:

   ```powershell
   dotnet restore Sentinel.slnx
   dotnet build Sentinel.slnx --configuration Release --no-restore
   dotnet test Sentinel.Tests/Sentinel.Tests.csproj --configuration Release --no-build --no-restore
   ```

4. Add or update focused tests whenever behaviour changes. Update the relevant
   user, security or deployment documentation as needed.

## Reporting bugs and requesting features

Use the GitHub issue templates and include concise reproduction steps, the
expected and actual result, Sentinel version, and only non-sensitive logs or
screenshots. Use the security reporting route described in
[SECURITY.md](SECURITY.md) for vulnerabilities rather than a public issue.

## Pull-request checklist

- The change is scoped and its purpose is clear.
- Inputs, authorization and error handling have been considered.
- Tests pass locally and documentation is current.
- No personal health information, credentials, keys, database files or build
  artefacts are included.

## Contribution licence

By submitting a contribution, you confirm that you have the right to submit
it and license your contribution under the same terms as Sentinel:
[GPL-3.0-or-later](LICENSE.md). Do not submit third-party code or assets unless
their licence is compatible and the required notices are included.
