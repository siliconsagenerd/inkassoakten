# Release Reconciliation — InkassoAkten

Date: 2026-10-28. No code, infra, or deployment changes made.

## 1. Intended final commit

- Intended final: `a97bb5b8744714ae63ba7f4619b883f60ee5de60` ("tighten UI: balance breakdown, payment preview, desaturated badges") == current `origin/main`.
- `090a5584483e9e9639dc3466e084bf9389cadff9` ("polish demo presentation") is an ancestor (verified: `git merge-base --is-ancestor 090a558 a97bb5b` -> YES). Between them: `a18486f` "Redesign UI" + `a97bb5b` "tighten UI", 9 files, +304/-266.
- Recommendation: retain `a97bb5b`. It is the tested successor containing 090a558's presentation work plus the redesign/tighten pass. CI passed.

## 2. Running source/image mapping

- Configured image ref (VM compose): `.../inkassoakten/web:a97bb5b87447`
- Local image ID (VM): `sha256:272ae046003292ea8e9b42e1b6eb039bf0d1e248bab22ab2e50523372f390cfc`
- RepoDigests (docker inspect): same `sha256:272ae...`
- Registry manifest digest (gcloud describe): same `sha256:272ae...`
- Tag == commit-SHA prefix; digest consistent across all four. Tag alone is not proof; the digest match is the evidence.

## 3. CI evidence

- https://github.com/siliconsagenerd/inkassoakten/actions/runs/40735795599 — conclusion success, completed, 1m23s, 2026-10-28T17:26:49Z, commit a97bb5b, branch main, event push. Domain + integration tests passed, 0 build errors.

## 4. Verification status

Public read-only: VERIFIED by automated tests.
- PublicReadOnly_Disables_Operator_Login_Endpoint (valid antiforgery token + correct creds -> redirect /login)
- PublicReadOnly_Rejects_Persisted_Mutations_Even_For_Operator (DbContext throws UnauthorizedAccessException)
- PublicReadOnly_Allows_Anonymous_Reads_And_Pdf
- PublicReadOnly_WithBypass_Allows_Persisted_Mutations
- Earlier /auth/login -> 400 was antiforgery (Login_Rejected_Without_Antiforgery_Token), not PublicReadOnly.
- Layers: login handler (!publicReadOnly), ZahlungPostingService.CanWrite, InkassoAktenDbContext.SaveChangesAsync guard.

Preview non-persistence: UI check PASS (limited); DB non-persistence UNVERIFIED. Preview is client-side (AkteDetail.UpdatePreview -> Verrechne), no DB call by construction, but no isolated-DB test covers it.

## 5. Deduplicated rollback inventory

| Tag | Registry digest | Source revision | Retrievable |
|---|---|---|---|
| a97bb5b87447 (running) | sha256:272ae046003292ea8e9b42e1b6eb039bf0d1e248bab22ab2e50523372f390cfc | a97bb5b | yes |
| a18486f19c4d | sha256:8c658e2000be2793bb55b958298a0110f1bbc65e2e04af2d2aa13ec9e3eaaff0 | a18486f | yes |
| 090a5584483e... | sha256:42bbd243b44110b0af80abdbd538c1499e2458bd02049eb80ee439325b324d68 | 090a558 | yes |
| 98476b67e6f7... | sha256:b4a0b273c7361392078898df61bfbf479ee07abf460ec2a24a9a2f832d47bc52 | 98476b6 | yes |
| 0bd99421c220... / 263c13c653c5... | sha256:16f643aa4f04c85cd0770142811e4dd414cefe21d59272605e34bcd098f373f7 (shared) | 0bd9942 / 263c13c | yes |

6 tags, 5 distinct digests. Retrievable = present in Artifact Registry. Executing rollback needs compose edit + restart (deployment action, approval required).

## 6. Current-state screenshots (desktop 1280x800, mobile 390x844)

- docs/screenshots/current-home-desktop.png / current-home-mobile.png
- docs/screenshots/current-akten-desktop.png / current-akten-mobile.png
- docs/screenshots/current-akte-desktop.png / current-akte-mobile.png
- docs/screenshots/current-berichte-desktop.png / current-berichte-mobile.png

No historical same-view before/after exists; not recreated.

## 7. Accessibility/keyboard

- Tab moves focus (verified); focus-visible outline on nav-tabs (2px accent); Bootstrap rings for buttons/inputs.
- Enter/Space = browser default (no custom handlers); no dialogs -> Escape N/A.
- Skip links, arrow-key table/tab nav, screen-reader semantics: UNVERIFIED.
- 31px "Filtern"/"Zurücksetzen" height: mobile usability observation, not an established WCAG failure.

## 8. Remaining limitations

- Paging not exercised across multiple pages (5 records, single page).
- Preview non-persistence DB state UNVERIFIED.
- Backup not automated (no cron/systemd timer; backup.sh manual; GCS upload unverified).
- Tap targets below 44px on filter buttons (usability).
- Keyboard/screen-reader beyond Tab/focus partially UNVERIFIED.
- Registry has extra digests beyond tagged rollback set (platform variants).

Conclusion: retain a97bb5b (sha256:272ae...). No deployment change, rollback, or redesign performed. If a deployment change is genuinely needed, request approval with exact target first.