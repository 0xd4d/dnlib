#!/usr/bin/env python3
"""Writes an HTML report that shows dnlib's source code with the covered and the uncovered lines.

usage: coverage-html.py [--output <directory>] <dnlib's src directory> <cobertura file>...

One page per source file (every class in the file is shown) and an index with the numbers per file and per
directory. The pages need no other files and no network.

The input files are Cobertura XML files written by dotnet-coverage (see coverage.sh). If there are several
(one per test leg or OS), a line counts as covered if any of them covers it. To merge the CI runs, download
the coverage-Ubuntu and coverage-Windows artifacts and pass both net10.0.cobertura.xml files, with the same
commit checked out.

The source code is read from the src directory: it must be the code the tests ran with, or the colors are on
the wrong lines.
"""
import argparse
import collections
import html
import os
import re
import sys
import xml.etree.ElementTree as ET


class Line:
    """Coverage of one source line"""
    __slots__ = ("covered", "class_branches")

    def __init__(self):
        self.covered = False
        # class name -> [covered branches, branches]. Almost always one class, but the code of a lambda is in
        # another class than the rest of the line.
        self.class_branches = {}

    @property
    def branches(self):
        return sum(branches for _, branches in self.class_branches.values())

    @property
    def covered_branches(self):
        return sum(covered for covered, _ in self.class_branches.values())

    @property
    def state(self):
        if not self.covered:
            return "u"
        if self.covered_branches < self.branches:
            return "p"
        return "c"


CONDITION_COVERAGE = re.compile(r"\((\d+)/(\d+)\)")


def read_coverage(source_directory, cobertura_files):
    """Returns {source file: {line number: Line}} of all files merged.

    Only files in source_directory are returned: the reports also have the code of other programs that ran
    (eg. the build tools).
    """
    source_directory = source_directory.replace("\\", "/").rstrip("/") + "/"
    # A build on GitHub Actions (ContinuousIntegrationBuild) stores the path of the repository as "/_/". Other
    # programs in the report (eg. msbuild) were built like that too, so the file must exist in dnlib.
    mapped_directory = "/_/" + os.path.basename(source_directory.rstrip("/")) + "/"
    files = collections.defaultdict(dict)
    for cobertura_file in cobertura_files:
        for _, element in ET.iterparse(cobertura_file, events=("end",)):
            if element.tag != "class":
                continue
            path = (element.get("filename") or "").replace("\\", "/")
            source_file = None
            if path.startswith(source_directory):
                source_file = path[len(source_directory):]
            elif path.startswith(mapped_directory) and os.path.isfile(source_directory + path[len(mapped_directory):]):
                source_file = path[len(mapped_directory):]
            if source_file is not None:
                lines = files[source_file]
                class_name = element.get("name")
                # The lines of the class: the methods have the same lines again
                for line_element in element.findall("lines/line"):
                    number = int(line_element.get("number"))
                    line = lines.get(number)
                    if line is None:
                        line = lines[number] = Line()
                    if int(line_element.get("hits")) > 0:
                        line.covered = True
                    # eg. "50% (2/4)"
                    match = CONDITION_COVERAGE.search(line_element.get("condition-coverage") or "")
                    if match:
                        covered, branches = int(match.group(1)), int(match.group(2))
                        old = line.class_branches.get(class_name)
                        # Two reports could cover different branches of the line, which ones isn't known: the
                        # highest number is used
                        if old is None or old[1] != branches or covered > old[0]:
                            line.class_branches[class_name] = [covered, branches]
            element.clear()
    return files


class Totals:
    __slots__ = ("lines", "covered", "branches", "covered_branches")

    def __init__(self, lines=None):
        self.lines = self.covered = self.branches = self.covered_branches = 0
        for line in (lines or {}).values():
            self.lines += 1
            self.covered += line.covered
            self.branches += line.branches
            self.covered_branches += line.covered_branches

    def add(self, other):
        self.lines += other.lines
        self.covered += other.covered
        self.branches += other.branches
        self.covered_branches += other.covered_branches


def percent(covered, total):
    return 100 * covered / total if total else 100.0


def percent_text(covered, total):
    return f"{percent(covered, total):.1f}%" if total else "–"


