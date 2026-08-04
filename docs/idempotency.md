# Idempotent Resource Handling

The extension keeps repeated `bicep local-deploy` executions safe by reusing existing Cloudflare resources when they already exist.

## DNS Records

- Before creating a record, the handler queries Cloudflare for an existing record with the same fully-qualified name and type.
- If a match exists, the `recordId` from Cloudflare is reused and a `PUT` request updates the record in place; otherwise a `POST` creates it.

## Security Rules

- On the first deploy the rule receives a stable `ref` value (by default the resource name, or the optional `reference` property if supplied).
- Each update evaluates existing rules in the `http_request_firewall_custom` entrypoint ruleset by matching `ruleId`, then `ref`, then expression and descriptions.
- When a match is found, the existing `ruleId` is reused and a `PATCH` updates the custom rule in place. New rules are created with `POST` calls.
