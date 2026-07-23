"""Generate a readable, non-overlapping PDF User Guide for Core AI Architect."""
from __future__ import annotations

from pathlib import Path

from fpdf import FPDF
from fpdf.enums import XPos, YPos

OUT = Path(__file__).resolve().parents[1] / "docs" / "Core_AI_Architect_User_Guide.pdf"

# Letter size usable width with 18mm margins: ~180mm
MARGIN = 18
BOTTOM_LIMIT = 20
NAVY = (30, 64, 110)
GRAY = (55, 55, 55)
LIGHT = (245, 248, 252)
TIP_BG = (232, 242, 252)
RULE = (210, 210, 210)


def clean(text: str) -> str:
    return (
        str(text)
        .replace("\u2014", "-")
        .replace("\u2013", "-")
        .replace("\u2018", "'")
        .replace("\u2019", "'")
        .replace("\u201c", '"')
        .replace("\u201d", '"')
        .replace("\u2022", "-")
        .replace("\u2248", "~")
        .replace("\u2192", "->")
        .replace("\xa0", " ")
    )


class GuidePDF(FPDF):
    def __init__(self) -> None:
        super().__init__(orientation="P", unit="mm", format="Letter")
        self.alias_nb_pages()
        self.set_auto_page_break(auto=False)  # we manage breaks
        self.set_margins(MARGIN, MARGIN, MARGIN)
        self._suppress_chrome = False

    def header(self) -> None:
        if self._suppress_chrome or self.page_no() == 1:
            return
        self.set_y(10)
        self.set_font("Helvetica", "I", 9)
        self.set_text_color(110, 110, 110)
        self.cell(0, 6, "Core AI Architect - User Guide", new_x=XPos.LMARGIN, new_y=YPos.NEXT)
        self.set_draw_color(*RULE)
        self.set_line_width(0.3)
        y = self.get_y() + 1
        self.line(self.l_margin, y, self.w - self.r_margin, y)
        self.set_y(y + 5)

    def footer(self) -> None:
        if self._suppress_chrome or self.page_no() == 1:
            return
        self.set_y(-14)
        self.set_font("Helvetica", "I", 8)
        self.set_text_color(120, 120, 120)
        self.cell(0, 8, f"Page {self.page_no()} of {{nb}}", align="C")

    def content_bottom(self) -> float:
        return self.h - BOTTOM_LIMIT

    def ensure_space(self, needed: float) -> None:
        if self.get_y() + needed > self.content_bottom():
            self.add_page()


def usable_width(pdf: GuidePDF) -> float:
    return pdf.w - pdf.l_margin - pdf.r_margin


def write_wrapped(pdf: GuidePDF, text: str, size: int = 10, style: str = "", color=GRAY, h: float = 5.2) -> None:
    pdf.set_font("Helvetica", style, size)
    pdf.set_text_color(*color)
    pdf.set_x(pdf.l_margin)
    pdf.multi_cell(usable_width(pdf), h, clean(text), new_x=XPos.LMARGIN, new_y=YPos.NEXT)


def h1(pdf: GuidePDF, text: str) -> None:
    pdf.ensure_space(22)
    pdf.ln(3)
    write_wrapped(pdf, text, size=15, style="B", color=NAVY, h=8)
    pdf.set_draw_color(*NAVY)
    pdf.set_line_width(0.5)
    y = pdf.get_y()
    pdf.line(pdf.l_margin, y, pdf.w - pdf.r_margin, y)
    pdf.ln(4)


def h2(pdf: GuidePDF, text: str) -> None:
    pdf.ensure_space(18)
    pdf.ln(4)
    write_wrapped(pdf, text, size=12, style="B", color=NAVY, h=7)
    pdf.ln(1)


def h3(pdf: GuidePDF, text: str) -> None:
    pdf.ensure_space(14)
    pdf.ln(2)
    write_wrapped(pdf, text, size=10, style="B", color=(45, 45, 45), h=6)
    pdf.ln(1)


def body(pdf: GuidePDF, text: str) -> None:
    pdf.ensure_space(12)
    write_wrapped(pdf, text, size=10, style="", color=GRAY, h=5.2)
    pdf.ln(2)


