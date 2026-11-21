# CI and releases

The versioning and release flow is ported from [uno-app-template](https://github.com/MartinZikmund/uno-app-template):
Nerdbank.GitVersioning (NBGV) computes the version, `main` never publishes, and a push to a `release/vX.Y`
branch is the release.

| Workflow | Trigger | What it does |
| --- | --- | --- |
| `ci.yml` | PR to any branch, push to `main` | Version sanity, unit tests, unsigned Android Release build (plus an exact-alarm permission guard), unsigned iOS simulator build |
| `build-main.yml` | push to `main` | Packages Android and iOS in the **Dev** channel as run artifacts. Publishes nothing |
| `prepare-release.yml` | manual | Cuts `release/vX.Y` from `main` and moves `main` to the next minor |
| `release.yml` | push to `release/v**` | Builds Android and iOS in the **Prod** channel, uploads to Play internal (draft) and TestFlight, tags `vX.Y.Z`, drafts a GitHub Release |
| `forward-merge.yml` | push to `release/v**` | Keeps one open `release/vX.Y → main` PR so a hotfix is never lost |
| `_build-android.yml`, `_build-ios.yml` | called | One reusable workflow per head. Only ever produces an artifact |

Every checkout uses `fetch-depth: 0`: NBGV needs the full history to count the git height.

## Cutting a release

**One-time setup:** `main` is protected by a ruleset that requires a PR, so the cut opens a PR for `main`'s version
bump, and `forward-merge.yml` opens release → main PRs. Both need **Settings → Actions → General → Workflow
permissions → Allow GitHub Actions to create and approve pull requests**. Without it the cut fails after pushing the
release branch, before it starts `Release` (start it by hand from the Actions tab, then bump `main` yourself).

1. Make sure the latest `CI` run on `main` is green (the cut refuses otherwise).
2. **Actions → Cut release branch → Run workflow**, with `dry_run` ticked. It prints the plan and pushes nothing.
3. Run it again without `dry_run`. `nbgv prepare-release` creates `release/v6.0` (version `6.0`) and moves `main`
   to `6.1-dev`. Because of the ruleset, the bump arrives as a PR - merge it before the next cut.
4. The workflow starts `Release` on the new branch. Without a GitHub App (`RELEASE_APP_ID`) the push is made with
   `GITHUB_TOKEN`, which triggers nothing, so it dispatches `Release` explicitly; the warning about it is expected.
5. When `Release` finishes: the AAB is a **draft** release on the Play **internal** track. Roll it out, then promote
   it in the Play Console. The iOS build lands in TestFlight once the iOS secrets exist.
6. Publish the draft GitHub Release `v6.0.Z` if you want one.

**A patch is just another commit on `release/vX.Y`.** Cherry-pick the fix from `main` (or PR it into the release
branch) and push; the height moves, every version moves with it, and `release.yml` runs again. Then merge the
forward-merge PR, keeping **main's** `version.json` in the conflict.

`release.yml` can also be dispatched by hand on a release branch: `only` (`android`, `ios`) builds and publishes one
head without tagging, and `force` re-uploads a commit that already reached a store.

## How versions are computed

`version.json` at the repo root holds `major.minor` and a prerelease tag. `main` is always the *next* release with
`-dev` (now `6.0-dev`); the tag is what gives `nbgv prepare-release` something to cut. `publicReleaseRefSpec` only
matches `release/vX.Y`, so `main` can never produce a stable version.

```
major, minor  <- version.json
patch Z       <- NBGV git height (commits since version.json's version was set)

simple = X.Y.Z                            ApplicationDisplayVersion, versionName,
                                          iOS CFBundleShortVersionString and CFBundleVersion
code   = X*10_000_000 + Y*100_000 + Z     android:versionCode  (+ vars.VERSION_CODE_OFFSET)
```

The `.github/actions/app-version` composite is the single place this is computed, and every packaging job passes it
to MSBuild as `-p:CiVersionInjected=true -p:ApplicationDisplayVersion=X.Y.Z -p:ApplicationVersion=<code or X.Y.Z>`.
Global properties are the only reliable route: the Android and iOS SDKs read these at evaluation time, before NBGV's
target computes anything. `src/Directory.Build.targets` fails a Prod CI build that lost the injection, and the build
workflows read the version back out of the generated `AndroidManifest.xml` / `Info.plist`.

For 6.0 that means `versionCode = 60_000_000 + Z`, comfortably above the 5.x builds already on Play (`500001`). The
height does not reset when the release branch is cut, so codes are monotonic within a train, and a minor bump adds
100 000, so `release/v6.1` (`60_100_000 + Z`) always beats anything `release/v6.0` can produce. Slot limits: major ≤ 209,
minor ≤ 99, patch ≤ 99 999 (Play's ceiling is 2 100 000 000).

`VERSION_CODE_OFFSET` is not needed today; it exists for an app whose Play versionCode is already above the
computed one.

> A **local** build without the injected properties gets NBGV's own Android scheme,
> `major<<24 + minor<<16 + height` (`100663297` for 6.0.1), which is *higher* than CI's. Never upload a locally built
> AAB to Play: it would push the Play floor above every CI build.

`AppChannel` is `Dev` everywhere except `release/v*` CI builds, which pass `Prod`. It only drives the version guard
and which configuration is packaged; the app id and name are the same in both channels.

## Secrets

Names are unchanged from the earlier `package-*` workflows. Each publish step checks for its whole set and skips
green with a `::warning::` when any is missing, so a missing secret is never a build failure.

| Secret | Used by | Missing ⇒ |
| --- | --- | --- |
| `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASSWORD`, `ANDROID_STORE_PASSWORD` | `_build-android.yml` (via `release.yml`, `build-main.yml`), `release.yml` Play upload | any one missing ⇒ unsigned AAB and the Play upload skips |
| `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON` | `release.yml` Play upload | Play upload skips |
| `APPLE_DISTRIBUTION_P12_BASE64`, `APPLE_P12_PASSWORD`, `APPLE_PROVISIONING_PROFILE_BASE64`, `APPLE_CODESIGN_KEY`, `APPLE_TEAM_ID` | `_build-ios.yml` (via `release.yml`), with `vars.APPLE_PROVISIONING_PROFILE_NAME` | any one missing ⇒ iOS simulator build and TestFlight skips |
| `KEYCHAIN_PASSWORD` | `_build-ios.yml` | optional: a random one is generated |
| `APPLE_ADHOC_PROVISIONING_PROFILE_BASE64` | `build-main.yml` iOS | `main` builds for the simulator |
| `APPSTORE_ISSUER_ID`, `APPSTORE_API_KEY_ID`, `APPSTORE_API_PRIVATE_KEY` | `release.yml` TestFlight upload | TestFlight skips |
| `APPSETTINGSPROD` | `_build-android.yml`, `_build-ios.yml` on Prod | **Prod build fails**: it would ship the test ad configuration |
| `RELEASE_APP_PRIVATE_KEY` | `prepare-release.yml`, `release.yml` tag | falls back to `GITHUB_TOKEN` + an explicit `Release` dispatch |

`APPSETTINGSPROD` is the full production `appsettings.json` (incl. `AndroidAdUnitId` and `IosAdUnitId`). Prod builds
overwrite `src/SmsTicket/appsettings.json` with it; Dev builds keep the checked-in file. Because the packages embed
it, the draft GitHub Release carries no binaries.

## Variables

| Variable | Purpose |
| --- | --- |
| `APPLE_PROVISIONING_PROFILE_NAME` | profile **name** as shown in the developer portal. Required for a signed iOS build |
| `ADMOB_IOS_APP_ID` | AdMob iOS app ID, passed to Prod iOS builds. Empty ⇒ ads stay off |
| `VERSION_CODE_OFFSET` | added to the Android versionCode. Leave unset |
| `RELEASE_APP_ID` | GitHub App id for the release cut. Empty selects the `GITHUB_TOKEN` fallback |
| `ENABLE_ANDROID`, `ENABLE_IOS` | set to `false` to skip that head in `build-main.yml` and `release.yml` |
