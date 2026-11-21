"""Composes store screenshots: headline + phone mockup on a branded background, rendered with headless Edge.

iPad variant of make.py: tablet mockup over raw-ipad/<lang>-<screen>.jpg, renders into out-ipad/.
"""
import os, pathlib, subprocess, sys

CW, CH, SCALE = 1032, 1376, 2  # renders 2064x2752, the App Store 13" iPad size

ROOT = pathlib.Path(__file__).parent
RAW = ROOT / "raw-ipad"
OUT = ROOT / "out-ipad"  # renders land here; copy them to ipad/
OUT.mkdir(exist_ok=True)
ICON_DIR = ROOT.parents[1] / "src" / "SmsTicket" / "Assets" / "Icons"
EDGE = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"

SLIDES = [
    # key, theme, en (headline, sub), cs (headline, sub)
    ("main", "blue",
     ("SMS ticket\nin seconds", "Favorites with one tap and a live countdown for every active ticket"),
     ("SMS jízdenka\nza pár vteřin", "Oblíbené na jedno klepnutí a odpočet pro každou platnou jízdenku")),
    ("picker", "teal",
     ("13 Czech cities,\none app", "Pick your city or let the app find the nearest one"),
     ("13 českých měst\nv jedné aplikaci", "Vyberte město, nebo nechte aplikaci najít to nejbližší")),
    ("history", "violet",
     ("Every ticket\nat a glance", "Purchase history with monthly totals"),
     ("Všechny jízdenky\npod kontrolou", "Historie nákupů s měsíčními součty")),
    ("dark", "night",
     ("Everything about\nyour city's transit", "Operator, SMS ticket rules and journey planner. In a beautiful dark mode."),
     ("Vše o MHD\nve vašem městě", "Dopravce, pravidla SMS jízdenek i vyhledávač spojení. I v tmavém režimu.")),
    ("settings", "sunset",
     ("Your app,\nyour way", "Light or dark theme, Czech or English"),
     ("Aplikace\npodle vás", "Světlý i tmavý motiv, čeština i angličtina")),
]

THEMES = {
    "blue":   ("#0A3D91", "#0472D8", "#3FA2FF"),
    "teal":   ("#04506B", "#0A8F9A", "#38D0C0"),
    "violet": ("#2B1B78", "#4F3BD1", "#8E7BFF"),
    "night":  ("#070B14", "#13203A", "#2C4C86"),
    "sunset": ("#7A1E4B", "#D8455C", "#FF9E6B"),
}

TEMPLATE = """<!DOCTYPE html><html><head><meta charset="utf-8"><style>
@font-face {{ font-family: OS; src: url('file:///C:/Windows/Fonts/OpenSans-Regular.ttf'); font-weight: 400; }}
@font-face {{ font-family: OS; src: url('file:///C:/Windows/Fonts/OpenSans-SemiBold.ttf'); font-weight: 600; }}
@font-face {{ font-family: OS; src: url('file:///C:/Windows/Fonts/OpenSans-ExtraBold.ttf'); font-weight: 800; }}
* {{ margin: 0; box-sizing: border-box; }}
html, body {{ width: {cw}px; height: {ch}px; overflow: hidden; }}
body {{ font-family: OS, 'Segoe UI', sans-serif; color: #fff; position: relative;
  background: radial-gradient(1200px 900px at 85% 8%, {c3}AA 0%, transparent 60%),
              radial-gradient(900px 900px at 0% 100%, {c2}CC 0%, transparent 65%),
              linear-gradient(160deg, {c1} 0%, {c2} 100%); }}
.wires {{ position: absolute; inset: 0; opacity: .16; }}
.brand {{ position: absolute; top: 56px; left: 0; right: 0; display: flex; justify-content: center; align-items: center; gap: 18px; }}
.icon {{ width: 72px; height: 72px; border-radius: 18px; overflow: hidden; position: relative; box-shadow: 0 8px 24px #0005; }}
.icon img {{ position: absolute; inset: 0; width: 100%; height: 100%; }}
.brand span {{ font-weight: 600; font-size: 34px; letter-spacing: .5px; opacity: .95; }}
h1 {{ position: absolute; top: 150px; left: 70px; right: 70px; text-align: center; font-weight: 800;
  font-size: 64px; line-height: 1.08; letter-spacing: -1.5px; white-space: pre-line; text-shadow: 0 4px 30px #0003; }}
p {{ position: absolute; top: {sub_top}px; left: 110px; right: 110px; text-align: center; font-size: 34px; line-height: 1.38; opacity: .9; }}
.phone {{ position: absolute; left: 50%; bottom: 36px; transform: translateX(-50%); width: {pw}px; height: {ph}px;
  border-radius: 44px; padding: 16px; background: linear-gradient(145deg, #2b2f36, #0c0d10);
  box-shadow: 0 50px 120px #0009, 0 0 0 2px #ffffff22 inset; }}
.phone img {{ width: 100%; height: 100%; border-radius: 30px; display: block; object-fit: cover; object-position: top; }}
.glow {{ position: absolute; left: 50%; bottom: 200px; width: 900px; height: 900px; transform: translateX(-50%);
  background: radial-gradient(circle, #ffffff30 0%, transparent 60%); }}
</style></head><body>
<svg class="wires" viewBox="0 0 {cw} {ch}" preserveAspectRatio="none">
  <g stroke="#fff" stroke-width="3" fill="none">
    <path d="M-50 330 C 300 360, 700 300, 1130 380"/>
    <path d="M-50 420 C 360 470, 760 400, 1130 520"/>
    <path d="M760 300 L 650 900"/><path d="M640 420 L 610 470 L 700 480"/>
  </g>
</svg>
<div class="glow"></div>
<div class="brand"><div class="icon"><img src="{icon_bg}"><img src="{icon_fg}"></div><span>{app}</span></div>
<h1>{headline}</h1>
<p>{sub}</p>
<div class="phone"><img src="{shot}"></div>
</body></html>"""


def render(lang, idx, key, theme, headline, sub):
    c1, c2, c3 = THEMES[theme]
    shot = (RAW / f"{lang}-{key}.jpg").as_uri()
    pw = 680
    ph = int((pw - 32) * 2752 / 2064) + 32
    html = TEMPLATE.format(
        c1=c1, c2=c2, c3=c3, pw=pw, ph=ph, cw=CW, ch=CH,
        sub_top=150 + 2 * 70 + 24,
        icon_bg=(ICON_DIR / "icon.svg").as_uri(), icon_fg=(ICON_DIR / "icon_foreground.svg").as_uri(),
        app="SMS jízdenka" if lang == "cs" else "SMS Ticket",
        headline=headline, sub=sub, shot=shot)
    page = OUT / f"{lang}-{idx}-{key}.html"
    page.write_text(html, encoding="utf-8")
    png = OUT / f"{lang}-{idx}-{key}.png"
    subprocess.run([EDGE, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--allow-file-access-from-files",
                    f"--screenshot={png}", f"--window-size={CW},{CH}", f"--force-device-scale-factor={SCALE}",
                    "--virtual-time-budget=3000", page.as_uri()], check=True, capture_output=True)
    print(png)


for lang in sys.argv[1:] or ["en", "cs"]:
    for i, (key, theme, en, cs) in enumerate(SLIDES, 1):
        h, s = en if lang == "en" else cs
        render(lang, i, key, theme, h, s)
