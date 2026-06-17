#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""Generate the 10-minute, 5-speaker presentation script as a .docx.
Run: python docs/make_script.py  ->  docs/SummitVR_演讲稿.docx
"""
import os
from docx import Document
from docx.shared import Pt, RGBColor, Inches
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml.ns import qn

HERE = os.path.dirname(os.path.abspath(__file__))
RED = RGBColor(0xC8, 0x10, 0x2E)
REDD = RGBColor(0x9E, 0x0B, 0x22)
MUT = RGBColor(0x6B, 0x72, 0x80)
INK = RGBColor(0x1C, 0x1F, 0x26)

doc = Document()

# base font (incl. East Asian)
style = doc.styles["Normal"]
style.font.name = "Microsoft YaHei"
style.font.size = Pt(11)
style.element.rPr.rFonts.set(qn("w:eastAsia"), "Microsoft YaHei")

def set_ea(run, font="Microsoft YaHei"):
    run.font.name = font
    rpr = run._element.get_or_add_rPr()
    rpr.rFonts.set(qn("w:eastAsia"), font)

def para(text="", size=11, color=INK, bold=False, align=None, after=6, before=0, italic=False):
    p = doc.add_paragraph()
    if align is not None: p.alignment = align
    p.paragraph_format.space_after = Pt(after)
    p.paragraph_format.space_before = Pt(before)
    p.paragraph_format.line_spacing = 1.4
    if text:
        r = p.add_run(text); r.font.size = Pt(size); r.font.color.rgb = color
        r.font.bold = bold; r.font.italic = italic; set_ea(r)
    return p

def rich(parts, size=11, after=6, line=1.4):
    """parts: list of (text, bold, color)"""
    p = doc.add_paragraph()
    p.paragraph_format.space_after = Pt(after)
    p.paragraph_format.line_spacing = line
    for (t, b, c) in parts:
        r = p.add_run(t); r.font.size = Pt(size); r.font.bold = b; r.font.color.rgb = c
        set_ea(r)
    return p

def hr():
    p = doc.add_paragraph()
    pPr = p._p.get_or_add_pPr()
    pb = pPr.makeelement(qn("w:pBdr"), {})
    bottom = pb.makeelement(qn("w:bottom"),
        {qn("w:val"): "single", qn("w:sz"): "12", qn("w:space"): "1", qn("w:color"): "C8102E"})
    pb.append(bottom); pPr.append(pb)

print("helpers ready")

# ========== TITLE BLOCK ==========
para("Summit VR — 项目汇报 演讲稿", 22, RED, bold=True, align=WD_ALIGN_PARAGRAPH.CENTER, after=2)
para("课程 AI3618 · 上海交通大学 · 10 分钟 · 五人分工汇报", 12, MUT, align=WD_ALIGN_PARAGRAPH.CENTER, after=4)
para("一句话主题：在只有头 + 两手柄的普通 VR 上，把真实抱石的「身体平衡 + 脚法落点」做成可玩机制。",
     11, INK, italic=True, align=WD_ALIGN_PARAGRAPH.CENTER, after=8)
hr()

# ========== HOW TO USE ==========
para("分工与节奏总览", 15, REDD, bold=True, before=6, after=4)
para("全程约 10 分钟，五人接力。每人到自己负责的幻灯片时接讲，讲完一句话过渡给下一位。"
     "Demo 视频在第 8、9 页，播放时讲解者配合旁白。建议各自把自己那段读熟、掐表，留 1 分钟 Q&A 缓冲。",
     11, INK, after=6)

# overview table
rows = [
  ("讲者","负责幻灯片","主题","时长"),
  ("P5 孙艺豪（主讲/开场）","封面 · 目录 · 背景问题","开场 + 我们要解决什么","~1.5 min"),
  ("P1 薛俊智（攀爬系统）","点子 · 调研 · 架构 · 平衡机制","核心技术：头当重心的平衡模型","~2.5 min"),
  ("P2 吴一轩（玩法规则）","脚法常量 · 线路库","脚法抽象 + 5 条线路设计","~2 min"),
  ("P3 邹沛霖（场景美术）","Demo · 化身演进","演示视频 + 画面打磨","~1.5 min"),
  ("P4 陶锐（XR/工程）","整体思路 · 评测体系 · 总结","开发方法论 + 交付总结 + 收尾","~2 min"),
]
table = doc.add_table(rows=len(rows), cols=4)
table.style = "Table Grid"
widths = [Inches(2.0), Inches(2.2), Inches(2.4), Inches(0.9)]
for ri, row in enumerate(rows):
    for ci, val in enumerate(row):
        cell = table.cell(ri, ci)
        cell.width = widths[ci]
        cell.text = ""
        p = cell.paragraphs[0]; p.paragraph_format.space_after = Pt(0)
        r = p.add_run(val); r.font.size = Pt(10); set_ea(r)
        if ri == 0:
            r.font.bold = True; r.font.color.rgb = RGBColor(0xFF,0xFF,0xFF)
            shd = cell._tc.get_or_add_tcPr().makeelement(qn("w:shd"),
                {qn("w:val"):"clear", qn("w:fill"):"C8102E"})
            cell._tc.get_or_add_tcPr().append(shd)
        else:
            r.font.color.rgb = INK
            if ci == 0: r.font.bold = True
para("", after=4)
hr()
print("title + overview done")

def part_header(num, speaker, slides, dur):
    para("", after=2)
    rich([("第 %s 部分　" % num, True, RED), (speaker, True, REDD)], size=16, after=2)
    rich([("对应幻灯片：", False, MUT), (slides, True, INK),
          ("　｜　时长：", False, MUT), (dur, True, INK)], size=10, after=4)

def script_line(text):
    """The actual words to say."""
    p = doc.add_paragraph()
    p.paragraph_format.space_after = Pt(8)
    p.paragraph_format.line_spacing = 1.45
    p.paragraph_format.left_indent = Inches(0.12)
    r = p.add_run(text); r.font.size = Pt(11.5); r.font.color.rgb = INK; set_ea(r)
    return p

def cue(text):
    """Stage direction / handoff cue."""
    rich([("【提示】", True, RED), (text, False, MUT)], size=10, after=8)

# ===================== PART 1 — P5 =====================
part_header("一", "P5 孙艺豪（主讲 · 开场）", "封面 / 目录 / 背景与问题", "约 1.5 分钟")
cue("站定，等封面页投出再开口。语速放慢，这是全场第一印象。")
script_line("各位老师、同学好。我们这组的项目叫 Summit VR——一个 VR 攀岩游戏。")
script_line("先说一个现象：VR 攀岩其实很成功，像 The Climb、Gorilla Tag 都很火，因为它舒适、不晕。"
            "但它们有一个共同的问题——几乎全是「纯手」：你只是用两只手抓、然后把自己拉上去，"
            "掉下来的原因也只有两个，要么松手，要么够不到。")
script_line("可真实的抱石恰恰相反。真正的难点往往不在手有多大力，而在你的重心怎么落在脚上、"
            "要不要换脚、身体会不会像门一样「开门」被甩出去。也就是说，真实攀岩最核心的"
            "身体平衡和脚法，被现有的 VR 攀岩全砍掉了。")
script_line("这就是我们要补的缺口：在只有头和两个手柄的普通 VR 设备上，把「平衡」和「脚法」"
            "这两层做回来，变成真正影响输赢的可玩机制。")
script_line("接下来由负责攀爬系统的薛俊智，讲我们具体是怎么想、怎么实现这套核心机制的。")
cue("说完转头示意 P1 接。目录页可一带而过，或在「这就是我们要补的缺口」后顺势点一句六个部分。")

# ===================== PART 2 — P1 =====================
part_header("二", "P1 薛俊智（攀爬系统）", "我们的点子 / 调研支撑 / 系统架构 / 核心平衡机制", "约 2.5 分钟")
cue("这是技术核心，最可能被追问。平衡机制那页放慢，讲清「踩脚为什么管用」。")
script_line("谢谢。我先讲我们的核心思路，一句话概括就是：测不到的就抽象，只模拟影响玩法的部分。")
script_line("具体三点。第一，头当重心：普通 VR 只能追踪头和两只手柄，追踪不到脚，"
            "所以我们用头显的位置当重心的代理，看头有没有歪出支撑范围。第二，脚做成抽象状态："
            "不去渲染那种会穿模的假腿，而是让虚拟脚自动吸附到身体下方的脚点，作用是撑宽支撑面。"
            "第三，平衡是一个渐变的条，带缓冲时间，不是一下子摔——可以预警、可以救回来。")
script_line("这套设计不是拍脑袋。有两篇文献支撑我们：一篇 CHI 2020 的研究发现，在 VR 攀岩里"
            "「脚」比「手」更影响真实感；另一篇 2026 年的研究验证了用头当重心来判断跌倒是可行的。"
            "我们就把这两个结论落到了消费级硬件上。")
script_line("架构上，最关键的一点是「加法式设计」：反向位移的手抓攀爬是成熟的基座，"
            "平衡和脚法是叠在上面的可选层，把它们关掉，游戏就退回成普通的纯手攀岩，"
            "所以新机制永远不会把稳的部分搞坏。")
script_line("那平衡到底怎么算？我们把支撑面压成一维的左右区间：把所有接触点——抓住的手点加上"
            "踩住的脚点——投影到墙的横轴上，得到一个区间。头的横向位置落在区间里就是稳，"
            "歪出去超过缓冲时间，平衡条就开始掉，掉空就脱落坠落。")
script_line("这里就能解释，为什么踩一只脚能救回来：因为踩脚给区间增加了一个新的接触点，"
            "把区间撑宽了，你的重心就重新被框回到里面。这一下，就是整个 demo 的高光。")
script_line("脚法和线路具体怎么设计，交给吴一轩。")
cue("「这一下就是整个 demo 的高光」说完停半拍，再过渡。")
print("part 1+2 done")

# ===================== PART 3 — P2 =====================
part_header("三", "P2 吴一轩（玩法规则）", "脚法抽象与关键常量 / 线路库", "约 2 分钟")
cue("线路页有视频缩略图，可以指着讲。强调「颜色只是建议」和 The Gap 的意义。")
script_line("谢谢。我接着讲脚和线路。前面说脚是抽象状态，具体来说：我们从头往下估一个落脚区，"
            "左右各找最近的脚点吸住；爬过去之后这只脚才脱离、再吸下一个，我们叫它 foot-gluing，"
            "就像真实攀岩里你踩稳一只脚才会去动它。")
script_line("有人会问，为什么不用 IK 或者生成式的腿？因为在贴墙、高抬腿、侧身这些攀岩姿势下，"
            "那些算法会崩、会穿模，而且跟机制无关——我们要的只是「脚在哪、撑不撑得住」这个信息，"
            "抽象状态就够了。所有平衡参数都是经验初值、可以在编辑器里随时调。")
script_line("再说一个真约束：手是有最大臂展的，大约 0.88 米，够不到的握点就是抓不住，"
            "你必须先把身体移过去。这正是攀岩的难点来源。")
script_line("线路方面，我们用程序化生成，不依赖美术也能跑，一共做了 5 条。"
            "其中 4 条可以完攀，难度递增：入门的 Warm-up、逼你用脚维持平衡的 Balance Test、"
            "用易碎点逼你别久留的 The Arête、考验体力管理的 Endurance。")
script_line("还有一个细节：握点颜色现在只是「建议用途」，不再是硬限制——和真实岩壁一样，"
            "手脚都能用任意点。但默认线路是故意设计成不能纯手通过的：两只手都在右边时重心会出界，"
            "你必须踩左脚或者用 flag 才能过去。")
script_line("最后第 5 条线路 The Gap，是故意做成不可完攀的：中间留了 2 米空白，怎么伸都够不到，"
            "它存在的意义就是证明「够不够得着」是个真机制。下面请邹沛霖带大家看 demo。")
cue("说到「看 demo」时把场子交给 P3，由他控制视频播放。")

# ===================== PART 4 — P3 =====================
part_header("四", "P3 邹沛霖（场景美术 · Demo）", "Demo 核心高光 / 化身演进", "约 1.5 分钟")
cue("先点开第 8 页的 demo 视频，边播边讲。视频约 25 秒，旁白要卡着画面。")
script_line("好，我们直接看这段演示。大家注意平衡条——现在这个人两只手都伸到了右边，"
            "平衡条开始变红、闪烁，这就是重心已经歪出支撑范围了。")
script_line("看这里——他踩上了左边这个橙色的脚点，平衡条立刻回来了，人稳住了。"
            "这一下「失衡变红、踩脚回正」，就是市面上所有 VR 攀岩都没有的东西。")
script_line("旁边是颜色图例：黄手、橙脚、紫任意、绿登顶。脱落之后会重生回检查点，"
            "登顶时顶部会显示 Summit 加上完成时间。")
cue("切到第 9 页线路视频，快速扫一遍四条完攀 + The Gap，再切第 11 页化身演进图。")
script_line("画面上这几段是四条线路的完整完攀，最后这条 The Gap 就是刚才说的够不到的那条。")
script_line("关于画面，我们也做了打磨。一开始的小人是直挺挺的假人，我们对标一款物理攀岩游戏，"
            "一轮轮逐帧比对，把它改成了身体挂在手上、单手悬挂时整个躯干会倒向那只手、"
            "头始终朝着岩壁的样子，看起来就像真人在爬。这些只在演示里，不影响游戏逻辑。")
script_line("接下来请陶锐讲我们整体的开发思路和项目交付情况。")

# ===================== PART 5 — P4 =====================
part_header("五", "P4 陶锐（XR / 工程 · 收尾）", "整体任务思路 / 评测体系 / 项目总结", "约 2 分钟")
cue("这是收尾，要稳。结尾「谢谢」之后停住，准备接 Q&A。")
script_line("谢谢。我讲一下我们整体是怎么推进这个项目的，以及最后交付了什么。")
script_line("我们的整体思路是「可验证优先」。在调手感、做美术之前，先写了一个脚本机器人，"
            "让它像真玩家一样去抓、去爬、去失衡，自动断言核心闭环——失衡会脱落、坠落会重生、"
            "登顶会计时——一共跑通 10 条，再加 9 条平衡数学的单元测试，全部通过。")
script_line("这样做的好处是：机制的正确性靠自动化保证，我们五个人可以并行推进、"
            "不需要轮流抢一个头显，每次改动都自动回归，不会把已经稳的部分改坏。"
            "人也可以直接用键鼠在两个视角里试玩。")
script_line("评测方面，我们把「能不能测」这件事先做扎实了：被试内对比纯手和「平衡加脚法」两个版本，"
            "完成时间、按原因分类的跌落、NASA-TLX 工作负荷、SSQ 晕动这些指标的采集脚本都就绪了，"
            "实验协议也写定了。我们的核心假设是——加了平衡和脚法之后，真实感和挑战会上升，"
            "但晕动不会上升，因为难度来自决策而不是运动冲突。")
script_line("做一个总结：平衡机制、脚法抽象、5 条线路、自动化自检、HUD 和音效、双视角试玩、"
            "演示视频、报告和答辩材料，全部已经交付；唯一留待执行的，就是真人实验本身。")
script_line("一句话收尾：Summit VR 是一个能跑的存在性证明——脚法和平衡，可以廉价、加法式地"
            "加进纯手柄 VR 攀岩。谢谢大家，我们可以回答问题。")
cue("「谢谢大家」后五人可一起致意。Q&A 细节见 docs/DEFENSE_QA.md，各模块问题由对应负责人接。")
print("parts 3+4+5 done")

# save
out = os.path.join(HERE, "SummitVR_演讲稿.docx")
doc.save(out)
print("SAVED ->", out)
