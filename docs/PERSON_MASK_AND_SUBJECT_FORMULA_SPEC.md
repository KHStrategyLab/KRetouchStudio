# PERSON_MASK_AND_SUBJECT_FORMULA_SPEC.md

Last updated: 2026-06-15

## 문서 역할

이 문서는 KRetouchPro의 **인물 마스크 체계 통합 기준서**이다.  
이 문서 하나를 기준으로 다음을 동시에 정의한다.

- 전체 엔진 흐름
- 마스크 계층 구조
- 각 마스크의 목적
- 왜 필요한가
- 부위별/객체별 규칙
- 기능별 사용 규칙
- 핵심 수식
- 금지 규칙

이 문서는 **기준 문서 1개**로 사용한다.

---

# 1. 전체 목차

## 1.1 엔진 큰 흐름

1. 이미지 입력
2. 기본 분석
3. 후보 마스크 생성
4. 확정 마스크 생성
5. 부위별 객체 분리
6. 기능별 라우팅
7. 렌더
8. 저장

---

# 2. 시작점

## 2.1 이미지 입력

\[
I_{src} = InputImage
\]

원본 이미지는 불변이다.

\[
I_{src} \rightarrow Analysis \rightarrow Masks \rightarrow RenderPlan \rightarrow I_{out}
\]

금지:

\[
PreviousPreview \rightarrow NextPreview
\]

항상 원본 또는 프록시 원본 기준으로 다시 계산한다.

---

## 2.2 전체 기준판 / ImageDomain

\[
1 = ImageDomain
\]

여기서 `1`은:

- 현재 작업하려고 선택한 캔버스 안의 사진 1장 전체
- 현재 선택된 작업 이미지의 전체 픽셀 도메인
- 이 문서의 모든 마스크 연산이 정의되는 최상위 기준판

따라서 모든 마스크는 기본적으로 `1` 안에서 정의된다.

\[
PersonMask \subset 1
\]

\[
BackgroundMask = 1 - PersonMask
\]

---

# 3. 기본 분석

## 3.1 기본 분석 결과

\[
A_{base} = DetectImageContent(I_{src})
\]

기본 분석 항목:

- image size
- visible person candidate
- face box
- face landmarks
- body landmarks
- rough foreground
- clothing candidate
- accessory candidate
- background candidate

---

# 4. 후보 마스크

## 4.1 1차 후보

\[
PersonCandidate = DetectVisiblePerson(I_{src})
\]

\[
BioCandidate = DetectBiologicalBody(I_{src})
\]

\[
ClothingCandidate = DetectClothing(I_{src})
\]

\[
AccessoryCandidate = DetectWornAccessories(I_{src})
\]

\[
BackgroundCandidate = 1 - PersonCandidate
\]

---

# 5. 확정 마스크 계층

## 5.1 최상위 구조

확정 마스크 계층은 모두 `ImageDomain = 1` 안에서 정의된다.

\[
PersonMaskRaw = BioMask \cup ClothingMask \cup AccessoryMask
\]

soft union:

\[
PersonMaskRaw = Clip01(BioMask + ClothingMask + AccessoryMask)
\]

또는:

\[
PersonMaskRaw = \max(BioMask,\ ClothingMask,\ AccessoryMask)
\]

최종 인물 마스크:

\[
PersonMask = FillSmallInternalHoles(PersonMaskRaw,\ maxArea,\ enclosedOnly)
\]

필요 시:

\[
SubjectMask = PersonMask \cup HandheldObjectMask_{optional}
\]

`FillSmallInternalHoles`는 인물 내부의 작은 누락 구멍만 메운다.  
`enclosedOnly`는 배경과 연결되지 않은 내부 홀만 대상으로 한다.  
팔짱 사이, 손가락 사이, 팔과 몸 사이처럼 실제 배경이 보이는 구조적 공간은 `PersonMask`가 아니며 메우지 않는다.

---

# 6. 각 마스크의 정의 / 목적 / 왜 필요한가

## 6.1 PersonMask

### 정의

배경 교체와 인물 보존에 사용하는 **최종 인물 외형 마스크**

### 포함

- 생체 부위
- 머리카락
- 옷
- 신발
- 착용 액세서리

### 제외

- 배경
- 그림자
- 의자
- 들고 있는 비착용 물체
- 주변 소품
- 다른 사람

### 목적

- 배경 교체
- 전체 인물 추출
- 인물 외곽 보존

### 왜 필요한가

이게 틀리면:

- 옷이 잘림
- 신발이 사라짐
- 안경/모자/귀걸이가 날아감
- 최종 인물 외형이 깨짐

---

## 6.2 BioMask

### 정의

사람의 **생체 영역만** 포함하는 마스크

