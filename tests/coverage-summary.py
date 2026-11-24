#!/usr/bin/env python3
"""Prints dnlib's line coverage per source directory and the files with the most uncovered lines.

usage: coverage-summary.py [--files N] <dnlib's src directory> <cobertura file>...

The input files are Cobertura XML files written by dotnet-coverage (see coverage.sh). If there are several
(one per test leg), a line counts as covered if any of them covers it: code in #if blocks only exists in
some builds of dnlib.
"""
import argparse
import sys
import collections
import xml.etree.ElementTree as ET

def read_line_hits(source_directory, cobertura_files):
    """Returns {source file: {line number: True if covered}} of all files merged.

    Only files in source_directory are returned: the reports also have the code of other programs that ran
    (eg. the build tools).
    """
    source_directory = source_directory.replace("\\", "/").rstrip("/") + "/"
    hits = collections.defaultdict(dict)
    for cobertura_file in cobertura_files:
        for _, element in ET.iterparse(cobertura_file, events=("end",)):
            if element.tag != "class":
                continue
            path = (element.get("filename") or "").replace("\\", "/")
            if path.startswith(source_directory):
                source_file = path[len(source_directory):]
                lines = hits[source_file]
                for line in element.iter("line"):
                    number = int(line.get("number"))
                    lines[number] = lines.get(number, False) or int(line.get("hits")) > 0
            element.clear()
    return hits


def get_group(source_file):
    """The directory a file is counted in, eg. DotNet/Writer. The PDB code is split by its sub directory."""
    parts = source_file.split("/")
    if len(parts) == 1:
        return "(root)"
    if parts[0] == "DotNet":
        if len(parts) == 2:
            return "DotNet (top level)"
        if parts[1] == "Pdb" and len(parts) > 3:
            return "/".join(parts[:3])
        return "/".join(parts[:2])
    return parts[0]


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--files", type=int, default=30, help="number of files to list (default: 30)")
    parser.add_argument("--minimum", type=float, default=0, help="fail if less than this percentage of all lines is covered")
    parser.add_argument("source_directory", help="full path of dnlib's src directory")
    parser.add_argument("cobertura_files", nargs="+")
    args = parser.parse_args()

    hits = read_line_hits(args.source_directory, args.cobertura_files)

    groups = collections.defaultdict(lambda: [0, 0])  # group -> [covered lines, lines]
    for source_file, lines in hits.items():
        group = groups[get_group(source_file)]
        group[0] += sum(lines.values())
        group[1] += len(lines)

    print(f"{'directory':32} {'lines':>7} {'covered':>8} {'%':>6}")
    for name, (covered, total) in sorted(groups.items(), key=lambda item: -item[1][1]):
        print(f"{name:32} {total:7} {covered:8} {100 * covered / max(total, 1):6.1f}")
    covered = sum(group[0] for group in groups.values())
    total = sum(group[1] for group in groups.values())
    print(f"{'TOTAL':32} {total:7} {covered:8} {100 * covered / max(total, 1):6.1f}")

    print("\nfiles with the most uncovered lines:")
    rows = sorted(((len(lines) - sum(lines.values()), len(lines), source_file) for source_file, lines in hits.items()), reverse=True)
    for uncovered, total, source_file in rows[:args.files]:
        print(f"  {uncovered:5} of {total:5} uncovered ({100 * (total - uncovered) / max(total, 1):5.1f}% covered)  {source_file}")

    percent = 100 * covered / max(total, 1)
    if percent < args.minimum:
        print(f"\nerror: {percent:.1f}% of the lines are covered, the minimum is {args.minimum}%", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
