#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""Generate the 4-speaker academic presentation script as a .docx.
Run: python docs/make_script.py  ->  docs/SummitVR_演讲稿.docx
Register: VR-climbing interaction research (AI3618 课程研究报告).
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

def part_header(num, speaker, slides, dur):
    para("", after=2)
    rich([("第 %s 部分　" % num, True, RED), (speaker, True, REDD)], size=16, after=2)
    rich([("对应幻灯片：", False, MUT), (slides, True, INK),
          ("　｜　建议时长：", False, MUT), (dur, True, INK)], size=10, after=4)

def script_line(text):
    p = doc.add_paragraph()
    p.paragraph_format.space_after = Pt(8)
    p.paragraph_format.line_spacing = 1.45
    p.paragraph_format.left_indent = Inches(0.12)
    r = p.add_run(text); r.font.size = Pt(11.5); r.font.color.rgb = INK; set_ea(r)
    return p

def cue(text):
    rich([("【提示】", True, RED), (text, False, MUT)], size=10, after=8)

print("helpers ready")
# <<<BODY>>>

# ========== TITLE BLOCK ==========
para("Summit VR — 课程研究报告 演讲稿", 22, RED, bold=True, align=WD_ALIGN_PARAGRAPH.CENTER, after=2)
para("AI3618《虚拟现实技术》· 上海交通大学 · 约 10–12 分钟 · 四人分工讲解", 12, MUT, align=WD_ALIGN_PARAGRAPH.CENTER, after=4)
para("研究问题：在仅有头显与两个手柄的三点追踪条件下，能否重建真实攀爬中的身体平衡与脚法？",
     11, INK, italic=True, align=WD_ALIGN_PARAGRAPH.CENTER, after=8)
hr()

# ========== HOW TO USE ==========
para("分工与节奏总览", 15, REDD, bold=True, before=6, after=4)
para("全程约 10–12 分钟，由 P1–P4 四人按幻灯片顺序接力讲解。每人讲完自己负责的部分后，"
     "用一句话过渡给下一位。演示视频位于「运行演示」「攀爬路径设计」两页，播放时由对应讲解者配合旁白。"
     "建议各自把本段读熟、掐表，整体留 1–2 分钟用于提问。", 11, INK, after=6)

rows = [
  ("讲者","对应幻灯片","讲解主题","建议时长"),
  ("P1 薛俊智","封面 · 背景 · 问题 · 相关工作","开场 + 研究背景与已有工作","~2.5 min"),
  ("P2 吴一轩","方法 · 系统架构 · 平衡算法","方法设计与核心平衡判定算法","~3 min"),
  ("P3 陶锐","脚法 · 参数 · 演示 · 路径","脚法建模、参数与运行演示","~2.5 min"),
  ("P4 邹沛霖","验证 · 评测 · 总结","开发验证、评测方案与结论","~2.5 min"),
]
table = doc.add_table(rows=len(rows), cols=4)
table.style = "Table Grid"
widths = [Inches(1.6), Inches(2.6), Inches(2.6), Inches(1.0)]
for ri, row in enumerate(rows):
    for ci, val in enumerate(row):
        cell = table.cell(ri, ci)
        cell.width = widths[ci]; cell.text = ""
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
# <<<PARTS>>>

# ===================== PART 1 — P1 =====================
part_header("一", "P1 薛俊智 · 开场与研究背景", "封面 / 研究背景 / 问题分析 / 相关工作", "约 2.5 分钟")
cue("等封面投出后开口，语速放稳。本部分确立研究问题与立项依据。")
script_line("各位老师、同学好。我们这一组的课程研究题目是 Summit VR——面向纯手柄 VR 攀爬的"
            "平衡与脚法建模。我先介绍研究背景，以及我们想要回答的问题。")
script_line("先看现状。现有的 VR 攀爬交互，基本都建立在「抓握—反向位移」这一范式上：用户用双手"
            "抓住墙面的握点，再把身体拉起；运动与手部动作紧密耦合，所以舒适、晕动较低。但代价是，"
            "在这种交互里，失败只可能来自两种情况——手脱开，或者臂展不够、够不到下一个握点。")
script_line("可是在真实攀爬中，决定成败的往往不是手的力量，而是重心与脚的协同。当身体的支撑点都偏在"
            "一侧时，重心会越出支撑范围，身体就会绕着支点旋转出去，这在攀岩里称为「开门效应」，也就是"
            "barn-door。要纠正它，标准做法是伸出一只脚去侧撑，把重心重新拉回支撑范围内。")
script_line("换句话说，身体平衡和脚法，是真实攀爬的核心技能，却恰好是现有 VR 攀爬交互所忽略的一层。"
            "我们这项工作要研究的，就是如何把这一层重新建模出来。")
