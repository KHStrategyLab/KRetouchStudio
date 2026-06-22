# CORE 2D Render Tricks

## 문서 역할

이 문서는 무거운 외부 3D 렌더러 없이도 KRetouchPro의 최종 결과물을 더 비싸 보이게 만드는 초경량 후반 렌더 트릭 3가지를 정리한다.

- 목적: 적은 연산으로 결과물 인상을 크게 끌어올린다.
- 위치: 검출/워프 이후, 최종 출력 직전의 후반 렌더 패스
- 성격: 부위 검출식이 아니라 출력 스타일링 기법

## 1. 3D LUT (Look-Up Table)

### 목적

포토샵이나 라이트룸에서 미리 만든 색감 프로파일을 엔진에서 즉시 적용한다.

### 원리

원본 RGB 값을 3차원 색상 배열의 인덱스로 사용해 미리 준비된 결과 색으로 치환한다.

\[
Result_{RGB} = LUT[Original_R][Original_G][Original_B]
\]

### 해설

- 연산량은 매우 낮고, 색감 변화는 매우 크다.
- `웜톤 피부`, `투명한 쿨톤`, `증명사진 표준톤` 같은 프리셋을 LUT로 보관할 수 있다.
- UI에서는 버튼 또는 프로파일 선택으로 LUT만 갈아끼우면 된다.

### 구현 포인트

- 단순 nearest lookup보다 `trilinear interpolation` 지원이 더 자연스럽다.
- LUT 적용은 피부/배경 개별 보정보다 뒤쪽에 두는 편이 안전하다.
- 출력 전역 톤 통일용 패스로 쓰는 것이 좋다.

### 권장 위치

\[
LocalRetouch \rightarrow LUT \rightarrow FinalComposite
\]

## 2. Fake Global Illumination

### 목적

3D 조명 없이도 위는 화사하고 아래는 차분한 공간감을 만들어 인물을 더 입체적으로 보이게 한다.

### 원리

두 장의 선형 그라데이션을 사용한다.

- 상단 따뜻한 빛: `Screen Blend`
- 하단 차가운 그림자: `Multiply Blend`

### 상단 광원식

\[
TopLight(x, y) = Gradient_{warm}(y)
\]

\[
Result_{top} = 1.0 - (1.0 - Original) \cdot (1.0 - TopLight)
\]

### 하단 그림자식

\[
BottomShade(x, y) = Gradient_{cool}(y)
\]

\[
Result_{bottom} = Original \cdot BottomShade
\]

### 해설

- 사람 픽셀 자체를 직접 다시 검출하지 않아도 전체 분위기를 빠르게 바꿀 수 있다.
- 위쪽은 따뜻하게 떠오르고 아래쪽은 눌리면서 깊이감이 생긴다.
- 필요하면 `Mask_person`를 곱해서 인물 중심으로만 제한할 수 있다.

### 구현 포인트

- 전역 패스로 쓰되, 과하면 값싸 보이므로 강도 슬라이더는 약하게 시작한다.
- 사람만 살리고 싶으면:

\[
LightMask = Mask_{person}
\]

\[
Result = Original + (LitResult - Original) \cdot LightMask
\]

### 권장 위치

\[
LUT \rightarrow FakeGI \rightarrow FinalSharpen
\]

## 3. High-Pass Overlay Sharpen

### 목적

피부 노이즈는 크게 건드리지 않으면서 눈, 머리카락, 윤곽선만 크리스탈처럼 또렷하게 만든다.

### 원리

원본에서 블러를 뺀 고주파 성분만 추출한 뒤, 이를 오버레이 또는 소프트 라이트로 다시 합성한다.

\[
Blur = GaussianBlur(Original, Radius)
\]

\[
HighPass = Original - Blur + 0.5
\]

### 오버레이 합성

\[
Result = Overlay(Original, HighPass)
\]

### 소프트 라이트 합성

\[
Result = SoftLight(Original, HighPass)
\]

### 해설

- 일반 언샵 마스크보다 출력 인상이 더 세련되게 잡힌다.
- 피부 전체를 세게 세우는 것이 아니라 극단적인 경계선을 더 또렷하게 만든다.
- 증명사진 특유의 평평한 느낌을 줄이는 데 효과적이다.

### 구현 포인트

- `Radius`와 `Amount`를 분리해야 한다.
- 강도를 너무 높이면 피부 잔노이즈가 살아난다.
- 최종 출력 직전 마지막 단계에 두는 것이 가장 안정적이다.

### 권장 위치

\[
ToneFinish \rightarrow HighPassSharpen \rightarrow Clamp \rightarrow Export
\]

## 권장 파이프라인

\[
Masking \rightarrow Warp \rightarrow LocalTone \rightarrow LUT \rightarrow FakeGI \rightarrow HighPassSharpen \rightarrow Clamp \rightarrow Export
\]

## 결론

이 세 기법은 부위 검출 로직을 다시 짜지 않아도 된다.

- `3D LUT` = 색감
- `Fake GI` = 공간감
- `High-Pass Overlay Sharpen` = 최종 또렷함

즉, 무거운 새 엔진 없이도 후반 렌더 스택만 얹어서 결과물의 체감 품질을 크게 올릴 수 있다.
