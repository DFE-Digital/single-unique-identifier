# Public documentation site

This directory contains the documentation site for Single Unique Identifier.

The site is for approved public guidance. Existing source documentation remains in [`../Docs`](../Docs); content should be moved or adapted here only when it is ready for public publication. Do not add personal data, credentials, incident procedures, or other restricted operational information.

## Local development

```bash
npm ci
npm start
```

Run these commands from this directory. Eleventy serves the site locally at `http://localhost:8080`.

## Publishing

The `Publish Documentation` workflow builds the site for pull requests targeting `main` and on pushes to `main` that change `public-docs/**`. It deploys only from `main`; manually running it from another branch builds without deploying. GitHub Pages must be enabled for this repository with **GitHub Actions** as the build and deployment source. The workflow publishes to the repository Pages URL, using `/single-unique-identifier/` as the production path prefix.

Pages publishing requires the repository's GitHub Pages deployment environment to be available to Actions.
