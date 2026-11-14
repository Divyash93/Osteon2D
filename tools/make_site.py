"""Builds docs/runtime-guide.html (the GitHub Pages runtime user guide) from docs/runtimes/USER_GUIDE.md,
and makes sure every page's navigation links to it.

Run after editing the guide:  python tools/make_site.py
The page shell (head, nav, footer) is taken from docs/runtimes.html, so the guide matches the rest of the site.
A small Markdown converter is included (headings, paragraphs, lists, tables, code blocks, quotes, inline code,
bold, italic, links), enough for the guide; no extra Python packages are needed.
"""
import html, os, re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DOCS = os.path.join(ROOT, "docs")
GH = "https://github.com/Divyash93/Osteon2D"
GUIDE_MD = os.path.join(DOCS, "runtimes", "USER_GUIDE.md")
OUT = os.path.join(DOCS, "runtime-guide.html")
NAV_LINK = '<a href="runtime-guide.html">Runtime guide</a>'


def slug(text):
    return re.sub(r"[^a-z0-9]+", "-", re.sub(r"<[^>]+>", "", text).lower()).strip("-")


def fix_link(url):
    # Repository-relative links in the guide point at GitHub; absolute links stay as they are.
    if re.match(r"^[a-z]+://", url) or url.startswith("#") or url.startswith("mailto:"):
        return url
    path = os.path.normpath(os.path.join("docs/runtimes", url)).replace("\\", "/")
    kind = "tree" if not os.path.splitext(path)[1] else "blob"
    return f"{GH}/{kind}/main/{path}"


def inline(text):
    # Links first, so link text may itself contain `code`.
    m = re.search(r"\[([^\]]+)\]\(([^)\s]+)\)", text)
    if m:
        return inline(text[:m.start()]) + f'<a href="{fix_link(m.group(2))}">{inline(m.group(1))}</a>' + inline(text[m.end():])
    m = re.search(r"\*\*(.+?)\*\*", text)   # bold may wrap `code` too
    if m:
        return inline(text[:m.start()]) + "<b>" + inline(m.group(1)) + "</b>" + inline(text[m.end():])
    parts = re.split(r"(`[^`]+`)", text)
    out = []
    for p in parts:
        if p.startswith("`") and p.endswith("`") and len(p) > 1:
            out.append("<code>" + html.escape(p[1:-1]) + "</code>")
            continue
        p = html.escape(p, quote=False)
        p = re.sub(r"&lt;(https?://[^&\s]+)&gt;", lambda m: f'<a href="{m.group(1)}">{m.group(1)}</a>', p)
        p = re.sub(r"\[([^\]]+)\]\(([^)]+)\)", lambda m: f'<a href="{fix_link(m.group(2))}">{m.group(1)}</a>', p)
        p = re.sub(r"\*\*([^*]+)\*\*", r"<b>\1</b>", p)
        p = re.sub(r"(?<![\w*])\*([^*\s][^*]*)\*(?![\w*])", r"<i>\1</i>", p)
        out.append(p)
    return "".join(out)


def table(rows):
    cells = [[c.strip() for c in r.strip().strip("|").split("|")] for r in rows]
    head, body = cells[0], [r for r in cells[2:]]
    h = "<tr>" + "".join(f"<th>{inline(c)}</th>" for c in head) + "</tr>"
    b = "".join("<tr>" + "".join(f"<td>{inline(c)}</td>" for c in r) + "</tr>" for r in body)
    return f"<table>{h}{b}</table>"


