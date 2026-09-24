# /// script
# requires-python = ">=3.12,<3.13"
# dependencies = ["manim==0.21.0", "fonttools"]
# ///

# Anima o gráfico de ritmo de desenvolvimento com e sem testes (o mesmo do
# pôster em /dev/tests-diagram) e gera um MP4 de 1080x1350 para o LinkedIn.
#
#   uv run Scripts/tests-pace-animation.py            # Scripts/.renders/tests-pace.mp4
#   uv run Scripts/tests-pace-animation.py --gif      # também gera o .gif
#   uv run Scripts/tests-pace-animation.py --fps 30   # render mais rápido, para iterar
#
# No Linux o manimpango compila contra o Cairo e o Pango do sistema:
#   apt install libcairo2-dev libpango1.0-dev pkg-config

import argparse
import re
import shutil
import statistics
import tempfile
import urllib.request
from pathlib import Path

import manimpango
import numpy as np
from fontTools.ttLib import TTFont
from manim import (
    DOWN, UP, AnimationGroup, Arrow, Circle, Create, DashedLine, FadeIn, FadeOut, Flash, GrowFromCenter,
    ImageMobject, LaggedStart, Rectangle, RoundedRectangle, Scene, SVGMobject, Succession, Text,
    ValueTracker, VGroup, VMobject, Wait, Write, config, rate_functions,
)
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
FONTS = ROOT / 'Scripts' / '.fonts'
OUT = ROOT / 'Scripts' / '.renders'
ICONS_TS = ROOT / 'Web' / 'app' / 'utils' / 'arch-icons.ts'

W, H, M = 1080, 1350, 64

BG = '#2f1c51'
CARD = '#211439'
TEXT = '#fafafa'
MUTED = '#a1a1aa'
VIOLET = '#8b5cf6'
VIOLET_LIGHT = '#c4b5fd'
FRAME_STROKE = '#a78bfa'
AXIS = '#d4d4d8'
ROSE = '#fb7185'
AMBER = '#F59E0B'
GREEN = '#34d399'
LINKEDIN = '#4aa3f0'

ESTUD_E = 'M 10.98 20.5Q 9.46 20.5 8.27 19.61Q 7.08 18.72 6.4 17.13Q 5.73 15.53 5.73 13.49Q 5.73 11.37 6.34 9.54Q 6.96 7.72 8.08 6.37Q 9.2 5.02 10.71 4.26Q 12.22 3.5 13.97 3.5Q 16.09 3.5 17.18 4.56Q 18.27 5.62 18.27 7.66Q 18.27 9.19 17.44 10.41Q 16.61 11.63 15.16 12.32Q 13.71 13.01 11.84 13.01Q 10.92 13.01 10.12 12.9Q 9.32 12.8 8.57 12.63L 8.65 11.63H 9.72Q 11.38 11.63 12.62 11.07Q 13.85 10.51 14.54 9.47Q 15.23 8.44 15.23 7.09Q 15.23 6.08 14.76 5.54Q 14.28 4.99 13.42 4.99Q 12.33 4.99 11.47 5.81Q 10.61 6.63 10.0 7.94Q 9.4 9.24 9.09 10.74Q 8.77 12.23 8.77 13.58Q 8.77 16.11 9.53 17.38Q 10.29 18.66 11.78 18.66Q 14.6 18.66 16.41 15.85Q 17.47 16.02 17.47 16.62Q 16.24 18.63 14.67 19.57Q 13.11 20.5 10.98 20.5Z'

TITLE_LEAD = 'Quanto custa '
TITLE_ACCENT = 'não testar?'
HANDLE = '/in/zaqueu-cavalcante'
QUOTE_LEAD = 'Cada teste é um checkpoint de comportamento.'
QUOTE_SUB = 'O git lembra como o código era; os testes lembram como o sistema deve ser.'
SOURCE = 'baseado em Khorikov, Unit Testing: Principles, Practices, and Patterns (2020)'

# Mesmas curvas do TestsPacePoster.vue: as três se cruzam em TURN.
TURN = 0.62
TURN_VALUE = 0.4 + 0.18 * TURN

CURVES = [
    {
        'label': 'Sem testes', 'color': ROSE, 'width': 3.5, 'final': ('left', 0.93),
        'value': lambda t: 0.08 + (TURN_VALUE - 0.08) * (np.exp(6 * t) - 1) / (np.exp(6 * TURN) - 1),
    },
    {
        'label': 'Com testes ruins', 'color': AMBER, 'width': 3.5, 'final': ('right', 0.72),
        'value': lambda t: 0.22 + (TURN_VALUE - 0.22) * (np.exp(3 * t) - 1) / (np.exp(3 * TURN) - 1),
    },
    {
        'label': 'Com testes bons', 'color': GREEN, 'width': 4.5, 'final': ('end', 1.0),
        'value': lambda t: 0.4 + 0.18 * t,
    },
]

