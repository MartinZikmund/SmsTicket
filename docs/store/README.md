# Store assets

| Folder | Store | Size |
| --- | --- | --- |
| `android/<lang>/phone-*.jpg` | Google Play phone screenshots | 1080×1920 |
| `android/<lang>/feature-graphic.jpg` | Google Play feature graphic | 1024×500 |
| `ios/<lang>/iphone-*.jpg` | App Store iPhone screenshots (upload resized to 1206×2622, the size the 6.3" slot accepts) | 1290×2796 |
| `ipad/<lang>/ipad-*.jpg` | App Store 13" iPad screenshots | 2064×2752 |

The app screens they're made from:

- `raw/<lang>-*.jpg`: phone, from the `smsticket-phone` emulator (1344×2992).
- `raw-ipad/<lang>-*.jpg`: the same emulator switched to `wm size 2064x2752` and `wm density 320`, with System UI demo mode on for a clean status bar.

Both come from a Debug build with ads turned off and a seeded purchase history.

## Regenerate
Replace the raw screens, then run (Windows, needs Microsoft Edge and Pillow):

```
python docs/store/make.py           # Play phone shots     -> out/<lang>-<n>-<screen>.png
set IOS=1 && python docs/store/make.py  # App Store iPhone -> out-ios/
python docs/store/ipad.py           # App Store iPad       -> out-ipad/
python docs/store/feature.py        # Play feature graphic -> out/feature-<lang>.png
```

Then copy the renders into the matching folders above. Headlines and colors are at the top of `make.py`, and `ipad.py` has its own copy.

Upload screenshots one at a time, in order: both consoles sort a multi-file upload by whichever finishes first.