def convert(md):
    lines = md.replace("\r\n", "\n").split("\n")
    out, toc, used = [], [], {}
    i = 0
    while i < len(lines):
        line = lines[i]
        if line.startswith("```"):
            lang = line[3:].strip()
            i += 1
            code = []
            while i < len(lines) and not lines[i].startswith("```"):
                code.append(lines[i]); i += 1
            i += 1
            out.append(f'<pre><code class="lang-{lang}">' + html.escape("\n".join(code)) + "</code></pre>")
            continue
        m = re.match(r"^(#{1,4})\s+(.*)", line)
        if m:
            level, text = len(m.group(1)), inline(m.group(2))
            sid = slug(text)
            used[sid] = used.get(sid, 0) + 1
            if used[sid] > 1:
                sid += "-" + str(used[sid])   # e.g. the second "Control it from a script"
            if level == 2:
                toc.append((sid, re.sub(r"<[^>]+>", "", text)))
            out.append(f'<h{level} id="{sid}">{text}</h{level}>')
            i += 1
            continue
        if line.strip() == "---":
            out.append("<hr>"); i += 1; continue
        if line.startswith("|"):
            rows = []
            while i < len(lines) and lines[i].startswith("|"):
                rows.append(lines[i]); i += 1
            out.append(table(rows))
            continue
        if line.startswith(">"):
            quote = []
            while i < len(lines) and lines[i].startswith(">"):
                quote.append(lines[i].lstrip(">").strip()); i += 1
            out.append('<p class="warn">' + inline(" ".join(quote)) + "</p>")
            continue
        if re.match(r"^(\d+\.|-)\s", line):
            # A list, possibly with indented sub-items or continuation lines.
            ordered = bool(re.match(r"^\d+\.", line))
            items = []
            while i < len(lines) and (re.match(r"^(\d+\.|-)\s", lines[i]) or (lines[i].startswith("   ") and lines[i].strip())):
                l = lines[i]
                if re.match(r"^(\d+\.|-)\s", l):
                    items.append([re.sub(r"^(\d+\.|-)\s+", "", l), []])
                else:
                    sub = l.strip()
                    if sub.startswith("- "):
                        items[-1][1].append(sub[2:])
                    else:
                        items[-1][0] += " " + sub
                i += 1
            tag = "ol" if ordered else "ul"
            body = ""
            for text, subs in items:
                subhtml = "<ul>" + "".join(f"<li>{inline(s)}</li>" for s in subs) + "</ul>" if subs else ""
                body += f"<li>{inline(text)}{subhtml}</li>"
            out.append(f"<{tag}>{body}</{tag}>")
            continue
        if not line.strip():
            i += 1
            continue
        para = []
        while i < len(lines) and lines[i].strip() and not re.match(r"^(#|\||>|```|---|\d+\.\s|-\s)", lines[i]):
            para.append(lines[i].strip()); i += 1
        out.append("<p>" + inline(" ".join(para)) + "</p>")
    return "\n".join(out), toc


def main():
    md = open(GUIDE_MD, encoding="utf-8").read()
    body, toc = convert(md)
    shell = open(os.path.join(DOCS, "runtimes.html"), encoding="utf-8").read()
    # The shell: everything up to the content wrapper after the nav, and the footer onward.
    nav_end = shell.index("</nav>") + len("</nav>")
    start = shell[: nav_end] + '<div class="wrap">'
    foot = shell[shell.index("<footer>"):]
    start = start.replace("<title>Runtimes - Osteon2D</title>", "<title>Runtime guide - Osteon2D</title>")
    start = re.sub(r'<meta name="description" content="[^"]*">', '<meta name="description" content="Step-by-step guide to playing Osteon2D animations in Godot and Unity.">', start)
    toc_html = '<p class="toc">' + " &middot; ".join(f'<a href="#{s}">{html.escape(t)}</a>' for s, t in toc) + "</p>"
    edit = f'<p style="color:var(--mute);font-size:14px">This page is built from <a href="{GH}/blob/main/docs/runtimes/USER_GUIDE.md">docs/runtimes/USER_GUIDE.md</a>.</p>'
    # Put the table of contents right after the title.
    first_h1 = body.index("</h1>") + len("</h1>")
    page = start + body[:first_h1] + toc_html + body[first_h1:] + edit + foot
    open(OUT, "w", encoding="utf-8").write(page)

    # Every page links to the guide, right after Runtimes; the guide page marks itself as current.
    for name in os.listdir(DOCS):
        if not name.endswith(".html"):
            continue
        p = os.path.join(DOCS, name)
        s = open(p, encoding="utf-8").read()
        s = s.replace(' class="cur"', "") if name == "runtime-guide.html" else s
        if NAV_LINK not in s and 'href="runtime-guide.html" class="cur"' not in s:
            s = re.sub(r'(<a href="runtimes.html"( class="cur")?>Runtimes</a>)', r"\1" + NAV_LINK, s, count=1)
        if name == "runtime-guide.html":
            s = s.replace(NAV_LINK, '<a href="runtime-guide.html" class="cur">Runtime guide</a>')
        open(p, "w", encoding="utf-8").write(s)
    print("wrote", OUT, len(page), "bytes;", len(toc), "sections")


if __name__ == "__main__":
    main()
