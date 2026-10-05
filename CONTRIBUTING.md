# Contributing to Sentinel

Sentinel is a community project for people who want better infectious-disease
surveillance tools. Contributions are not limited to code: trying a workflow,
sharing public-health expertise, testing an HL7 integration, improving a guide,
or reporting a confusing screen can all make Sentinel better.

One person may contribute in several ways. There is no hierarchy between
practical, clinical, academic, documentation and technical contributions.

Please use only synthetic or appropriately de-identified data in issues, pull
requests, screenshots and test fixtures.

## Ways of contributing to Sentinel

### Try Sentinel and share what happened

The most useful early contribution is often to try Sentinel in a realistic,
safe evaluation workflow and report the result. For example, you could:

- install Sentinel with Docker Desktop or Docker Engine;
- complete the first-time setup and basic organisation configuration;
- configure surveillance for a disease you know, including biomarkers and case
  definitions;
- create a task, survey or field mapping that reflects a real investigation
  workflow;
- try a report or outbreak workflow and identify what is clear or confusing;
- improve a user guide after following it yourself.

Start with an open [issue](https://github.com/christianpeut95/Sentinel/issues)
labelled as a contribution opportunity, or open a structured validation report
when one is available. A useful report records the Sentinel version, the broad
deployment environment, the workflow attempted, what worked, what did not,
and any safe suggestions for improvement.

### Help test HL7 interoperability

Laboratory and health-information professionals can make a particularly
valuable contribution by testing an HL7 workflow. This can include configuring
a file-drop integration, mapping codes, checking review-queue behaviour, and
confirming that an expected synthetic or de-identified message is processed
correctly.

Never put a live message, patient information, credentials, setup tokens,
connection strings or raw production logs in a public GitHub issue. A public
report should describe the message type, source-system context at a high level,
the expected outcome, the actual outcome, and only safely redacted diagnostics.

### Share domain and implementation insight

Sentinel should reflect how surveillance is actually practised. Contributions
can include:

- disease-specific case-definition and notification insights;
- suggestions for surveys, interview workflows, contact tracing and review
  queues;
- feedback on terminology, accessibility, usability or training needs;
- guidance about jurisdictional, laboratory or operational requirements;
- examples of a workflow that Sentinel should support.

Use a GitHub issue for a focused, actionable proposal or a
[GitHub Discussion](https://github.com/christianpeut95/Sentinel/discussions)
when the idea benefits from broader conversation.

### Improve documentation

Documentation contributions are welcome, especially from people who have just
installed Sentinel or completed a workflow for the first time. Clarifying a
step, correcting an assumption, adding a screenshot using synthetic data, or
writing a short how-to guide can remove a significant barrier for the next
person.

### Report a bug or suggest an improvement

Use the GitHub issue templates and include concise reproduction steps, the
expected and actual result, Sentinel version, and only non-sensitive logs or
screenshots. Screenshots should not contain patient data, user details,
credentials, setup tokens or connection strings. Use the security reporting
route described in [SECURITY.md](SECURITY.md) for a vulnerability rather than a
public issue.

### Submit code

Small, focused code contributions are welcome. If you are considering a larger
change, open an issue or discussion first so the work is aligned with Sentinel's
roadmap and current architecture.

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

## Pull-request checklist

- The change is scoped and its purpose is clear.
- Inputs, authorization and error handling have been considered.
- Tests pass locally and documentation is current.
- No personal health information, credentials, keys, database files or build
  artefacts are included.

## Recognition

Sentinel values contributions to testing, implementation insight,
documentation, design and code. Contributors who would like public
acknowledgement can be recognised in the project documentation with their
consent.

## Contribution licence

By submitting a contribution, you confirm that you have the right to submit
it and license your contribution under the same terms as Sentinel:
[GPL-3.0-or-later](LICENSE.md). Do not submit third-party code or assets unless
their licence is compatible and the required notices are included.
