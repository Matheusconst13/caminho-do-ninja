"""Gera todos os sprites (pixel art) e sons do jogo "Caminho do Ninja".

Tudo é desenhado/sintetizado por código, sem assets de terceiros.
Uso: python Tools/gerar_assets.py   (precisa de Pillow e numpy)
"""
import math
import os
import wave

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

RAIZ = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
SPRITES = os.path.join(RAIZ, "Assets", "Sprites", "Ninja")
AUDIO = os.path.join(RAIZ, "Assets", "Audio")
DOCS = os.path.join(RAIZ, "Docs")
FONTE_JP = "/usr/share/fonts/opentype/noto/NotoSerifCJK-Bold.ttc"
FONTE = "/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf"

# ---------------------------------------------------------------- sprites

PALETA = {
    ".": None,
    "K": (22, 20, 32),     # contorno
    "N": (43, 45, 66),     # roupa ninja
    "n": (72, 76, 108),    # brilho da roupa
    "R": (214, 40, 40),    # vermelho
    "r": (140, 24, 30),    # vermelho escuro
    "S": (242, 199, 155),  # pele
    "W": (255, 255, 255),
    "G": (141, 153, 174),  # metal
    "g": (210, 218, 230),  # metal claro
    "M": (58, 58, 74),     # elmo
    "Y": (244, 201, 93),   # dourado / areia
    "B": (140, 90, 43),    # madeira
    "C": (191, 233, 255),  # vidro
    "D": (70, 70, 84),     # coração vazio
    "E": (255, 59, 59),    # olhos do ninja sombra
}

NINJA_A = [
    "................",
    "....KKKKKKKK....",
    "...KNNNNNNNNK...",
    "..KNNNNNNNNNNK..",
    "RRKRRRRRRRRRRK..",
    ".RKNSSSSSSSSNK..",
    "..KNSKSSSSKSNK..",
    "..KNNNNNNNNNNK..",
    "...KNNNNNNNNK...",
    "..KNKnNNNNnKNK..",
    ".KSKNnNNNNnNKSK.",
    ".KKKNNGGGGNNKKK.",
    "...KNNNNNNNNK...",
    "...KNNNKKNNNK...",
    "...KNNK..KNNK...",
    "...KKKK..KKKK...",
]
NINJA_B = NINJA_A[:13] + [
    "...KNNNKKNNNK...",
    "..KNNK....KNNK..",
    "..KKKK....KKKK..",
]

SAMURAI_A = [
    "...Y........Y...",
    "...YY.KKKK.YY...",
    "....YKMMMMKY....",
    "...KMMMMMMMMK...",
    "..KMMMMMMMMMMK..",
    "..KKSSSSSSSSKK..",
    "...KSKSSSSKSK...",
    "...KSSSKKSSSK...",
    "..KKRRRRRRRRKK..",
    ".KRRrRRRRRRrRRK.",
    ".KRKrRRYYRRrKRK.",
    ".KSKRRRRRRRRKSK.",
    "..KKrrrrrrrrKK..",
    "...KMMMKKMMMK...",
    "...KMMK..KMMK...",
    "...KKKK..KKKK...",
]
SAMURAI_B = SAMURAI_A[:13] + [
    "...KMMMKKMMMK...",
    "..KMMK....KMMK..",
    "..KKKK....KKKK..",
]

AMPULHETA = [
    "..KKKKKKKKKKKK..",
    "..KBBBBBBBBBBK..",
    "...KCCCCCCCCK...",
    "...KCYYYYYYCK...",
    "....KCYYYYCK....",
    ".....KCYYCK.....",
    "......KCCK......",
    "......KYCK......",
    ".....KCYCCK.....",
    "....KCCYCCCK....",
    "...KCCCYYCCCK...",
    "...KCYYYYYYCK...",
    "...KYYYYYYYYK...",
    "..KBBBBBBBBBBK..",
    "..KKKKKKKKKKKK..",
    "................",
]

