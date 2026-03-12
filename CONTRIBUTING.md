# Contributing

We appreciate contributions of any kind. By participating in this project, you agree to abide by our code of conduct.

## How to Contribute

### Reporting Issues

To report bugs, suggest enhancements, or raise any other issues, please create a new issue in the GitHub repository. We will review your issue promptly.

### Pull Requests

We love pull requests from everyone.

1. **Clone** the repository to your local machine:

   ```bash
   git clone git@github.com:your-username/single-unique-identifier.git
   ```

1. All changes should be based on the main branch.

1. Make your changes, ensuring each commit has a clear and descriptive message summarising the purpose of the change.

   1. We implement a variant of the [Conventional Commits](https://www.conventionalcommits.org) specification to ensure consistency of commit messages and to allow for automated tooling to process commit history.
   1. The enforcement of 'types' in commit messages helps to ensure commits remain concise and helps alleviate scope creep. For example, refactors and new features must be in separate `refactor:` and `feat:` commits respectively, which helps break up work into manageable chunks for reviewing, merging, and potential rollbacks.
   1. All commit messages must include a type, an optional scope, a Jira ticket reference, and a description of the changes. Commit messages can also have a body and footer if further information is required.
      The format for commit messages should look like this:

      ```
      <type>[optional scope]: <description>

      [optional body]

      [optional footer(s)]
      ```

      Here are some example commit message:

      - `docs: JIRA-123 - add turbo-encabulator documentation`
      - `fix: JIRA-321 - use a drawn reciprocation dingle arm to reduce sinusoidal repleneration`
      - ```
        feat(turbo-encabulator): JIRA-8359 - automatically synchronize cardinal grammeters

        Adds automatic synchronization of cardinal grammeters while simultaniously supplying
        inverse reactive current for use in unilateral phase detractors.
        Replaces the original machine that had a base plate of pre-famulated amulite surmounted by a malleable logaraithmic casing.

        fixes: JIRA-621
        ```

1. Push your changes to your branch.

1. Open a pull request against the main branch of our repository.

1. All tests should pass before merging.

1. After submitting a pull request, our team will review your changes. Please be patient during this process and respond to any feedback or comments provided by our reviewers.

### Tips for Successful Pull Request

- Update README with any needed changes.
- Ensure each commit has a clear and descriptive message, utilising the [Conventional Commits](https://www.conventionalcommits.org) specification, summarising the purpose of the change. This helps us understand the rationale behind your modifications.
- Follow our [coding standards and conventions](#coding-standards-and-conventions).

### Coding standards and conventions

#### Code style

##### .NET - CSharpier

For all .NET projects, we utilise [CSharpier](https://csharpier.com) to standardise code style and formatting. There are pre-commit hooks configured in this repository via [Husky.Net](https://github.com/alirezanet/husky.net) to ensure code is formatted before being committed.

##### Secret scanning

Pre-commit hooks also run GitLeaks against staged changes. The repository uses a pinned GitLeaks `8.30.0` binary, installed on demand into a user cache directory by [`scripts/security/run-gitleaks.ps1`](./scripts/security/run-gitleaks.ps1). The auto-install path currently supports macOS and Linux on x64 and arm64, plus Windows on x64.

If a finding is expected because you are working with tracked fixtures or local development examples, update `.gitleaks.toml` or regenerate `.gitleaks.baseline.json` intentionally:

```bash
dotnet pwsh ./scripts/security/run-gitleaks.ps1 -Mode Baseline
```

For urgent one-off commits only, you can bypass the local GitLeaks hook with:

```bash
SUI_SKIP_GITLEAKS=1 git commit
```

It is recommended that you install the CSharpier extension for your preferred IDE to allow you to format code as you work on it. This is especially useful when combined with the "format on save" feature of most editors. Please follow the official [Editor Integration](https://csharpier.com/docs/Editors) documentation for guidance on how to set this up for your preferred editor.
