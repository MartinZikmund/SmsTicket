# Store assets

- `android/<lang>/phone-*.jpg`: Google Play phone screenshots (1080×1920).
- `android/<lang>/feature-graphic.jpg`: Play feature graphic (1024×500).
- `raw/<lang>-*.png`: the app screens they're made from. They were taken on the `smsticket-phone` emulator (1344×2992), from a Debug build with ads turned off and a seeded purchase history.

## Regenerate
Replace the `raw` screens, then run (Windows, needs Microsoft Edge and Pillow):

```
python docs/store/make.py      # renders out/<lang>-<n>-<screen>.png
python docs/store/feature.py   # renders out/feature-<lang>.png
```

Then copy the renders from `out/` into `android/<lang>/`. Headlines and colors are at the top of `make.py`.