### 포함

- 얼굴
- 피부
- 머리카락
- 눈썹
- 속눈썹
- 귀
- 수염
- 목
- 몸통
- 팔
- 손
- 다리
- 발
- 몸털

### 제외

- 옷
- 신발
- 안경
- 모자
- 시계
- 벨트
- 액세서리

### 목적

- 생체 구조 분석
- 부위별 분해
- 머리/팔/다리/손/발 구조 계산

### 왜 필요한가

이게 없으면:

- 생체와 의상을 분리해서 생각할 수 없음
- 피부 보정과 배경 교체가 뒤섞임
- Head Tilt 기준이 흐려짐

---

## 6.3 SkinMask

### 정의

피부 보정 전용 **피부 마스크**

### 포함

- 얼굴 피부
- 귀 피부
- 목 피부
- 팔 피부
- 손 피부
- 다리 피부
- 발 피부
- 드러난 몸통 피부

### 제외

- 머리카락
- 눈썹
- 속눈썹
- 수염
- 옷
- 신발
- 액세서리
- 손톱 / 발톱

### 목적

- 피부 보정
- 잡티 보정
- 피부톤 보정
- 주름/트러블 보정

### 왜 필요한가

이게 틀리면:

- 옷까지 피부 보정됨
- 머리카락이 뭉개짐
- 눈썹/수염 질감이 날아감

---

## 6.4 ClothingMask

### 정의

착용 의상 마스크

### 포함

- 상의
- 하의
- 소매
- 양말
- 신발
- 벨트
- 넥타이
- 스카프

### 목적

- 의상 보호
- 의상 색보정
- 배경 교체 시 외형 보존

### 왜 필요한가

이게 없으면:

- 피부 보정이 옷까지 번짐
- 의상 경계가 망가짐
- 배경 교체에서 의상이 잘림

---

## 6.5 AccessoryMask

### 정의

착용 액세서리 마스크

### 포함

- 안경
- 모자
- 귀걸이
- 목걸이
- 시계
- 팔찌
- 반지
- 머리핀
- 머리띠

### 목적

- 액세서리 보호
- 배경 교체 시 외형 보존

### 왜 필요한가

이게 없으면:

- 안경/모자/귀걸이가 인물에서 빠짐
- 피부 보정 시 액세서리까지 오염됨

---

## 6.6 SubjectMask

### 정의

필요 시 손에 든 물체까지 포함하는 확장 피사체 마스크

### 기본식

\[
SubjectMask = PersonMask
\]

손에 든 물체를 포함할 때:

\[
SubjectMask = PersonMask \cup HandheldObjectMask
\]

### 목적

- 특수 합성
- 소품 포함 추출

### 왜 필요한가

기본 `PersonMask`는 손에 든 물체를 포함하지 않기 때문

---

# 7. 기능별 사용표

## 7.1 기능 → 마스크

\[
BackgroundReplace \Rightarrow PersonMask,\ PersonAlpha
\]

\[
SkinRetouch \Rightarrow SkinMask,\ SkinSafeMask
\]

\[
HairColor \Rightarrow HairMask
\]

\[
HeadTilt \Rightarrow HeadCarrierMask
\]

\[
ClothingProtect \Rightarrow ClothingMask
\]

\[
AccessoryProtect \Rightarrow AccessoryMask
\]

\[
HeldObjectPreserve \Rightarrow SubjectMask
\]

---

# 8. 핵심 보조 마스크

## 8.1 SkinSafeMask

\[
SkinSafeMask =
SkinMask
\cdot (1 - EyeMask)
\cdot (1 - HairMask)
\cdot (1 - EyebrowMask)
\cdot (1 - EyelashMask)
\cdot (1 - NoseMask)
\cdot (1 - JawlineMask)
\cdot (1 - FacialHairMask)
\cdot (1 - ClothingMask)
\cdot (1 - AccessoryMask)
\]

---

## 8.2 HeadCarrierMask

\[
HeadCarrierMask =
FaceRegionMask
\cup HairMask
\cup EarMask
\cup FacialHairMask
\]

옵션:

\[
HeadCarrierMask =
HeadCarrierMask
\cup HairAccessoryMask
\cup HatMask_{optional}
\]

왜 필요한가:

- 얼굴만 움직이면 머리카락/귀가 따로 놀음
- 머리 전체를 하나의 운반 단위로 다뤄야 함

---

## 8.3 HairColorTargetMask

\[
HairColorTargetMask =
HairMask
\cdot (1 - EyebrowMask)
\cdot (1 - EyelashMask)
\cdot (1 - FacialHairMask)
\cdot (1 - AccessoryMask)
\]

---

# 9. 객체 분리 규칙

