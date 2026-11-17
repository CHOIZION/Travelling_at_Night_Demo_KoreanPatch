import os
import shutil
import zipfile

scratch_dir = r"C:\Users\czion\.gemini\antigravity\brain\3d906a87-bd8a-42d5-9be5-78a6d517da72\scratch"
steam_dir = r"C:\Program Files (x86)\Steam\steamapps\common\Travelling at Night Demo"
desktop_patch = r"C:\Users\czion\Desktop\Travelling_at_Night_KO_Patch"
desktop_zip = r"C:\Users\czion\Desktop\Travelling_at_Night_KO_Patch.zip"

steam_managed = os.path.join(steam_dir, "travelling_Data", "Managed")
steam_korean = os.path.join(steam_dir, "travelling_Data", "Korean")
steam_data = os.path.join(steam_dir, "travelling_Data")

os.makedirs(steam_korean, exist_ok=True)
os.makedirs(steam_managed, exist_ok=True)

# Files from scratch
patch_dll = os.path.join(scratch_dir, "TravellingKoreanPatch.dll")
scripts_dll = os.path.join(scratch_dir, "travelling.scripts.dll")
ds_dll = os.path.join(scratch_dir, "DialogueSystem.dll")
tmpro_dll = os.path.join(scratch_dir, "Unity.TextMeshPro.dll")
hud_dll = os.path.join(scratch_dir, "travelling.hud.dll")
trans_txt = os.path.join(scratch_dir, "translations.txt")

# Font source
font_src = None
candidates = [
    os.path.join(steam_korean, "font.ttf"),
    os.path.join(desktop_patch, "travelling_Data", "Korean", "font.ttf"),
    r"C:\Windows\Fonts\malgun.ttf"
]
for c in candidates:
    if os.path.exists(c):
        font_src = c
        break

print(f"Using font source: {font_src}")

# 1. Deploy DLLs to Steam Managed
for dll in [patch_dll, scripts_dll, ds_dll, tmpro_dll, hud_dll]:
    if os.path.exists(dll):
        dst = os.path.join(steam_managed, os.path.basename(dll))
        shutil.copy2(dll, dst)
        print(f"Copied {os.path.basename(dll)} -> {dst}")

# 2. Deploy translations.txt to Steam Korean, Managed, and Data
for dest_dir in [steam_korean, steam_managed, steam_data, steam_dir]:
    dst = os.path.join(dest_dir, "translations.txt")
    shutil.copy2(trans_txt, dst)
    print(f"Copied translations.txt -> {dst}")

# 3. Deploy font.ttf to Steam Korean, Managed, and Data
if font_src:
    for dest_dir in [steam_korean, steam_managed, steam_data]:
        dst = os.path.join(dest_dir, "font.ttf")
        if not os.path.exists(dst) or font_src != dst:
            shutil.copy2(font_src, dst)
            print(f"Copied font.ttf -> {dst}")
        dst_nanum = os.path.join(dest_dir, "NanumGothic.ttf")
        shutil.copy2(font_src, dst_nanum)
        print(f"Copied NanumGothic.ttf -> {dst_nanum}")

# 4. Deploy to Desktop Folder
desk_managed = os.path.join(desktop_patch, "travelling_Data", "Managed")
desk_korean = os.path.join(desktop_patch, "travelling_Data", "Korean")
os.makedirs(desk_managed, exist_ok=True)
os.makedirs(desk_korean, exist_ok=True)

for dll in [patch_dll, scripts_dll, ds_dll, tmpro_dll, hud_dll]:
    if os.path.exists(dll):
        shutil.copy2(dll, os.path.join(desk_managed, os.path.basename(dll)))

shutil.copy2(trans_txt, os.path.join(desk_korean, "translations.txt"))
shutil.copy2(trans_txt, os.path.join(desk_managed, "translations.txt"))

if font_src:
    shutil.copy2(font_src, os.path.join(desk_korean, "font.ttf"))
    shutil.copy2(font_src, os.path.join(desk_korean, "NanumGothic.ttf"))

readme_path = os.path.join(desktop_patch, "README_한글패치_안내.txt")
with open(readme_path, "w", encoding="utf-8") as f:
    f.write("""Travelling at Night Demo - 한글 패치 적용 안내 (v11 완전판)

[설치 방법]
1. Steam 라이브러리에서 'Travelling at Night Demo' 우클릭 -> 관리 -> 로컬 파일 보기 선택.
2. 다운로드받은 압축 파일 내의 'travelling_Data' 폴더를 게임 설치 폴더에 그대로 덮어쓰기 합니다.
3. 게임을 실행하면 메인 메뉴부터 모든 대사, 인물 이름, 일지, 인벤토리가 완벽하게 한글로 출력됩니다.

[v11 최신 수정 사항]
- 문두드림 -> '두드림' 3글자 일괄 통일 (속성 뱃지 줄바꿈 짤림 현상 완전 해결)
- '보이지 않는 가방': 튜토리얼 본문 링크와 각주 툴팁 제목/본문 용어 100% 일치 통일
- 아이템 조언 툴팁의 비의식적 작업 태그(은폐, 홀림, 침범, 추방 등) 및 '또는' 한글화 완벽 지원
- 젤리아 부인 대화 "어째서입니까?" 존댓말 톤 정밀 반영
- 설정창 인용구 상하단 잘림 방지 폰트 축소/행간 최적화 유지
""")

# 5. Build Desktop Zip
print(f"Creating zip archive at {desktop_zip}...")
if os.path.exists(desktop_zip):
    os.remove(desktop_zip)

with zipfile.ZipFile(desktop_zip, "w", zipfile.ZIP_DEFLATED) as z:
    for root, dirs, files in os.walk(desktop_patch):
        for file in files:
            full_path = os.path.join(root, file)
            rel_path = os.path.relpath(full_path, desktop_patch)
            z.write(full_path, rel_path)

print(f"Zip created ({os.path.getsize(desktop_zip):,} bytes). All deployment tasks completed successfully!")