# The line terminators of C#. str.splitlines() also splits at other characters (eg. form feed), the line
# numbers would then be wrong
NEW_LINE = re.compile("\r\n|[\r\n\u0085  ]")

# C# syntax highlighting. The token kinds are also the CSS classes: k keyword, t name of a declared type,
# s string or char, n number, m comment, d doc comment, r preprocessor directive, "" the rest.
# The code isn't parsed, eg. a contextual keyword such as "var" is colored wherever it's used as a name.
KEYWORDS = frozenset("""
abstract as base bool break byte case catch char checked class const continue decimal default delegate do
double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface
internal is lock long namespace new null object operator out override params private protected public readonly
ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof uint
ulong unchecked unsafe ushort using virtual void volatile while
and async await dynamic global managed nameof nint not notnull nuint or partial record required scoped
unmanaged var when where with yield
""".split())
# Only keywords before these: property and event accessors
ACCESSOR_KEYWORDS = frozenset("get set init add remove".split())
ACCESSOR_END = re.compile(r"\s*(?:[;{]|=>)")
# The next name is the name of the new type
TYPE_KEYWORDS = frozenset("class struct interface enum record".split())
TYPE_NAME = re.compile(r"(\s+)(@?[^\W\d]\w*)")
_TOKEN = r"""
  (?P<d>///[^\n]*)
| (?P<m>//[^\n]*|/\*.*?(?:\*/|\Z))
| (?P<raw>\$*"{3,})
| (?P<interpolated>\$@"|@\$"|\$")
| (?P<s>@"(?:[^"]|"")*(?:"|\Z)
      |"(?:\\.|[^"\\\n])*(?:"|(?=\n)|\Z)
      |'(?:\\(?:x[0-9A-Fa-f]{1,4}|u[0-9A-Fa-f]{4}|U[0-9A-Fa-f]{8}|.)|[^\\'\n])')
| (?P<n>\b(?:0[xX][0-9A-Fa-f_]+|0[bB][01_]+|[0-9][0-9_]*(?:\.[0-9][0-9_]*)?(?:[eE][+-]?[0-9]+)?)[uUlLfFdDmM]*)
| (?P<name>@?[^\W\d]\w*)
| (?P<r>(?:(?<=\n)|\A)[ \t]*\#[^\n]*)
"""
TOKEN = re.compile(_TOKEN, re.S | re.X)
# In the {} of an interpolated string, the end must be found: the '}' that isn't in (), [] or {}, or a ':'
# there, it starts the format string (eg. {value:X8})
HOLE_TOKEN = re.compile(_TOKEN + r"| (?P<open>[(\[{]) | (?P<close>[)\]}]) | (?P<colon>(?<!:):(?!:))", re.S | re.X)


def tokenize(text, tokens, position=0, in_hole=False):
    """Appends (kind, text) of all of text to tokens. text has '\n' line terminators only.

    in_hole: text[position] is the first char in the {} of an interpolated string, it returns at its end.
    Returns the new position.
    """
    pattern = HOLE_TOKEN if in_hole else TOKEN
    depth = 0
    while True:
        match = pattern.search(text, position)
        if match is None:
            tokens.append(("", text[position:]))
            return len(text)
        if match.start() != position:
            tokens.append(("", text[position:match.start()]))
        kind = match.lastgroup
        value = match.group()
        position = match.end()
        if kind == "name":
            if value in KEYWORDS or (value in ACCESSOR_KEYWORDS and ACCESSOR_END.match(text, position)):
                tokens.append(("k", value))
                type_name = TYPE_NAME.match(text, position) if value in TYPE_KEYWORDS else None
                if type_name and type_name.group(2) not in KEYWORDS:
                    tokens.append(("", type_name.group(1)))
                    tokens.append(("t", type_name.group(2)))
                    position = type_name.end()
            else:
                tokens.append(("", value))
        elif kind == "interpolated":
            position = tokenize_interpolated_string(text, tokens, match.start(), position, "@" in value)
        elif kind == "raw":
            # Raw string: it ends at as many quotes as it starts with. An interpolated one isn't split.
            quotes = value.lstrip("$")
            end = re.compile('(?<!")' + quotes + '(?!")').search(text, position)
            position = end.end() if end else len(text)
            tokens.append(("s", text[match.start():position]))
        elif kind == "open":
            depth += 1
            tokens.append(("", value))
        elif kind == "close" or kind == "colon":
            if depth == 0:
                return match.start()
            if kind == "close":
                depth -= 1
            tokens.append(("", value))
        else:
            tokens.append((kind, value))


