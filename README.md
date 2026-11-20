# Travelling at Night Demo - 한국어 패치 (v12 완전판)

Weather Factory의 신작 게임 **Travelling at Night Demo** (2026.11 업데이트 버전)를 위한 무설치 순수 어셈블리 인젝션 기반 한국어 패치 및 개발 소스 코드 리포지토리입니다.

---

## 🌟 주요 특징 및 기술 스펙

1. **무설치 순수 어셈블리 인젝션 (No BepInEx / No External Modloader)**
   - 별도의 외부 모드 로더 없이 Mono.Cecil을 활용하여 원본 Unity/C# 어셈블리(`travelling.scripts.dll`, `DialogueSystem.dll`, `Unity.TextMeshPro.dll`, `travelling.hud.dll`)에 직접 현지화 후킹 코드를 주입합니다.
   - 가볍고 충돌이 없으며 게임 실행 속도에 전혀 영향을 주지 않습니다.

2. **완벽한 한국어 현지화 (100% 번역 완성)**
   - **대화 및 독백**: 게임 내 4,930개 대사(Shakespeare 템페스트/헨리 5세/리처드 2세 독백, 정열 진화 대화 등 2026.11 신규 대사 355개 전수 포함) 100% 번역.
   - **가면(Mask) 및 공연(Performance) 시스템**: 왕수사슴(The King Stag) 공연 시나리오, 4개 가면(메타프라스트, 발레르, 카산드로, 스카팽), 공연 표식 및 카노바초 관련 텍스트 전수 번역.
   - **신규 인물/화자(MaskedActors)**: 아스토레, 베베트, 베그, 폴치넬레, 엘리즈, 카산드로, 스카팽, 발레르, 메타프라스트, 메즈탱, 기르비, 판크라타 수녀, 베르나데트 수녀, 몹시 이모 등 완벽 매핑.
   - **신규 아이템 및 각주**: 결정적 모래시계, 반창고 한 갑, 창백한 피리, 그을린 유리 코안경, 밀수꾼의 허리띠, 스프라그 라파포트 청진기, 도둑맞은 세월, 과격한 운율(주해본), 매듭의 자매단, 지네, 가슴흰웃음지빠귀 등 전수 번역.
   - **최신 패치 노트**: `2026.11.r.22` ("당신의 영혼은 선택받은 풍경이어라") 50여 줄의 상세 업데이트 내역 전수 번역.
   - **UI 및 설정**: `UI_PERFORMANCE_CURRENT_SIGN` ("현재 당신의 표식: 공연 중 다음에 영향을 미칩니다"), `UI_SCREENMODE_MAXIMIZEDWINDOW` ("최대화된 창 화면") 등 신규 키 추가.

3. **고품질 폰트 렌더링 및 텍스트 오버플로우 방지**
   - TextMeshPro 동적 Fallback 폰트 주입(`NanumGothic.ttf` / `font.ttf`)으로 글자 깨짐 및 네모 칸 현상 방지.
   - '두드림', '보이지 않는 가방', '어째서입니까?' 등 공인 용어 및 인용구 상하단 여백 최적화 유지.

---

## 📁 디렉터리 구조

```
Travelling_at_Night_Demo_KoreanPatch-main/
├── bin/                       # 컴파일된 DLL 및 실행 파일 (TravellingKoreanPatch.dll, MultiInjector.exe 등)
├── data/                      # 폰트, 태그 맵, 마스터 번역 텍스트 데이터 (01~04 파일 및 translations.txt)
├── lib/                       # 빌드 종속 라이브러리 (Mono.Cecil.dll)
├── scripts/                   # 빌드 및 배포 자동화 스크립트 (deploy.py, compile_patch.py, unified_terms.py)
├── src/                       # 핵심 소스 코드
│   ├── TravellingKoreanPatch.cs   # 게임 런타임에서 작동하는 한국어 번역 엔진 및 폰트 Fallback 모듈
│   └── MultiInjector.cs           # 게임 어셈블리에 후킹 코드를 주입하는 인젝터
├── tests/                     # 버전별 검증 테스트 스위트 (VerifyV10.cs, VerifyV11.cs, VerifyV12.cs)
├── build.bat                  # 원클릭 컴파일, 인젝션, 배포 배치 파일
└── README.md                  # 본 문서
```

---

## 🛠️ 빌드 및 배포 방법

1. **원클릭 빌드**:
   `build.bat`을 실행하면 다음 과정이 자동으로 진행됩니다:
   - `TravellingKoreanPatch.dll` 컴파일 (.NET 4.0 `csc.exe`)
   - `MultiInjector.exe` 컴파일
   - 게임 어셈블리 인젝션 수행 (`bin/` 내에 패치된 DLL 생성)
   - `scripts/deploy.py` 실행을 통한 스팀 경로 복사, 릴리즈 폴더 복사 및 `Travelling_at_Night_KO_Patch.zip` 압축 파일 생성

2. **단위 검증 테스트 실행**:
   `tests/VerifyV12.cs`를 컴파일 및 실행하여 번역 딕셔너리와 후킹 로직이 정상 동작하는지 테스트할 수 있습니다.