def contrib(pdf: GuidePDF, text: str) -> None:
    pdf.ensure_space(12)
    write_wrapped(
        pdf,
        f"What this contributes to the built app: {text}",
        size=9,
        style="I",
        color=(80, 80, 80),
        h=5,
    )
    pdf.ln(2)


def tip(pdf: GuidePDF, text: str) -> None:
    text = clean(text)
    width = usable_width(pdf)
    pad = 3
    inner_w = width - 2 * pad

    pdf.set_font("Helvetica", "B", 9)
    tip_lines = pdf.multi_cell(inner_w, 4.8, "TIP", dry_run=True, output="LINES")
    pdf.set_font("Helvetica", "", 9)
    body_lines = pdf.multi_cell(inner_w, 4.8, text, dry_run=True, output="LINES")
    box_h = 4 + len(tip_lines) * 4.8 + len(body_lines) * 4.8 + 4

    pdf.ensure_space(box_h + 4)
    x = pdf.l_margin
    y = pdf.get_y() + 1

    pdf.set_fill_color(*TIP_BG)
    pdf.set_draw_color(*NAVY)
    pdf.set_line_width(0.3)
    pdf.rect(x, y, width, box_h, style="DF")
    pdf.set_line_width(1.4)
    pdf.line(x, y, x, y + box_h)
    pdf.set_line_width(0.2)

    pdf.set_xy(x + pad, y + 2)
    pdf.set_font("Helvetica", "B", 9)
    pdf.set_text_color(*NAVY)
    pdf.multi_cell(inner_w, 4.8, "TIP", new_x=XPos.LMARGIN, new_y=YPos.NEXT)
    pdf.set_x(x + pad)
    pdf.set_font("Helvetica", "", 9)
    pdf.set_text_color(40, 40, 40)
    pdf.multi_cell(inner_w, 4.8, text, new_x=XPos.LMARGIN, new_y=YPos.NEXT)
    pdf.set_y(y + box_h + 3)


def numbered(pdf: GuidePDF, items: list[str]) -> None:
    for i, item in enumerate(items, 1):
        pdf.ensure_space(10)
        write_wrapped(pdf, f"{i}. {clean(item)}", size=10, h=5.2)
    pdf.ln(2)


def checkbox(pdf: GuidePDF, text: str) -> None:
    pdf.ensure_space(8)
    write_wrapped(pdf, f"[ ]  {clean(text)}", size=10, h=5.5)


def option_card(
    pdf: GuidePDF,
    name: str,
    required: str,
    meaning: str,
    effect: str,
) -> None:
    """One option as a stacked card - avoids cramped overlapping table cells."""
    width = usable_width(pdf)
    pad = 3
    inner = width - 2 * pad
    name = clean(name)
    required = clean(required)
    meaning = clean(meaning)
    effect = clean(effect)

    pdf.set_font("Helvetica", "B", 10)
    title = f"{name}   ({required})"
    title_lines = pdf.multi_cell(inner, 5, title, dry_run=True, output="LINES")
    pdf.set_font("Helvetica", "", 9)
    m_lines = pdf.multi_cell(inner, 4.8, f"Meaning: {meaning}", dry_run=True, output="LINES")
    e_lines = pdf.multi_cell(inner, 4.8, f"Effect on built app: {effect}", dry_run=True, output="LINES")
    box_h = 3 + len(title_lines) * 5 + len(m_lines) * 4.8 + len(e_lines) * 4.8 + 4

    pdf.ensure_space(box_h + 3)
    x = pdf.l_margin
    y = pdf.get_y()

    pdf.set_fill_color(255, 255, 255)
    pdf.set_draw_color(*RULE)
    pdf.set_line_width(0.3)
    pdf.rect(x, y, width, box_h, style="D")
    pdf.set_fill_color(*LIGHT)
    pdf.rect(x, y, width, 2 + len(title_lines) * 5, style="F")
    pdf.rect(x, y, width, box_h, style="D")

    pdf.set_xy(x + pad, y + 2)
    pdf.set_font("Helvetica", "B", 10)
    pdf.set_text_color(*NAVY)
    pdf.multi_cell(inner, 5, title, new_x=XPos.LMARGIN, new_y=YPos.NEXT)

    pdf.set_x(x + pad)
    pdf.set_font("Helvetica", "", 9)
    pdf.set_text_color(*GRAY)
    pdf.multi_cell(inner, 4.8, f"Meaning: {meaning}", new_x=XPos.LMARGIN, new_y=YPos.NEXT)
    pdf.set_x(x + pad)
    pdf.multi_cell(inner, 4.8, f"Effect on built app: {effect}", new_x=XPos.LMARGIN, new_y=YPos.NEXT)
    pdf.set_y(y + box_h + 2.5)


