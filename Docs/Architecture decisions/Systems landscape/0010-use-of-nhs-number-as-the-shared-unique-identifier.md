# ADR-SUI-0010: Use of NHS Number as the Shared Unique Identifier (SUI)

Date: 26 January 2026  
Author: Simon Parsons  
Decision owners: SUI Service Team  
Category: Identity and matching

## Status
Proposed

---

## Context

The SUI service requires a **shared unique identifier (SUI)** to support cross-organisational discovery and matching of records relating to the same individual.

At the time of writing there are two design patterns being considered which are
1) **Distributed Id** where a SUI is provided to custodians who are expected to store them in their data, which they can then use for their own local matching strategy and for responding to requests issued by the central service when establishing which records exist for a given person

2) **Managed ID Register** where a central table is maintained of who knows what about which people.

At the time of writing the **Distributed Id** design pattern is our preferred option for reasons that will be discussed below.

Within the **Distributed Id** architectural pattern, two alternative approaches are being considered for the format of the SUI:

1) **NHS Number in the Clear**  
   The SUI is the NHS number itself and is shared directly with custodians.

2) **Derived / Alternative SUI**  
   The SUI is not the NHS number, but a centrally issued synthetic identifier or a cryptographic derivative (for example encrypted or double-encrypted NHS number).

Both options are technically supported by the overall SUI architecture. However, they represent fundamentally different positions in terms of operational usability, data quality, governance, security, cost, and long-term sustainability.

This ADR records the analysis of both options and the resulting architectural preference.

## Decision

The preferred design is:

 
>**Use NHS number in the clear as the SUI.**

For the time being the architecture could be adapted to support Encryption again, but in Alpha our assumption is that where a SUI is shared with custodians it will be the NHS nuumber we believe matches the subject being indexed, or searched for.

## Rationale

The SUI service operates in a **real-world, multi-agency, human-in-the-loop environment** where:

- Data quality and matching accuracy are critical.
- Human workflows (phone calls, case conferences, safeguarding processes) are first-class.
- Custodians already legitimately hold NHS numbers.
- The central service is not intended to become a national identity authority.

Using NHS number in the clear standardises and governs an identifier that already exists in practice, rather than introducing a parallel identity regime with higher complexity and weaker operational outcomes.

The alternative approach introduces cryptographic and governance complexity while providing only marginal improvements in theoretical privacy posture.

## Options Considered

### Option A – NHS Number in the Clear
The SUI is the NHS number itself and is shared directly with custodians.

### Option B – Derived / Alternative SUI
The SUI is a synthetic or cryptographic identifier derived from NHS number or issued centrally.

## Comparative Analysis

### Strategic & Policy Considerations

| Consideration | What this means in real terms | Option A – Use NHS number directly as the SUI | Option B – Use a new synthetic or encrypted identifier instead of NHS number |
|--------------|--------------------------------|----------------------------------------------|-----------------------------------------------------------------------------|
| Does the SUI align with existing national identifiers already in widespread operational use? | Are we standardising what already exists, or inventing a parallel identity regime? | ✅ Uses the NHS number itself, which is already embedded across health and many public sector systems. | ❌ Introduces a new identifier that is not currently used operationally and must be adopted as a parallel regime. |
| Is there a legal or policy requirement that prohibits using NHS number as a shared identifier? | Is there a hard legal blocker on sharing NHS number at all? | ⚠️ No general prohibition on using NHS number; constraints apply mainly to acquiring it via PDS, not to using legitimately held values. | ✅ Avoids PDS constraints entirely by never using NHS number as the shared identifier. |
| Does the design materially improve outcomes for citizens, or mainly improve theoretical compliance posture? | Does this help services work better, or mainly satisfy principles? | ✅ Improves operational outcomes by using an identifier services already understand and can act on. | ⚠️ Improves compliance posture, but with limited direct benefit to real-world service delivery. |
| Is the identifier intended to become a long-term national asset or a transitional mechanism? | Are we creating something that must exist forever? | ⚠️ Reinforces NHS number as the long-term national join key across sectors. | ❌ Creates a brand new national identifier that must be governed, maintained, and supported indefinitely. |
| Does the approach support future convergence with other national identity programmes? | Does this align with other national data initiatives? | ✅ Strong alignment with health-led and cross-sector identity initiatives that already use NHS number. | ⚠️ Does not block convergence, but requires other programmes to explicitly adopt a new identifier. |
| **Total score** |  | **+3** | **–1** |



### Data Quality & Matching

