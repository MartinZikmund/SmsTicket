# CI and releases

| Workflow | Trigger | What it does |
| --- | --- | --- |
| `ci.yml` | PR to any branch, push to `main` | Unit tests, unsigned Android Release build, unsigned iOS simulator build |
| `package-android.yml` | manual | Signed AAB, uploaded to Google Play internal track |
| `package-ios.yml` | manual | Signed IPA, uploaded to TestFlight (app must exist in App Store Connect first) |

Release builds overwrite `src/SmsTicket/appsettings.json` with the `APPSETTINGSPROD` secret (production ad unit IDs).
Version code is `500001 + run number`; the display version comes from the csproj.

## Secrets

Android: `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASSWORD`, `ANDROID_STORE_PASSWORD`, `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON`

iOS: `APPLE_DISTRIBUTION_P12_BASE64`, `APPLE_P12_PASSWORD`, `KEYCHAIN_PASSWORD` (any random string), `APPLE_PROVISIONING_PROFILE_BASE64`, `APPLE_CODESIGN_KEY`, `APPLE_TEAM_ID`, `APPSTORE_ISSUER_ID`, `APPSTORE_API_KEY_ID`, `APPSTORE_API_PRIVATE_KEY`

Shared: `APPSETTINGSPROD` (full production `appsettings.json`, incl. `AndroidAdUnitId` and `IosAdUnitId`)

## Variables

`APPLE_PROVISIONING_PROFILE_NAME` (profile name as shown in the developer portal), `ADMOB_IOS_APP_ID` (AdMob iOS app ID, used once iOS ads land)
