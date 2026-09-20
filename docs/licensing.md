# Licensing and third-party release policy

Sentinel-owned source code is licensed under GPL-3.0-or-later. The project is
intended to be useful to public-health organisations and the wider community,
while allowing organisations or service providers to charge for hosting,
implementation, support, training, or other services permitted by the GPL.

## What a maintainer may claim

- The **Sentinel-owned source code** is GPL-3.0-or-later.
- A release is **not** necessarily a GPL-only distribution: it can contain
  separately licensed third-party material.
- Do not describe a container image, installer, or binary release as wholly
  GPL-compliant until every separately licensed component in
  `THIRD_PARTY_NOTICES.md` is compatible with that form of distribution.

## Current release conditions

The project uses MIT, Apache-2.0, BSD-3-Clause, LGPL-2.1-or-later, and MPL-2.0
NuGet dependencies. Their notices and conditions must be retained. The
Spreadsheet import uses ClosedXML (MIT); Sentinel does not use EPPlus.

One browser-side component requires an explicit pre-release decision:

1. **WebDataRocks** powers report pivots and is loaded from the vendor CDN. It
   has a separate EULA and requires attribution. Sentinel includes attribution,
   but a maintainer must still confirm that the EULA fits the intended release
   and deployment model, or replace the component.

SurveyJS Creator is not part of the standard Sentinel source archive or Docker
image. The project provides an operator-managed demo override that
mounts separately obtained Creator assets at deployment time. This does not
grant a vendor licence: the operator enabling it is responsible for the terms
that apply to their use and any distribution of those assets. See
[optional SurveyJS Creator deployment](deployment/survey-designer.md).

Neither condition can be solved merely by adding a copyright notice.

## Contribution rule

By submitting a contribution, a contributor agrees that their contribution may
be distributed under GPL-3.0-or-later. Contributors must not add code, browser
assets, sample data, fonts, images, or packages unless they have the right to
distribute them under terms compatible with Sentinel's intended distribution.

## Practical release check

1. Update versions in `THIRD_PARTY_NOTICES.md` after every dependency change.
2. Run `dotnet list Sentinel/Sentinel.csproj package --include-transitive`.
3. Inspect new browser assets and external CDNs for their licence and privacy
   implications.
4. Keep required notices in the source archive and copy `LICENSE.md` and
   `THIRD_PARTY_NOTICES.md` into release artifacts.
5. Check that the release notes accurately state which third-party conditions
   still apply.
