# ManipulatorStudio

**6축 매니퓰레이터 구조 설계를 위한 경량 디지털 트윈**

> 📖 **[프로젝트 전체 설계 문서 보기](https://substantial-willow-3ff.notion.site/ManipulatorStudio-2fed85a553a7800ea91ffcaf509fdb4e)**


Unity 기반 6-DOF 로봇 매니퓰레이터 FK 시뮬레이터.  
복잡한 URDF/ROS 환경 없이 관절 체인 구조와 Forward Kinematics를  
빠르게 실험하고 시각화할 수 있습니다.

## Demo Video
[▶ Watch Demo Video (mp4)](https://github.com/user-attachments/assets/69097c6a-77c5-40ef-9f84-4125c5727965)

*v0.1.0-alpha.1 데모 - Inspector에서 관절 각도 제어*

---

## Why ManipulatorStudio?
- 🎓 교육/연구: FK/IK 개념 학습
- 🔧 프로토타입: 하드웨어 설계 전 검증
- 🚀 경량화: 메쉬 대신 라인으로 빠른 시각화

---

## Current Status (v0.1.0-alpha.1)
⚠️ **개발 초기 단계입니다**
현재는 FK 체인 동작 검증에 집중하고 있습니다:
- Inspector에서 J1~J6 각도 직접 조작 (임시)
- 각 관절은 Local X축 기준 회전
- UR5 프리셋은 v0.3에서 추가 예정
**주의**: 아직 정확한 DH 파라미터 구현 전 단계입니다.

---

## Features (v0.1.0-alpha.1)
- ✅ Transform Chain 기반 FK
- ✅ 실시간 라인 시각화
- ✅ 6개 관절 슬라이더 제어

---

## Quick Start
1. Unity 6.3 LTS 설치
2. 레포 클론: `git clone https://github.com/grenn-gru/ManipulatorStudio.git`
3. Unity Hub에서 프로젝트 열기
4. `Assets/Scenes/Main.unity` 실행
5. Play → Hierarchy에서 `FkController` 선택
6. Inspector에서 각도 슬라이더 조작

**현재 제약사항**: UI 슬라이더는 v0.2 예정

---

## Roadmap
- [ ] v0.2: IK 솔버
- [ ] v0.3: 프리셋 모드 (UR5, ABB 등)
- [ ] v0.4: ROS2 연동

---

## Documentation
- [DEVLOG.md](DEVLOG.md) - 개발 과정
- [AGENTS.md](AGENTS.md) - AI 개발 가이드라인

---

## Related Projects
프로덕션급 도구가 필요하다면:
- [Preliy/Flange](https://github.com/Preliy/Flange) - Unity 산업용 로봇 패키지
- [realvirtual.io](http://realvirtual.io) - 엔터프라이즈 디지털 트윈

---

## License
MIT License - 자유롭게 사용, 수정, 배포 가능