def simple_table(pdf: GuidePDF, headers: list[str], rows: list[list[str]], ratios: list[float]) -> None:
    """Reliable table using RIGHT/TOP cell positioning so text never overlaps."""
    width = usable_width(pdf)
    total = sum(ratios)
    cols = [width * (r / total) for r in ratios]
    line_h = 4.6

    def measure(cells: list[str], bold: bool) -> float:
        pdf.set_font("Helvetica", "B" if bold else "", 8)
        tallest = line_h
        for i, cell in enumerate(cells):
            lines = pdf.multi_cell(cols[i] - 2, line_h, clean(cell), dry_run=True, output="LINES")
            tallest = max(tallest, line_h * max(1, len(lines)))
        return tallest + 1.5

    def draw_row(cells: list[str], header: bool = False) -> None:
        h = measure(cells, header)
        pdf.ensure_space(h + 1)
        x0 = pdf.l_margin
        y0 = pdf.get_y()

        if header:
            pdf.set_fill_color(*NAVY)
            pdf.set_text_color(255, 255, 255)
            font_style = "B"
        else:
            pdf.set_fill_color(255, 255, 255)
            pdf.set_text_color(35, 35, 35)
            font_style = ""

        # background + borders first
        x = x0
        for w in cols:
            pdf.rect(x, y0, w, h, style="DF" if header else "D")
            x += w

        # text per cell, keep cursor at top-right of each cell
        x = x0
        pdf.set_font("Helvetica", font_style, 8)
        for i, cell in enumerate(cells):
            pdf.set_xy(x + 1, y0 + 1)
            pdf.multi_cell(
                cols[i] - 2,
                line_h,
                clean(cell),
                border=0,
                new_x=XPos.RIGHT,
                new_y=YPos.TOP,
            )
            x += cols[i]

        pdf.set_xy(x0, y0 + h)

    draw_row(headers, header=True)
    for row in rows:
        # pad short rows
        while len(row) < len(headers):
            row.append("")
        draw_row(row[: len(headers)], header=False)
    pdf.ln(3)


def title_page(pdf: GuidePDF) -> None:
    pdf._suppress_chrome = True
    pdf.add_page()
    pdf.ln(48)

    def centered(text: str, size: int, style: str = "", color=NAVY, h: float = 8) -> None:
        pdf.set_font("Helvetica", style, size)
        pdf.set_text_color(*color)
        pdf.set_x(pdf.l_margin)
        pdf.multi_cell(usable_width(pdf), h, clean(text), align="C", new_x=XPos.LMARGIN, new_y=YPos.NEXT)

    centered("Core AI Architect", 28, "B", NAVY, 14)
    pdf.ln(2)
    centered("User Guide", 18, "", (70, 70, 70), 10)
    pdf.ln(8)
    pdf.set_draw_color(*NAVY)
    pdf.set_line_width(0.7)
    mid = pdf.w / 2
    y = pdf.get_y()
    pdf.line(mid - 36, y, mid + 36, y)
    pdf.ln(12)
    centered(
        "A practical guide for defining an application blueprint (SSOT JSON)\n"
        "so an AI coding agent can build your app with clear instructions.",
        12,
        "",
        (60, 60, 60),
        7,
    )
    pdf.ln(18)
    centered(
        "Coresynth Studios  |  SSOT authoring for Windows app builds",
        10,
        "I",
        (110, 110, 110),
        6,
    )
    # Keep chrome suppressed until this page is finished (footer runs on next add_page).
    # page_no()==1 guard in footer/header also covers this.


