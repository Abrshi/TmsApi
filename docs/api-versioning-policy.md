# TMS API Versioning Policy

## Overview
This document defines the rules and commitments for versioning, deprecating, and retiring API endpoints across the TMS platform to prevent client-side breakage.

---

## Versioning Schemes
1. **Primary Scheme (URL Segment):** All standard endpoints carry explicit version identifiers in the path (e.g., `/api/v1/courses`, `/api/v2/courses`).
2. **Escape Hatch (Header-based):** For clients using cached static or CDN URLs, versioning can be specified via the `X-Api-Version` HTTP header (e.g., `X-Api-Version: 2.0` targeting `/api/courses`).

---

## Breaking vs. Additive Changes

### Breaking Changes (Requires New Major Version)
* Renaming or removing existing JSON properties/fields.
* Changing data types or format expectations for existing fields.
* Adding new mandatory parameters or request body fields.
* Changing HTTP status codes for existing failure scenarios.
* Tightening validation rules on existing endpoints.

### Non-Breaking / Additive Changes (Allowed in Current Version)
* Adding new optional fields to response objects.
* Adding new optional request body fields or query parameters.
* Adding completely new endpoints.

---

## Deprecation and Sunset Strategy

* **Minimum Sunset Window:** When a new major API version is released (e.g., V2), legacy versions (V1) are guaranteed to remain active for a **minimum of 6 months** to support quarterly maintenance windows.
* **HTTP Signaling:** Deprecated endpoints explicitly announce their status on every response using RFC standard headers:
  * `Deprecation: true`
  * `Sunset: <RFC 7231 Date>`
  * `Link: </api/v2/...>; rel="successor-version"`
* **Communication:** Deprecations are communicated through HTTP headers, CHANGELOG entries, direct email notifications to API key owners, and scheduled shutdown calendar invites.

---

## Version Skipping
* Clients are permitted to skip intermediate versions (e.g., migrating directly from V1 to V3) without needing to adopt each intermediate release.