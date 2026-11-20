# -*- coding: utf-8 -*-
r"""
deploy.py
Deploys the patched assemblies, Korean translations, fonts, and metadata to:
1. Steam Game Directory: C:\Program Files (x86)\Steam\steamapps\common\Travelling at Night Demo
2. Release Distribution Folder: C:\Users\czion\Desktop\ttt\Travelling_at_Night_KO_Patch
3. Release Zip Archive: C:\Users\czion\Desktop\ttt\Travelling_at_Night_KO_Patch.zip
"""

import os
import shutil
import zipfile

script_dir = os.path.dirname(os.path.abspath(__file__))
repo_root = os.path.dirname(script_dir)
bin_dir = os.path.join(repo_root, "bin")

scratch_dir = r"C:\Users\czion\.gemini\antigravity\brain\14a94f7b-a996-493a-b6cd-adc1e07e46f1\scratch"
steam_dir = r"C:\Program Files (x86)\Steam\steamapps\common\Travelling at Night Demo"
release_dir = r"C:\Users\czion\Desktop\ttt\Travelling_at_Night_KO_Patch"
release_zip = r"C:\Users\czion\Desktop\ttt\Travelling_at_Night_KO_Patch.zip"

steam_managed = os.path.join(steam_dir, "travelling_Data", "Managed")
steam_korean = os.path.join(steam_dir, "travelling_Data", "Korean")
steam_data = os.path.join(steam_dir, "travelling_Data")

os.makedirs(steam_korean, exist_ok=True)
os.makedirs(steam_managed, exist_ok=True)

# Find sources for DLLs (first check bin_dir, then scratch_dir)
dll_names = [
    "TravellingKoreanPatch.dll",
    "travelling.scripts.dll",
    "DialogueSystem.dll",
    "Unity.TextMeshPro.dll",
    "travelling.hud.dll"
]

dll_sources = {}
for dll in dll_names:
    src_bin = os.path.join(bin_dir, dll)
    src_scratch = os.path.join(scratch_dir, dll)
    if os.path.exists(src_bin):
        dll_sources[dll] = src_bin
    elif os.path.exists(src_scratch):
        dll_sources[dll] = src_scratch
    else:
        raise FileNotFoundError(f"Cannot find compiled assembly: {dll}")

# Translation files
trans_files = [
    "translations.txt",
    "01_dialogue.txt",
    "02_ui.txt",
    "03_lore_and_footnotes.txt",
    "04_patch_notes.txt"
]

trans_sources = {}
for tf in trans_files:
    src_scratch = os.path.join(scratch_dir, tf)
    src_release = os.path.join(release_dir, "travelling_Data", "Korean", tf)
    if os.path.exists(src_scratch):
        trans_sources[tf] = src_scratch
    elif os.path.exists(src_release):
        trans_sources[tf] = src_release
    else:
        raise FileNotFoundError(f"Cannot find translation file: {tf}")

# Font source
font_src = None
candidates = [
    os.path.join(release_dir, "travelling_Data", "Korean", "font.ttf"),
    os.path.join(steam_korean, "font.ttf"),
    os.path.join(repo_root, "data", "NanumGothic.ttf"),
    r"C:\Windows\Fonts\malgun.ttf"
]
for c in candidates:
    if os.path.exists(c):
        font_src = c
        break

tag_map_src = None
tag_candidates = [
    os.path.join(release_dir, "travelling_Data", "Korean", "tag_map.json"),
    os.path.join(repo_root, "data", "tag_map.json")
]
for c in tag_candidates:
    if os.path.exists(c):
        tag_map_src = c
        break

print(f"Using font source: {font_src}")
print(f"Using tag_map source: {tag_map_src}")

# 1. Deploy DLLs to Steam Managed
print("\n--- [1] Deploying DLLs to Steam Managed ---")
for dll_name, src_path in dll_sources.items():
    dst = os.path.join(steam_managed, dll_name)
    shutil.copy2(src_path, dst)
    print(f"  Copied {dll_name} -> {dst}")

# 2. Deploy translation files to Steam Korean & Managed
print("\n--- [2] Deploying translations to Steam ---")
for tf, src in trans_sources.items():
    dst = os.path.join(steam_korean, tf)
    shutil.copy2(src, dst)
    print(f"  Copied {tf} -> {dst}")

# translations.txt is also placed in Managed, Data, and Root for maximum fallback safety
master_trans = trans_sources["translations.txt"]
for extra_dir in [steam_managed, steam_data, steam_dir]:
    shutil.copy2(master_trans, os.path.join(extra_dir, "translations.txt"))

# 3. Deploy Font and Tag Map to Steam
print("\n--- [3] Deploying Font to Steam ---")
if font_src:
    for dest_dir in [steam_korean, steam_managed, steam_data]:
        dst1 = os.path.join(dest_dir, "font.ttf")
        if os.path.abspath(font_src) != os.path.abspath(dst1):
            shutil.copy2(font_src, dst1)
        dst2 = os.path.join(dest_dir, "NanumGothic.ttf")
        if os.path.abspath(font_src) != os.path.abspath(dst2):
            shutil.copy2(font_src, dst2)
if tag_map_src:
    dst_tag = os.path.join(steam_korean, "tag_map.json")
    if os.path.abspath(tag_map_src) != os.path.abspath(dst_tag):
        shutil.copy2(tag_map_src, dst_tag)

