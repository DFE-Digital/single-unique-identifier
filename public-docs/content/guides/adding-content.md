---
layout: page
title: Adding content
includeInBreadcrumbs: true
eleventyNavigation:
  key: Adding content
  parent: Home
  order: 10
---

Use this guide when adding an approved public page to the Single Unique Identifier documentation site. The [example integration guide](../example-integration-guide/) shows one way to structure a longer page.

## Before you write

Check that the information is approved for public use and has an owner who can review it when the service changes. Keep operational procedures, credentials, personal data, incident details and supplier-specific configuration in their authorised private locations. Do not copy a document into this site until it has been checked for those details.

Use confirmed service behaviour and contracts as the source. If a requirement, support route or technical detail is still being decided, leave it out of public guidance or make its draft status explicit in an appropriate non-public document.

## Create a page

Add a Markdown file under `public-docs/content/`. Folders help organise the source files; Eleventy uses the file path to create the page URL. For example, `content/guides/registration.md` becomes `/guides/registration/`.

Start the file with front matter:

```yaml
---
layout: page
title: Page title
includeInBreadcrumbs: true
eleventyNavigation:
  key: Page title
  parent: Home
  order: 30
---
```

Use a distinct `key` for each page. Set `parent` to the `key` of the navigation section where the page belongs; use `Home` for a top-level page. Set `order` to place the page among its siblings. Add a clear title and use `layout: page` for standard guidance.

## Write for the reader

- Address one audience and task per page. Explain specialist terms the first time they appear.
- Use descriptive headings in order, starting with `##` for the first section after the page title.
- Put prerequisites before procedures. Use numbered steps for actions that must happen in sequence.
- Use descriptive link text and link to the relevant page, rather than saying "click here".
- Use tables for short comparisons or lists of requirements; use headings and lists for longer explanations.
- Keep examples synthetic. Never include real credentials, personal data or production values.
- Remove template instructions, square-bracket placeholders and unverified claims before requesting publication review.

## Preview and submit

From `public-docs/`, install dependencies and start the local site:

```bash
npm ci
npm start
```

Open the local address printed by Eleventy. Check the page content, navigation order, breadcrumbs, links and layout at a narrow viewport as well as on desktop. For a production-path build, run:

```bash
ELEVENTY_ENV=production npm run build
```

Submit the page in a pull request. The documentation workflow builds the site for pull requests targeting `main`; reviewers should confirm the content is accurate, approved for public use and free of restricted information before it is merged.