ESTREPES = [
    "................",
    "...K.......K....",
    "..KGK.....KgK...",
    ".KGgGK...KGgGK..",
    "..KGK.....KGK...",
    "...K.......K....",
    "................",
    "......K.........",
    ".....KgK....K...",
    "....KGgGK..KgK..",
    ".....KGK..KGgGK.",
    "......K....KGK..",
    "............K...",
    "..K.............",
    ".KgK............",
    "..K.............",
]

CORACAO = [
    "................",
    "...KKK....KKK...",
    "..KRRRK..KRRRK..",
    ".KRWWRRKKRRRRRK.",
    ".KRWRRRRRRRRRRK.",
    ".KRRRRRRRRRRRRK.",
    ".KRRRRRRRRRRRRK.",
    "..KRRRRRRRRRRK..",
    "...KRRRRRRRRK...",
    "....KRRRRRRK....",
    ".....KRRRRK.....",
    "......KRRK......",
    ".......KK.......",
    "................",
    "................",
    "................",
]


def de_ascii(linhas, troca=None):
    paleta = dict(PALETA)
    if troca:
        paleta.update(troca)
    assert all(len(l) == 16 for l in linhas), [l for l in linhas if len(l) != 16]
    img = Image.new("RGBA", (16, len(linhas)), (0, 0, 0, 0))
    for y, linha in enumerate(linhas):
        for x, c in enumerate(linha):
            cor = paleta[c]
            if cor:
                img.putpixel((x, y), cor + (255,))
    return img


def shuriken():
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    c = 7.5
    pontos = []
    for i in range(8):
        ang = math.radians(i * 45 + 22.5)
        r = 7.4 if i % 2 == 0 else 2.6
        pontos.append((c + r * math.cos(ang), c + r * math.sin(ang)))
    d.polygon(pontos, fill=PALETA["g"] + (255,), outline=PALETA["K"] + (255,))
    d.ellipse((6, 6, 9, 9), fill=PALETA["K"] + (255,))
    return img


def tatame():
    rng = np.random.default_rng(7)
    px = np.zeros((32, 32, 3), np.float32)
    base = np.array([201, 183, 122], np.float32)
    for y in range(32):
        listra = 1.0 + (0.06 if y % 2 == 0 else -0.04)
        for x in range(32):
            px[y, x] = base * (listra + rng.uniform(-0.03, 0.03))
    px[0:2, :] = (63, 91, 58)   # borda (heri) verde
    px[:, 0:2] = (63, 91, 58)
    px[2, 2:] *= 0.85
    return Image.fromarray(np.clip(px, 0, 255).astype(np.uint8), "RGB").convert("RGBA")


def madeira(caixa=False):
    rng = np.random.default_rng(3 if caixa else 5)
    px = np.zeros((16, 16, 3), np.float32)
    for x in range(16):
        tabua = x // 4
        tom = np.array([120, 74, 38], np.float32) * (1.0 + 0.08 * ((tabua % 2) - 0.5))
        for y in range(16):
            px[y, x] = tom * (1 + rng.uniform(-0.05, 0.05))
        if x % 4 == 0:
            px[:, x] = (70, 40, 20)
    if caixa:
        px[0, :] = px[15, :] = px[:, 0] = px[:, 15] = (58, 32, 16)
        for i in range(16):
            px[i, i] = px[i, 15 - i] = (84, 50, 24)
    else:
        px[3, 1:16:4] = px[12, 1:16:4] = (190, 160, 110)  # pregos
    return Image.fromarray(np.clip(px, 0, 255).astype(np.uint8), "RGB").convert("RGBA")


