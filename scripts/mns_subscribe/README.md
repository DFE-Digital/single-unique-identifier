# MNS subscriptions

`subscribe.cs` creates the [MNS](https://digital.nhs.uk/developer/api-catalogue/multicast-notification-service)
subscriptions that deliver `pds-record-change-2` events to our MESH mailbox, where
[NotificationService](../../Apps/NotificationService/README.md) reads them. Run it once per environment.

It creates two subscriptions (criteria are at the top of `subscribe.cs`):

- `eventType='pds-record-change-2' AND changed_gp=True`
- `eventType='pds-record-change-2' AND changed_nhsnumber=True`

## Prerequisites

1. **MESH mailbox** exists and you know its ID.
2. **Workflow ID** to tag the MESH messages with. MNS needs one but does not require a specific
   value, so it is arbitrary (e.g. `someWorkflowId001`). NotificationService does not filter on it.
3. **MNS onboarding approved** for `pds-record-change-2` with the filters above, via the
   [MNS ServiceNow form](https://digital.nhs.uk/services/multicast-notification-service/subscribe-to-the-multicast-notification-service).
4. **MNS API added** to the GetAnIdentifier application in the [NHS developer portal](https://onboarding.prod.api.platform.nhs.uk/).
5. **Credentials**: the same client ID, key ID and private key GetAnIdentifier uses for PDS FHIR.

## Run

From the repository root. Use `--env int` or `--env prod`; `--help` lists all options.

```bash
export NHS_DIGITAL_CLIENT_ID=... NHS_DIGITAL_KID=... NHS_DIGITAL_PRIVATE_KEY=...   # PEM

# Plan: check existing subscriptions and show what would be created or skipped (creates nothing)
dotnet run scripts/mns_subscribe/subscribe.cs -- create --env int --mailbox <MAILBOX_ID> --workflow-id <WORKFLOW_ID> --dry-run

# Show existing subscriptions
dotnet run scripts/mns_subscribe/subscribe.cs -- list --env int

# Create (skips any that already exist, so safe to re-run)
dotnet run scripts/mns_subscribe/subscribe.cs -- create --env int --mailbox <MAILBOX_ID> --workflow-id <WORKFLOW_ID>

# Remove: lists what it will delete and asks you to type 'yes'. Removes every subscription that
# delivers to the mailbox, or only the given --id(s) (repeatable; each must deliver to that mailbox).
dotnet run scripts/mns_subscribe/subscribe.cs -- remove --env int --mailbox <MAILBOX_ID>
dotnet run scripts/mns_subscribe/subscribe.cs -- remove --env int --mailbox <MAILBOX_ID> --id <ID> --id <ID>

unset NHS_DIGITAL_CLIENT_ID NHS_DIGITAL_KID NHS_DIGITAL_PRIVATE_KEY
```

Record the subscription and correlation IDs it prints. Run `list` afterwards and check both are `active`.

## Under-18 filter

MNS has no age filter yet. When it does: get it approved, add it to both criteria in `subscribe.cs`,
run `create` (the new criteria create new subscriptions alongside the old), confirm they are `active`,
then delete the old ones with `remove --env <env> --mailbox <MAILBOX_ID> --id <OLD_GP_ID> --id <OLD_NHS_NUMBER_ID>`.