def tokenize_interpolated_string(text, tokens, start, position, verbatim):
    """Appends the tokens of the interpolated string at text[start], its text starts at text[position]. The
    code in its {} isn't a string. Returns the position after the string."""
    while position < len(text):
        char = text[position]
        if char == '"':
            if verbatim and text.startswith('""', position):
                position += 2
                continue
            position += 1
            break
        if char == "\n" and not verbatim:
            break
        if char == "\\" and not verbatim:
            position += 2
        elif char == "{" and not text.startswith("{{", position):
            tokens.append(("s", text[start:position]))
            tokens.append(("", "{"))
            position = start = tokenize(text, tokens, position + 1, in_hole=True)
            # The format string, or code that wasn't understood: it's part of the string up to the '}'
            while position < len(text) and text[position] not in '}"\n':
                position += 1
            if position < len(text) and text[position] == "}":
                tokens.append(("s", text[start:position]))
                tokens.append(("", "}"))
                position = start = position + 1
        elif char in "{}":
            position += 2
        else:
            position += 1
    position = min(position, len(text))
    tokens.append(("s", text[start:position]))
    return position


def highlight_lines(text):
    """Returns the HTML of every line of text, which has '\n' line terminators only"""
    tokens = []
    try:
        tokenize(text, tokens)
        ok = "".join(value for _, value in tokens) == text
    except RecursionError:
        ok = False
    if not ok:
        # A bug in the tokenizer must not lose or change code
        return None
    lines = []
    line = []
    for kind, value in tokens:
        pieces = value.split("\n")
        for index, piece in enumerate(pieces):
            if index != 0:
                lines.append("".join(line))
                line = []
            if piece:
                escaped = html.escape(piece, quote=False)
                line.append(f"<i class={kind}>{escaped}</i>" if kind else escaped)
    lines.append("".join(line))
    return lines


