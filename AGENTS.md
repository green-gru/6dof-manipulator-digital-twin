# ManipulatorStudio Development Guidelines
> 이 문서는 AI 코딩 어시스턴트와 협업 시 사용하는 규칙입니다.  
> This document contains guidelines for AI-assisted development.

### 개발환경
- unity 6.3 LTS
- URP project

### 사용자 지정
- 대화는 한국어로 제공. 주요 컴포넌트와 함수에 한국어와 영어 주석 병기.
- 예시: 
// 이것은 개발자 한 줄 주석입니다. | This is a developer one-line annotation.

```
/// <summary>
/// 이것은 문서화 주석(XML) 예시입니다.
/// This is a documented annotation (XML) example.
/// </summary>
```

- 하나의 스크립트가 너무 길어지거나 다양한 기능을 포함하고 있을 경우 컴포넌트 분리 제안해줘.
- 스크립트, 컴포넌트로 각도 계산시 유니티 오일러각 절대 사용금지. 쿼터니언 계산 필수.


## 좌표계/단위 규칙 | Coordinate System & Units
- Unity 좌표계 기준: X=Right, Y=Up, Z=Forward
- 모든 Joint 회전축은 Local Space 기준으로 정의한다.
- 길이 단위는 meters(m)로 통일한다.
- 각도 입력(UI Slider)은 degrees, 내부 계산은 radians로 통일한다.

## Transform Chain 규칙 | Joint Chain Rules
- Joint Empty는 레스트 포즈에서 localRotation = Quaternion.identity를 유지한다.
- 링크 길이는 localPosition(offsetToNext)으로만 정의한다.
- FK는 joint.localRotation에 Quaternion.AngleAxis를 적용하는 방식으로 구현한다.

## 릴리즈 스코프 제한 | Release Scope Guardrail
- v0.1.0-alpha.1에서는 FK + Joint Slider Demo만 구현한다.
- IK Solver, Developer Mode, ROS/WebSocket 연동은 v0.2 이후로 미룬다.

## 외부 코드/라이선스 | External Sources
- 외부 오픈소스 파일을 그대로 복사하여 포함하지 않는다.
- ThirdParty 포함 시 반드시 LICENSE 및 출처를 명시한다.

## 프로젝트 구조 | Project Structure
- `ManipulatorStudio/Assets/Scripts/Core/` - 핵심 로직 (Kinematics Core (Transform Chain 기반), FK/IK 솔버)
- `ManipulatorStudio/Assets/Scripts/UI/` - 사용자 인터페이스
- `ManipulatorStudio/Assets/Scripts/Data/` - 데이터 모델 (프리셋, 설정)
- `ManipulatorStudio/Assets/Scripts/Connectors/` - 외부 통신 (WebSocket, ROS2)
- `ManipulatorStudio/Assets/Scripts/Visualization/` - 라인렌더러, 시각화

## 네이밍 규칙 | Naming Conventions
- 클래스: `PascalCase` (예: `ParameterPreset`)
- 메서드/함수: `PascalCase` (예: `CalculateForwardKinematics()`)
- private 필드: `_camelCase` (예: `_linkLength`)
- public 필드/프로퍼티: `PascalCase` (예: `JointAngle`)
- 상수: `UPPER_SNAKE_CASE` (예: `MAX_JOINT_COUNT`)

## 코드 품질 | Code Quality
- 한 함수는 50줄 이하 권장
- 한 클래스는 300줄 이하 권장 (초과 시 분리 제안)
- 순환 참조(Circular Dependency) 금지
- 매직 넘버 사용 금지 (상수화 필수)

## 에러 처리 | Error Handling
- 사용자 입력 검증은 반드시 try-catch 또는 validation 체크
- 에러 메시지는 한/영 병기: "유효하지 않은 각도 | Invalid angle"
- Debug.LogError/Warning/Info 적극 활용
- 치명적 에러는 즉시 중단, 경고는 계속 실행

## 작업로그 기록 필수
- `DEVLOG.md`에 변경내용, 변경 시간 병기하여 누적 작성.
- 포맷: 아래와 같이 한국어, 영어 병기. 어떤 코드 수정했는지 명시.
```
## [2026-02-05 21:58] `example.cs`
조인트 체인 라인/Gizmos 시각화 컴포넌트 추가
Add joint chain line/Gizmos visualization component
## [2026-02-05 22:17] `example.cs`
링크별 LineRenderer 기반 조인트 체인 시각화 및 URP 기본 머티리얼 처리 추가
Add LineRender-based joint chain visualization and URP basic material processing by link

```