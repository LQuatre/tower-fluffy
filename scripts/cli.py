from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
SOLUTION = REPO_ROOT / "TowerFluffy.slnx"
UI_PROJECT = REPO_ROOT / "src" / "UI.Desktop" / "TowerFluffy.UI.Desktop.csproj"


def run(command: list[str]) -> None:
    print("+", " ".join(command))
    subprocess.run(command, cwd=REPO_ROOT, check=True)


def cmd_restore() -> None:
    run(["dotnet", "restore", str(SOLUTION)])


def cmd_build(configuration: str) -> None:
    run(["dotnet", "build", str(SOLUTION), "-c", configuration])


def cmd_test(configuration: str) -> None:
    run(["dotnet", "test", str(SOLUTION), "-c", configuration])


def cmd_run_ui(configuration: str) -> None:
    run(["dotnet", "run", "--project", str(UI_PROJECT), "-c", configuration])


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="command", required=True)

    sub.add_parser("restore")

    build = sub.add_parser("build")
    build.add_argument("--configuration", default="Release")

    test = sub.add_parser("test")
    test.add_argument("--configuration", default="Release")

    run_ui = sub.add_parser("run-ui")
    run_ui.add_argument("--configuration", default="Debug")

    return parser


def main(argv: list[str]) -> int:
    args = build_parser().parse_args(argv)

    if args.command == "restore":
        cmd_restore()
        return 0

    if args.command == "build":
        cmd_build(args.configuration)
        return 0

    if args.command == "test":
        cmd_test(args.configuration)
        return 0

    if args.command == "run-ui":
        cmd_run_ui(args.configuration)
        return 0

    raise NotImplementedError(args.command)


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
