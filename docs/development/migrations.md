# Entity Framework Core migrations

Sentinel uses Entity Framework Core migrations for schema changes. The current
migration history is retained for upgrades of existing installations; do not
edit applied migration files or their designer files.

## Normal operation

- The supported Docker Compose deployment applies pending migrations when the
  application starts. Operators should use the repository [deployment
  guidance](../../README.md#docker-deployment), not run ad-hoc SQL against a
  production database.
- Before upgrading an existing installation, back up the database and test the
  upgrade with a representative non-production copy.
- Local developers can inspect the current state with:

  ```powershell
  dotnet ef migrations list --project Sentinel/Sentinel.csproj --context ApplicationDbContext
  dotnet ef migrations has-pending-model-changes --project Sentinel/Sentinel.csproj --context ApplicationDbContext
  ```

## Creating a migration

From the repository root, after reviewing the model change:

```powershell
dotnet ef migrations add DescriptiveMigrationName --project Sentinel/Sentinel.csproj --context ApplicationDbContext
```

Review the generated `Up` and `Down` operations before committing. In
particular, identify data-loss operations, large table rewrites, unique-index
creation over existing data and any data backfill that needs a deployment plan.

## Legacy manual scripts

The `Sentinel/Migrations` directory contains a small number of historical SQL
cleanup and diagnostic scripts. They are **not** part of the normal installer
or upgrade path. Treat a script that modifies or deletes data as a last-resort
recovery action: take a verified backup, review it against the affected
database, test it first, and record operator approval.

Do not use an old troubleshooting script simply because a similarly named
error occurs. Start with the current logs, database backup and supported
migration path.

## Future baseline migration

When Sentinel intentionally replaces its migration history with a fresh beta
baseline, update this document in the same change. The new baseline must have
a documented upgrade path for every installation that can still exist; it is
not safe to delete history while upgrades from those databases are supported.