CHART = {'x': M, 'y': 136, 'w': W - 2 * M, 'h': 1014}
QUOTE = {'x': M, 'y': 1188, 'w': W - 2 * M, 'h': 130}

OX = CHART['x'] + 90
OY = CHART['y'] + CHART['h'] - 100
AXIS_END = CHART['x'] + CHART['w'] - 40
AXIS_TOP = CHART['y'] + 100
PLOT_X = OX + 6
PLOT_W = AXIS_END - PLOT_X - 36
PLOT_H = OY - AXIS_TOP - 40

SANS = 'Saira'
MONO = 'JetBrains Mono'
WEIGHTS = {400: 'NORMAL', 600: 'SEMIBOLD', 700: 'BOLD', 800: 'ULTRABOLD'}
NO_DESCENDER = set('ABCDEFGHIKLMNORSTUVWXZabcdefhiklmnorstuvwxz0123456789')

TMP = Path(tempfile.mkdtemp(prefix='estud-manim-'))
FONT_SCALE: dict[str, float] = {}


def ensure_fonts():
    FONTS.mkdir(parents=True, exist_ok=True)
    css_url = 'https://fonts.googleapis.com/css2?family=Saira:wght@400;600;700;800&family=JetBrains+Mono:wght@400;600'

    # Com um user agent antigo o Google Fonts devolve TTF em vez de WOFF2, que é o que o Pango lê.
    files = sorted(FONTS.glob('*.ttf'))
    if len(files) < 6:
        request = urllib.request.Request(css_url, headers={'User-Agent': 'Mozilla/4.0'})
        css = urllib.request.urlopen(request).read().decode()

        for block in css.split('@font-face')[1:]:
            family = re.search(r"font-family: '([^']+)'", block).group(1)
            weight = re.search(r'font-weight: (\d+)', block).group(1)
            url = re.search(r'src: url\(([^)]+)\)', block).group(1)
            target = FONTS / f"{family.replace(' ', '')}-{weight}.ttf"
            if not target.exists():
                urllib.request.urlretrieve(url, target)

        files = sorted(FONTS.glob('*.ttf'))

    for file in files:
        manimpango.register_font(str(file))

    for family, prefix in [(SANS, 'Saira'), (MONO, 'JetBrainsMono')]:
        font = TTFont(FONTS / f'{prefix}-400.ttf')
        cap = font['OS/2'].sCapHeight / font['head'].unitsPerEm
        FONT_SCALE[family] = cap / Text('H', font=family, font_size=100).height


def P(x, y):
    return np.array([x / 100 - W / 200, H / 200 - y / 100, 0])


def px(t):
    return PLOT_X + t * PLOT_W


def py(v):
    return OY - v * PLOT_H


def baseline(mob, value):
    chars = [c for c in value if not c.isspace()]
    if len(chars) != len(mob.submobjects):
        return mob.get_bottom()[1]

    bottoms = [g.get_bottom()[1] for c, g in zip(chars, mob.submobjects) if c in NO_DESCENDER]
    return statistics.median(bottoms) if bottoms else mob.get_bottom()[1]


def text(value, size, x, y, weight=400, color=TEXT, mono=False, anchor='start', **kwargs):
    mob = sized_text(value, size, weight, color, MONO if mono else SANS, **kwargs)
    place(mob, value, x, y, anchor)
    return mob


def sized_text(value, size, weight, color, family=SANS, **kwargs):
    # Com font_size pequeno o Pango erra o kerning (letras grudadas ou espaçadas
    # demais), então o texto nasce 8x maior e é reduzido depois.
    mob = Text(value, font=family, weight=WEIGHTS[weight], font_size=size * FONT_SCALE[family] * 8, color=color, **kwargs)
    return mob.scale(1 / 8)


def place(mob, value, x, y, anchor='start'):
    edge = {'start': mob.get_left()[0], 'middle': mob.get_center()[0], 'end': mob.get_right()[0]}[anchor]
    mob.shift([P(x, 0)[0] - edge, P(0, y)[1] - baseline(mob, value), 0])


ICONS = {name: body.replace("\\'", "'") for name, body in re.findall(r"'([^']+)': '((?:[^'\\]|\\.)*)'", ICONS_TS.read_text())}