| Consideration | What this means in real terms | Option A – Use NHS number directly as the SUI | Option B – Use a new synthetic or encrypted identifier instead of NHS number |
|--------------|-------------------------------|----------------------------------------------|-----------------------------------------------------------------------------|
| Does the SUI enhance an authority’s existing internal data matching processes? | Does this improve how an organisation links its own systems? | ✅ Authorities can use the NHS number they already store to link internal systems more accurately. | ❌ Authorities receive a new identifier that does not exist in their legacy systems and cannot be used for internal joins. |
| Does the SUI reduce false negatives in cross-system matching? | Are we less likely to miss real matches? | ✅ The central service provides high-confidence NHS numbers and authorities can validate them against values they already hold. | ⚠️ Central matching may be strong, but authorities cannot independently validate synthetic identifiers against existing records. |
| Can the SUI be used to reconcile historical datasets that predate the service? | Can old data be joined without reprocessing? | ✅ Historical datasets that already contain NHS number can be joined immediately. | ❌ Historical datasets must be reprocessed or mapped to the new identifier. |
| Does the design support progressive data quality improvement over time? | Will data quality improve across the ecosystem? | ✅ Both the central service and authorities can iteratively correct mismatches using a shared, real-world identifier. | ❌ Almost all responsibility for correcting bad data sits with the central service, as authorities cannot validate the new identifier. |
| Can custodians validate or challenge matches using information they already trust? | Can organisations independently verify identity? | ✅ Authorities can compare the NHS number received from the service with their own records. | ❌ Authorities must trust the central service’s identifier with no independent verification path. |
| **Total score** |  | **+5** | **–3** |


### Operational & Human Workflow

| Consideration | What this means in real terms | Option A – Use NHS number directly as the SUI | Option B – Use a new synthetic or encrypted identifier instead of NHS number |
|--------------|-------------------------------|----------------------------------------------|-----------------------------------------------------------------------------|
| Can two professionals discuss the same person offline using the identifier? | Can people use this in phone calls and meetings? | ✅ Staff can read out and recognise the NHS number when discussing a case. | ❌ Staff cannot meaningfully discuss a synthetic identifier because each organisation may see a different value or not recognise it.  |
| Does the SUI support semi-manual workflows and human judgement? | Does this work outside fully automated systems? | ✅ The NHS number supports mixed digital and human workflows such as safeguarding meetings and case conferences. | ❌ The synthetic identifier only works inside tightly controlled system-to-system flows. |
| Can the identifier be practically read, typed, or communicated by humans? | Is it short, simple, and usable by people? | ✅ The NHS number is a short, familiar numeric format already used in frontline services. | ⚠️ Synthetic identifiers are typically longer and opaque, making them harder to communicate or remember. |
| Does the design work in low-tech or degraded environments? | Can this be used with paper, phone, or minimal IT? | ✅ The NHS number can be written down or spoken without needing the central service online. | ❌ The synthetic identifier requires the central service to resolve or interpret it. |
| Does it align with how frontline staff actually work today? | Does this match current behaviour? | ✅ Frontline staff already use NHS number in many cross-agency workflows. | ❌ Frontline staff would need to adopt entirely new processes and identifiers. |
| **Total score** |  | **+5** | **–3** |


### Security & Privacy

| Consideration | What this means in real terms | Option A – Use NHS number directly as the SUI | Option B – Use a new synthetic or encrypted identifier instead of NHS number |
|--------------|-------------------------------|----------------------------------------------|-----------------------------------------------------------------------------|
| Where does identity risk primarily reside in this design – with custodians or with the central service? | Are we reducing overall system risk, or relocating it into a national component? | ❌ Identity risk is distributed across custodians because NHS numbers are shared and stored in multiple systems. | ⚠️ Identity risk is reduced within individual custodians (especially with per-custodian encryption), but is relocated into a central national identity resolution dataset/service with a much larger systemic blast radius. |
| Is the identifier itself considered sensitive personal data? | Is the identifier inherently high-risk if exposed? | ⚠️ The NHS number is already classified as sensitive personal data across the public sector. | ⚠️ Yes. It is still a person identifier inside the custodian’s systems and can enable record access or linkage within that organisation, even if it is not usable across organisations. |
| Does the design meaningfully reduce harm in the event of a custodian data breach? | Would a breach in one organisation expose other organisations? | ❌ A custodian breach exposes NHS numbers that can be reused to correlate data elsewhere. | ✅ With per-custodian encryption, a breach in one organisation does not expose identifiers usable in other organisations. |
| Does the design prevent cross-organisational correlation of data? | Can different organisations link their data without going through the platform? | ❌ NHS numbers enable direct cross-organisational correlation by default. | ✅ With per-custodian encryption, different organisations cannot correlate identifiers without using the central service. |
| Does the design rely on cryptographic key management as a critical control? | Does system security depend on secrets remaining uncompromised? | ✅ No cryptographic secrets are required to protect the identifier itself. | ❌ Security depends on encryption keys and key management processes remaining secure over time. |
| **Total score** |  | **–2** | **+1** |


