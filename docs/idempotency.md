# Idempotent Resource Handling

The extension keeps repeated `bicep local-deploy` executions safe by reusing existing CloudFlare resources whenever they already exist. Below is the quick reference for the two resource types with custom logic.

## DNS Records

- Before creating a record, the handler queries Cloudflare for an existing record with the same fully-qualified name and type.
- If a match exists, the `recordId` from Cloudflare is reused and a `PUT` request updates the record in place; otherwise a `POST` creates it.

## Security Rules

- On the first deploy a rule is created with a stable `ref` value matching the resource name, so future runs can find it.
- Each update first attempts a lookup by rule name/description; if that fails, it falls back to downloading a paged rule list for the zone.
- When a matching rule is found, the existing `ruleId` (and filter) are reused and a `PUT` updates the rule. New rules are created with `POST` calls.

Whilst this has a performance hit, it stops subsequent template deployments failing entirely which is a better compromise at this stage.