def icon(name, size, x, y, color):
    body = ICONS[name].replace('currentColor', color)
    path = TMP / f'{name}-{color[1:]}.svg'

    # O retângulo invisível fixa o viewBox inteiro como caixa do ícone, senão o
    # SVGMobject escala pelo desenho e cada ícone sai de um tamanho.
    path.write_text(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><rect width="24" height="24" fill="{color}" fill-opacity="0" stroke="{color}" stroke-opacity="0"/>{body}</svg>')

    stroked = 'stroke="currentColor"' in ICONS[name]
    mob = SVGMobject(str(path), stroke_width=2 * size / 24 if stroked else 0)
    mob.scale_to_fit_height(size / 100)
    mob.move_to(P(x + size / 2, y + size / 2))
    return mob


def frame(f, rx):
    mob = RoundedRectangle(corner_radius=rx / 100, width=f['w'] / 100, height=f['h'] / 100)
    mob.set_fill('#ffffff', opacity=0.025).set_stroke(FRAME_STROKE, width=2, opacity=0.45)
    mob.move_to(P(f['x'] + f['w'] / 2, f['y'] + f['h'] / 2))
    return mob


def background():
    image = Image.new('RGB', (W, H), BG)
    dots = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(dots)
    for x in range(2, W, 28):
        for y in range(2, H, 28):
            draw.ellipse([x - 1.2, y - 1.2, x + 1.2, y + 1.2], fill=(255, 255, 255, 15))
    image.paste(dots, (0, 0), dots)

    mob = ImageMobject(np.array(image))
    mob.set_height(H / 100)
    return mob


def exit_t(value):
    if value(1.0) <= 1:
        return 1.0

    lo, hi = 0.0, 1.0
    while hi - lo > 1e-5:
        mid = (lo + hi) / 2
        lo, hi = (lo, mid) if value(mid) > 1 else (mid, hi)
    return lo


def t_at(value, target):
    lo, hi = 0.0, 1.0
    while hi - lo > 1e-5:
        mid = (lo + hi) / 2
        lo, hi = (lo, mid) if value(mid) > target else (mid, hi)
    return lo


def smoothstep(a, b, x):
    k = min(max((x - a) / (b - a), 0), 1)
    return k * k * (3 - 2 * k)


class TestsPace(Scene):
    def construct(self):
        self.add(background())

        title = text(TITLE_LEAD + TITLE_ACCENT, 52, M, 72, weight=800, t2g={TITLE_ACCENT: (VIOLET_LIGHT, VIOLET)})
        handle = VGroup(icon('i-simple-icons-linkedin', 19, M + 4, 87, LINKEDIN), text(HANDLE, 16, M + 34, 102, weight=600, color=MUTED, mono=True))

        logo_path = TMP / 'estud-logo.svg'
        logo_path.write_text(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><rect width="24" height="24" rx="6" fill="#7c3aed"/><path fill="#fff" d="{ESTUD_E}"/></svg>')
        logo = SVGMobject(str(logo_path), stroke_width=0).scale_to_fit_height(0.52).move_to(P(W - M - 26, 54))

        self.play(Write(title, run_time=1.4), FadeIn(handle, shift=UP * 0.15, run_time=0.8), GrowFromCenter(logo, run_time=0.8))

        chart = frame(CHART, 28)
        header = VGroup(
            icon('i-lucide-trending-up', 30, CHART['x'] + 30, CHART['y'] + 20, GREEN),
            text('Ritmo de desenvolvimento', 26, CHART['x'] + 72, CHART['y'] + 45, weight=700),
            text('custo de cada nova entrega', 14, CHART['x'] + CHART['w'] - 30, CHART['y'] + 43, color=MUTED, mono=True, anchor='end'),
        )

        x_axis = Arrow(P(OX, OY), P(AXIS_END, OY), buff=0, stroke_width=3, tip_length=0.16, max_stroke_width_to_length_ratio=100, color=AXIS)
        y_axis = Arrow(P(OX, OY), P(OX, AXIS_TOP), buff=0, stroke_width=3, tip_length=0.16, max_stroke_width_to_length_ratio=100, color=AXIS)
        grid = VGroup(*[
            DashedLine(P(PLOT_X, py(v)), P(AXIS_END, py(v)), dash_length=0.05, dashed_ratio=0.35, stroke_width=1.2).set_stroke('#ffffff', opacity=0.08)
            for v in (0.25, 0.5, 0.75, 1)
        ])
        axis_labels = VGroup(
            text('Horas gastas', 18, OX + 20, AXIS_TOP + 14, weight=700),
            text('Progresso', 18, AXIS_END, OY - 16, weight=700, anchor='end'),
        )

        self.play(Create(chart, run_time=1.0), FadeIn(header, shift=DOWN * 0.1, run_time=0.8))
        self.play(Create(x_axis), Create(y_axis), Create(grid), FadeIn(axis_labels), run_time=1.0)

        T = ValueTracker(0)
        turn_x = px(TURN)

        before_region = Rectangle(width=(turn_x - PLOT_X) / 100, height=(OY - AXIS_TOP) / 100).set_stroke(width=0).set_fill(ROSE, opacity=0.05)
        before_region.move_to(P((PLOT_X + turn_x) / 2, (OY + AXIS_TOP) / 2))
        after_region = Rectangle(width=(AXIS_END - turn_x) / 100, height=(OY - AXIS_TOP) / 100).set_stroke(width=0).set_fill(GREEN, opacity=0.05)
        after_region.move_to(P((turn_x + AXIS_END) / 2, (OY + AXIS_TOP) / 2))
        before_caption = text('parece mais rápido', 14, (PLOT_X + turn_x) / 2, OY + 38, color=ROSE, mono=True, anchor='middle')
        after_caption = text('os testes se pagam', 14, (turn_x + AXIS_END) / 2, OY + 38, color=GREEN, mono=True, anchor='middle')

        self.add(before_region, after_region)
        before_region.set_fill(opacity=0)
        after_region.set_fill(opacity=0)

        sweep = DashedLine(P(PLOT_X, OY), P(PLOT_X, AXIS_TOP), dash_length=0.06, dashed_ratio=0.4, stroke_width=1.5).set_stroke(VIOLET_LIGHT, opacity=0.35)
        sweep.add_updater(lambda m: m.move_to(P(px(T.get_value()), (OY + AXIS_TOP) / 2)))

        def curve_path(c, end):
            upto = min(T.get_value(), end)
            n = max(2, int(300 * upto) + 1)
            ts = np.linspace(0, upto, n)
            return [P(px(t), py(min(c['value'](t), 1))) for t in ts]

        curves, dots, tips = VGroup(), VGroup(), VGroup()
        for c in CURVES:
            end = exit_t(c['value'])

            def draw(c=c, end=end):
                points = curve_path(c, end)
                layers = VGroup()
                for width, opacity in ((c['width'] * 5, 0.07), (c['width'] * 2.6, 0.16), (c['width'], 1)):
                    layers.add(VMobject().set_points_as_corners(points).set_stroke(c['color'], width=width, opacity=opacity))
                return layers

            curves.add(_redraw(draw))

            dot = VGroup(
                Circle(radius=0.17).set_stroke(width=0).set_fill(c['color'], opacity=0.22),
                Circle(radius=0.075).set_fill(c['color'], opacity=1).set_stroke('#ffffff', width=2.5),
            )
            dot.add_updater(lambda m, c=c, end=end: m.move_to(P(px(min(T.get_value(), end)), py(c['value'](min(T.get_value(), end))))))
            dots.add(dot)

            tip = sized_text(c['label'], 20, 700, c['color'])
            is_good = c['final'][0] == 'end'

            def follow(m, c=c, end=end, is_good=is_good):
                t = min(T.get_value(), end)
                v = c['value'](t)
                x, y = px(t), py(v)
                w = m.width * 100

                if is_good:
                    place(m, c['label'], x - w * t, y - 20)
                    opacity = 1.0
                else:
                    place(m, c['label'], x + 18, y + 7)
                    opacity = 1 - smoothstep(0.78, 0.88, v)

                near_turn = smoothstep(0.02, 0.07, abs(T.get_value() - TURN))
                m.set_opacity(opacity * near_turn)

            tip.add_updater(follow)
            tips.add(tip)

        self.add(sweep, curves, dots, tips)
        self.play(FadeIn(sweep), *[GrowFromCenter(d) for d in dots], FadeIn(tips), run_time=0.6)

        self.play(AnimationGroup(
            T.animate(run_time=4.8, rate_func=rate_functions.smooth).set_value(TURN),
            Succession(Wait(1.4), AnimationGroup(before_region.animate.set_fill(opacity=0.05), FadeIn(before_caption, shift=UP * 0.1), run_time=0.9)),
        ))

        cross = P(turn_x, py(TURN_VALUE))
        marker = VGroup(
            Circle(radius=0.2).set_stroke(width=0).set_fill(VIOLET_LIGHT, opacity=0.25),
            Circle(radius=0.08).set_fill(CARD, opacity=1).set_stroke('#ffffff', width=3),
        ).move_to(cross)
        turn_line = DashedLine(P(turn_x, py(TURN_VALUE) + 12), P(turn_x, OY), dash_length=0.06, dashed_ratio=0.35, stroke_width=2).set_stroke(VIOLET_LIGHT, opacity=0.6)

        pill_label = 'ponto de virada'
        pill_w = len(pill_label) * 9.4 + 28
        pill_y = py(0.22)
        pill = VGroup(
            RoundedRectangle(corner_radius=0.15, width=pill_w / 100, height=0.3).set_fill('#1c1830', opacity=1).set_stroke(VIOLET, width=1.2, opacity=0.55).move_to(P(turn_x, pill_y)),
            text(pill_label, 15, turn_x, pill_y + 5, weight=600, color=VIOLET_LIGHT, mono=True, anchor='middle'),
        )

        self.play(Flash(cross, color=VIOLET_LIGHT, line_length=0.22, num_lines=14, flash_radius=0.32, time_width=0.4), FadeIn(marker, scale=0.4), run_time=0.9)
        self.play(Create(turn_line), FadeIn(pill, shift=UP * 0.12), run_time=0.7)
        self.wait(0.8)

        self.play(AnimationGroup(
            T.animate(run_time=5.0, rate_func=rate_functions.smooth).set_value(1),
            AnimationGroup(after_region.animate.set_fill(opacity=0.05), FadeIn(after_caption, shift=UP * 0.1), run_time=1.0),
        ))

        for tip in tips:
            tip.clear_updaters()

        finals = VGroup()
        for c in CURVES[:2]:
            side, at = c['final']
            t = t_at(c['value'], at)
            x, y = px(t), py(c['value'](t))
            finals.add(text(c['label'], 20, x - 18 if side == 'left' else x + 18, y + 7, weight=700, color=c['color'], anchor='end' if side == 'left' else 'start'))

        quote = VGroup(
            frame(QUOTE, 22),
            icon('i-lucide-quote', 34, QUOTE['x'] + 30, QUOTE['y'] + 36, VIOLET_LIGHT),
            text(QUOTE_LEAD, 24, QUOTE['x'] + 84, QUOTE['y'] + 54, weight=700),
            text(QUOTE_SUB, 17, QUOTE['x'] + 84, QUOTE['y'] + 88, color=MUTED),
        )
        source = text(SOURCE, 12, CHART['x'] + CHART['w'] - 30, CHART['y'] + CHART['h'] - 22, color=MUTED, mono=True, anchor='end')

        self.play(FadeOut(sweep), FadeOut(VGroup(*tips[:2])), LaggedStart(*[FadeIn(f, shift=UP * 0.08) for f in finals], lag_ratio=0.3), run_time=0.9)
        self.play(LaggedStart(Create(quote[0]), FadeIn(quote[1:], shift=UP * 0.1), lag_ratio=0.35), FadeIn(source), run_time=1.2)
        self.wait(3.5)


def _redraw(draw):
    mob = draw()
    mob.add_updater(lambda m: m.become(draw()))
    return mob


def to_gif(mp4: Path, gif: Path, width=540, fps=20):
    import av

    frames = []
    with av.open(str(mp4)) as container:
        stream = container.streams.video[0]
        step = max(1, round(float(stream.average_rate) / fps))
        for i, frame_ in enumerate(container.decode(stream)):
            if i % step:
                continue
            image = frame_.to_image()
            frames.append(image.resize((width, round(image.height * width / image.width)), Image.LANCZOS))

    palette = frames[-1].quantize(colors=255, method=Image.Quantize.MEDIANCUT)
    frames = [f.quantize(palette=palette, dither=Image.Dither.NONE) for f in frames]
    frames[0].save(gif, save_all=True, append_images=frames[1:], duration=round(1000 / fps), loop=0, optimize=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--fps', type=int, default=60)
    parser.add_argument('--gif', action='store_true')
    args = parser.parse_args()

    config.pixel_width = W
    config.pixel_height = H
    config.frame_width = W / 100
    config.frame_height = H / 100
    config.frame_rate = args.fps
    config.background_color = BG
    config.media_dir = str(TMP / 'media')
    config.verbosity = 'WARNING'
    config.progress_bar = 'display'

    ensure_fonts()

    scene = TestsPace()
    scene.render()

    OUT.mkdir(parents=True, exist_ok=True)
    mp4 = OUT / 'tests-pace.mp4'
    shutil.copy(scene.renderer.file_writer.movie_file_path, mp4)
    print(f'MP4 → {mp4}')

    if args.gif:
        gif = OUT / 'tests-pace.gif'
        to_gif(mp4, gif)
        print(f'GIF → {gif}')

    shutil.rmtree(TMP, ignore_errors=True)


if __name__ == '__main__':
    main()