## 9.1 핵심 규칙

좌우가 있고 각각 따로 움직일 수 있는 부위는  
**기하 변형 시 반드시 개별 객체**로 처리한다.

색/톤/밝기/질감 보정은 묶어도 된다.  
이동/회전/크기/기울기/워프는 각각 처리한다.

---

## 9.2 예시

묶은 이름:

\[
EarMask = LeftEarMask \cup RightEarMask
\]

하지만 기하 변형은:

\[
LeftEarResult = Transform(LeftEarMask,\ Pivot_{L},\ T_{L})
\]

\[
RightEarResult = Transform(RightEarMask,\ Pivot_{R},\ T_{R})
\]

안전식:

\[
Result =
Transform(LeftEarMask,\ Pivot_{L},\ T_{L})
\cup
Transform(RightEarMask,\ Pivot_{R},\ T_{R})
\]

위험식:

\[
Result = Transform(EarMask,\ Pivot,\ T)
\]

---

## 9.3 반드시 개별 객체가 필요한 부위

### 얼굴

- LeftEarMask / RightEarMask
- LeftEyeMask / RightEyeMask
- LeftEyebrowMask / RightEyebrowMask
- LeftEyelashMask / RightEyelashMask
- LeftCheekMask / RightCheekMask
- LeftMouthCornerMask / RightMouthCornerMask
- LeftNostrilWingMask / RightNostrilWingMask

### 팔

- LeftShoulderMask / RightShoulderMask
- LeftUpperArmMask / RightUpperArmMask
- LeftElbowMask / RightElbowMask
- LeftForearmMask / RightForearmMask
- LeftWristMask / RightWristMask
- LeftHandMask / RightHandMask
- LeftFingerMasks / RightFingerMasks

### 다리

- LeftThighMask / RightThighMask
- LeftKneeMask / RightKneeMask
- LeftLowerLegMask / RightLowerLegMask
- LeftAnkleMask / RightAnkleMask
- LeftFootMask / RightFootMask
- LeftToeMasks / RightToeMasks

### 의상

- LeftSleeveMask / RightSleeveMask
- LeftPantsLegMask / RightPantsLegMask
- LeftSockMask / RightSockMask
- LeftShoeMask / RightShoeMask

---

# 10. 생체 부위 분해식

## 10.1 BioMask

\[
BioMask = HeadBioMask \cup NeckBioMask \cup TorsoBioMask \cup UpperLimbBioMask \cup LowerLimbBioMask
\]

---

## 10.2 HeadBioMask

\[
HeadBioMask =
FaceRegionMask
\cup HairMask
\cup EarMask
\cup EyebrowMask
\cup EyelashMask
\cup FacialHairMask
\]

---

## 10.3 FaceRegionMask

\[
FaceRegionMask = FaceMask
\]

---

## 10.4 FaceSkinMask

\[
FaceSkinMask =
FaceRegionMask
\cdot SkinProb
\cdot (1 - HairMask)
\cdot (1 - EyebrowMask)
\cdot (1 - EyelashMask)
\cdot (1 - FacialHairMask)
\]

---

## 10.5 HairCandidateMask

\[
HeadROI = Dilate(FaceRegionMask, r_{head}) \cap BioMask
\]

\[
HairCandidateMask = (BioMask \setminus FaceRegionMask) \cap HeadROI
\]

---

## 10.6 HairMask

\[
HairMask =
HairCandidateMask
\cdot (1 - SkinProb)
\cdot HairTextureProb
\]

\[
HairMask = ConnectedTo(HairMask,\ HeadROI)
\]

\[
HairAlpha = HairMask \cdot SmoothStep(s_h,e_h,d_{hair})
\]

---

## 10.7 EarMask

\[
EarMask = LeftEarMask \cup RightEarMask
\]

\[
LeftEarMask = BioMask \cap LeftEarROI \cdot EarProb
\]

\[
RightEarMask = BioMask \cap RightEarROI \cdot EarProb
\]

---

## 10.8 EyebrowMask

\[
EyebrowMask = LeftEyebrowMask \cup RightEyebrowMask
\]

\[
LeftEyebrowMask = FaceRegionMask \cap LeftBrowROI \cdot BrowProb
\]

\[
RightEyebrowMask = FaceRegionMask \cap RightBrowROI \cdot BrowProb
\]

---

## 10.9 EyelashMask

\[
EyelashMask = LeftEyelashMask \cup RightEyelashMask
\]

\[
LeftEyelashMask = FaceRegionMask \cap LeftLashROI \cdot LashProb
\]

\[
RightEyelashMask = FaceRegionMask \cap RightLashROI \cdot LashProb
\]

---

## 10.10 FacialHairMask

