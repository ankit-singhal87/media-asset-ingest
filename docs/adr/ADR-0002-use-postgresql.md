# ADR-0002: Use PostgreSQL

## Status

Accepted

## Context

The system needs durable ingest state, auditability, and a transactional outbox.

## Decision

Use PostgreSQL for business state and the transactional outbox. Local
development uses a PostgreSQL container. Azure production targets Azure Database
for PostgreSQL Flexible Server.

## Consequences

- Outbox dispatch can use transactional writes with business state.
- Business state remains queryable for the control plane UI.
- Cosmos DB is deferred because the relational ingest/audit/outbox model is a
  better v1 fit.