### Governance & Trust

| Consideration | What this means in real terms | Option A – Use NHS number directly as the SUI | Option B – Use a new synthetic or encrypted identifier instead of NHS number |
|--------------|-------------------------------|----------------------------------------------|-----------------------------------------------------------------------------|
| Do custodians trust the provenance of the identifier? | Do organisations trust where the identifier comes from and what it represents? | ✅ Custodians already recognise and trust NHS number as a legitimate national identifier issued by the health system. | ⚠️ Custodians must trust the central SUI service as the sole issuer and authority for a new identifier they have never used before. |
| Can custodians independently validate the identifier’s legitimacy? | Can an organisation verify the identifier without calling the central service? | ✅ Custodians can validate NHS numbers using existing rules and compare them with values they already hold. | ❌ Custodians cannot validate synthetic identifiers independently and must rely entirely on the central service. |
| Does the design reinforce or weaken trust between organisations? | Does this make inter-organisational working easier or harder? | ✅ A shared real-world identifier supports transparent discussion and mutual validation between organisations. | ⚠️ Trust shifts away from peer organisations and towards the central platform as the single source of truth. |
| Is the central service assuming long-term custodianship responsibility? | Does the platform become responsible for maintaining national identity? | ✅ No. The platform uses an existing national identifier and does not become an identity authority. | ❌ Yes. The platform becomes the national issuer, resolver, and long-term custodian of a new identity scheme. |
| Who is accountable when identifiers are wrong? | Who takes responsibility for identity errors? | ⚠️ Accountability is shared: the central service provides NHS numbers, but custodians can validate and challenge them. | ❌ Accountability sits primarily with the central service because custodians cannot independently verify synthetic identifiers. |
| **Total score** |  | **+4** | **–3** |


### Technical Complexity

| Consideration | What this means in real terms | Option A – Use NHS number directly as the SUI | Option B – Use a new synthetic or encrypted identifier instead of NHS number |
|--------------|-------------------------------|----------------------------------------------|-----------------------------------------------------------------------------|
| Does the design require cryptography, key management, or token services? | Does the platform need specialist security infrastructure just to make the identifier work? | ✅ No. The platform can use NHS number as-is without introducing cryptographic services. | ❌ Yes. The platform must implement encryption, decryption, and key management to generate and resolve identifiers. |
| Is key rotation required and what does that operationally imply? | Do we need to periodically change secrets and re-issue identifiers? | ✅ No. There are no cryptographic keys associated with the identifier itself. | ❌ Yes. Key rotation requires re-encrypting or re-issuing identifiers and coordinating changes across custodians. |
| Does the identifier format fit easily into existing systems? | Do partner systems need schema or validation changes? | ✅ Partner systems are far more likely to already support NHS number, or be able to accommodate it with minimal change. | ❌ Partner systems are far less likely to already support a new synthetic identifier, and will typically require new fields, validation rules, and integration changes. |
| Does the design impose additional performance overhead on every query? | Is there extra latency just to resolve the identifier? | ✅ No. The identifier can be used directly without translation. | ❌ Yes. Every operation may require encryption/decryption or central resolution. |
| Is the system robust to partial failure of central components? | Does identity still work if the central service is unavailable? | ✅ Yes. Custodians can still use and recognise NHS numbers locally. | ❌ No. Custodians cannot interpret or use synthetic identifiers without the central service. |
| **Total score** |  | **+5** | **–5** |


### Cost & Delivery Risk

| Consideration | What this means in real terms | Option A – Use NHS number directly as the SUI | Option B – Use a new synthetic or encrypted identifier instead of NHS number |
|--------------|-------------------------------|----------------------------------------------|-----------------------------------------------------------------------------|
| What is the technical and operational cost of running this design long-term? | What does it cost to build, operate, and support the platform itself? | ✅ Lower technical cost because the platform does not require cryptographic services, key management, or identity resolution infrastructure. | ❌ Higher technical cost due to operating encryption services, key management, rotation, and a central identity resolution capability. |
| What is the governance, legal, and compliance burden of this design? | What is the cost of DPIAs, legal agreements, assurance, and regulatory scrutiny? | ⚠️ Higher distributed compliance burden because NHS numbers are handled directly by many custodians, requiring strong IG controls and legal agreements. | ⚠️ Higher centralised compliance burden because the platform becomes a national identity authority requiring DPIAs, legal frameworks, and security accreditation. |
| How much additional development is required for custodians? | How much rework do partner organisations need to do? | ✅ Likely minimal, as partners are far more likely to already support NHS numbers or accommodate them with minimal change. | ❌ Likely significant, as partners must add new fields, validation rules, and integration logic for a new identifier. |
| Does the design introduce specialist operational roles? | Do we need new skills or teams to run this? | ✅ No specialist roles beyond standard data governance and support. | ❌ Requires ongoing security operations, cryptographic key management, and specialist monitoring. |
| Does it increase onboarding friction for new custodians? | Is it harder for new organisations to join the ecosystem? | ✅ Low friction, as new partners can use an identifier they already understand. | ❌ Higher friction, as new partners must implement and test support for a new identity scheme. |
| Is the design testable and observable in production? | Can issues be debugged and diagnosed easily? | ✅ Behaviour is transparent and easier to reason about using familiar identifiers. | ❌ More complex failure modes due to encryption, key issues, and central resolution dependencies. |
| **Total score** |  | **+3** | **–3** |