\[
FacialHairMask =
MustacheMask
\cup ChinBeardMask
\cup UnderChinBeardMask
\cup CheekBeardMask
\cup SideburnMask
\]

---

## 10.11 NeckBioMask

\[
NeckROI = Capsule(ChinBase, NeckBase, w_{neck})
\]

\[
NeckBioMask = BioMask \cap NeckROI \cdot NeckProb
\]

---

## 10.12 UpperLimbBioMask

\[
UpperLimbBioMask = LeftUpperLimbBioMask \cup RightUpperLimbBioMask
\]

\[
LeftUpperLimbBioMask =
LeftUpperArmMask
\cup LeftElbowMask
\cup LeftForearmMask
\cup LeftWristMask
\cup LeftHandMask
\]

\[
RightUpperLimbBioMask =
RightUpperArmMask
\cup RightElbowMask
\cup RightForearmMask
\cup RightWristMask
\cup RightHandMask
\]

---

## 10.13 LowerLimbBioMask

\[
LowerLimbBioMask = LeftLowerLimbBioMask \cup RightLowerLimbBioMask
\]

\[
LeftLowerLimbBioMask =
LeftThighMask
\cup LeftKneeMask
\cup LeftLowerLegMask
\cup LeftAnkleMask
\cup LeftFootMask
\]

\[
RightLowerLimbBioMask =
RightThighMask
\cup RightKneeMask
\cup RightLowerLegMask
\cup RightAnkleMask
\cup RightFootMask
\]

---

# 11. 의상 분해식

## 11.1 ClothingMask

\[
ClothingMask =
TopGarmentMask
\cup LowerGarmentMask
\cup FootwearMask
\cup SockMask
\cup GarmentAccessoryMask
\]

---

## 11.2 TopGarmentMask

\[
TopGarmentMask = ClothingMask \cap UpperBodyClothingROI
\]

---

## 11.3 LowerGarmentMask

\[
LowerGarmentMask =
PantsMask
\cup SkirtMask
\cup ShortsMask
\cup LeggingsMask
\]

---

## 11.4 FootwearMask

\[
FootwearMask = ClothingMask \cap FootROI \cdot ShoeProb
\]

---

## 11.5 SockMask

\[
SockMask = ClothingMask \cap AnkleFootROI \cdot SockProb
\]

---

# 12. 액세서리 분해식

## 12.1 AccessoryMask

\[
AccessoryMask =
GlassesMask
\cup HatMask
\cup EarringMask
\cup NecklaceMask
\cup BraceletMask
\cup WatchMask
\cup RingMask
\cup HairAccessoryMask
\]

---

## 12.2 GlassesMask

\[
GlassesMask = FaceRegionMask \cap EyeNoseBridgeROI \cdot GlassesProb
\]

---

## 12.3 HatMask

\[
HatMask = HeadTopROI \cap AccessoryProb \cdot HatShapeProb
\]

---

# 13. 알파

## 13.1 BioAlpha

\[
BioAlpha = BioMask \cdot SmoothStep(s_b,e_b,d_b)
\]

## 13.2 ClothingAlpha

\[
ClothingAlpha = ClothingMask \cdot SmoothStep(s_c,e_c,d_c)
\]

## 13.3 AccessoryAlpha

\[
AccessoryAlpha = AccessoryMask \cdot SmoothStep(s_a,e_a,d_a)
\]

## 13.4 HairAlpha

\[
HairAlpha = HairMask \cdot SmoothStep(s_h,e_h,d_h)
\]

## 13.5 PersonAlpha

\[
PersonAlpha = \max(BioAlpha,\ ClothingAlpha,\ AccessoryAlpha,\ HairAlpha)
\]

---

# 14. 배경 교체

\[
Composite(x,y) =
Foreground(x,y) \cdot PersonAlpha(x,y)
+
NewBackground(x,y) \cdot (1 - PersonAlpha(x,y))
\]

---

# 15. 금지 규칙

## 15.1 금지

- `PersonMask = 생체만` 으로 정의하지 말 것
- `PersonMask`를 피부 보정에 직접 쓰지 말 것
- `PreviousPreview -> NextPreview` 구조 금지
- 좌우 객체를 하나로 묶어 기하 변형하지 말 것
- `PersonMask - FaceMask`를 최종 `HairMask`라고 부르지 말 것
- 액세서리를 피부/머리카락으로 병합하지 말 것
- 옷을 `BioMask`에 넣지 말 것
- 손에 든 물체를 기본 `PersonMask`에 넣지 말 것

---

# 16. 이름 정정 규칙

이전 잘못된 개념:

```text
PersonMask = biological-only
```

사용 금지.

정정:

