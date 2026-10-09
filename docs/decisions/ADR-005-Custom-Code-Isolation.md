# ADR-005: Custom Code Isolation (Threat Model Baseline)

## Status
Proposed (baseline; implementation not yet planned)

## Context
Future feature: administrators may provide custom C# code (SMS providers, payment gateways, workflow blocks) stored in DB and executed later. This is untrusted code.

## Decision (Baseline)
- Treat all stored custom code as **untrusted executable code**
- **Do not** execute arbitrary code inside the main web application process
- **Do not** rely solely on Roslyn sandboxing/reflection restrictions as security boundary
- Isolation required: separate process/container with strict controls
- Before implementation, complete full threat model and isolation architecture

## Required Controls (when implementing)
- Non-root execution
- Filesystem/network restrictions (egress limited)
- CPU/memory limits, execution timeouts, process termination
- Strict input/output serialization over controlled contract
- Secret isolation; no access to app secrets/DB creds/host files/Docker socket
- Explicit capability interfaces (only permitted operations)
- Dependency restrictions
- Comprehensive logging/auditing
- Defense-in-depth; consider WASI/VM/container runtime

## Alternatives Considered
- In-process Roslyn with AllowList: insufficient as general sandbox
- AssemblyLoadContext restrictions only: brittle against native/unsafe access in general
- Out-of-process gRPC/HTTP to isolated worker: viable with strong controls

## Consequences
- Security requirement blocks naive implementation
- Adds operational complexity (worker pool/containers)
- Must design controlled contracts upfront

## Unresolved
Full threat model, transport, worker lifecycle, resource quotas, deployment model
