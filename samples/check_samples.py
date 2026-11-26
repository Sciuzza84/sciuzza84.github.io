"""Run the sample checks without opening the Unity Editor."""

import argparse
from pathlib import Path
import shutil
import subprocess


def main():
    root = Path(__file__).resolve().parent
    projects = {
        project.parent.name: project
        for project in sorted((root / "Tests").glob("*/*.Checks.csproj"))
    }
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("samples", nargs="*", metavar="SAMPLE", help="Test folder names. Omit to check every sample.")
    options = parser.parse_args()
    if not projects:
        parser.error("The Tests directory has no sample projects.")

    unknown = sorted(set(options.samples).difference(projects))
    if unknown:
        parser.error(f"Unknown sample {', '.join(unknown)}. Available: {', '.join(projects)}.")

    dotnet = shutil.which("dotnet")
    if not dotnet:
        parser.error("Install the .NET 8 SDK or a compatible newer SDK and runtime.")

    selected = list(dict.fromkeys(options.samples)) if options.samples else list(projects)
    failed = []
    for name in selected:
        print(f"Checking {name}", flush=True)
        result = subprocess.run(
            [dotnet, "run", "--project", str(projects[name]), "--configuration", "Release"],
            cwd=root,
            check=False,
        )
        if result.returncode:
            failed.append(name)

    print(f"{len(selected) - len(failed)}/{len(selected)} sample checks passed.")
    if failed:
        print(f"Failed: {', '.join(failed)}.")
    return int(bool(failed))


if __name__ == "__main__":
    raise SystemExit(main())
