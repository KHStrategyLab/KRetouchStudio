# Core Formulas

모든 픽셀 색상과 마스크 값은 `0.0 ~ 1.0` 정규화 기준을 따른다.

## Related CORE Documents

- [CORE Formula Companion](CORE_FORMULA_COMPANION.md)
- [CORE 2D Render Tricks](CORE_2D_RENDER_TRICKS.md)

## Core Rules

### Meaning Of `1`

그래픽 프로그래밍에서 `1`은 단순한 숫자 1이 아니라 `100%(전체 이미지)`를 뜻하는 기준값이다.

- `1 = ImageDomain`
- `1 = 전체 기준판`
- `1 = 100% 선택`

즉, 모든 마스크 연산은 기본적으로 `1`이라는 전체 기준판 안에서 정의된다.

### Meaning Of `1 - Mask`

`1 - Mask`는 현재 마스크를 뒤집는 `Invert` 또는 `반대쪽 영역 선택`을 뜻한다.

\[
InvertedMask = 1.0 - Mask
\]

\[
BackgroundMask = 1.0 - PersonMask
\]

이 식은 의미상 `전체 공간에서 현재 선택된 마스크를 빼라`는 뜻이다.

### Pixel Interpretation Examples

- 사람 중심 픽셀: `PersonMask = 1.0` 이면 `1.0 - 1.0 = 0.0`
- 배경 픽셀: `PersonMask = 0.0` 이면 `1.0 - 0.0 = 1.0`
- 경계선 픽셀: `PersonMask = 0.7` 이면 `1.0 - 0.7 = 0.3`

즉, 경계에서는 사람과 배경이 부드럽게 섞인다.

### Base Compositing Formula

조건문 대신 마스크 산술식으로 바로 합성한다.

\[
Result = Mask \cdot Foreground + (1.0 - Mask) \cdot Background
\]

이 방식은 `if / else` 분기 없이 사람 쪽과 배경 쪽을 동시에 계산하므로 픽셀 튐을 줄이고 렌더링 파이프라인에 자연스럽게 연결된다.

### Notes

#### 1. 컴퓨터 그래픽스에서 `1` = `100% (전체)`다

우리가 픽셀 값을 `0.0 ~ 1.0`으로 정규화했을 때:

- `0.0 = 0%`
- `1.0 = 100%`

즉, 수식에서 `1`은 그냥 숫자 1이 아니라 이 픽셀이 가질 수 있는 최대치이자 전체 기준판을 뜻한다.

#### 2. `1 - Mask`는 `반전(Invert)`이다

전체가 `1.0`이고 사람이 `0.7`이면 남은 값은 다음과 같다.

\[
1.0 - 0.7 = 0.3
\]

즉, 현재 마스크를 전체에서 빼서 반대쪽 영역, 남은 영역, 여집합을 구하는 뜻이다.

- 사람이 `1.0`이면: `1.0 - 1.0 = 0.0`
- 배경이 `0.0`이면: `1.0 - 0.0 = 1.0`
- 경계가 `0.4`이면: `1.0 - 0.4 = 0.6`

그래서 경계 픽셀에서는 사람과 배경이 비율대로 자연스럽게 섞인다.

#### 3. 결론

수식에 있는 `1`은 전체 `100%`에서 현재 마스크 값을 빼서 그 나머지 영역을 구하라는 뜻이다.

이 방식을 쓰면 픽셀마다 `if` 분기를 거는 대신 연속적인 산술식으로 합성할 수 있어서, CPU/GPU 렌더링 파이프라인에 더 자연스럽고 효율적으로 연결된다.

## Part 1: Space Partition And Masks

01. 사람 마스크  
정의: 배경과 사람을 분리하는 절대 기준 마스크.  
\[
Mask_{person}(x, y) \in [0, 1]
\]

02. 얼굴 방수 구역  
정의: 랜드마크 중심 반경 기준의 타원형 얼굴 보호 영역.  
\[
Mask_{face}(x, y) = 1.0 - \mathrm{SmoothStep}(0.62, 1.06, d^2)
\]