STYLE = """
:root { color-scheme: light dark; --bg: #fff; --fg: #1f2328; --dim: #6e7781; --border: #d0d7de; --head: #f6f8fa;
  --c: #d7f5dd; --u: #ffd8d3; --p: #fff1b8; --cn: #1a7f37; --un: #cf222e; --pn: #9a6700; --link: #0969da;
  /* The code: they must be readable on the --c, --u and --p backgrounds too */
  --keyword: #0033b3; --type: #00627a; --string: #8c3a00; --number: #7a3e9d; --comment: #5c6670; --directive: #6f42c1; }
@media (prefers-color-scheme: dark) { :root { --bg: #0d1117; --fg: #e6edf3; --dim: #8b949e; --border: #30363d;
  --head: #161b22; --c: #12361f; --u: #4a1b1b; --p: #3f3200; --cn: #3fb950; --un: #f85149; --pn: #d29922; --link: #58a6ff;
  --keyword: #79c0ff; --type: #56d4dd; --string: #ffa657; --number: #d2a8ff; --comment: #9aa4af; --directive: #d2a8ff; } }
body { margin: 0; background: var(--bg); color: var(--fg); font: 14px system-ui, sans-serif; }
a { color: var(--link); text-decoration: none; } a:hover { text-decoration: underline; }
header { position: sticky; top: 0; z-index: 1; background: var(--head); border-bottom: 1px solid var(--border);
  padding: 8px 16px; display: flex; flex-wrap: wrap; gap: 6px 18px; align-items: baseline; }
header h1 { font-size: 15px; margin: 0; } header .dim, .dim { color: var(--dim); }
button { font: inherit; padding: 1px 8px; }
main { padding: 12px 16px; }
table { border-collapse: collapse; margin-bottom: 24px; }
th, td { padding: 3px 10px; border-bottom: 1px solid var(--border); text-align: right; white-space: nowrap; }
th:first-child, td:first-child { text-align: left; }
th { background: var(--head); cursor: pointer; user-select: none; }
tr.total td { font-weight: 600; }
.bar { display: inline-block; width: 90px; height: 9px; background: var(--un); vertical-align: middle; }
.bar i { display: block; height: 100%; background: var(--cn); }
/* The same width for every percentage ("100.0%" is the widest), or the bars left of it wouldn't line up */
.pct { display: inline-block; width: 6ch; margin-left: 1ch; text-align: right; font-variant-numeric: tabular-nums; }
td { font-variant-numeric: tabular-nums; }
input[type=search] { font: inherit; padding: 2px 6px; width: 260px; }
pre { margin: 0; font: 12.5px/1.45 ui-monospace, "Cascadia Mono", Consolas, "DejaVu Sans Mono", monospace; tab-size: 4;
  counter-reset: line; width: max-content; min-width: 100%; }
pre span { display: block; counter-increment: line; padding-right: 16px; scroll-margin-top: 120px; }
/* The gutter: covered/all branches of the line (if it has any) and the line number. It isn't copied with the code.
   Its width (with the margin) is a multiple of the tab size, or the first tab of a line would be shorter. */
pre span::before { content: attr(data-b) "  " counter(line); display: inline-block; box-sizing: border-box; width: 15ch;
  padding-right: 1ch; margin-right: 1ch; text-align: right; color: var(--dim); user-select: none; border-right: 3px solid transparent; }
.c { background: var(--c); } .u { background: var(--u); } .p { background: var(--p); }
pre span.c::before { border-right-color: var(--cn); } pre span.u::before { border-right-color: var(--un); }
pre span.p::before { border-right-color: var(--pn); }
pre i { font-style: normal; } .k { color: var(--keyword); } .t { color: var(--type); } .s { color: var(--string); }
.n { color: var(--number); } .m, .d { color: var(--comment); font-style: italic; } .r { color: var(--directive); }
/* The block the prev/next buttons jumped to */
pre span.cur::before { color: var(--fg); font-weight: 700; border-right-width: 7px; padding-right: calc(1ch - 4px); }
pre span:target { outline: 2px solid var(--link); outline-offset: -2px; }
.legend span { padding: 0 6px; margin-right: 4px; }
"""

FILE_SCRIPT = """
(function () {
  // The blocks of lines next to each other that are not covered or have branches that aren't covered
  var blocks = [], block = null;
  document.querySelectorAll('pre span').forEach(function (line) {
    var bad = line.classList.contains('u') || line.classList.contains('p');
    if (!bad) block = null;
    else if (block) block.push(line);
    else blocks.push(block = [line]);
  });
  var current = -1, scrollAfter = -1;
  // Where the first line of a block is put: a third down the window, so the code above it is seen too (but
  // always below the header)
  function topOffset() {
    return Math.max(document.querySelector('header').offsetHeight + 2 * blocks[0][0].offsetHeight, Math.round(window.innerHeight / 3));
  }
  function go(direction) {
    if (!blocks.length) return;
    var index = -1, i;
    if (current >= 0 && Math.abs(window.scrollY - scrollAfter) < 2) {
      // Not scrolled since the last jump. The position can't be used: at the end of the file, a block can't
      // be scrolled to the top
      index = current + direction;
    }
    else {
      var position = window.scrollY + topOffset();
      if (direction > 0) { for (i = 0; i < blocks.length; i++) if (blocks[i][0].offsetTop > position + 2) { index = i; break; } }
      else { for (i = blocks.length - 1; i >= 0; i--) if (blocks[i][0].offsetTop < position - 2) { index = i; break; } }
    }
    if (index < 0 || index >= blocks.length) return;
    if (current >= 0) blocks[current].forEach(function (line) { line.classList.remove('cur'); });
    current = index;
    blocks[current].forEach(function (line) { line.classList.add('cur'); });
    window.scrollTo(window.scrollX, Math.max(0, blocks[current][0].offsetTop - topOffset()));
    scrollAfter = window.scrollY;
    document.getElementById('position').textContent = (current + 1) + ' of ' + blocks.length;
  }
  document.getElementById('position').textContent = blocks.length + (blocks.length === 1 ? ' block' : ' blocks');
  document.getElementById('next').onclick = function () { go(1); };
  document.getElementById('prev').onclick = function () { go(-1); };
  document.addEventListener('keydown', function (e) {
    if (e.ctrlKey || e.altKey || e.metaKey || /input|textarea/i.test(e.target.tagName)) return;
    if (e.key === 'n') go(1); else if (e.key === 'p') go(-1);
  });
  // Line numbers aren't elements: a click left of the code sets the #L anchor
  document.querySelector('pre').addEventListener('click', function (e) {
    var line = e.target.closest('pre span');
    if (line && e.target === line && e.offsetX < 100 && !String(window.getSelection())) location.replace('#' + line.id);
  });
})();
"""

