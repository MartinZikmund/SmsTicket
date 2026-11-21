"""Play Store feature graphic (1024x500), rendered with headless Edge."""
import pathlib, subprocess

ROOT = pathlib.Path(__file__).parent
OUT = ROOT / "out"  # renders land here; copy the PNGs to android/<lang>/
OUT.mkdir(exist_ok=True)
ICON_DIR = ROOT.parents[1] / "src" / "SmsTicket" / "Assets" / "Icons"
EDGE = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"

TEXT = {
    "en": ("SMS Ticket", "Public transport tickets by SMS\nfor 13 Czech cities"),
    "cs": ("SMS jízdenka", "Jízdenky MHD přes SMS\npro 13 českých měst"),
}

HTML = """<!DOCTYPE html><html><head><meta charset="utf-8"><style>
@font-face {{ font-family: OS; src: url('file:///C:/Windows/Fonts/OpenSans-Regular.ttf'); font-weight: 400; }}
@font-face {{ font-family: OS; src: url('file:///C:/Windows/Fonts/OpenSans-ExtraBold.ttf'); font-weight: 800; }}
* {{ margin: 0; box-sizing: border-box; }}
html, body {{ width: 1024px; height: 500px; overflow: hidden; }}
body {{ font-family: OS, sans-serif; color: #fff; position: relative;
  background: radial-gradient(700px 500px at 90% 0%, #3FA2FFAA 0%, transparent 60%), linear-gradient(135deg, #0A3D91 0%, #0472D8 100%); }}
.wires {{ position: absolute; inset: 0; opacity: .16; }}
.left {{ position: absolute; left: 64px; top: 0; bottom: 0; width: 560px; display: flex; flex-direction: column; justify-content: center; gap: 22px; }}
.row {{ display: flex; align-items: center; gap: 22px; }}
.icon {{ width: 96px; height: 96px; border-radius: 24px; overflow: hidden; position: relative; box-shadow: 0 10px 30px #0005; flex: none; }}
.icon img {{ position: absolute; inset: 0; width: 100%; height: 100%; }}
h1 {{ font-weight: 800; font-size: 64px; letter-spacing: -1px; line-height: 1; }}
p {{ font-size: 30px; line-height: 1.35; opacity: .92; white-space: pre-line; }}
.phone {{ position: absolute; right: 70px; top: 46px; width: 300px; height: 640px; border-radius: 40px; padding: 10px;
  background: linear-gradient(145deg, #2b2f36, #0c0d10); transform: rotate(8deg); box-shadow: 0 40px 80px #0008; }}
.phone img {{ width: 100%; height: 100%; border-radius: 31px; object-fit: cover; object-position: top; }}
</style></head><body>
<svg class="wires" viewBox="0 0 1024 500" preserveAspectRatio="none"><g stroke="#fff" stroke-width="3" fill="none">
<path d="M-20 90 C 300 120, 650 60, 1050 140"/><path d="M-20 150 C 340 200, 700 130, 1050 240"/></g></svg>
<div class="left"><div class="row"><div class="icon"><img src="{bg}"><img src="{fg}"></div><h1>{name}</h1></div><p>{tag}</p></div>
<div class="phone"><img src="{shot}"></div>
</body></html>"""

for lang, (name, tag) in TEXT.items():
    page = OUT / f"feature-{lang}.html"
    page.write_text(HTML.format(bg=(ICON_DIR / "icon.svg").as_uri(), fg=(ICON_DIR / "icon_foreground.svg").as_uri(),
                                name=name, tag=tag, shot=(ROOT / "raw" / f"{lang}-main.jpg").as_uri()), encoding="utf-8")
    png = OUT / f"feature-{lang}.png"
    subprocess.run([EDGE, "--headless=new", "--disable-gpu", "--hide-scrollbars", f"--screenshot={png}",
                    "--window-size=1024,500", "--force-device-scale-factor=1", "--virtual-time-budget=3000", page.as_uri()],
                   check=True, capture_output=True)
    print(png)