03. 순수 헤어 영역  
정의: 사람 마스크에서 얼굴 영역을 제외한 헤어 전용 영역.  
\[
Mask_{hair}(x, y) = Mask_{person}(x, y) \cdot (1.0 - Mask_{face}(x, y))
\]

04. 목 회전축  
정의: 고개 회전 시 어깨 파손을 막기 위한 기준 Y축.  
\[
NeckPivot_Y = FaceBox_Y + (FaceBox_{Height} \cdot 0.4)
\]

05. 노이즈 구멍 채우기  
정의: 마스크 내부의 작은 빈 구멍을 닫는 정리식.  
\[
Mask_{clean} = MorphologicalClose(Mask_{raw}, Kernel)
\]

06. 부드러운 경계  
정의: 마스크 경계를 부드럽게 만드는 기본 보간식.  
\[
S(x) = x^2 \cdot (3.0 - 2.0x)
\]

07. 어깨 보호 방수벽  
정의: 목 아래 인물 픽셀 이동을 막는 감쇠 마스크.  
\[
Damping(x, y) =
\begin{cases}
1.0, & y > NeckPivot_Y \text{ and } Mask_{person}(x, y) > 0.5 \\
0.0, & \text{otherwise}
\end{cases}
\]

07A. Upper Head Block And Shoulder Detection Rule  
Definition: Upper-body symmetry must use the existing background alpha/person alpha mask as the primary source for the large visible person silhouette. This includes the full head, hair mass, neck, shoulders, and upper body. FaceMesh proportions are only a fallback when alpha data is missing.

Primary flow:

1. Use FaceMesh only for `Chin`, jawline, face bounds, and `CenterX`.
2. Scan the alpha foreground silhouette from the head/hair region through the shoulder region.
3. Extract left and right foreground boundaries from the alpha contour.
4. Treat the head/hair region as one frozen block and correct large head tilt as a block transform.
5. Start below the chin and find the neck row from the narrowest valid foreground width.
6. Find `ShoulderLeft` and `ShoulderRight` where the contour expands outward from the neck toward the body boundary.
7. Derive `ShoulderLine` from `ShoulderLeft` and `ShoulderRight`.
8. Use the head block controls plus the shoulder line for the `Upper` button's large silhouette balance.

\[
Foreground(x, y) = Alpha_{person}(x, y) > T
\]

\[
ContourRow(y) = [X_{left}(y), X_{right}(y)]
\]

\[
NeckRow = \arg\min_y (X_{right}(y) - X_{left}(y)),\quad y > Chin_Y
\]

\[
ShoulderLeft =
FirstRowWhere(X_{left}^{neck} - X_{left}(y) \ge k \cdot MaxLeftExtension)
\]

\[
ShoulderRight =
FirstRowWhere(X_{right}(y) - X_{right}^{neck} \ge k \cdot MaxRightExtension)
\]

\[
HeadBlock' = Rotate(HeadBlock,\ Pivot_{neck},\ -HeadTilt) + CenterShift
\]

Neck anchors should later be refined from jawline, face centerline, skin-tone continuity, and clothing boundary inside the alpha foreground. This refinement is separate from the first shoulder-line extraction step.

## Part 2: Geometric Warp

08. 2D 로컬 회전  
정의: 기준축을 중심으로 얼굴 또는 부위를 국소 회전한다.  
\[
X_{new} = Pivot_X + (x - Pivot_X)\cos(A) - (y - Pivot_Y)\sin(A)
\]
\[
Y_{new} = Pivot_Y + (x - Pivot_X)\sin(A) + (y - Pivot_Y)\cos(A)
\]

09. 거리 감쇠  
정의: 워프 중심에서 멀어질수록 힘을 줄이는 브러시 감쇠식.  
\[
W_{dist} = (1.0 - d)^2 \cdot (3.0 - 2.0(1.0 - d))
\]

10. 방향성 밀기  
정의: 사용자가 민 방향으로 픽셀을 이동시키는 기본 워프식.  
\[
P_{new} = P_{old} + Vector \cdot W_{dist} \cdot (1.0 - Damping)
\]

