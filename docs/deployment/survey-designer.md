# Optional SurveyJS Creator deployment

The standard Sentinel source archive and Docker image include the MIT-licensed
SurveyJS Form Library used to render and complete surveys. They deliberately do
not include SurveyJS Creator, the separate visual survey-design component.
Accordingly, the visual designer is disabled by default. Existing survey
templates and survey completion continue to work normally.

This document describes an **operator-managed demo override**. It is
intended for a controlled Sentinel demonstration such as the project demo site.
It does not grant a SurveyJS licence or change the vendor's terms. The operator
who enables it remains responsible for obtaining any necessary SurveyJS rights
and configuring any vendor licence or domain restrictions.

## Prepare private assets

Obtain the Creator assets through the operator's approved SurveyJS source and
place them on the Docker host outside the Sentinel repository, for example:

```text
/srv/sentinel/surveyjs-creator-assets/
  survey-creator-core/
    survey-creator-core.min.css
    survey-creator-core.min.js
    survey-creator-core.i18n.min.js (optional translations)
  survey-creator-knockout/
    survey-creator-knockout.min.js
```

The asset directory must contain the non-optional files shown above. Store it outside the
Sentinel repository: both asset folders are ignored by Git and excluded from
the standard Docker build context. Do not copy them into a public Sentinel
source release or image.

## Enable the demo override

From the `Sentinel` directory, set the host path in `.env`:

```dotenv
SURVEYJS_CREATOR_ASSETS_DIR=/srv/sentinel/surveyjs-creator-assets
```

Start the normal stack with the additional override file:

```bash
docker compose \
  -f docker-compose.yml \
  -f docker-compose.survey-designer-demo.yml \
  up -d
```

The override mounts the two asset folders read-only and sets
`SurveyDesigner__Provider=SurveyJsCreator`. Sentinel also checks for all
required files before loading the designer page; a configuration typo or a
missing mount displays an unavailable message rather than loading a broken
editor.

To return to the standard distributable configuration, stop the override and
start only the base Compose file:

```bash
docker compose -f docker-compose.yml up -d
```

## Local development

For a local development instance with private assets already present, set the
environment variable `SurveyDesigner__Provider=SurveyJsCreator`. Do not change
the committed `appsettings.json` default or add Creator assets to source
control.