```text
Old biological PersonMask -> BioMask
New PersonMask = BioMask + ClothingMask + AccessoryMask
```

---

---

# 17. 목적별 적용 / 보호 기준 문서

이 문서의 핵심은 “마스크를 어떻게 만들 것인가”만이 아니라,  
**목적별로 어디까지 적용하고, 어디를 보호/차단할 것인가**를 정하는 것이다.

즉, 각 기능은 반드시 다음 4가지를 가진다.

- 적용 대상 마스크 `ApplyMask`
- 보호 마스크 `ProtectMask`
- 제외/차단 마스크 `BlockMask`
- 최종 작업 마스크 `WorkMask`

기본식:

\[
WorkMask = ApplyMask \cdot (1 - ProtectMask) \cdot (1 - BlockMask)
\]

또는 필요 시:

\[
WorkMask = ApplyMask \setminus (ProtectMask \cup BlockMask)
\]

---

## 18.1 배경 교체

### 목적
인물 외형 전체를 보존하고 배경만 교체한다.

### 기준
- ApplyMask = `PersonMask`
- ProtectMask = `HairAlpha`
- BlockMask = `BackgroundMask`

### 식

\[
BackgroundReplaceWorkMask = PersonMask
\]

\[
Composite =
Foreground \cdot PersonAlpha
+
NewBackground \cdot (1 - PersonAlpha)
\]

### 왜
- 옷, 신발, 안경, 모자까지 사람으로 남아야 함
- 머리카락 외곽은 soft alpha로 보호해야 함

---

## 18.2 피부 보정

### 목적
피부만 보정하고 머리카락, 눈썹, 속눈썹, 수염, 옷, 액세서리는 건드리지 않는다.

### 기준
- ApplyMask = `SkinMask`
- ProtectMask = `EyeMask \cup HairMask \cup EyebrowMask \cup EyelashMask \cup NoseMask \cup JawlineMask \cup FacialHairMask`
- BlockMask = `ClothingMask \cup AccessoryMask`

### 식

\[
SkinSafeMask =
SkinMask
\cdot (1 - EyeMask)
\cdot (1 - HairMask)
\cdot (1 - EyebrowMask)
\cdot (1 - EyelashMask)
\cdot (1 - NoseMask)
\cdot (1 - JawlineMask)
\cdot (1 - FacialHairMask)
\cdot (1 - ClothingMask)
\cdot (1 - AccessoryMask)
\]

\[
SkinRetouchWorkMask = SkinSafeMask
\]

### 왜
- 피부만 만져야 함
- 눈, 코, 턱선 구조가 같이 뭉개지면 안 됨
- 눈썹/수염/머리카락 질감 손실 방지
- 의상 오염 방지

---

## 18.3 머리 염색

### 목적
두피 머리카락만 염색하고 눈썹, 속눈썹, 수염, 귀, 액세서리는 제외한다.

### 기준
- ApplyMask = `HairMask`
- ProtectMask = `FaceSkinMask \cup EarMask`
- BlockMask = `EyebrowMask \cup EyelashMask \cup FacialHairMask \cup AccessoryMask`

### 식

\[
HairColorTargetMask =
HairMask
\cdot (1 - FaceSkinMask)
\cdot (1 - EarMask)
\cdot (1 - EyebrowMask)
\cdot (1 - EyelashMask)
\cdot (1 - FacialHairMask)
\cdot (1 - AccessoryMask)
\]

### 왜
- 머리만 염색해야 함
- 눈썹/수염까지 같이 물들면 안 됨

---

## 18.4 귀 크기 조절

### 목적
귀만 기하 변형하고 얼굴, 머리카락, 목은 보호한다.

### 기준
- ApplyMask = `LeftEarMask` or `RightEarMask`
- ProtectMask = `HairMask`
- BlockMask = `FaceRegionMask \cup NeckBioMask`

### 식

\[
LeftEarWorkMask =
LeftEarMask
\cdot (1 - HairMask)
\cdot (1 - FaceRegionMask)
\cdot (1 - NeckBioMask)
\]

\[
RightEarWorkMask =
RightEarMask
\cdot (1 - HairMask)
\cdot (1 - FaceRegionMask)
\cdot (1 - NeckBioMask)
\]

### 왜
- 귀는 좌우 개별 객체
- 묶어서 변형하면 중심점과 회전이 깨짐

---

## 18.5 Head Tilt

### 목적
머리 전체를 같이 움직이고, 목/어깨/의상/배경은 보호한다.

### 기준
- ApplyMask = `HeadCarrierMask`
- ProtectMask = `NeckBioMask \cup ShoulderBioMask`
- BlockMask = `ClothingMask \cup BackgroundMask`

### 식