INDEX_SCRIPT = """
(function () {
  document.querySelectorAll('table').forEach(function (table) {
    var body = table.tBodies[0];
    table.querySelectorAll('th').forEach(function (th, column) {
      var descending = column !== 0;
      th.onclick = function () {
        var rows = Array.prototype.slice.call(body.rows);
        rows.sort(function (a, b) {
          var x = a.cells[column].dataset.v, y = b.cells[column].dataset.v;
          var result = column === 0 ? x.localeCompare(y) : parseFloat(x) - parseFloat(y);
          return descending ? -result : result;
        });
        descending = !descending;
        rows.forEach(function (row) { body.appendChild(row); });
      };
    });
  });
  var filter = document.getElementById('filter');
  filter.oninput = function () {
    var text = filter.value.toLowerCase();
    document.querySelectorAll('#files tbody tr').forEach(function (row) {
      row.style.display = row.cells[0].dataset.v.toLowerCase().indexOf(text) >= 0 ? '' : 'none';
    });
  };
})();
"""


def page(title, header, body, script, style_path):
    return (f'<!DOCTYPE html>\n<html lang="en"><head><meta charset="utf-8"><title>{html.escape(title)}</title>'
            f'<meta name="viewport" content="width=device-width, initial-scale=1">'
            f'<link rel="stylesheet" href="{style_path}"></head>\n<body><header>{header}</header>\n{body}\n'
            f'<script>{script}</script></body></html>\n')


def totals_text(totals):
    return (f'<span>lines: <b>{percent_text(totals.covered, totals.lines)}</b> '
            f'<span class="dim">({totals.covered} of {totals.lines}, {totals.lines - totals.covered} uncovered)</span></span>'
            f'<span>branches: <b>{percent_text(totals.covered_branches, totals.branches)}</b> '
            f'<span class="dim">({totals.covered_branches} of {totals.branches})</span></span>')


def write_file_page(output_directory, source_directory, source_file, lines, totals):
    depth = source_file.count("/") + 1
    with open(os.path.join(source_directory, source_file), encoding="utf-8-sig", errors="replace") as file:
        text = NEW_LINE.sub("\n", file.read())
    source_lines = highlight_lines(text)
    if source_lines is None:
        print(f"warning: {source_file}: the syntax highlighting failed, the code is shown without it", file=sys.stderr)
        source_lines = [html.escape(line, quote=False) for line in text.split("\n")]
    if source_lines and source_lines[-1] == "":
        source_lines.pop()
    last_line = max(lines) if lines else 0
    if last_line > len(source_lines):
        print(f"warning: {source_file} has {len(source_lines)} lines, the report has line {last_line}: is it the same code?", file=sys.stderr)
        source_lines += [""] * (last_line - len(source_lines))

    parts = []
    for number, text in enumerate(source_lines, 1):  # text is HTML
        line = lines.get(number)
        attributes = f' id="L{number}"'
        if line is not None:
            attributes += f' class="{line.state}"'
            if line.branches:
                attributes += f' data-b="{line.covered_branches}/{line.branches}"'
        parts.append(f"<span{attributes}>{text}\n</span>")

    header = (f'<h1><a href="{"../" * depth}index.html">coverage</a> / {html.escape(source_file)}</h1>{totals_text(totals)}'
              f'<span><button id="prev" title="Previous uncovered code (p)">▲ prev</button> '
              f'<button id="next" title="Next uncovered code (n)">▼ next</button> <span class="dim">red or yellow block (keys: p, n): <span id="position"></span></span></span>'
              f'<span class="legend"><span class="c">covered</span><span class="p">some branches not covered</span>'
              f'<span class="u">not covered</span></span>')
    body = f'<pre>{"".join(parts)}</pre>'
    path = os.path.join(output_directory, "files", source_file + ".html")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as file:
        file.write(page(source_file, header, body, FILE_SCRIPT, "../" * depth + "style.css"))


