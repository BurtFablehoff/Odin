"""Provision portable Odin tools inside this checkout (Python 3.8+ bootstrap)."""
import hashlib
import json
import subprocess
import sys
import urllib.request
import zipfile
from pathlib import Path

PROJECT = Path(__file__).resolve().parents[2]
TOOLS = PROJECT / "local-data" / "toolchain"
DOWNLOADS = TOOLS / "downloads"
LOCK = json.loads(Path(__file__).with_name("toolchain-lock.json").read_text())


def download(name):
    entry = LOCK[name]
    target = DOWNLOADS / entry["filename"]
    if not target.exists() or hashlib.sha256(target.read_bytes()).hexdigest() != entry["sha256"]:
        print("Downloading " + entry["filename"], flush=True)
        urllib.request.urlretrieve(entry["url"], str(target))
    if hashlib.sha256(target.read_bytes()).hexdigest() != entry["sha256"]:
        raise RuntimeError("Download checksum mismatch: " + str(target))
    return target


def run(*args):
    subprocess.run([str(arg) for arg in args], check=True, cwd=str(PROJECT))


def main():
    DOWNLOADS.mkdir(parents=True, exist_ok=True)
    python = TOOLS / "python" / "python.exe"
    if not python.exists():
        with zipfile.ZipFile(download("python")) as archive:
            archive.extractall(python.parent)
    pth = python.parent / "python313._pth"
    lines = pth.read_text().replace("#import site", "import site").splitlines()
    if "Lib\\site-packages" not in lines:
        lines.append("Lib\\site-packages")
    pth.write_text("\n".join(lines) + "\n", encoding="ascii")
    pip = download("pip")
    bootstrap = "import sys;sys.path.insert(0,sys.argv[1]);from pip._internal.cli.main import main;sys.exit(main(sys.argv[2:]))"
    run(python, "-c", bootstrap, pip, "install", "--no-cache-dir", "--disable-pip-version-check",
        "--no-warn-script-location", "pip==26.3", "-r", Path(__file__).with_name("requirements-art.txt"))
    sevenzip = TOOLS / "7zip" / "x64" / "7za.exe"
    if not sevenzip.exists():
        run(download("sevenZipBootstrap"), "x", download("sevenZipExtra"),
            "-o" + str(TOOLS / "7zip"), "-y", "-bso0")
    inno = TOOLS / "inno" / "ISCC.exe"
    if not inno.exists():
        unpacker = TOOLS / "innounp" / "innounp.exe"
        with zipfile.ZipFile(download("innounp")) as archive:
            archive.extractall(unpacker.parent)
        extracted = TOOLS / "inno-unpacked"
        # Extract the official compiler instead of registering an installation.
        run(unpacker, "-x", "-y", "-d" + str(extracted), download("inno"))
        app = extracted / "{app}"
        if not (app / "ISCC.exe").is_file():
            raise RuntimeError("Inno Setup compiler was not extracted")
        app.rename(inno.parent)
    run(python, "-c", "import PIL,numpy,cv2;print('Image tools ready:',PIL.__version__,numpy.__version__,cv2.__version__)")
    run(inno, "--version")
    print("Local toolchain ready. No PATH or system Python changes.")


if __name__ == "__main__":
    main()
