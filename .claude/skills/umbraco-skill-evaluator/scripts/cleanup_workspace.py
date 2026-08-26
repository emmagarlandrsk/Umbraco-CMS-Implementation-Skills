#!/usr/bin/env python3
"""Safely remove a completed skill-evaluator workspace."""

import argparse
import shutil
import sys
from pathlib import Path


def validate_workspace(path: Path) -> None:
    if not path.exists() or not path.is_dir():
        raise ValueError(f"Workspace does not exist or is not a directory: {path}")
    if not path.name.endswith("-workspace"):
        raise ValueError("Refusing to clean a directory whose name does not end with '-workspace'.")
    if not any(path.glob("iteration-*/")):
        raise ValueError("Refusing to clean a directory without an iteration-* child directory.")
    if not (path / "iteration-1" / "benchmark.json").exists() and not any(
        path.glob("iteration-*/benchmark.json")
    ):
        raise ValueError("Refusing to clean a directory without evaluator benchmark output.")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("workspace", type=Path)
    parser.add_argument(
        "--yes",
        action="store_true",
        help="Delete the validated workspace.",
    )
    parser.add_argument(
        "--final-review-complete",
        action="store_true",
        help="Confirm that qualitative review and feedback processing are complete.",
    )
    parser.add_argument(
        "--clean-python-caches",
        action="store_true",
        help="Also remove __pycache__ directories and *.pyc files beneath the workspace parent.",
    )
    args = parser.parse_args()
    workspace = args.workspace.resolve()

    try:
        validate_workspace(workspace)
    except ValueError as error:
        print(f"error: {error}", file=sys.stderr)
        return 2

    if not args.yes:
        print(f"Validated evaluator workspace; nothing deleted: {workspace}")
        print("After review is complete, re-run with --yes --final-review-complete to remove it.")
        return 0

    if not args.final_review_complete:
        print(
            "Refusing to delete before final review confirmation; "
            "add --final-review-complete after feedback processing is complete.",
            file=sys.stderr,
        )
        return 2

    shutil.rmtree(workspace)
    print(f"Removed evaluator workspace: {workspace}")

    if args.clean_python_caches:
        cache_root = workspace.parent
        cache_dirs = [path for path in cache_root.rglob("__pycache__") if path.is_dir()]
        pyc_files = [path for path in cache_root.rglob("*.pyc") if path.is_file()]
        for path in pyc_files:
            path.unlink()
        for path in sorted(cache_dirs, key=lambda item: len(item.parts), reverse=True):
            if path.exists():
                shutil.rmtree(path)
        print(
            f"Removed Python caches beneath {cache_root}: "
            f"{len(cache_dirs)} directories, {len(pyc_files)} files"
        )

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
