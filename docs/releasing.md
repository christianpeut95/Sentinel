# Preparing a Sentinel release

This checklist is for maintainers preparing a source or container release. It
does not replace each organisation's deployment validation.

## Before creating a release

1. Confirm the planned version in `Sentinel/Sentinel.csproj` and add concise,
   user-facing notes to [CHANGELOG.md](../CHANGELOG.md).
2. Review `git status`, including staged changes. Do not include local
   configuration, secrets, uploaded files, data exports, backups, `bin_temp`
   or other generated artefacts.
3. Run the release checks from the repository root:

   ```powershell
   dotnet restore Sentinel.slnx
   dotnet build Sentinel.slnx --configuration Release --no-restore
   dotnet test Sentinel.Tests/Sentinel.Tests.csproj --configuration Release --no-build --no-restore
   dotnet list Sentinel.slnx package --vulnerable --include-transitive --no-restore
   docker build --file Sentinel/Dockerfile .
   ```

4. Review the current security assessment and dependency records in
   [docs/security](security/README.md). Complete the production checks that
   can only be performed by an installation operator.
5. Validate the Docker Compose configuration with real deployment values in a
   non-production environment. Confirm persistent database, protected-file,
   data-protection-key and Caddy volumes survive a service recreation.
6. Verify that the GPL terms in [LICENSE.md](../LICENSE.md) and the inventory
   in [THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md) are current. Confirm
   that separately licensed SurveyJS Creator assets have not entered the
   public source archive or standard image. Verify the optional WebDataRocks
   acceptance gate, attribution, and disabled-state behaviour described in
   [licensing guidance](licensing.md) before publishing a bundled public
   artefact.

## Publishing

1. Create a clean commit and an annotated version tag after the checklist has
   passed.
2. Build and publish the container image using the approved registry process.
3. Create a GitHub release from the tag, copying the relevant changelog notes
   and documenting any upgrade or configuration actions.
4. Verify the release artefacts from a clean machine or container before
   announcing them.