def fundo_menu(w=480, h=300):
    img = Image.new("RGBA", (w, h))
    d = ImageDraw.Draw(img)
    topo, base = np.array([16, 14, 40]), np.array([92, 30, 52])
    for y in range(h):
        t = (y / h) ** 1.4
        d.line([(0, y), (w, y)], fill=tuple(int(v) for v in topo * (1 - t) + base * t))
    d.ellipse((w * 0.58, h * 0.12, w * 0.58 + 120, h * 0.12 + 120), fill=(232, 93, 93))
    rng = np.random.default_rng(1)
    for cor, alt, rug in (((44, 22, 48), 0.62, 30), ((28, 14, 34), 0.74, 22), ((14, 8, 20), 0.86, 14)):
        pts = [(0, h)]
        for x in range(0, w + 8, 8):
            pts.append((x, h * alt + rug * math.sin(x / 37 + alt * 9) + rng.uniform(-4, 4)))
        pts.append((w, h))
        d.polygon(pts, fill=cor)
    for x0 in (10, 34, 52, w - 60, w - 38, w - 16):  # bambus
        for y in range(0, h, 2):
            d.line([(x0, y), (x0 + 5, y)], fill=(10, 24, 16) if y % 34 else (30, 60, 36))
    for _ in range(40):  # pétalas de sakura
        x, y = rng.uniform(70, w - 70), rng.uniform(0, h * 0.8)
        d.rectangle((x, y, x + 1, y + 1), fill=(255, 183, 197))
    return img


def capa(icones):
    w, h = 630, 500
    img = fundo_menu(315, 250).resize((w, h), Image.NEAREST)
    d = ImageDraw.Draw(img)
    try:
        jp = ImageFont.truetype(FONTE_JP, 210, index=0)
        d.text((w * 0.72, h * 0.36), "忍", font=jp, fill=(20, 10, 24, 200), anchor="mm")
    except OSError:
        pass
    ninja = icones["ninja"].resize((192, 192), Image.NEAREST)
    img.alpha_composite(ninja, (70, 220))
    for i, (x, y) in enumerate(((300, 330), (380, 280), (460, 340))):
        s = icones["shuriken"].resize((48, 48), Image.NEAREST).rotate(i * 25)
        img.alpha_composite(s, (x, y))
    titulo = ImageFont.truetype(FONTE, 58)
    for dx, dy in ((3, 3), (0, 0)):
        d.text((w / 2 + dx, 70 + dy), "CAMINHO DO", font=titulo, anchor="mm",
               fill=(0, 0, 0) if dx else (255, 240, 230))
        d.text((w / 2 + dx, 135 + dy), "NINJA", font=ImageFont.truetype(FONTE, 84), anchor="mm",
               fill=(0, 0, 0) if dx else (214, 40, 40))
    return img


def salvar(img, nome, pasta=SPRITES):
    os.makedirs(pasta, exist_ok=True)
    img.save(os.path.join(pasta, nome))


# ---------------------------------------------------------------- áudio

SR = 44100


def nota(n):
    """Frequência da nota MIDI n."""
    return 440.0 * 2 ** ((n - 69) / 12)


def env(n, ataque=0.005, decai=8.0):
    t = np.arange(n) / SR
    e = np.exp(-t * decai)
    a = int(ataque * SR)
    if a:
        e[:a] *= np.linspace(0, 1, a)
    return e


def quadrada(f, n, duty=0.25):
    t = np.arange(n) / SR
    return np.where((t * f) % 1 < duty, 1.0, -1.0)


def triangular(f, n):
    t = np.arange(n) / SR
    return 2 * np.abs(2 * ((t * f) % 1) - 1) - 1


def ruido(n, seed=0):
    return np.random.default_rng(seed).uniform(-1, 1, n)


def taiko(n):
    t = np.arange(n) / SR
    f = 55 + 60 * np.exp(-t * 30)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * env(n, 0.002, 9) + 0.25 * ruido(n, 4) * env(n, 0.001, 40)


def koto(f, n, seed=0):
    """Corda dedilhada (Karplus-Strong)."""
    p = max(2, int(SR / f))
    buf = np.random.default_rng(seed).uniform(-1, 1, p)
    out = np.zeros(n)
    for i in range(n):
        out[i] = buf[i % p]
        buf[i % p] = 0.5 * (buf[i % p] + buf[(i + 1) % p]) * 0.996
    return out


def mix_em(dst, sinal, inicio):
    fim = min(len(dst), inicio + len(sinal))
    dst[inicio:fim] += sinal[: fim - inicio]


