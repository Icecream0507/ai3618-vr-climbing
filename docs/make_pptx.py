#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""Generate Summit VR pre-presentation as a .pptx (SJTU red/white theme).
Run: python docs/make_pptx.py  ->  docs/SummitVR_Pre.pptx
"""
import os
from pptx import Presentation
from pptx.util import Inches, Pt, Emu
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from pptx.enum.shapes import MSO_SHAPE
from pptx.oxml.ns import qn

HERE = os.path.dirname(os.path.abspath(__file__))
DEMO = os.path.join(HERE, "..", "Demo")
TH = os.path.join(HERE, "_thumbs")

# SJTU palette
RED   = RGBColor(0xC8, 0x10, 0x2E)
REDD  = RGBColor(0x9E, 0x0B, 0x22)
REDL  = RGBColor(0xE8, 0x34, 0x4C)
INK   = RGBColor(0x1C, 0x1F, 0x26)
MUT   = RGBColor(0x6B, 0x72, 0x80)
PAPER2= RGBColor(0xFB, 0xF7, 0xF8)
LINE  = RGBColor(0xE7, 0xD6, 0xD9)
WHITE = RGBColor(0xFF, 0xFF, 0xFF)
REDWASH = RGBColor(0xFB, 0xE9, 0xEC)

prs = Presentation()
prs.slide_width  = Inches(13.333)
prs.slide_height = Inches(7.5)
SW, SH = prs.slide_width, prs.slide_height
BLANK = prs.slide_layouts[6]

def slide():
    return prs.slides.add_slide(BLANK)

def fill(sp, color):
    sp.fill.solid(); sp.fill.fore_color.rgb = color; sp.line.fill.background()

def rect(s, x, y, w, h, color, shape=MSO_SHAPE.RECTANGLE):
    sp = s.shapes.add_shape(shape, x, y, w, h); fill(sp, color); return sp

def grad_band(s, x, y, w, h):
    sp = s.shapes.add_shape(MSO_SHAPE.RECTANGLE, x, y, w, h)
    sp.line.fill.background()
    f = sp.fill; f.gradient()
    f.gradient_stops[0].color.rgb = RED
    f.gradient_stops[1].color.rgb = REDL
    try: f.gradient_angle = 0.0
    except Exception: pass
    return sp

def txt(s, x, y, w, h, runs, align=PP_ALIGN.LEFT, anchor=MSO_ANCHOR.TOP,
        space_after=6, line=1.12):
    tb = s.shapes.add_textbox(x, y, w, h); tf = tb.text_frame
    tf.word_wrap = True; tf.vertical_anchor = anchor
    tf.margin_left = tf.margin_right = Pt(2)
    tf.margin_top = tf.margin_bottom = Pt(2)
    if isinstance(runs[0], tuple): runs = [runs]
    for i, para in enumerate(runs):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.alignment = align; p.space_after = Pt(space_after); p.line_spacing = line
        for (t, sz, col, b) in para:
            r = p.add_run(); r.text = t
            r.font.size = Pt(sz); r.font.color.rgb = col; r.font.bold = b
            r.font.name = "Microsoft YaHei"
    return tb

def kicker(s, t, y=Inches(0.42)):
    rect(s, Inches(0.55), y+Inches(0.02), Inches(0.06), Inches(0.30), RED)
    txt(s, Inches(0.72), y, Inches(8), Inches(0.4),
        [[(t, 14, RED, True)]])

def header_band(s):
    grad_band(s, 0, 0, SW, Inches(0.16))

def footer(s, left, right="SJTU · AI3618"):
    rect(s, 0, SH-Inches(0.42), SW, Inches(0.42), RED)
    txt(s, Inches(0.4), SH-Inches(0.40), Inches(7), Inches(0.36),
        [[(left, 11, WHITE, True)]], anchor=MSO_ANCHOR.MIDDLE)
    txt(s, SW-Inches(5.4), SH-Inches(0.40), Inches(5), Inches(0.36),
        [[(right, 11, WHITE, False)]], align=PP_ALIGN.RIGHT, anchor=MSO_ANCHOR.MIDDLE)

def card(s, x, y, w, h, red_bg=False):
    sp = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, x, y, w, h)
    if red_bg:
        f = sp.fill; f.gradient()
        f.gradient_stops[0].color.rgb = RED
        f.gradient_stops[1].color.rgb = REDD
        sp.line.fill.background()
    else:
        fill(sp, PAPER2); sp.line.color.rgb = LINE; sp.line.width = Pt(1)
    sp.shadow.inherit = False
    return sp

def speak(s, t):
    y = SH - Inches(1.30)
    rect(s, Inches(0.55), y, Inches(0.06), Inches(0.72), RED)
    bx = s.shapes.add_textbox(Inches(0.72), y, SW-Inches(1.5), Inches(0.72))
    tfx = bx.text_frame; tfx.word_wrap = True
    p = tfx.paragraphs[0]; p.line_spacing = 1.15
    r = p.add_run(); r.text = "讲："; r.font.bold = True; r.font.color.rgb = RED
    r.font.size = Pt(11); r.font.name = "Microsoft YaHei"
    r2 = p.add_run(); r2.text = t; r2.font.size = Pt(11)
    r2.font.color.rgb = RGBColor(0x5A,0x3A,0x3E); r2.font.name = "Microsoft YaHei"

def bullets(s, x, y, w, h, items, size=15, gap=7):
    tb = s.shapes.add_textbox(x, y, w, h); tf = tb.text_frame
    tf.word_wrap = True
    for i, it in enumerate(items):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.space_after = Pt(gap); p.line_spacing = 1.25
        for (t, b, c) in it:
            r = p.add_run(); r.text = t; r.font.size = Pt(size)
            r.font.bold = b; r.font.color.rgb = c; r.font.name = "Microsoft YaHei"
    return tb

def add_video(s, name, x, y, w, h):
    """Embed mp4 with thumbnail poster; falls back to image if embed fails."""
    mp4 = os.path.join(DEMO, "SummitVR_%s.mp4" % name)
    thumb = os.path.join(TH, "%s.png" % name)
    poster = thumb if os.path.exists(thumb) else None
    try:
        mov = s.shapes.add_movie(mp4, x, y, w, h, poster_frame_image=poster,
                                 mime_type="video/mp4")
        return mov
    except Exception as e:
        print("  movie embed failed (%s): %s -> image" % (name, e))
        if poster:
            return s.shapes.add_picture(poster, x, y, w, h)
        return None

# ============== SLIDE 0 — COVER ==============
s = slide()
# full red right band, white left
rect(s, 0, 0, SW, SH, WHITE)
rect(s, Inches(8.4), 0, SW-Inches(8.4), SH, RED)
grad_band(s, 0, 0, Inches(8.4), Inches(0.18))
# decorative diagonal accent on red panel
tri = s.shapes.add_shape(MSO_SHAPE.RIGHT_TRIANGLE, Inches(8.4), Inches(5.4), Inches(2.2), Inches(2.1))
fill(tri, REDD); tri.shadow.inherit = False; tri.rotation = 180
txt(s, Inches(0.8), Inches(1.05), Inches(7.4), Inches(0.5),
    [[("🦅 上海交通大学 · SHANGHAI JIAO TONG UNIVERSITY", 14, MUT, True)]])
txt(s, Inches(0.75), Inches(2.0), Inches(7.6), Inches(1.6),
    [[("Summit ", 72, INK, True), ("VR", 72, RED, True)]])
rect(s, Inches(0.85), Inches(3.5), Inches(2.2), Pt(4), RED)
txt(s, Inches(0.8), Inches(3.75), Inches(7.4), Inches(1.2),
    [[("带「平衡 + 脚法」的纯手柄 VR 抱石", 24, INK, True)],
     [("A Balance-and-Footwork Layer for Controller-Only VR Bouldering", 14, MUT, False)]],
    space_after=8, line=1.3)
txt(s, Inches(0.8), Inches(5.3), Inches(7.4), Inches(0.5),
    [[("课程 AI3618 · 项目汇报 (Pre)", 16, REDD, True)]])
txt(s, Inches(0.8), Inches(6.5), Inches(7.4), Inches(0.5),
    [[("Group N · 薛俊智 · 吴一轩 · 邹沛霖 · 陶锐 · 孙艺豪", 13, MUT, False)]])
# right panel text
txt(s, Inches(8.8), Inches(2.5), Inches(4.2), Inches(3.0),
    [[("把真实抱石", 20, WHITE, True)],
     [("最核心的两件事", 20, WHITE, True)],
     [("", 8, WHITE, False)],
     [("身体平衡", 26, RGBColor(0xFF,0xD9,0xDF), True)],
     [("脚法落点", 26, RGBColor(0xFF,0xD9,0xDF), True)],
     [("", 8, WHITE, False)],
     [("带回 VR 攀岩", 20, WHITE, True)]],
    space_after=2, line=1.2)
print("slide 0 (cover) done")

# ============== SLIDE 1 — TITLE ==============
s = slide()
header_band(s)
txt(s, Inches(0.6), Inches(0.5), Inches(11), Inches(0.4),
    [[("🦅 上海交通大学  SHANGHAI JIAO TONG UNIVERSITY   |   课程 AI3618", 13, MUT, True)]])
txt(s, Inches(0.55), Inches(1.0), Inches(11), Inches(1.2),
    [[("Summit ", 54, INK, True), ("VR", 54, RED, True)]])
txt(s, Inches(0.6), Inches(2.15), Inches(11.8), Inches(1.0),
    [[("在只有头 + 两手柄的普通 VR 上，把真实抱石的两件核心——", 18, MUT, False),
      ("身体平衡", 18, RED, True), (" 与 ", 18, MUT, False),
      ("脚法落点", 18, RED, True), ("——做成可玩机制。", 18, MUT, False)]], line=1.3)
pills = [("stock Quest · 3-point", True), ("引擎端到端 10/10 PASS", False),
         ("数学单测 9/9 PASS", False), ("5 条线路 + 演示视频", False)]
px = Inches(0.6)
for label, filled in pills:
    w = Inches(0.135*len(label)+0.55)
    sp = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, px, Inches(3.05), w, Inches(0.42))
    sp.adjustments[0] = 0.5
    if filled: fill(sp, RED)
    else:
        fill(sp, WHITE); sp.line.color.rgb = RED; sp.line.width = Pt(1.5)
    sp.shadow.inherit = False
    tf = sp.text_frame
    p = tf.paragraphs[0]; p.alignment = PP_ALIGN.CENTER
    r = p.add_run(); r.text = label; r.font.size = Pt(11); r.font.bold = True
    r.font.color.rgb = WHITE if filled else RED; r.font.name = "Microsoft YaHei"
    px = px + w + Inches(0.18)
team = [("P1","薛俊智","攀爬系统","反向位移·平衡·脚法"),
        ("P2","吴一轩","玩法规则","状态机·体力·线路"),
        ("P3","邹沛霖","场景美术","墙·握点·灯光·化身"),
        ("P4","陶锐","XR集成构建","rig·输入·仿真验证"),
        ("P5","孙艺豪","UX音频报告视频","HUD·音效·主讲")]
tw = Inches(2.36); gap = Inches(0.13); tx = Inches(0.6); ty = Inches(3.85)
for code, nm, role, tk in team:
    card(s, tx, ty, tw, Inches(2.25))
    rect(s, tx, ty, tw, Inches(0.10), RED)
    circ = s.shapes.add_shape(MSO_SHAPE.OVAL, tx+tw/2-Inches(0.42), ty+Inches(0.32),
                              Inches(0.84), Inches(0.84))
    fill(circ, RED); circ.shadow.inherit = False
    cp = circ.text_frame.paragraphs[0]; cp.alignment = PP_ALIGN.CENTER
    cr = cp.add_run(); cr.text = code; cr.font.size = Pt(20); cr.font.bold = True
    cr.font.color.rgb = WHITE; cr.font.name = "Microsoft YaHei"
    txt(s, tx, ty+Inches(1.30), tw, Inches(0.85),
        [[(nm, 18, INK, True)], [(role, 11, RED, True)], [(tk, 9.5, MUT, False)]],
        align=PP_ALIGN.CENTER, space_after=2, line=1.05)
    tx = tx + tw + gap
footer(s, "Summit VR", "github.com/Icecream0507/ai3618-vr-climbing")
print("slide 1 done")

# ============== AGENDA / MENU ==============
s = slide(); header_band(s)
kicker(s, "目录 · AGENDA")
txt(s, Inches(0.55), Inches(0.85), Inches(12), Inches(0.7),
    [[("本次汇报", 30, INK, True), ("六个部分", 30, RED, True)]])
agenda = [
  ("01","背景与问题","VR 攀岩为何「纯手」、我们补什么缺口"),
  ("02","我们的点子 + 调研","头当重心 · 脚做抽象 · 文献支撑"),
  ("03","系统架构与核心机制","加法式设计 · 一维支撑区间平衡模型"),
  ("04","脚法 · 常量 · Demo","脚法抽象 · 关键参数 · 演示视频"),
  ("05","线路库与整体思路","5 条线路 · 可验证驱动的开发方法论"),
  ("06","评测体系与项目总结","已就绪的评测协议 · 已交付清单"),
]
# two columns of three
colx = [Inches(0.6), Inches(6.7)]
for i, (n, h, body) in enumerate(agenda):
    cx = colx[i // 3]; cy = Inches(2.0) + Inches(1.55) * (i % 3)
    card(s, cx, cy, Inches(5.9), Inches(1.35))
    # number badge
    badge = s.shapes.add_shape(MSO_SHAPE.OVAL, cx+Inches(0.25), cy+Inches(0.32), Inches(0.7), Inches(0.7))
    fill(badge, RED); badge.shadow.inherit=False
    bp = badge.text_frame.paragraphs[0]; bp.alignment = PP_ALIGN.CENTER
    br = bp.add_run(); br.text = n; br.font.size = Pt(20); br.font.bold = True
    br.font.color.rgb = WHITE; br.font.name = "Microsoft YaHei"
    txt(s, cx+Inches(1.2), cy+Inches(0.22), Inches(4.5), Inches(0.45), [[(h, 17, INK, True)]])
    txt(s, cx+Inches(1.2), cy+Inches(0.72), Inches(4.6), Inches(0.5), [[(body, 12, MUT, False)]], line=1.2)
speak(s, "整个汇报六块：先讲为什么做、怎么想，再讲系统怎么搭、机制怎么算，然后看 demo 和线路，最后是评测体系和项目总结。")
footer(s, "目录 · Agenda")
print("agenda done")

# ============== SLIDE 2 — THE GAP ==============
s = slide(); header_band(s)
kicker(s, "背景与问题")
txt(s, Inches(0.55), Inches(0.85), Inches(12), Inches(0.7),
    [[("VR 攀岩很火，但", 30, INK, True), ("几乎全是「纯手」", 30, RED, True)]])
bullets(s, Inches(0.6), Inches(1.95), Inches(6.6), Inches(3.5), [
    [("现状：", True, REDD), ("The Climb · Gorilla Tag——舒适、不晕、商业成功。但掉下来只有两种原因：", False, INK),
     ("松手", True, RED), (" 或 ", False, INK), ("够不到", True, RED), ("。", False, INK)],
    [("真实抱石恰恰相反：", True, REDD), ("难点常常不在手有多大力，而在", False, INK),
     ("重心怎么落在脚上", True, RED), ("、要不要换脚、会不会「开门」(barn-door) 被甩出去。", False, INK)],
    [("2026 新出的平面游戏 New Heights 主打的正是平衡 / 脚法 → 玩家确实想要这一层。", False, MUT)],
], size=16, gap=12)
c = card(s, Inches(7.5), Inches(1.95), Inches(5.2), Inches(3.0), red_bg=True)
txt(s, Inches(7.8), Inches(2.2), Inches(4.6), Inches(2.5),
    [[("我们要补的缺口", 18, WHITE, True)],
     [("市面 VR 攀岩把真实攀岩最核心的脚法和身体平衡全砍了。", 15, WHITE, False)],
     [("Summit VR 把这一层补回来——而且只用消费级硬件。", 15, WHITE, True)]],
    space_after=12, line=1.3)
speak(s, "真实抱石的功夫在脚上，不在手上。市面 VR 攀岩把这块全砍了——这就是我们的切入点。")
footer(s, "背景与问题")
print("slide 2 done")

# ============== SLIDE 3 — OUR IDEA ==============
s = slide(); header_band(s)
kicker(s, "我们的点子")
txt(s, Inches(0.55), Inches(0.85), Inches(12.4), Inches(0.7),
    [[("测不到的就", 30, INK, True), ("抽象", 30, RED, True),
      ("，只模拟", 30, INK, True), ("影响玩法", 30, RED, True), ("的部分", 30, INK, True)]])
cards3 = [("01","头 = 重心代理","只用 3 个追踪点（头+两手柄，无脚部追踪）。看头有没有歪出支撑范围——这就是平衡判定。"),
          ("02","脚 = 抽象状态","不是 IK 腿。虚拟脚自动吸附到身体下方的脚点，撑宽支撑面，不去渲染会穿模的假腿。"),
          ("03","平衡 = 渐变条","带迟滞 + 缓冲时间，不是一下子掉。可预警、可救回——是技巧判定，不是掷骰子。")]
cw = Inches(4.0); cx = Inches(0.6); cy = Inches(1.95)
for n, h, body in cards3:
    card(s, cx, cy, cw, Inches(2.5))
    txt(s, cx+Inches(0.25), cy+Inches(0.15), cw-Inches(0.5), Inches(0.5),
        [[(n, 30, REDL, True)]])
    txt(s, cx+Inches(0.25), cy+Inches(0.72), cw-Inches(0.5), Inches(0.4),
        [[(h, 16, RED, True)]])
    txt(s, cx+Inches(0.25), cy+Inches(1.18), cw-Inches(0.5), Inches(1.2),
        [[(body, 13, INK, False)]], line=1.3)
    cx = cx + cw + Inches(0.33)
mono = card(s, Inches(0.6), Inches(4.75), Inches(12.1), Inches(0.55))
txt(s, Inches(0.85), Inches(4.83), Inches(11.6), Inches(0.4),
    [[("abstract what we can't sense — simulate only what changes play", 15, RED, True)]])
speak(s, "我们不渲染会穿模的假腿，只模拟脚带来的后果：更宽的支撑面。三者都是加法层，关掉就退回普通纯手攀爬。")
footer(s, "我们的点子")
print("slide 3 done")

# ============== SLIDE 4 — RESEARCH ==============
s = slide(); header_band(s)
kicker(s, "调研支撑")
txt(s, Inches(0.55), Inches(0.85), Inches(12), Inches(0.7),
    [[("不是拍脑袋——文献给了 ", 30, INK, True), ("why", 30, RED, True),
      (" 和 ", 30, INK, True), ("how", 30, RED, True)]])
research = [("Kosmalla et al. · CHI 2020","真实墙实验：「脚」比「手」更影响动作准确感与攀爬乐趣。","→ 告诉我们为什么值得做脚"),
            ("Mitsuda & Kimura · Frontiers VR 2026","头当重心、头歪出无支撑脚即判掉落（n=24 已验证）。","→ 告诉我们怎么用头做平衡判定")]
cx = Inches(0.6)
for h, body, note in research:
    card(s, cx, Inches(1.95), Inches(6.0), Inches(2.1))
    txt(s, cx+Inches(0.3), Inches(2.12), Inches(5.4), Inches(0.4), [[(h, 16, RED, True)]])
    txt(s, cx+Inches(0.3), Inches(2.62), Inches(5.4), Inches(0.9), [[(body, 14, INK, False)]], line=1.3)
    txt(s, cx+Inches(0.3), Inches(3.55), Inches(5.4), Inches(0.4), [[(note, 12, MUT, False)]])
    cx = cx + Inches(6.1)
c = card(s, Inches(0.6), Inches(4.25), Inches(12.1), Inches(1.0))
rect(s, Inches(0.6), Inches(4.25), Inches(0.08), Inches(1.0), RED)
txt(s, Inches(0.9), Inches(4.42), Inches(11.6), Inches(0.7),
    [[("机器人平衡理论", 14, REDD, True),
      ("提供形式化词汇：支撑多边形 · ZMP(零力矩点) · capture point——说明可信的平衡", 14, INK, False),
      ("不需要", 14, RED, True), ("昂贵追踪或全身 IK，几何判据足矣。", 14, INK, False)]], line=1.3)
speak(s, "一篇论文告诉我们要做脚，一篇告诉我们用头当重心怎么做；机器人学给了我们『支撑面』这个数学语言。")
footer(s, "调研支撑")
print("slide 4 done")

def codebox(s, x, y, w, h, lines, size=11):
    sp = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, x, y, w, h)
    fill(sp, PAPER2); sp.line.color.rgb = LINE; sp.line.width = Pt(1); sp.shadow.inherit = False
    rect(s, x, y, Inches(0.08), h, RED)
    tb = s.shapes.add_textbox(x+Inches(0.22), y+Inches(0.12), w-Inches(0.4), h-Inches(0.24))
    tf = tb.text_frame; tf.word_wrap = True
    for i, (t, hot) in enumerate(lines):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.line_spacing = 1.18
        r = p.add_run(); r.text = t; r.font.size = Pt(size)
        r.font.name = "Consolas"; r.font.color.rgb = RED if hot else RGBColor(0x33,0x37,0x3F)
        r.font.bold = hot

# ============== SLIDE 5 — ARCHITECTURE ==============
s = slide(); header_band(s)
kicker(s, "系统架构")
txt(s, Inches(0.55), Inches(0.85), Inches(12.4), Inches(0.7),
    [[("加法式设计：拿掉平衡 / 脚法，", 28, INK, True), ("退回普通纯手攀爬", 28, RED, True)]])
arch = [
 ("Input(grip)                      Physics overlap (Hold layer)", False),
 ("    |                                    |", False),
 ("    v                                    v", False),
 ("ClimbingHand x2 --grab/release--> ClimbHold {role, type}  (臂展约束 ~0.88m)", False),
 ("    |                                    ^", False),
 ("    |                  FootPlacementSystem --auto-snap 虚拟脚→脚点", False),
 ("    v                                    |", False),
 ("ClimbController --反向位移+重力+坠落/重生--> CharacterController (XR Origin)", False),
 ("    ^   | contacts(手+脚) 把你留在墙上          |", False),
 ("    |   +----------------> BalanceSystem (头=重心; 横向支撑判定)", True),
 ("    |  PeelOff(balance==0) <---------+  探出支撑就扣血", False),
 ("GameManager(状态/计时/跌落) --events--> GameHUD(计时/体力条/平衡条)", False),
]
codebox(s, Inches(0.6), Inches(1.85), Inches(12.1), Inches(3.05), arch, size=12)
txt(s, Inches(0.6), Inches(5.05), Inches(12.1), Inches(0.5),
    [[("14 个 C# 脚本，三命名空间：Climbing(攀爬+平衡核心) · Gameplay(规则+线路) · UI/Util(HUD+触觉)。所有对 BalanceSystem / FootPlacementSystem 的引用都做了 null 检查。", 12, MUT, False)]], line=1.25)
speak(s, "核心是反向位移攀爬这个成熟基座；平衡和脚法是叠在上面的可选层，永远不会把稳的部分搞坏。（P1 补技术细节）")
footer(s, "系统架构")
print("slide 5 done")

# ============== SLIDE 6 — BALANCE MECHANIC ==============
s = slide(); header_band(s)
kicker(s, "核心机制 · 整个项目的心脏")
txt(s, Inches(0.55), Inches(0.85), Inches(12), Inches(0.7),
    [[("把支撑面压成", 30, INK, True), ("一维左右区间", 30, RED, True)]])
# left: diagram drawn from shapes
dx, dy, dw, dh = Inches(0.6), Inches(1.9), Inches(6.0), Inches(3.0)
diag = card(s, dx, dy, dw, dh)
# support interval bar
barY = dy + Inches(2.05)
rect(s, dx+Inches(1.2), barY, Inches(3.6), Inches(0.04), LINE)
sup = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, dx+Inches(1.9), barY-Inches(0.16), Inches(2.0), Inches(0.34))
sf = sup.fill; sf.solid(); sf.fore_color.rgb = REDWASH; sup.line.color.rgb = RED; sup.line.width=Pt(1.2); sup.line.dash_style=None; sup.shadow.inherit=False
txt(s, dx+Inches(1.5), barY+Inches(0.30), Inches(3.0), Inches(0.3),
    [[("[low, high] 支撑区间", 12, RED, True)]], align=PP_ALIGN.CENTER)
# contacts
def dot(cx_in, color, label, lcol):
    d = s.shapes.add_shape(MSO_SHAPE.OVAL, dx+cx_in-Inches(0.10), barY-Inches(0.10), Inches(0.20), Inches(0.20))
    fill(d, color); d.shadow.inherit=False
    txt(s, dx+cx_in-Inches(0.4), barY-Inches(0.50), Inches(0.8), Inches(0.3),
        [[(label, 11, lcol, True)]], align=PP_ALIGN.CENTER)
dot(Inches(2.1), RGBColor(0xF5,0xB3,0x01), "手", RGBColor(0xB5,0x86,0x0A))
dot(Inches(3.7), RGBColor(0xF5,0xB3,0x01), "手", RGBColor(0xB5,0x86,0x0A))
dot(Inches(1.9), RGBColor(0xFF,0x7A,0x1A), "脚", RGBColor(0xD8,0x59,0x0A))
# head/CoM
head = s.shapes.add_shape(MSO_SHAPE.OVAL, dx+Inches(2.9)-Inches(0.18), dy+Inches(0.35), Inches(0.36), Inches(0.36))
fill(head, INK); head.shadow.inherit=False
rect(s, dx+Inches(2.9)-Inches(0.01), dy+Inches(0.71), Inches(0.03), Inches(1.18), INK)
txt(s, dx+Inches(2.4), dy+Inches(0.05), Inches(1.0), Inches(0.3), [[("头 = 重心", 11, INK, True)]], align=PP_ALIGN.CENTER)
txt(s, dx+Inches(0.5), dy+Inches(2.62), dw-Inches(1.0), Inches(0.3),
    [[("重心横向坐标 = 0（落在区间内 → 稳）", 11, MUT, False)]], align=PP_ALIGN.CENTER)
# right text
rxt = Inches(7.0)
bullets(s, rxt, Inches(1.95), Inches(5.7), Inches(2.8), [
  [("所有接触点（手点 + 脚点）投影到墙横轴，得到跨度 [low, high]。", False, INK)],
  [("头的横向位置在区间内 → ", False, INK), ("稳", True, RED), ("（条回升）；歪出去 → ", False, INK),
   ("不稳", True, RED), ("（缓冲 0.35s 后条下掉）。", False, INK)],
  [("救法：", True, REDD), ("踩一只脚 / 抓对侧点 → 撑宽区间 → 重心回到里面。", False, INK)],
], size=15, gap=10)
mono2 = card(s, rxt, Inches(4.0), Inches(5.7), Inches(0.55))
txt(s, rxt+Inches(0.2), Inches(4.08), Inches(5.3), Inches(0.4),
    [[("s ∈ [−1,+1] → 平衡条 B → B≤0 则 PeelOff 脱落", 13, RED, True)]])
txt(s, rxt, Inches(4.7), Inches(5.7), Inches(0.6),
    [[("不是物理仿真——是轻量几何判据：可调、可解释、跑得动。ClimbMath.StabilityScore 纯函数 + 9 条单测。", 11, MUT, False)]], line=1.25)
speak(s, "踩一只脚为什么管用？因为它撑宽了这个区间，把你的重心重新框进去——这一下就是整个 demo 的高光。")
footer(s, "核心机制 · 平衡")
print("slide 6 done")

# ============== SLIDE 7 — FOOTWORK + CONSTANTS ==============
s = slide(); header_band(s)
kicker(s, "脚法抽象 · 关键常量")
txt(s, Inches(0.55), Inches(0.85), Inches(12.4), Inches(0.7),
    [[("脚是", 28, INK, True), ("从握点派生的状态", 28, RED, True), ("，不是 IK 腿", 28, INK, True)]])
bullets(s, Inches(0.6), Inches(1.9), Inches(6.4), Inches(3.2), [
  [("从头往下估一个「落脚区」，左右各找最近脚点吸住；爬过去后脱离再吸下一个（", False, INK), ("foot-gluing", True, RED), ("）。", False, INK)],
  [("为什么不用 IK / 生成式腿？", True, REDD), ("贴墙、高抬腿、侧身时会崩、会穿模，且与机制无关——我们只要『脚在哪、撑不撑得住』这个信息。", False, INK)],
  [("演示里的人形是阻尼弹簧摆 + 两段 IK 的「有重量感」运动学表现（纯演示、不进游戏逻辑），对标物理攀岩游戏 Klifur 逐帧打磨。", False, MUT)],
], size=14, gap=11)
# constants table
rows = [("系统","常量","值"),
        ("BalanceSystem","supportMargin","0.06 m"),
        ("BalanceSystem","maxOvershoot","0.30 m"),
        ("BalanceSystem","drain / regen","0.6 / 0.7 /s"),
        ("BalanceSystem","graceTime","0.35 s"),
        ("FootPlacement","footReach","0.45 m"),
        ("ClimbingHand","ArmReach 臂展","~0.88 m"),
        ("ClimbController","gravity","−9.81 m/s²")]
tx, ty, tw_ = Inches(7.2), Inches(1.9), Inches(5.5)
tbl = s.shapes.add_table(len(rows), 3, tx, ty, tw_, Inches(3.2)).table
tbl.columns[0].width = Inches(2.1); tbl.columns[1].width = Inches(2.1); tbl.columns[2].width = Inches(1.3)
for ri, row in enumerate(rows):
    for ci, val in enumerate(row):
        cell = tbl.cell(ri, ci); cell.text = val
        para = cell.text_frame.paragraphs[0]; para.runs[0].font.size = Pt(11)
        para.runs[0].font.name = "Microsoft YaHei"
        if ri == 0:
            cell.fill.solid(); cell.fill.fore_color.rgb = RED
            para.runs[0].font.color.rgb = WHITE; para.runs[0].font.bold = True
        else:
            cell.fill.solid(); cell.fill.fore_color.rgb = WHITE if ri % 2 else PAPER2
            para.runs[0].font.color.rgb = INK
speak(s, "都是经验初值、Inspector 可调，文档里如实标注 by feel。drain<regen 让扶正就回血；臂展约 0.88m 是真约束。")
footer(s, "脚法 · 常量")
print("slide 7 done")

def legend(s, x, y):
    items = [("手","F5B301"),("脚","FF7A1A"),("任意","9B51E0"),("登顶","1FAA59"),("易碎","E23B3B"),("休息","2D8CFF")]
    cx = x
    for label, hexc in items:
        col = RGBColor.from_string(hexc)
        d = s.shapes.add_shape(MSO_SHAPE.OVAL, cx, y, Inches(0.16), Inches(0.16))
        fill(d, col); d.shadow.inherit=False
        txt(s, cx+Inches(0.2), y-Inches(0.05), Inches(0.7), Inches(0.3), [[(label, 11, MUT, False)]])
        cx = cx + Inches(0.2) + Inches(0.14*len(label)+0.45)

# ============== SLIDE 8 — DEMO MONEY SHOT ==============
s = slide(); header_band(s)
kicker(s, "Demo · 重点")
txt(s, Inches(0.55), Inches(0.85), Inches(12.4), Inches(0.7),
    [[("失衡变红 → 踩脚回正，", 28, INK, True), ("这一下", 28, RED, True), ("市面没有", 28, INK, True)]])
add_video(s, "demo", Inches(0.6), Inches(1.95), Inches(7.1), Inches(4.0))
txt(s, Inches(0.6), Inches(5.95), Inches(7.1), Inches(0.3),
    [[("Demo/SummitVR_demo.mp4 · 同侧硬够 → 平衡条变红 → 踩橙色脚点 → 回正", 11, MUT, False)]], align=PP_ALIGN.CENTER)
txt(s, Inches(8.0), Inches(1.95), Inches(4.7), Inches(0.4), [[("颜色图例（建议用途）", 14, RED, True)]])
legend(s, Inches(8.0), Inches(2.5))
bullets(s, Inches(8.0), Inches(2.95), Inches(4.7), Inches(2.5), [
  [("颜色现在只是建议、不再硬限制——和真实岩壁一样，手脚都能用任意点。", False, INK)],
  [("默认线路故意不可纯手通过：两手都在右边 → 重心出界 → 必须踩左脚或 flag。", False, INK)],
  [("脱落 → 重生回检查点 → 顶部 “Summit!” + 计时。", False, MUT)],
], size=13, gap=10)
speak(s, "两只手都在右边，平衡条开始变红、闪……现在踩上左边这只脚……条回来了。这一下是市面所有 VR 攀岩都没有的。")
footer(s, "Demo · 核心高光")
print("slide 8 done")

# ============== SLIDE 9 — ROUTES ==============
s = slide(); header_band(s)
kicker(s, "线路库 · 程序化生成")
txt(s, Inches(0.55), Inches(0.82), Inches(12.4), Inches(0.6),
    [[("4 条可完攀 + 1 条", 26, INK, True), ("故意不可完攀", 26, RED, True)]])
routes = [("V1","Warm-up · 入门"),("V2","Balance Test · 同侧逼用脚"),
          ("V3","The Arête · 易碎逼快走"),("V4","Endurance · 体力管理")]
vw = Inches(2.85); vx = Inches(0.6); vy = Inches(1.7)
for name, cap in routes:
    add_video(s, name, vx, vy, vw, Inches(1.6))
    txt(s, vx, vy+Inches(1.62), vw, Inches(0.3), [[(cap, 10.5, MUT, True)]], align=PP_ALIGN.CENTER)
    vx = vx + vw + Inches(0.12)
add_video(s, "impossible", Inches(0.6), Inches(3.95), Inches(3.6), Inches(2.0))
txt(s, Inches(0.6), Inches(5.97), Inches(3.6), Inches(0.3),
    [[("The Gap · 中段 2m 空白", 10.5, RED, True)]], align=PP_ALIGN.CENTER)
bullets(s, Inches(4.5), Inches(4.0), Inches(8.2), Inches(2.0), [
  [("The Gap 演示「臂展是真约束」：", True, REDD), ("中段留 2m 空白，无论怎么伸都够不到——手臂会伸直但够不着，验证了 ~0.88m 臂展限制不是摆设。", False, INK)],
  [("线路用 RouteBuilder 从基本几何体程序化搭建——墙 + 彩色握点 + 登顶触发器，无需美术也能跑。", False, MUT)],
], size=14, gap=12)
speak(s, "4 条难度递增的线路各录了干净完攀，再加一条故意够不到的 The Gap——它存在的意义就是证明『够不够得着』是真机制。（P2 补线路设计）")
footer(s, "线路库 · 5 条")
print("slide 9 done")

# ============== SLIDE 10 — OVERALL APPROACH ==============
s = slide(); header_band(s)
kicker(s, "整体任务思路 · 工程方法论")
txt(s, Inches(0.55), Inches(0.85), Inches(12.4), Inches(0.7),
    [[("先把「能不能验证」做扎实，再谈手感与美术", 28, INK, True)]])
metrics = [("10/10","引擎端到端","SimulatedClimber 驱动真实 gameplay：失衡→脱落→重生→登顶"),
           ("9/9","平衡 / 位移数学","ClimbMath 纯函数单测"),
           ("2","真人可玩视角","第三人称 Play.unity / 第一视角 VR.unity，键鼠即玩")]
cx = Inches(0.6)
for big, h, body in metrics:
    card(s, cx, Inches(1.95), Inches(4.0), Inches(2.0))
    txt(s, cx, Inches(2.1), Inches(4.0), Inches(0.7), [[(big, 40, RED, True)]], align=PP_ALIGN.CENTER)
    txt(s, cx, Inches(2.95), Inches(4.0), Inches(0.35), [[(h, 14, INK, True)]], align=PP_ALIGN.CENTER)
    txt(s, cx+Inches(0.25), Inches(3.35), Inches(3.5), Inches(0.6), [[(body, 10.5, MUT, False)]], align=PP_ALIGN.CENTER, line=1.2)
    cx = cx + Inches(4.05)
c = card(s, Inches(0.6), Inches(4.2), Inches(12.1), Inches(1.15), red_bg=True)
txt(s, Inches(0.9), Inches(4.35), Inches(11.5), Inches(0.95),
    [[("整体任务思路：可验证驱动的迭代闭环", 15, WHITE, True)],
     [("先写脚本机器人驱动真实游戏逻辑做端到端自检 → 无头批渲染产出视频 → 与参考逐帧比对打磨 → 每次改动都回归自检。机制正确性靠自动化保证，团队五人可并行、无需共享头显即可推进。", 13, WHITE, False)]],
    space_after=4, line=1.3)
speak(s, "我们的整体思路是『可验证优先』：用机器人把核心闭环锁死，再叠美术、调手感。这样五个人能并行推进，每次改动都自动回归，不会把稳的部分改坏。（P4 补 XR 接线）")
footer(s, "整体任务思路")
print("slide 10 done")

# ============== SLIDE 11 — AVATAR EVOLUTION ==============
s = slide(); header_band(s)
kicker(s, "演示打磨 · 化身演进")
txt(s, Inches(0.55), Inches(0.85), Inches(12.4), Inches(0.7),
    [[("从「直挺挺的假人」到", 28, INK, True), ("像真人在爬", 28, RED, True)]])
img = os.path.join(HERE, "avatar-evolution.png")
if os.path.exists(img):
    s.shapes.add_picture(img, Inches(0.6), Inches(1.95), Inches(7.4))
txt(s, Inches(0.6), Inches(6.05), Inches(7.4), Inches(0.3),
    [[("docs/avatar-evolution.png · 对标物理攀岩游戏 Klifur 逐帧打磨", 11, MUT, False)]], align=PP_ALIGN.CENTER)
bullets(s, Inches(8.3), Inches(1.95), Inches(4.4), Inches(3.5), [
  [("身体挂在手上", True, REDD), ("而非顶在头上：单手悬挂时整个躯干倒向那只手（对角张力）。", False, INK)],
  [("头朝岩壁", True, REDD), ("、被钳制在脖子活动锥内；脊柱转髋贴墙（带约束）；肘膝不反关节，够不到的握点手臂伸直但差一截。", False, INK)],
  [("工作流：无头渲染逐帧 → ffmpeg 合成 → 与 climbing.gif 逐帧比对 → 改 → 再渲染。每轮都跑端到端自检，始终 10/10。", False, MUT)],
], size=13, gap=11)
speak(s, "这些全部只在演示里，不进游戏逻辑——端到端自检走原来的快速运动，10/10 不受影响。（P3 补美术）")
footer(s, "化身演进")
print("slide 11 done")

# ============== SLIDE 12 — EVAL FRAMEWORK + DELIVERED ==============
s = slide(); header_band(s)
kicker(s, "评测体系 · 项目总结")
txt(s, Inches(0.55), Inches(0.85), Inches(12.4), Inches(0.7),
    [[("评测体系", 28, INK, True), ("已就绪可执行", 28, RED, True), ("，系统全部交付", 28, INK, True)]])
bullets(s, Inches(0.6), Inches(1.95), Inches(6.4), Inches(2.0), [
  [("被试内对比", True, REDD), ("（同线路、顺序平衡）：A 纯手 vs B 平衡+脚法，协议已写定。", False, INK)],
  [("指标已仪器化：", True, REDD), ("登顶时间 · 跌落按原因分（松手/失衡/体力）· 平衡余量曲线 · NASA-TLX · SSQ · 真实感量表——采集脚本就绪。", False, INK)],
], size=14, gap=11)
c = card(s, Inches(0.6), Inches(3.85), Inches(6.4), Inches(1.5), red_bg=True)
txt(s, Inches(0.85), Inches(4.0), Inches(5.9), Inches(1.2),
    [[("核心假设 H（待真人实验检验）", 14, WHITE, True)],
     [("B 的 真实感↑ · 挑战↑，但 晕动 SSQ 不升——挑战来自决策难度而非运动冲突，这是市面 VR 攀岩都没有的差异化卖点。", 13, WHITE, False)]],
    space_after=4, line=1.3)
txt(s, Inches(7.3), Inches(1.95), Inches(5.4), Inches(0.4), [[("已交付清单 ✓", 14, RED, True)]])
bullets(s, Inches(7.3), Inches(2.45), Inches(5.4), Inches(3.0), [
  [("✓ 平衡机制（头=重心、横向支撑判定）", False, INK)],
  [("✓ 脚法抽象（自动吸附、foot-gluing）", False, INK)],
  [("✓ 5 条程序化线路（4 可完攀 + The Gap）", False, INK)],
  [("✓ 引擎端到端 10/10 + 数学 9/9 自检", False, INK)],
  [("✓ HUD + 音效接线、双视角真人可玩", False, INK)],
  [("✓ 演示视频、报告初稿、答辩问答", False, INK)],
  [("✓ 评测协议 + 采集仪器（待执行实验）", False, MUT)],
], size=13, gap=7)
speak(s, "系统、仪器、协议全部交付；机制正确性已用仿真闭环验证。唯一留待执行的是真人实验本身——我们把『能不能测』这件事先做扎实了。")
footer(s, "评测体系 · 项目总结")
print("slide 12 done")

# ============== SLIDE 13 — CLOSING ==============
s = slide()
rect(s, 0, 0, Inches(7.6), SH, WHITE)
rect(s, Inches(7.6), 0, SW-Inches(7.6), SH, RED)
grad_band(s, 0, 0, Inches(7.6), Inches(0.16))
txt(s, Inches(0.6), Inches(1.2), Inches(6.6), Inches(0.4), [[("结语", 14, RED, True)]])
txt(s, Inches(0.55), Inches(1.7), Inches(6.8), Inches(1.4),
    [[("一个", 40, INK, True), ("能跑", 40, RED, True), ("的存在性证明", 40, INK, True)]], line=1.1)
txt(s, Inches(0.6), Inches(3.3), Inches(6.6), Inches(1.6),
    [[("脚法和平衡，可以", 17, MUT, False), ("廉价、加法式", 17, REDD, True),
      ("地加进纯手柄 VR 攀岩。剩下的工作，是去测它到底有没有让体验更像真攀岩。", 17, MUT, False)]], line=1.35)
mono3 = card(s, Inches(0.6), Inches(5.0), Inches(6.5), Inches(0.55))
txt(s, Inches(0.8), Inches(5.08), Inches(6.1), Inches(0.4),
    [[("github.com/Icecream0507/ai3618-vr-climbing", 14, RED, True)]])
txt(s, Inches(0.6), Inches(5.75), Inches(6.6), Inches(0.4),
    [[("薛俊智 · 吴一轩 · 邹沛霖 · 陶锐 · 孙艺豪   |   答辩问答见 docs/DEFENSE_QA.md", 11, MUT, False)]])
# right panel
txt(s, Inches(8.0), Inches(1.5), Inches(4.8), Inches(0.6), [[("一句话总结", 22, WHITE, True)]])
rect(s, Inches(8.0), Inches(2.15), Inches(4.5), Pt(2), RGBColor(0xFF,0xB0,0xBC))
txt(s, Inches(8.0), Inches(2.4), Inches(4.8), Inches(2.2),
    [[("在只有头 + 两手柄的普通 VR 上，用", 15, WHITE, False), ("头当重心", 15, RGBColor(0xFF,0xD9,0xDF), True),
      (" + ", 15, WHITE, False), ("自动吸附的抽象落脚点", 15, RGBColor(0xFF,0xD9,0xDF), True),
      ("，把『重心有没有落在支撑面内』变成可玩的失衡-脱落机制。", 15, WHITE, False)]], line=1.5)
txt(s, Inches(8.0), Inches(4.7), Inches(4.8), Inches(0.8),
    [[("同侧硬够不踩脚就会被甩下墙，踩对脚就稳住。", 15, WHITE, False)]], line=1.4)
txt(s, Inches(8.0), Inches(5.7), Inches(4.8), Inches(0.8), [[("Thanks · Q&A", 32, WHITE, True)]])
print("slide 13 done")

out = os.path.join(HERE, "SummitVR_Pre.pptx")
prs.save(out)
print("SAVED ->", out)