### Interoperability & Ecosystem Effects

| Consideration | What this means in real terms | Option A – Use NHS number directly as the SUI | Option B – Use a new synthetic or encrypted identifier instead of NHS number |
|--------------|-------------------------------|----------------------------------------------|-----------------------------------------------------------------------------|
| Does the SUI help or hinder interoperability between organisations outside the platform? | Does this help multi-agency working beyond just the SUI service? | ✅ A shared NHS number directly supports interoperability between health, local authorities, safeguarding partners, and shared care records. | ⚠️ Interoperability is largely limited to organisations integrated with the SUI platform, as the synthetic identifier has no meaning outside it. |
| Can the identifier be reused in other national services or data initiatives? | Is this useful beyond this specific programme? | ✅ NHS number is already used widely and can be reused across multiple national services. | ❌ The synthetic identifier is specific to the SUI platform and is unlikely to be adopted elsewhere. |
| Does the design lock the ecosystem into a central service? | Can organisations meaningfully work without the platform? | ✅ No lock-in. Organisations can still use NHS numbers independently of the SUI service. | ❌ Yes. Organisations become dependent on the SUI service to resolve and interpret identifiers. |
| Can custodians use the identifier independently of the platform? | Does the identifier have value outside SUI? | ✅ NHS number is meaningful and usable outside the SUI context. | ❌ The synthetic identifier is meaningless without the central service. |
| Does it create positive network effects over time? | Does value increase as more organisations adopt it? | ✅ Yes. The more organisations use NHS number consistently, the more valuable interoperability becomes across the whole ecosystem. | ⚠️ Yes, but mainly within the closed SUI platform ecosystem, not across the wider public sector. |
| **Total score** |  | **+5** | **–3** |


### Evolution & Future-Proofing

| Consideration | What this means in real terms | Option A – Use NHS number directly as the SUI | Option B – Use a new synthetic or encrypted identifier instead of NHS number |
|--------------|-------------------------------|----------------------------------------------|-----------------------------------------------------------------------------|
| Can the system evolve safely if national policy on identifiers changes? | What happens if policy later restricts or changes how NHS numbers can be used? | ❌ Higher risk: if policy tightens around NHS number usage after rollout, the system would require significant redesign. | ⚠️ Better insulated from NHS-specific policy changes, but still subject to new policy around operating a national identity service. |
| Is migration between schemes feasible later? | Could we switch from one approach to the other in future? | ⚠️ Technically feasible, but would involve significant cost, reprocessing, and risk to existing integrations. | ⚠️ Also technically feasible, but similarly costly and risks wasting earlier investment. |
| Does the design allow for multiple identifiers in future? | Can we support additional national or sector identifiers later? | ⚠️ Additional identifiers can be layered on, but NHS number would remain the dominant join key. | ⚠️ Additional identifiers add further complexity and central resolution logic. |
| Is backward compatibility with historical data achievable? | Can older datasets still be used without major transformation? | ✅ Historical datasets containing NHS number remain directly usable. | ❌ Historical datasets require mapping or reprocessing to align with the new identifier. |
| Does the design support incremental rollout and partial adoption? | Can early adopters get value before full national uptake? | ✅ Immediate value: organisations can use NHS number even if others have not yet joined. | ⚠️ Limited value initially: synthetic identifiers are only useful once multiple organisations adopt the platform. |
| **Total score** |  | **+1** | **–1** |


##
# Overall Indicative Score

| Option | Total score |
|--------|-------------|
| Option A – NHS number in the clear | +29 |
| Option B – Derived / alternative SUI | –21 |

## Consequences

### Positive
- Maximises data quality and matching accuracy.
- Supports real-world multi-agency workflows.
- Avoids turning the SUI platform into a national identity provider.
- Minimises technical and operational complexity.
- Provides immediate value to early adopters.

### Trade-offs / Risks
- NHS number remains a sensitive identifier and requires strong governance, audit, and access controls.
- Future policy changes around NHS number usage could force migration.
- Breach impact is not reduced through technical abstraction.

These risks are considered acceptable and preferable to the structural, operational, and governance risks introduced by a synthetic or encrypted identifier.