\[
HeadCarrierMask =
FaceRegionMask
\cup HairMask
\cup EarMask
\cup FacialHairMask
\]

\[
HeadTiltWorkMask =
HeadCarrierMask
\cdot (1 - NeckBioMask)
\cdot (1 - ShoulderBioMask)
\cdot (1 - ClothingMask)
\]

### 왜
- 얼굴만 움직이면 머리카락/귀가 따로 놈
- 목과 어깨가 같이 비틀리면 부자연스러움

---

## 18.6 의상 색보정

### 목적
의상만 색보정하고 피부와 머리카락은 보호한다.

### 기준
- ApplyMask = `ClothingMask`
- ProtectMask = `SkinMask \cup HairMask`
- BlockMask = `AccessoryMask`

### 식

\[
ClothingColorWorkMask =
ClothingMask
\cdot (1 - SkinMask)
\cdot (1 - HairMask)
\cdot (1 - AccessoryMask)
\]

### 왜
- 의상만 조절해야 함
- 피부톤 오염 방지

---

## 18.7 액세서리 보호

### 목적
피부/헤어/의상 작업 중 액세서리를 건드리지 않게 한다.

### 기준
- ApplyMask = `AccessoryMask`
- ProtectMask = none
- BlockMask = none

### 식

\[
AccessoryProtectMask = AccessoryMask
\]

### 왜
- 안경, 귀걸이, 시계, 목걸이 손상 방지

---

## 18.8 눈 보정

### 목적
눈만 보정하고 눈썹, 속눈썹, 피부, 안경을 분리한다.

### 기준
- ApplyMask = `LeftEyeMask \cup RightEyeMask`
- ProtectMask = `EyebrowMask \cup EyelashMask`
- BlockMask = `GlassesMask`

### 식

\[
EyeRetouchWorkMask =
EyeMask
\cdot (1 - EyebrowMask)
\cdot (1 - EyelashMask)
\cdot (1 - GlassesMask)
\]

### 왜
- 눈 보정 시 안경/눈썹/속눈썹까지 같이 망가지면 안 됨

---

## 18.9 입술 보정

### 목적
입술만 보정하고 치아, 피부, 수염은 보호한다.

### 기준
- ApplyMask = `LipMask`
- ProtectMask = `ToothMask`
- BlockMask = `FaceSkinMask \cup FacialHairMask`

### 식

\[
LipRetouchWorkMask =
LipMask
\cdot (1 - ToothMask)
\cdot (1 - FaceSkinMask)
\cdot (1 - FacialHairMask)
\]

### 왜
- 입술 색만 바꿔야 함
- 치아와 피부에 번지면 안 됨

---

## 18.10 손 보정

### 목적
손만 보정하고 배경, 의상 소매, 반지/시계는 보호한다.

### 기준
- ApplyMask = `LeftHandMask \cup RightHandMask`
- ProtectMask = `AccessoryMask`
- BlockMask = `SleeveMask \cup BackgroundMask`

### 식

\[
HandRetouchWorkMask =
HandMask
\cdot (1 - AccessoryMask)
\cdot (1 - SleeveMask)
\cdot (1 - BackgroundMask)
\]

### 왜
- 손 피부만 보정해야 함
- 반지/시계/소매 오염 방지

---

## 18.11 목적별 핵심 원칙

### 첫번째.
기능마다 `ApplyMask`, `ProtectMask`, `BlockMask`가 있어야 한다.

### 두번째.
같은 부위라도 목적이 다르면 마스크가 달라진다.

예:
- 배경 교체의 머리카락 = `PersonAlpha` 일부
- 피부 보정의 머리카락 = 보호/차단 대상
- 머리 염색의 머리카락 = 적용 대상

### 세번째.
기하 변형은 좌우 개별 객체 기준이다.

### 네번째.
색/톤/질감 보정은 합쳐진 마스크를 사용할 수 있다.

### 다섯번째.
문서의 목적은 “어디까지 잡을까?”보다
**“무슨 목적에서 어디를 적용하고 어디를 막을까?”**를 정하는 것이다.


# 18. 최종 한 줄 정의

## PersonMask

배경 교체와 인물 보존에 사용하는, 생체 + 머리카락 + 옷 + 신발 + 착용 액세서리를 포함한 최종 인물 외형 마스크

## BioMask

생체 전용 마스크

## SkinMask

피부 보정 전용 마스크

## ClothingMask

착용 의상 마스크

## AccessoryMask

착용 액세서리 마스크

## SubjectMask

필요 시 손에 든 물체까지 포함하는 확장 피사체 마스크


---

# 19. 신체 특징 / 상태 파생 마스크

이 섹션은 **신체 특징이나 국소 상태를 별도 마스크로 분리**하기 위한 기준이다.

