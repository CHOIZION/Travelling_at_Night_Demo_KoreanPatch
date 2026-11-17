import subprocess

managed = r"C:\Program Files (x86)\Steam\steamapps\common\Travelling at Night Demo\travelling_Data\Managed"
scratch = r"C:\Users\czion\.gemini\antigravity\brain\3d906a87-bd8a-42d5-9be5-78a6d517da72\scratch"
csc = r"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

refs = [
    f"{managed}\\netstandard.dll",
    f"{managed}\\UnityEngine.CoreModule.dll",
    f"{managed}\\UnityEngine.UI.dll",
    f"{managed}\\UnityEngine.TextRenderingModule.dll",
    f"{managed}\\UnityEngine.TextCoreTextEngineModule.dll",
    f"{managed}\\UnityEngine.TextCoreFontEngineModule.dll",
    f"{managed}\\Unity.TextMeshPro.dll.bak"
]

ref_args = " ".join([f'/r:"{r}"' for r in refs])
cmd = f'"{csc}" /nologo /t:library {ref_args} /out:"{scratch}\\TravellingKoreanPatch.dll" "{scratch}\\TravellingKoreanPatch.cs"'

print("Compiling TravellingKoreanPatch.dll...")
p = subprocess.run(cmd, capture_output=True, text=True, encoding='cp949', errors='replace', shell=True)
print("Exit code:", p.returncode)
if p.returncode != 0:
    print("STDOUT:", p.stdout)
    print("STDERR:", p.stderr)
else:
    print("TravellingKoreanPatch.dll compiled successfully!")
