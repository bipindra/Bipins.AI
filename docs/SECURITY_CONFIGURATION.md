# API security configuration

The API accepts explicitly configured API keys or signed JWT bearer tokens. Anonymous requests, Basic authentication, and the former built-in test key are rejected.

Provision API keys through a secret provider or environment variables:
- `Authentication__ApiKeys__client1__Key`: a generated secret
- `Authentication__ApiKeys__client1__TenantId`: the client's tenant
- `Authentication__ApiKeys__client1__Roles__0`: `User` (or `Admin` for deliberately provisioned administrators)

Send the key in `X-API-Key` or `Authorization: ApiKey <key>`. Keys are case-sensitive. An empty key configuration denies every key. Do not commit actual keys.

For JWT, configure `JWT_SECRET`, `JWT_ISSUER`, and `JWT_AUDIENCE` (issuer/audience default to `Bipins.AI`). Tokens must be signed, unexpired, and contain a nonempty `tenantId` claim. Use the `role` claim for roles. Missing signing configuration does not enable an insecure fallback.

The authenticated tenant controls access. `X-Tenant-Id` cannot change identity. Request-body or URL tenant IDs must match the authenticated tenant unless the principal has the `Admin` role.

## Batch ingestion migration

HTTP `POST /v1/ingest/batch` accepts `texts`, not `sourceUris`. Read documents in a trusted client and submit their text. The library's filesystem loader remains available for trusted local ingestion. Optional `maxConcurrency` must be between 1 and 32.

## Document updates

Replacement writes complete before old-version deletion. Updates use fresh vector IDs to avoid overwriting old records during a failed replacement. Cleanup uses the actual embedding vector and bounded pages rather than a fixed dimension or total-result limit. Cleanup failures are reported in `IndexResult.Errors`, while successfully written replacement vectors remain available.

The vector-store adapter must complete its write before acknowledging success. Qdrant writes and deletes now request `wait=true`. This is not a transaction across an entire document: an interrupted cleanup can leave both versions present, and simultaneous updates of the same document should be serialized by the caller.

## CI

The CI target runs unit tests and fails on test failures. It installs the .NET 10 SDK for the library's target frameworks. Integration tests requiring external services remain separate.