def gravar(nome, sinal, vol=0.9):
    sinal = sinal / (np.max(np.abs(sinal)) + 1e-9) * vol
    os.makedirs(AUDIO, exist_ok=True)
    with wave.open(os.path.join(AUDIO, nome), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((sinal * 32767).astype("<i2").tobytes())


# Escala "In" (miyako-bushi) a partir de Ré: D Eb G A Bb
ESCALA = [62, 63, 67, 69, 70, 74, 75, 79, 81, 82]


def musica_jogo():
    bpm, compassos = 132, 8
    sem = 60 / bpm / 4                       # semicolcheia
    n = int(round(sem * 16 * compassos * SR))
    out = np.zeros(n)
    melodia = [5, -1, 4, 3, 2, -1, 3, -1, 0, -1, 2, 3, 4, -1, -1, -1,
               5, -1, 7, 6, 5, -1, 4, -1, 3, 4, 3, 2, 0, -1, -1, -1]
    graves = [50, 50, 55, 57, 50, 50, 58, 57]
    for c in range(compassos):
        base = c * 16
        # bumbo taiko nos tempos 1 e 3 (+ um extra a cada 2 compassos)
        for passo in (0, 8) + ((14,) if c % 2 else ()):
            mix_em(out, 0.9 * taiko(int(0.5 * SR)), int((base + passo) * sem * SR))
        for passo in range(0, 16, 2):        # chocalho
            mix_em(out, 0.06 * ruido(int(0.04 * SR), passo) * env(int(0.04 * SR), 0.001, 90),
                   int((base + passo + 1) * sem * SR))
        g = graves[c]
        for passo in (0, 6, 8, 12):
            k = int(sem * 3 * SR)
            mix_em(out, 0.35 * triangular(nota(g), k) * env(k, 0.005, 5), int((base + passo) * sem * SR))
        for passo in range(16):
            idx = melodia[(c % 2) * 16 + passo]
            if idx < 0 or (c in (6, 7) and passo % 4 == 1):
                continue
            k = int(sem * 1.8 * SR)
            f = nota(ESCALA[idx] + (12 if c >= 4 and c < 6 else 0))
            mix_em(out, 0.18 * quadrada(f, k, 0.25) * env(k, 0.003, 7), int((base + passo) * sem * SR))
    # o que passou do fim volta para o início: loop sem emenda
    return out


def musica_menu():
    bpm, compassos = 90, 6
    tempo = 60 / bpm
    n = int(round(tempo * 4 * compassos * SR))
    out = np.zeros(n)
    acordes = [[62, 69, 74, 75], [60, 67, 72, 74], [58, 65, 70, 74], [57, 63, 69, 70],
               [62, 69, 74, 79], [63, 70, 75, 74]]
    for c, ac in enumerate(acordes):
        for i in range(8):
            k = int(1.6 * SR)
            f = nota(ac[i % 4] + (12 if i in (5, 6) else 0))
            mix_em(out, 0.3 * koto(f, k, c * 10 + i) * env(k, 0.001, 2.5),
                   int((c * 4 + i * 0.5) * tempo * SR))
        k = int(tempo * 4 * SR)
        mix_em(out, 0.12 * np.sin(2 * np.pi * nota(ac[0] - 12) * np.arange(k) / SR) * env(k, 0.3, 0.6),
               int(c * 4 * tempo * SR))
    eco = np.zeros(n)
    for atraso, ganho in ((0.33, 0.35), (0.66, 0.18)):
        eco += np.roll(out, int(atraso * SR)) * ganho   # eco circular = loop perfeito
    return out + eco


def sfx():
    def k(seg):
        return int(seg * SR)
    t = lambda n: np.arange(n) / SR
    n = k(0.4)
    gravar("ShurikenColetar.wav", sum(np.sin(2 * np.pi * f * t(n)) * a for f, a in
                                     ((2300, 1), (3450, .6), (4890, .35))) * env(n, 0.002, 11)
           + 0.15 * ruido(n, 1) * env(n, 0.001, 60), 0.6)
    n = k(0.6)
    s = np.zeros(n)
    for i, m in enumerate((74, 79, 86)):
        kk = k(0.4)
        mix_em(s, np.sin(2 * np.pi * nota(m) * t(kk)) * env(kk, 0.002, 6), k(0.08 * i))
    gravar("Ampulheta.wav", s, 0.6)
    n = k(0.35)
    f = 420 * np.exp(-t(n) * 7) + 90
    gravar("Dano.wav", np.sign(np.sin(2 * np.pi * np.cumsum(f) / SR)) * env(n, 0.002, 9) * 0.5
           + taiko(n) * 0.8 + 0.3 * ruido(n, 2) * env(n, 0.001, 25), 0.8)
    n = k(0.28)
    r = ruido(n, 3)
    janela = np.convolve(r, np.ones(12) / 12, mode="same")
    gravar("Dash.wav", (janela * np.sin(np.pi * t(n) / t(n)[-1]) ** 2), 0.6)
    n = k(1.3)
    s = np.zeros(n)
    for i, m in enumerate((62, 67, 69, 74, 79)):
        kk = k(0.5 if i < 4 else 0.8)
        mix_em(s, (0.6 * quadrada(nota(m), kk, 0.5) + triangular(nota(m - 12), kk)) * env(kk, 0.003, 4), k(0.12 * i))
    gravar("Vitoria.wav", s, 0.7)
    n = k(1.4)
    s = np.zeros(n)
    for i, m in enumerate((70, 69, 67, 63, 62)):
        kk = k(0.45 if i < 4 else 0.9)
        mix_em(s, (0.6 * quadrada(nota(m), kk, 0.5) + triangular(nota(m - 12), kk)) * env(kk, 0.003, 3), k(0.2 * i))
    gravar("Derrota.wav", s, 0.7)
    n = k(0.06)
    gravar("Clique.wav", quadrada(1200, n, 0.5) * env(n, 0.001, 50), 0.4)
    n = k(0.05)
    gravar("Tique.wav", np.sin(2 * np.pi * 1800 * t(n)) * env(n, 0.001, 80), 0.35)
    n = k(0.25)
    s = np.zeros(n)
    for i, m in enumerate((84, 91)):
        kk = k(0.12)
        mix_em(s, quadrada(nota(m), kk, 0.5) * env(kk, 0.001, 15), k(0.07 * i))
    gravar("Alerta.wav", s, 0.45)


if __name__ == "__main__":
    icones = {
        "ninja": de_ascii(NINJA_A),
        "shuriken": shuriken(),
    }
    salvar(de_ascii(NINJA_A), "Ninja_0.png")
    salvar(de_ascii(NINJA_B), "Ninja_1.png")
    sombra = {"N": (46, 22, 74), "n": (94, 58, 138), "R": (94, 58, 138), "S": (30, 14, 48), "K": (12, 6, 20),
              "G": (94, 58, 138)}
    sombra["E"] = (255, 59, 59)  # olhos vermelhos brilhando
    olhos = lambda linhas: [l.replace("NSKSSSSKSN", "NSESSSSESN") for l in linhas]
    salvar(de_ascii(olhos(NINJA_A), sombra), "Sombra_0.png")
    salvar(de_ascii(olhos(NINJA_B), sombra), "Sombra_1.png")
    salvar(de_ascii(SAMURAI_A), "Samurai_0.png")
    salvar(de_ascii(SAMURAI_B), "Samurai_1.png")
    salvar(icones["shuriken"], "Shuriken.png")
    salvar(de_ascii(AMPULHETA), "Ampulheta.png")
    salvar(de_ascii(ESTREPES), "Estrepes.png")
    salvar(de_ascii(CORACAO), "Coracao.png")
    salvar(de_ascii(CORACAO, {"R": PALETA["D"], "W": (110, 110, 124)}), "CoracaoVazio.png")
    salvar(tatame(), "Tatame.png")
    salvar(madeira(), "Madeira.png")
    salvar(madeira(caixa=True), "Caixa.png")
    salvar(fundo_menu().resize((1920, 1200), Image.NEAREST), "FundoMenu.png")
    os.makedirs(DOCS, exist_ok=True)
    capa(icones).convert("RGB").save(os.path.join(DOCS, "capa_itch.png"))

    gravar("MusicaJogo.wav", musica_jogo(), 0.7)
    gravar("MusicaMenu.wav", musica_menu(), 0.7)
    sfx()
    print("assets gerados")