def table_row(name, link, totals, css_class=""):
    line_percent = percent(totals.covered, totals.lines)
    name_html = f'<a href="{html.escape(link)}">{html.escape(name)}</a>' if link else html.escape(name)
    return (f'<tr class="{css_class}"><td data-v="{html.escape(name)}">{name_html}</td>'
            f'<td data-v="{line_percent:.3f}"><span class="bar"><i style="width:{line_percent:.1f}%"></i></span><span class="pct">{percent_text(totals.covered, totals.lines)}</span></td>'
            f'<td data-v="{totals.lines - totals.covered}">{totals.lines - totals.covered}</td>'
            f'<td data-v="{totals.covered}">{totals.covered}</td><td data-v="{totals.lines}">{totals.lines}</td>'
            f'<td data-v="{percent(totals.covered_branches, totals.branches):.3f}">{percent_text(totals.covered_branches, totals.branches)}</td>'
            f'<td data-v="{totals.branches - totals.covered_branches}">{totals.branches - totals.covered_branches}</td>'
            f'<td data-v="{totals.branches}">{totals.branches}</td></tr>')


TABLE_HEAD = ('<thead><tr><th>{}</th><th>lines covered</th><th>uncovered</th><th>covered</th><th>lines</th>'
              '<th>branches covered</th><th>uncovered</th><th>branches</th></tr></thead>')


def write_index(output_directory, file_totals, cobertura_files):
    total = Totals()
    directories = collections.defaultdict(Totals)
    for source_file, totals in file_totals.items():
        total.add(totals)
        directories[os.path.dirname(source_file) or "(root)"].add(totals)

    directory_rows = "".join(table_row(name, None, totals) for name, totals in sorted(directories.items()))
    file_rows = "".join(table_row(name, "files/" + name + ".html", totals) for name, totals in sorted(file_totals.items()))
    header = f'<h1>dnlib test coverage</h1>{totals_text(total)}<span class="dim">{len(file_totals)} files, {len(cobertura_files)} report(s) merged</span>'
    body = (f'<main><h2>Directories</h2><table>{TABLE_HEAD.format("directory")}<tbody>{directory_rows}</tbody>'
            f'<tfoot>{table_row("TOTAL", None, total, "total")}</tfoot></table>'
            f'<h2>Files</h2><p><input type="search" id="filter" placeholder="Filter files"> '
            f'<span class="dim">Click a column to sort</span></p>'
            f'<table id="files">{TABLE_HEAD.format("file")}<tbody>{file_rows}</tbody></table></main>')
    with open(os.path.join(output_directory, "index.html"), "w", encoding="utf-8", newline="\n") as file:
        file.write(page("dnlib test coverage", header, body, INDEX_SCRIPT, "style.css"))
    return total


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--output", default="coverage/html", help="directory of the report (default: coverage/html)")
    parser.add_argument("source_directory", help="full path of dnlib's src directory")
    parser.add_argument("cobertura_files", nargs="+")
    args = parser.parse_args()

    files = read_coverage(args.source_directory, args.cobertura_files)
    if not files:
        print("error: the reports have no file of the source directory", file=sys.stderr)
        return 1
    os.makedirs(args.output, exist_ok=True)
    with open(os.path.join(args.output, "style.css"), "w", encoding="utf-8", newline="\n") as file:
        file.write(STYLE)
    file_totals = {}
    for source_file, lines in files.items():
        file_totals[source_file] = Totals(lines)
        write_file_page(args.output, args.source_directory, source_file, lines, file_totals[source_file])
    total = write_index(args.output, file_totals, args.cobertura_files)
    print(f"HTML report: {os.path.abspath(os.path.join(args.output, 'index.html'))} "
          f"({len(files)} files, {percent_text(total.covered, total.lines)} of the lines covered)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