script_line("难点在于硬件。主流头显只能提供三个追踪点：一个头显，两个手柄。它没有脚部或躯干追踪，"
            "下半身的姿态是不可观测的。如果用逆向运动学或者生成式方法去反解腿部，在攀爬这种高抬腿、"
            "侧身的极端姿势下，结果往往会失真、穿模。所以我们的研究目标是：在不增加任何硬件的前提下，"
            "只用这三个追踪点，去近似判断「重心有没有落在支撑范围内」，并把脚法表征为「是否提供了"
            "有效支撑」，从而让平衡与脚法成为可建模、可验证的交互维度。")
script_line("这个思路不是凭空设想，已有研究提供了依据。第一，Kosmalla 等人在 CHI 2020 的实验表明，"
            "在攀爬中呈现「脚」比呈现「手」更能提升动作准确度的感知，说明脚值得被表征。第二，"
            "Mitsuda 和 Kimura 在 2026 年的工作，用头显近似重心、当重心越过无支撑的脚就判定失稳，"
            "并通过 24 人的实验验证了可行性。第三，机器人学里的支撑多边形、零力矩点等概念，"
            "为「重心是否在支撑区内」提供了形式化的判据。我们正是把这些结论，落实到无脚追踪的"
            "受限设备上。下面请吴一轩介绍我们具体的方法与系统设计。")
cue("讲到「相关工作」一页时，三张卡片各点一句即可，不必展开论文细节。")

# ===================== PART 2 — P2 =====================
part_header("二", "P2 吴一轩 · 方法与核心算法", "方法概述 / 系统架构 / 平衡判定算法", "约 3 分钟")
cue("本部分是技术核心，最可能被追问。平衡算法一页务必放慢，把四步讲清。")
script_line("谢谢俊智。我来介绍我们的方法设计和核心算法。我们的总体设计原则可以概括为一句话："
            "抽象掉不可观测的量，只对真正影响交互的部分建模。")
script_line("据此我们做了三个设计决定。第一，以头显近似重心。在三个追踪点里，头显是唯一稳定可得的"
            "高位信号，它的横向位置可以近似反映躯干是否偏离了支撑。第二，以状态来表征脚法。"
            "我们不去重建腿部几何，而是把脚抽象成一个离散状态——它位于某个握点上，为身体提供一个"
            "支撑点。第三，以分级量来表征失稳。失稳不是一瞬间的判定，而是一个带缓冲、能回升的连续量，"
            "这样它就可以预警、可以恢复。")
script_line("需要强调的是，这三层是「加法式」叠加在「抓握—反向位移」这一基础范式之上的可选模块。"
            "如果把它们移除，系统会退化成常规的纯手部攀爬，基础交互不受影响——这保证了我们新增的"
            "机制不会破坏已经稳定的部分。")
script_line("系统在实现上由十四个 C# 脚本组成，分为攀爬与平衡核心、规则与路径、界面与工具三部分。"
            "数据流大致是：手柄的握力输入触发抓握，脚法模块自动把虚拟脚吸附到下方的握点，"
            "这些接触点共同决定身体是否留在墙上；平衡模块据此判定是否失稳，失稳到一定程度就触发脱落，"
            "进入坠落与重置流程；状态、计时这些信息再驱动界面显示。")
script_line("核心算法是平衡判定，我重点讲。我们把二维的支撑面，简化成墙面横轴上的一维区间。"
            "具体分四步：第一步，取当前所有接触点——也就是抓握的手和踩住的脚——在横轴上的投影，"
            "构成一个区间，记作 low 到 high。第二步，计算重心的横向坐标到这个区间的有符号余量 s，"
            "落在区间内为正、落在区间外为负，取值在正负一之间。第三步，当失稳持续超过一个缓冲时间后，"
            "平衡量按 s 的大小衰减；一旦重心回到区间内，平衡量就回升。第四步，平衡量降到零就触发脱落。")
script_line("这里也就解释了脚法为什么有用：当你踩上一只脚，就给区间增加了一个新的接触点，"
            "区间被撑宽，原本偏在外面的重心，就重新落回了支撑范围内，平衡量随之回升。"
            "脚法是怎么具体建模的、参数怎么定，请陶锐继续。")
cue("「重新落回支撑范围内」这句是全场逻辑闭环，说完停半拍再过渡。")
# <<<MORE>>>

# ===================== PART 3 — P3 =====================
part_header("三", "P3 陶锐 · 脚法建模与运行演示", "脚法建模 / 关键参数 / 运行演示 / 攀爬路径设计", "约 2.5 分钟")
cue("「运行演示」与「攀爬路径设计」两页含视频，由本人控制播放，旁白卡着画面。")
script_line("谢谢一轩。我来讲脚法是怎么建模的，再带大家看实际运行的演示。")
script_line("前面提到，脚被抽象成一个支撑状态，而不是一条腿。具体来说：我们在身体下方估计一个落脚区，"
            "左右各选取最近的可踩握点吸附上去；当身体向上移动、原来的脚点够不到了，这只脚就脱离，"
            "再吸附到下一个更低的握点——这个机制我们称为 foot-gluing。每一只踩住的脚，"
            "都会向平衡判定贡献一个支撑点，从而扩大横向的支撑区间。")