핵심 원칙:

- 이들은 대부분 `BioMask` 또는 `SkinMask` 내부의 **파생 마스크**다.
- 일부는 구조(feature shape), 일부는 색/질감 상태(feature state)다.
- 기능 구현 전이라도, 위치와 목적은 먼저 정의해야 한다.

기본 관계:

\[
FeatureMask \subset BioMask
\]

또는 피부 기반일 때:

\[
FeatureMask \subset SkinMask
\]

기본 작업식:

\[
FeatureWorkMask = FeatureMask \cdot (1 - ProtectMask) \cdot (1 - BlockMask)
\]

---

## 19.1 ScarMask / 흉터

### 정의
피부 위에 존재하는 선형 또는 면형의 **흉터 영역**

### 포함 예
- 얼굴 흉터
- 목 흉터
- 팔 흉터
- 손 흉터
- 다리 흉터

### 기본식

\[
ScarMask = SkinMask \cap ScarProb
\]

좀 더 보수적으로:

\[
ScarMask =
SkinMask
\cdot TextureIrregularityProb
\cdot ColorDeviationProb
\cdot LinearOrPatchShapeProb
\]

### 목적
- 흉터 완화
- 흉터 보호
- 흉터 유지 여부 선택

---

## 19.2 TattooMask / 문신

### 정의
피부 위에 존재하는 인공 색소/도안 영역

### 기본식

\[
TattooMask = SkinMask \cap TattooProb
\]

또는:

\[
TattooMask =
SkinMask
\cdot PigmentPatternProb
\cdot EdgeInkProb
\]

### 목적
- 문신 유지
- 문신 완화
- 피부 보정 시 문신 보호

### 주의
피부 보정에서 자동으로 지우면 안 됨.

---

## 19.3 MoleMask / 점

### 정의
피부 위의 작은 색소성 점 영역

### 기본식

\[
MoleMask = SkinMask \cap SmallDarkSpotProb
\]

### 목적
- 점 제거
- 점 유지
- 미용점 보호

---

## 19.4 BirthmarkMask / 반점 / 모반

### 정의
피부 위의 비교적 넓은 색 변화 영역

### 기본식

\[
BirthmarkMask =
SkinMask
\cdot LargePigmentVariationProb
\]

### 목적
- 피부톤 보정 시 선택적 보호
- 강도 완화
- 유지 선택

---

## 19.5 WrinkleMask / 주름

### 정의
피부 표면의 선형 주름 구조

### 기본식

\[
WrinkleMask =
SkinMask
\cdot WrinkleProb
\]

### 세부 예
- ForeheadWrinkleMask
- NasolabialFoldMask
- CrowsFeetMask
- UnderEyeWrinkleMask
- NeckWrinkleMask
- HandWrinkleMask

### 목적
- 주름 완화
- 주름 유지
- 부위별 강도 조절

---

## 19.6 DoubleChinMask / 이중턱

### 정의
턱선 아래, 목 상부에서 발생하는 추가적인 접힘/볼륨/그림자 영역

### 기본식

\[
DoubleChinMask =
UnderJawROI
\cap NeckBioMask
\cap Below(JawLine)
\cap Above(NeckBaseLine)
\cdot DoubleChinProb
\]

### 목적
- 이중턱 톤 완화
- 이중턱 형태 보정
- 턱선 재정리

### 작업 예

\[
DoubleChinWorkMask =
DoubleChinMask
\cdot (1 - BeardMask)
\cdot (1 - ClothingMask)
\cdot (1 - AccessoryMask)
\]

---

## 19.7 LumpMask / 혹 / 돌출 조직

### 정의
피부 또는 신체 외곽에서 국소적으로 돌출된 구조

### 기본식

\[
LumpMask =
BioMask
\cap LocalBulgeROI
\cdot LumpProb
\]

피부 기반이면:

\[
LumpMask =
SkinMask
\cdot LocalBulgeROI
\cdot LumpProb
\]

### 목적
- 혹 완화
- 돌출부 보정
- 보호/유지 선택

### 주의
`LumpMask`는 구조형(shape feature)이다.  
색보정보다 **형태 보정**과 더 관련이 크다.

---

## 19.8 SkinBlemishMask / 피부 잡티

### 정의
작은 피부 결점의 통합 마스크

### 포함 예
- 여드름
- 뾰루지
- 작은 붉은 점
- 작은 색소 잡티
- 미세 흉터

### 기본식

\[
SkinBlemishMask =
AcneMask
\cup BlemishSpotMask
\cup SmallScarMask
\cup RedSpotMask
\]

### 목적
- 국소 보정
- 자동 리터치 대상

---

## 19.9 AcneMask / 여드름