# 4. Deploy to Release Folder
print(f"\n--- [4] Deploying to Release Folder: {release_dir} ---")
rel_managed = os.path.join(release_dir, "travelling_Data", "Managed")
rel_korean = os.path.join(release_dir, "travelling_Data", "Korean")
os.makedirs(rel_managed, exist_ok=True)
os.makedirs(rel_korean, exist_ok=True)

for dll_name, src_path in dll_sources.items():
    dst = os.path.join(rel_managed, dll_name)
    if os.path.abspath(src_path) != os.path.abspath(dst):
        shutil.copy2(src_path, dst)

for tf, src in trans_sources.items():
    dst = os.path.join(rel_korean, tf)
    if os.path.abspath(src) != os.path.abspath(dst):
        shutil.copy2(src, dst)

dst_managed_trans = os.path.join(rel_managed, "translations.txt")
if os.path.abspath(master_trans) != os.path.abspath(dst_managed_trans):
    shutil.copy2(master_trans, dst_managed_trans)

if font_src:
    dst_font = os.path.join(rel_korean, "font.ttf")
    if os.path.abspath(font_src) != os.path.abspath(dst_font):
        shutil.copy2(font_src, dst_font)
    dst_nanum = os.path.join(rel_korean, "NanumGothic.ttf")
    if os.path.abspath(font_src) != os.path.abspath(dst_nanum):
        shutil.copy2(font_src, dst_nanum)

if tag_map_src:
    dst_rel_tag = os.path.join(rel_korean, "tag_map.json")
    if os.path.abspath(tag_map_src) != os.path.abspath(dst_rel_tag):
        shutil.copy2(tag_map_src, dst_rel_tag)

# Also ensure bin in repo has all the DLLs
os.makedirs(bin_dir, exist_ok=True)
for dll_name, src_path in dll_sources.items():
    dst = os.path.join(bin_dir, dll_name)
    if os.path.abspath(src_path) != os.path.abspath(dst):
        shutil.copy2(src_path, dst)

readme_path = os.path.join(release_dir, "README_한글패치_안내.txt")
with open(readme_path, "w", encoding="utf-8") as f:
    f.write("""Travelling at Night Demo - 한국어 패치 안내 (v12 완전판 - 최신 2026.11 업데이트 대응)

[패치 개요]
본 한국어 패치는 최신 스팀 데모 빌드(2026.11.r.24 / 2026.11.r.22+)에 완벽하게 대응합니다.
별도의 BepInEx 모드 로더 없이 순수 어셈블리 인젝션 방식으로 동작하여 가볍고 충돌이 없습니다.

[설치 방법]
1. Steam 라이브러리에서 'Travelling at Night Demo'를 우클릭 -> 관리 -> 로컬 파일 보기 선택.
2. 다운로드받은 압축 파일 내의 'travelling_Data' 폴더를 게임 설치 폴더에 그대로 덮어쓰기 합니다.
3. 게임을 실행하면 메인 메뉴, 설정, 대사, 인물 이름, 저널, 인벤토리, 공연, 최신 패치 노트까지 모두 깔끔한 한국어로 출력됩니다.

[v12 최신 업데이트 내용]
- 최신 2026.11 엔진 업데이트 대응 (새로운 Unity/C# 어셈블리 완벽 재인젝션 및 호환)
- 최신 업데이트 패치 노트(2026.11.r.22 - "당신의 영혼은 선택받은 풍경이어라") 100% 한글화
- 신규 Loc 키 번역:
  * UI_PERFORMANCE_CURRENT_SIGN ("현재 당신의 표식: 공연 중 다음에 영향을 미칩니다")
  * UI_SCREENMODE_MAXIMIZEDWINDOW ("최대화된 창 화면")
- 신규 등장인물/화자(MaskedActors) 한글화:
  * 아스토레, 베베트, 베그, 폴치넬레, 엘리즈, 카산드로, 스카팽, 발레르, 메타프라스트, 메즈탱, 기르비, 판크라타 수녀, 베르나데트 수녀, 몹시 이모 등
- 신규 대사 355개 및 메뉴 선택지 100% 완전 번역 (셰익스피어 템페스트/헨리 5세/리처드 2세 독백 및 정열 진화 대사 포함)
- 신규 아이템, 각주, 의상 패키지/주문서, 가면, 시나리오(왕수사슴) 완전 번역:
  * 결정적 모래시계, 반창고 한 갑, 창백한 피리, 그을린 유리 코안경, 밀수꾼의 허리띠, 스프라그 라파포트 청진기, 도둑맞은 세월, 과격한 운율 (주해본), 기묘한 우표들, 어두운 물 한 병 등
- 기존 v11의 '두드림', '보이지 않는 가방', '어째서입니까?' 등 공인 용어 100% 일관성 유지
""")

# 5. Build Release Zip
print(f"\n--- [5] Creating zip archive at: {release_zip} ---")
if os.path.exists(release_zip):
    os.remove(release_zip)

with zipfile.ZipFile(release_zip, "w", zipfile.ZIP_DEFLATED) as z:
    for root, dirs, files in os.walk(release_dir):
        for file in files:
            full_path = os.path.join(root, file)
            rel_path = os.path.relpath(full_path, release_dir)
            z.write(full_path, rel_path)

print(f"Zip created ({os.path.getsize(release_zip):,} bytes). All deployment tasks completed successfully!")