11. 방사형 수축  
정의: 중심점을 향해 끌어당겨 턱선이나 볼살을 줄인다.  
\[
\Delta = (Center - P_{old}) \cdot W_{dist} \cdot Strength
\]

12. 방사형 팽창  
정의: 중심점에서 바깥으로 밀어내어 눈이나 볼륨을 키운다.  
\[
\Delta = (P_{old} - Center) \cdot W_{dist} \cdot Strength
\]

13. 대칭 보정  
정의: 목표 대칭선 기준으로 좌우 비대칭을 완화한다.  
\[
X_{new} = X_{old} + (X_{target} - X_{old}) \cdot Balance \cdot W_{dist}
\]

14. 안면 비율 조절  
정의: 특정 부위를 Y축 방향으로 이동시켜 비율을 조정한다.  
\[
Y_{new} = Y_{old} + \Delta Y_{feature} \cdot Mask_{face} \cdot (1.0 - Damping)
\]

15. 픽셀 보간  
정의: 소수점 좌표를 주변 4픽셀의 가중 평균으로 샘플링한다.  
\[
Color(x, y) = \sum_{i=0}^{1}\sum_{j=0}^{1} Color(\lfloor x \rfloor + i,\lfloor y \rfloor + j) \cdot W(i, j)
\]

## Part 3: Tone, Color, And Light

16. 명도 추출  
정의: RGB를 시각 가중 명도로 변환한다.  
\[
L = 0.2126R + 0.7152G + 0.0722B
\]

17. 노출 보정  
정의: 하이라이트 보호값을 고려해 밝기를 올리거나 내린다.  
\[
Result = Original + Offset \cdot (1.0 - HighlightProtection)
\]

18. 대비 조절  
정의: 중간 회색을 기준으로 명암 차를 확대 또는 축소한다.  
\[
Result = (Original - 0.5) \cdot ContrastFactor + 0.5
\]

19. 채도 조절  
정의: 명도는 유지하고 색 차이만 증폭 또는 감소시킨다.  
\[
Result_{RGB} = L + (Original_{RGB} - L) \cdot SaturationFactor
\]

20. 커브 매핑  
정의: LUT를 참조해 비선형 톤 조정을 적용한다.  
\[
Result_{RGB} = LUT[Original_{RGB}] \cdot Amount
\]

21. 화이트 밸런스  
정의: 적색과 청색 채널 게인을 조절해 색온도를 맞춘다.  
\[
R_{new} = R \cdot Gain_R,\quad B_{new} = B \cdot Gain_B
\]

22. 피부톤 균일화  
정의: 원본과 블러를 혼합해 얼룩 톤을 부드럽게 정리한다.  
\[
Result_{RGB} = Original_{RGB} \cdot (1 - Amount) + Blur_{RGB} \cdot Amount
\]

23. 소프트 블러  
정의: 주변 픽셀의 가우시안 가중 평균으로 부드럽게 확산한다.  
\[
Color_{blur} = \frac{\sum (Color_i \cdot Kernel_i)}{\sum Kernel_i}
\]

24. 샤프닝  
정의: 블러와의 차이를 원본에 더해 윤곽을 강화한다.  
\[
Result_{RGB} = Original_{RGB} + (Original_{RGB} - Blur_{RGB}) \cdot SharpenAmount
\]

## Part 4: Retouching And Compositing

25. 배경 교체 합성  
정의: 인물은 유지하고 배경만 대체 색상 또는 이미지로 치환한다.  
\[
Result = Mask_{person} \cdot Original + (1.0 - Mask_{person}) \cdot Color_{BG}
\]

26. 새치 검출  
정의: 밝고 채도가 낮은 픽셀을 새치 후보로 분리한다.  
\[
Mask_{gray} =
\begin{cases}
1.0, & L > Thresh_L \text{ and } Saturation < Thresh_S \\
0.0, & \text{otherwise}
\end{cases}
\]

27. 새치 커버  
정의: 새치 후보의 명도를 낮춰 주변 헤어 톤에 맞춘다.  
\[
L_{new} = L_{old} \cdot (1.0 - Amount \cdot Mask_{gray})
\]