### 정의
융기 또는 색 변화가 있는 여드름 영역

### 기본식

\[
AcneMask =
SkinMask
\cdot AcneProb
\]

### 목적
- 여드름 제거/완화
- 강도별 슬라이더 연결

---

## 19.10 StretchMarkMask / 튼살

### 정의
피부 위에 생긴 다발성 선형 변형 흔적

### 기본식

\[
StretchMarkMask =
SkinMask
\cdot ParallelLinearTextureProb
\cdot StretchMarkProb
\]

### 목적
- 몸 피부 보정
- 유지/완화 선택

---

## 19.11 VeinMask / 혈관 비침

### 정의
피부 아래 비쳐 보이는 혈관성 선형/망상 구조

### 기본식

\[
VeinMask =
SkinMask
\cdot VeinColorProb
\cdot VesselPatternProb
\]

### 목적
- 손/팔/다리 혈관 완화
- 피부톤 정리

---

## 19.12 CelluliteMask / 셀룰라이트

### 정의
피부 표면의 요철/패임 질감 영역

### 기본식

\[
CelluliteMask =
SkinMask
\cdot SurfaceDimpleProb
\]

### 목적
- 다리/허벅지 피부 질감 완화

---

## 19.13 FreckleMask / 주근깨

### 정의
작은 다발성 색소 점들의 군집 영역

### 기본식

\[
FreckleMask =
SkinMask
\cdot MultiSmallPigmentSpotProb
\]

### 목적
- 유지/완화 선택
- 피부 보정 시 분리 보호

---

## 19.14 DarkCircleMask / 다크서클

### 정의
눈 아래의 어두운 색 변화 영역

### 기본식

\[
DarkCircleMask =
UnderEyeROI
\cap SkinMask
\cdot DarkCircleProb
\]

### 목적
- 눈밑 톤 개선
- 얼굴 피로도 완화

---

## 19.15 UnderEyeBagMask / 눈밑 지방/불룩함

### 정의
눈 아래의 볼륨성 돌출 또는 경계 그림자 영역

### 기본식

\[
UnderEyeBagMask =
UnderEyeROI
\cap SkinMask
\cdot UnderEyeBagProb
\]

### 목적
- 눈밑 불룩함 완화
- 그림자와 볼륨 분리 보정

---

## 19.16 NasolabialFoldMask / 팔자주름

### 정의
코 옆에서 입가 방향으로 내려오는 주름/접힘 영역

### 기본식

\[
NasolabialFoldMask =
CheekLowerROI
\cap SkinMask
\cdot NasolabialFoldProb
\]

### 목적
- 팔자주름 완화
- 얼굴 볼륨감 재정리

---

## 19.17 JawlineMask / 턱선

### 정의
아래턱 외곽의 경계 구조

### 기본식

\[
JawlineMask =
Boundary(FaceRegionMask,\ NeckBioMask)
\cdot JawlineProb
\]

### 목적
- 턱선 강화
- 얼굴 윤곽 보정
- 이중턱 보정 기준선

---

## 19.18 SubmentalRegionMask / 턱밑 영역

### 정의
턱 아래와 목 위 사이의 중간 영역

### 기본식

\[
SubmentalRegionMask =
UnderJawROI
\cap Above(NeckBaseLine)
\cap Below(JawLine)
\]

### 목적
- 이중턱
- 턱밑 그림자
- 턱밑 수염
- 턱밑 톤 보정

---

## 19.19 ShoulderHumpMask / 승모/어깨 돌출

### 정의
목과 어깨 연결부에서 부피가 도드라진 구조 영역

### 기본식

\[
ShoulderHumpMask =
ShoulderBioMask
\cap NeckBioMask_{near}
\cdot ShoulderHumpProb
\]

### 목적
- 승모근 완화
- 어깨선 보정

---

## 19.20 BodyFeaturePolicy

### 원칙 1
신체 특징은 `BioMask` 또는 `SkinMask` 내부의 **별도 파생 마스크**로 분리한다.

### 원칙 2
프로그램이 아직 기능을 몰라도,  
문서에는 먼저 **이름 / 위치 / 목적 / 기본식**을 둔다.

### 원칙 3
특징은 크게 3종이다.

- 색 변화형: 문신, 반점, 다크서클, 주근깨
- 질감형: 주름, 흉터, 잡티, 튼살
- 형태형: 이중턱, 혹, 눈밑 불룩함, 승모 돌출

### 원칙 4
특징 마스크는 나중에 기능별로 `ApplyMask / ProtectMask / BlockMask`에 연결한다.

### 원칙 5
기본 엔진이 몰라도 되는 것이 아니라,  
**문서가 먼저 가르쳐야 한다.**