script_line("有人可能会问，为什么不直接反解出一条腿？因为在攀爬这种姿势下，逆向运动学或生成式方法"
            "很容易失真、穿模，而且这跟我们的机制无关——机制需要的只是「脚有没有提供支撑」这一个信息。"
            "至于演示画面里那个会随重力摆动的人形，是用阻尼摆加两段 IK 做出来的视觉表现，"
            "它只负责好看，不参与任何交互判定计算。")
script_line("机制里的几个关键参数，都是按手感设定的经验值，并在编辑器里暴露可调，我们在报告里也"
            "如实标注了这一点。比如支撑区间两侧留 0.06 米的容差；平衡量的衰减速率小于回升速率，"
            "这样用户一旦把重心扶正就能较快恢复；还有一个真实约束是臂展，大约 0.88 米，"
            "超出这个范围的握点就抓不到，必须先移动身体才能够到。")
script_line("我们看一段实际运行的记录。请注意屏幕上的平衡量指示：当用户把两只手都伸向同一侧时，"
            "重心偏出了支撑区间，平衡量开始下降并变红；接着踩上左侧这个橙色脚点，支撑区间被撑宽，"
            "重心回到范围内，平衡量恢复、人稳住。这一过程完整体现了我们前面讲的判定逻辑。")
script_line("攀爬路径方面，我们用程序化方式生成了五条：四条可以完成，难度依次递增——基础路径、"
            "强制使用脚的同侧连续路径、含易碎握点需要快速通过的路径、以及考验体力管理的路径；"
            "另外还有一条对照路径故意不可完成，中段留出约两米的空白，手臂伸直也够不到，"
            "用来验证臂展约束确实生效，而不是装饰性设定。下面请邹沛霖介绍我们的验证与评测。")

# ===================== PART 4 — P4 =====================
part_header("四", "P4 邹沛霖 · 验证、评测与总结", "开发与验证方法 / 评测方案 / 总结与展望", "约 2.5 分钟")
cue("本部分收尾，语气沉稳。结尾「谢谢」后停住，准备进入提问环节。")
script_line("谢谢陶锐。最后由我介绍我们是如何验证这套系统的、评测方案怎么设计，以及整体结论。")
script_line("我们采取的是「以自动化验证驱动开发」的方法。在调手感、做画面之前，我们先写了一个"
            "脚本化的虚拟受试者，让它像真实用户一样去抓握、移动、制造失稳，然后自动检查完整的交互流程："
            "失稳会不会脱落、脱落会不会重置、到顶会不会计时等等，一共十项断言，全部通过；"
            "再加上对平衡与位移核心数学的九项单元测试，也全部通过。")
script_line("这样做有一个实际好处：机制的正确性由自动化保证，因此我们五名成员可以并行开发，"
            "不必排队共用同一台头显设备，每次改动都会自动回归验证，避免破坏已经稳定的部分。"
            "我们还提供了第三人称和第一人称两个场景，用键盘鼠标就能直接操作验证。")
script_line("评测方案我们已经设计完成，待开展真人实验。设计是被试内对比：条件 A 只有手部，"
            "条件 B 加入平衡与脚法，两组使用相同路径并平衡顺序。客观指标包括到顶用时、"
            "按成因分类的脱落次数、以及平衡余量随时间的变化曲线；主观量表采用 NASA-TLX 任务负荷、"
            "SSQ 模拟器晕动，以及真实感与偏好量表。我们的研究假设是：相比 A，B 的真实感与挑战度更高，"
            "而模拟器晕动没有显著上升——因为它增加的是决策判断的难度，而不是画面运动带来的不适。")
script_line("做个总结。我们提出并实现了一种在三点追踪条件下的平衡与脚法建模方法："
            "以头显近似重心、以横向支撑区间判定失稳，以自动吸附的虚拟脚提供支撑，"
            "使脚法能够影响成败；系统已完整实现，并通过了自动化的端到端验证。"
            "未来工作是开展受试者评测，并向二维支撑判定、受试者自主选点、以及真机部署方向扩展。")
script_line("以上就是我们小组的全部汇报。谢谢各位，欢迎提问。")
cue("「谢谢各位」后四人一同致意。各模块的提问由对应负责人回答，细节参见 docs/DEFENSE_QA.md。")
print("parts done")
# <<<MORE>>>

out = os.path.join(HERE, "SummitVR_演讲稿.docx")
doc.save(out)
print("SAVED ->", out)