def build() -> Path:
    pdf = GuidePDF()

    title_page(pdf)

    # Contents
    pdf.add_page()
    h1(pdf, "Contents")
    for item in [
        "1. What this tool is (and is not)",
        "2. Typical workflow",
        "3. Menu reference",
        "4. Section-by-section reference",
        "     4.1 Project",
        "     4.2 Environment",
        "     4.3 Architecture",
        "     4.4 UI (UI Layout Builder)",
        "     4.5 Domain",
        "     4.6 Workflows (Workflow Builder)",
        "     4.7 Constraints",
        "     4.8 Data",
        "     4.9 Build",
        "     4.10 Deployment",
        "     4.11 Testing",
        "     4.12 Documentation",
        "5. Recommended fill order",
        "6. Completeness checklist",
        "7. After export - using the SSOT",
    ]:
        pdf.ensure_space(8)
        write_wrapped(pdf, item, size=11, h=6.2)

    # 1
    pdf.add_page()
    h1(pdf, "1. What this tool is (and is not)")
    body(
        pdf,
        "Core AI Architect does not write your app's code. It helps you author a structured blueprint "
        "(the SSOT JSON). You - or an AI coding agent - then use that blueprint to generate the real solution.",
    )
    simple_table(
        pdf,
        ["This tool does", "This tool does not"],
        [
            ["Collect project choices in guided sections", "Compile or publish the target app"],
            ["Validate required fields", "Invent screens/entities if you left them empty"],
            ["Export JSON SSOT and Markdown docs", "Replace Visual Studio or your coding agent"],
        ],
        [1, 1],
    )
    tip(
        pdf,
        'Empty lists (screens, entities, workflows, modules, etc.) mean "intentionally blank." '
        "A careful agent should not invent those for you. Fill the sections that matter for the app you want built.",
    )

    # 2
    h1(pdf, "2. Typical workflow")
    numbered(
        pdf,
        [
            "File -> New - pick a form profile (for example, WPF Desktop App).",
            "Walk the left section list (Project through Documentation).",
            "Use Previous / Next or click a section name.",
            "Tools -> Validate when you think you are done.",
            "File -> Save (keeps your SSOT) and/or Export JSON / Export Markdown.",
            "Hand the .ssot.json (plus SSOT_CONTRACT.md) to your coding agent.",
        ],
    )
    h2(pdf, "Form profiles")
    body(pdf, "Profiles prefill environment defaults and tune available choices. They do not create demo content.")
    simple_table(
        pdf,
        ["Profile", "Best for"],
        [
            ["WPF Desktop App", "Windows desktop GUI (WPF)"],
            ["WinPE Tool", "WinPE / recovery-style tools"],
            ["Web API", "ASP.NET APIs"],
            ["CLI Tool", "Console commands"],
            ["PowerShell Module", "Cmdlets / modules"],
            ["Deployment Automation", "Pipelines / orchestration"],
            ["Custom", "Broadest option sets (blank canvas)"],
        ],
        [1.1, 1.5],
    )

    # 3
    h1(pdf, "3. Menu reference")
    simple_table(
        pdf,
        ["Menu", "Action", "Purpose"],
        [
            ["File", "New", "Start a new SSOT with a profile"],
            ["File", "Open", "Open an existing .json SSOT"],
            ["File", "Save / Save As", "Persist your working SSOT file"],
            ["File", "Export JSON", "Handoff file for coding agents"],
            ["File", "Export Markdown", "Human-readable overview of filled sections"],
            ["Tools", "Validate", "Check required fields and warnings"],
            ["Tools", "Copy PowerShell Command", "Helper for PowerShell 7+ module use"],
            ["Help", "About / Module Help", "Schemas path and orientation"],
        ],
        [0.7, 1.2, 1.8],
    )

    # 4
    pdf.add_page()
    h1(pdf, "4. Section-by-section reference")
    body(
        pdf,
        "Each left-nav section becomes part of the exported SSOT. Together, these choices tell a coding agent "
        "exactly what kind of app to build. Each option below is shown as its own card so nothing overlaps.",
    )

    # 4.1
    h2(pdf, "4.1 Project")
    contrib(pdf, "Identity and intent for the whole app (names, ownership, summary).")
    for args in [
        ("Project Id", "Required", "Machine-friendly key (prefer kebab-case), e.g. core-lottery-generator", "Filenames, package identity hints, stable references"),
        ("Project Name", "Required", "Human title, e.g. Core Lottery Generator", "Window titles, README headings, branding"),
        ("Description", "Required", "Plain-language purpose and scope", "Primary narrative for what to build"),
        ("Owner", "Optional", "Person, team, or company", "Optional metadata in docs"),
        ("Tags", "Optional", "Short labels (add one at a time)", "Categorization / search metadata"),
    ]:
        option_card(pdf, *args)
    tip(
        pdf,
        "Put why and scope in Description. Put structured rules (entities, screens, workflows) in later sections - not only in the description paragraph.",
    )

    # 4.2
    h2(pdf, "4.2 Environment")
    contrib(pdf, "Platform stack the generated solution must use.")
    for args in [
        ("Target OS", "Required", "Where the app runs (e.g. Windows 11)", "OS assumptions, APIs, packaging"),
        ("Runtime", "Required", "Execution stack (e.g. .NET 8)", "SDK / target framework"),
        ("UI Framework", "Required", "WPF, WinUI, Console, ASP.NET, etc.", "Project type and UI libraries"),
        ("Language", "Required", "C#, PowerShell, or F#", "Language of generated code"),
    ]:
        option_card(pdf, *args)
    tip(pdf, "Keep UI Framework consistent with UI -> Navigation Pattern (for example WPF + Window+Dialogs).")

    # 4.3
    pdf.add_page()
    h2(pdf, "4.3 Architecture")
    contrib(pdf, "How code is organized (pattern, layers, feature modules).")
    for args in [
        ("Pattern", "Required", "MVVM, MVC, Clean Architecture, etc.", "Folder layout, ViewModels vs controllers"),
        ("Layers", "Optional", "Named layers as a list, e.g. UI, Domain, Infrastructure", "Keep those layers separate in code"),
        ("Modules (Add Item)", "Optional", "Feature areas of the product", "Splits the solution into logical modules"),
    ]:
        option_card(pdf, *args)
    h3(pdf, "Each module item")
    for args in [
        ("Name", "Required for item", "Short module name (e.g. Drawing)", "Module / folder / project naming"),
        ("Description", "Optional", "What that area does", "Scope for that module's code"),
        ("Dependencies", "Optional", "Other module names it needs", "References between modules"),
    ]:
        option_card(pdf, *args)
    tip(pdf, "Modules are feature areas, not NuGet package names. Leave empty only if the app is tiny.")

    # 4.4
    pdf.add_page()
    h2(pdf, "4.4 UI (UI Layout Builder)")
    contrib(pdf, "Windows/screens, controls, navigation, and look - so the agent knows what to put on screen.")
    body(pdf, "This section uses a dedicated UI Layout Builder (not only a simple form).")
    for args in [
        ("Theme", "Required", "Visual style (Default, Fluent, Custom, ...)", "Styling approach"),
        ("Navigation Pattern", "Required", "How users move between surfaces", "Shell: dialogs, tabs, frame, routes"),
        ("Screens", "Strongly recommended", "Each window/page/surface", "Creates those views"),
        ("Controls (per screen)", "Strongly recommended", "Buttons, lists, inputs, etc.", "Concrete UI elements and actions"),
    ]:
        option_card(pdf, *args)
    h3(pdf, "Each screen")
    for args in [
        ("Id", "Required", "Stable screen key (e.g. main)", "Code identifiers, navigation targets"),
        ("Title", "Required", "User-visible title", "Window/page header"),
        ("Route", "Optional", "Path or route hint (e.g. /)", "Navigation routing when applicable"),
    ]:
        option_card(pdf, *args)
    h3(pdf, "Each control")
    for args in [
        ("Id", "Required", "Stable control key (e.g. generateButton)", "Bindings; workflow Trigger should match this"),
        ("Type", "Required", "Button, TextBox, ComboBox, etc.", "Which UI widget to create"),
        ("Label", "Optional", "Visible text", "Button/caption text"),
        ("Action", "Optional", "Named behavior (e.g. GenerateNumbers)", "Maps click/command to a workflow"),
    ]:
        option_card(pdf, *args)
    tip(pdf, "If Screens is empty, the agent has no UI contract - only your Description prose.")

    # 4.5
    pdf.add_page()
    h2(pdf, "4.5 Domain")
    contrib(pdf, 'Data model - the "things" the app knows about.')
    option_card(
        pdf,
        "Entities (Add Item)",
        "Strongly recommended",
        "Business objects (e.g. LotteryGame, DrawResult)",
        "Classes / records / tables",
    )
    h3(pdf, "Each entity")
    for args in [
        ("Name", "Required", "Type name", "Class / table name"),
        ("Description", "Optional", "What it represents", "Modeling guidance"),
        ("Fields", "Optional", "Properties of the entity", "Typed properties with required flags"),
    ]:
        option_card(pdf, *args)
    h3(pdf, "Each entity field")
    for args in [
        ("Name", "Required", "Property name", "Code property name"),
        ("Type", "Required", "string, int, bool, decimal, DateTime, Guid", "Property / column type"),
        ("Required", "Optional", "Must have a value", "Validation / non-nullability"),
    ]:
        option_card(pdf, *args)
    tip(
        pdf,
        "Encode product rules here when possible (game name, ball count, min/max). Do not rely only on Project Description.",
    )

    # 4.6
    pdf.add_page()
    h2(pdf, "4.6 Workflows (Workflow Builder)")
    contrib(pdf, "User/system processes - what happens when someone clicks Generate, Save, etc.")
    body(pdf, "Uses a dedicated Workflow Builder.")
    option_card(pdf, "Workflows (Add)", "Strongly recommended", "Named processes", "Commands / use-case methods")
    h3(pdf, "Each workflow")
    for args in [
        ("Id", "Required", "Stable key (e.g. generateNumbers)", "Code identifier"),
        ("Name", "Required", "Display name", "Docs and UI labels"),
        ("Trigger", "Optional", "What starts it (prefer a control Id from UI)", "Wires UI action to workflow"),
        ("Steps", "Optional", "Ordered plain-language steps", "Implementation checklist for the agent"),
        ("Error Strategy", "Optional", "How failures are handled", "MessageBox vs throw vs retry, etc."),
        ("Log Level", "Optional", "Severity for logging on errors", "Logging behavior"),
    ]:
        option_card(pdf, *args)
    tip(pdf, "Fill UI controls first, then set Trigger to those control Ids so validation can warn on mismatches.")

    # 4.7
    h2(pdf, "4.7 Constraints")
    contrib(pdf, "Hard rules the agent must obey (libraries, logging, telemetry).")
    for args in [
        ("Allowed Libraries", "Optional", "Preferred packages (e.g. CommunityToolkit.Mvvm)", "Agent should prefer these when needed"),
        ("Forbidden Libraries", "Optional", "Packages that must not be used", "Agent must not add them"),
        ("Logging Required", "Required", "Whether logging must exist", "Adds logging if true"),
        ("Telemetry Allowed", "Required", "Analytics/telemetry permission", "No telemetry if false"),
        ("Third-Party Bootloaders Allowed", "Required", "Policy flag for bootloader-style third parties", "Agent must respect this policy"),
    ]:
        option_card(pdf, *args)

    # 4.8
    pdf.add_page()
    h2(pdf, "4.8 Data")
    contrib(pdf, "How (and if) the app persists data.")
    for args in [
        ("Storage Type", "Required", "None, LocalJson, SQLite, SQLServer, ...", "Persistence technology"),
        ("Connection String Mode", "Required", "Where connection/path config lives", "Config file vs env vs secrets vs none"),
        ("Migrations Required", "Required", "Whether DB schema migrations are needed", "Migration projects/tooling if true"),
    ]:
        option_card(pdf, *args)
    tip(
        pdf,
        "Local file history/settings: often LocalJson + connection mode None. Real database: SQLite/SQLServer + ConfigFile or UserSecrets.",
    )

    # 4.9
    h2(pdf, "4.9 Build")
    contrib(pdf, "How the solution is compiled and for which platforms.")
    for args in [
        ("Output Type", "Required", "WinExe (GUI) vs Exe (console/host)", "Project output type"),
        ("Configurations", "Required", "Usually Debug and Release", "Build configurations"),
        ("Targets (Add Item)", "Recommended", "Named publish targets", "RIDs / publish profiles"),
    ]:
        option_card(pdf, *args)
    h3(pdf, "Each target")
    for args in [
        ("Name", "Required", "Label (e.g. Desktop)", "Human name for the target"),
        ("Runtime Identifier", "Required", "e.g. win-x64", "Publish RID"),
    ]:
        option_card(pdf, *args)

    # 4.10
    h2(pdf, "4.10 Deployment")
    contrib(pdf, "How users receive the finished app.")
    for args in [
        ("Packaging", "Required", "None, MSIX, Zip, ...", "Packaging approach"),
        ("Auto Update", "Required", "Whether auto-update is in scope", "Update mechanism (or none)"),
    ]:
        option_card(pdf, *args)

    # 4.11
    pdf.add_page()
    h2(pdf, "4.11 Testing")
    contrib(pdf, "Expected automated test approach for the generated app.")
    for args in [
        ("Unit Test Framework", "Required", "xUnit, NUnit, MSTest, Pester, None", "Test project type"),
        ("UI Test Framework", "Required", "None, FlaUI, WinAppDriver, Playwright", "UI automation stack"),
        ("Coverage Target", "Required", "Number from 0 to 1 (e.g. 0.6 ~ 60%)", "Coverage goal for agents/CI"),
    ]:
        option_card(pdf, *args)
    tip(
        pdf,
        "For desktop WPF/WinUI, prefer FlaUI or None - not Playwright (web-oriented) - unless you intentionally have a web UI.",
    )

    # 4.12
    h2(pdf, "4.12 Documentation")
    contrib(pdf, "Which docs the agent (or Markdown export) should produce.")
    for args in [
        ("Generate README", "Required", "Want a README", "Includes overview-style README content"),
        ("Generate Architecture Doc", "Required", "Want architecture documentation", "Architecture write-up when data exists"),
        ("Sections", "Optional", "Overview, Architecture, Workflows, Entities, ...", "Which topics appear when data is filled"),
    ]:
        option_card(pdf, *args)
    tip(pdf, "Empty product sections are omitted from Markdown export (no filler text).")

    # 5
    pdf.add_page()
    h1(pdf, "5. Recommended fill order")
    body(pdf, "For a complete agent handoff, fill sections in this order:")
    numbered(
        pdf,
        [
            "Project - Id, Name, Description",
            "Environment - OS, Runtime, UI Framework, Language",
            "Architecture - Pattern; add Layers/Modules if useful",
            "UI - Theme, navigation, screens, controls",
            "Domain - Entities and fields (rules as data)",
            "Workflows - Processes tied to control Ids",
            "Constraints - Libraries, logging, telemetry",
            "Data - Storage choices",
            "Build - Output type + at least one Target (win-x64)",
            "Deployment - Packaging",
            "Testing - Frameworks + coverage",
            "Documentation - Flags/sections",
            "Validate -> Save -> Export JSON",
        ],
    )

    # 6
    h1(pdf, "6. Completeness checklist")
    body(pdf, "Before handing SSOT to an agent, confirm:")
    for item in [
        "Project Id / Name / Description filled",
        "Environment matches the kind of app you want",
        "At least one screen with meaningful controls (for GUI apps)",
        "Core entities defined (not only described in prose)",
        "Key workflows with steps and triggers",
        "Data/storage choice matches whether you need history/settings",
        "Build target RID set (e.g. win-x64)",
        "UI test framework makes sense for desktop vs web",
        "Validate reports no unexpected errors",
    ]:
        checkbox(pdf, item)
    pdf.ln(3)

    # 7
    h1(pdf, "7. After export - using the SSOT")
    numbered(
        pdf,
        [
            "Keep your saved .ssot.json as the contract.",
            "Point the coding agent at that file and docs/SSOT_CONTRACT.md.",
            "Expect the agent to follow environment, UI, domain, workflows, and constraints strictly.",
            "If something was left empty, either fill it in Core AI Architect and re-export, or explicitly tell the agent it may propose those parts.",
        ],
    )
    pdf.ln(4)
    write_wrapped(
        pdf,
        "Related files: README.md (build/run), docs/SSOT_CONTRACT.md (agent rules), "
        "docs/USER_GUIDE.md (Markdown source for this PDF).",
        size=9,
        style="I",
        color=(110, 110, 110),
        h=5,
    )

    OUT.parent.mkdir(parents=True, exist_ok=True)
    pdf.output(str(OUT))
    return OUT


if __name__ == "__main__":
    path = build()
    print(f"Wrote {path}")
