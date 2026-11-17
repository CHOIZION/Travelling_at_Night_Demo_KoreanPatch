# Travelling at Night Demo - 한국어 패치 엔진 및 인젝터 소스 코드

Travelling_at_Night_KO/
├── src/                               # 핵심 C# 소스 코드
│   ├── TravellingKoreanPatch.cs      # 런타임 패치 엔진 (TMP 후킹, 태그 변환, 조사 자동 계산, 파서)
│   └── MultiInjector.cs              # Mono.Cecil 기반 Unity 어셈블리 IL 인젝터
├── data/                              # 오픈 소스 에셋
│   ├── NanumGothic.ttf               # SIL Open Font License 한글 폴백 폰트
│   └── tag_map.json                  # Secret Histories 용어 매핑 정의
├── scripts/                           # 빌드 및 컴파일 스크립트
│   ├── compile_patch.py              # TravellingKoreanPatch.dll 컴파일러
│   ├── deploy.py                     # Steam 설치 경로 배포 및 패키징 스크립트
│   └── unified_terms.py              # 용어집 정의 파일
├── tests/                             # 단위 검증 테스트 스위트
│   ├── VerifyV10.cs                  # 인용구 상하단 짤림 방지 및 TMP 회귀 검증
│   └── VerifyV11.cs                  # 비의식적 작업 태그, 각주 표제어 일치 검증
├── lib/
│   └── Mono.Cecil.dll                # IL 바이트코드 조작 라이브러리
├── build.bat                          # 원클릭 C# 컴파일 및 어셈블리 인젝션 스크립트
├── .gitignore                         # 원문 덤프 텍스트(*.txt, *.jsonl) 및 바이너리 제외
└── README.md                          # 저장소 안내 문서