28. 머리 염색  
정의: 명도는 보존하고 대상 색상을 헤어 마스크에만 혼합한다.  
\[
Result_{RGB} = L_{old} + (Color_{Target} - L_{old}) \cdot Amount \cdot Mask_{hair}
\]

29. 로컬 마스크 블렌딩  
정의: 필터 결과를 지정 마스크 영역 안에서만 가중 적용한다.  
\[
Final = Original + (Filtered - Original) \cdot Mask_{local}(x, y)
\]

30. 클램핑  
정의: 연산 결과를 안전한 정규화 범위로 제한한다.  
\[
Output = \max(0.0, \min(1.0, Result))
\]

## Part 5: High-End Skin Retouching

31. 저주파 톤  
정의: 피부의 큰 얼룩과 색 흐름만 남기는 저주파 분리식.  
\[
Low = GaussianBlur(Original, Sigma)
\]

32. 고주파 질감  
정의: 모공과 잔주름 등 세부 질감만 따로 분리한다.  
\[
High = Original - Low + 0.5
\]

33. 주파수 재합성  
정의: 보정된 저주파와 고주파를 다시 합쳐 자연스럽게 복원한다.  
\[
Result = Low_{edited} + High_{edited} - 0.5
\]

## Part 6: Blemish Removal And Pixel Clone

34. 포아송 복제  
정의: 마스크 내부를 주변 조명과 이어지게 심리스하게 복제한다.  
\[
\nabla^2(Result) = \nabla^2(Source)
\]

35. 페더링 블렌딩  
정의: 빠른 복제를 위해 소스와 타깃 패치를 알파 혼합한다.  
\[
Result = Source_{patch} \cdot Alpha + Target_{patch} \cdot (1.0 - Alpha)
\]

## Part 7: Dimensional Light

36. 닷지  
정의: 명부를 끌어올려 하이라이트를 강조한다.  
\[
Result = \min(1.0, \frac{Original}{1.0 - Mask_{dodge}})
\]

37. 번  
정의: 암부를 눌러 그림자와 입체감을 강화한다.  
\[
Result = \max(0.0, 1.0 - \frac{1.0 - Original}{Mask_{burn}})
\]

## Part 8: Local Whitening

38. 치아/흰자위 미백  
정의: 채도는 낮추고 명도는 올려 국소 미백을 만든다.  
\[
Sat_{new} = Sat_{old} \cdot (1.0 - Amount)
\]
\[
L_{new} = L_{old} + (1.0 - L_{old}) \cdot Amount
\]

39. 눈동자 맑게  
정의: 홍채 평균 밝도를 기준으로 대비를 높여 선명도를 준다.  
\[
Result = (Original - Mean_{Iris}) \cdot ContrastFactor + Mean_{Iris}
\]

40. 적목 제거  
정의: 과도한 적색 채널을 녹청 평균 이하로 눌러 적목을 줄인다.  
\[
R_{new} = \min\left(R, \frac{G + B}{2}\right)
\]

## Part 9: Digital Makeup

41. 곱하기 혼합  
정의: 립이나 섀도우를 피부 톤에 자연스럽게 곱해 물들인다.  
\[
Result = Original \cdot Color_{Makeup}
\]

42. 스크린 혼합  
정의: 하이라이터나 광채 메이크업을 밝게 얹는다.  
\[
Result = 1.0 - (1.0 - Original) \cdot (1.0 - Color_{Makeup})
\]

43. 컬러 모드  
정의: 원본 명도는 유지하고 메이크업의 색상과 채도만 덮는다.  
\[
Result_{LCH} = (L_{Original}, C_{Makeup}, H_{Makeup})
\]

## Part 10: Final Finishing

44. 비네팅  
정의: 외곽을 어둡게 눌러 시선을 중심으로 모은다.  
\[
Result = Original \cdot \left(1.0 - Amount \cdot \left(\frac{Distance}{Max_{Radius}}\right)^2\right)
\]

45. 필름 노이즈  
정의: 미세한 가우시안 노이즈를 더해 디지털 인공감을 줄인다.  
\[
Result = Clamp(Original + GaussianNoise \cdot Amount)
\]
