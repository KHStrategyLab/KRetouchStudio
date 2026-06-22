# KRetouchPro Photographic Terms Location And Retouch Dictionary

Last updated: 2026-06-15

## Purpose

This document defines practical photographic terms for KRetouchPro.

The purpose is not to preserve every historical darkroom term.
The purpose is to keep terms that can help the app, Codex, and future engine code describe:

- tone and exposure
- color and white balance
- contrast and gradation
- sharpness and blur
- lens distortion and optical artifacts
- portrait lighting
- cropping and framing
- retouch-like darkroom operations
- print/film reference terms that are useful as analogies

Use stable English IDs in code and logs.
Keep Korean labels as display aliases.

## Selection Rule

The uploaded glossary contains many classic photography, film, darkroom, camera, and optical terms.
For KRetouchPro, prioritize terms that affect portrait retouching, preview rendering, color correction, lighting interpretation, background replacement, face/body masking, and export quality.

Terms that are mostly historical or equipment-only may remain as reference terms, but should not drive product behavior unless a feature needs them.

---

# 1. Tone, Exposure, and Density Terms

## Exposure / 노출

- **Type**: capture/tone term
- **Definition**: Amount of light recorded by film or sensor.
- **KRetouchPro use**: Base concept for brightness, exposure slider, highlight/shadow recovery, and preview tone response.
- **Do not confuse with**: Brightness display only. Exposure implies capture-level light amount.

## OverExposure / 노출 과다

- **Type**: exposure status
- **Definition**: Image or region received too much light and appears too bright or washed out.
- **Visual cues**: Pale skin, clipped highlights, lost texture in bright areas.
- **KRetouchPro use**: Highlight warning, skin highlight protection, white clothing protection.
- **Do not confuse with**: Intentional high-key lighting.

## UnderExposure / 노출 부족

- **Type**: exposure status
- **Definition**: Image or region received too little light and appears too dark.
- **Visual cues**: Dark face, blocked shadows, weak skin detail.
- **KRetouchPro use**: Shadow lift, face exposure correction, low-light portrait cleanup.
- **Do not confuse with**: Intentional low-key lighting.

## Density / 농도

- **Type**: tone/darkroom term
- **Definition**: Dark or light density of a photographic material or image region.
- **KRetouchPro use**: Useful analogy for how dark or thick a tone area appears.
- **Modern mapping**: Pixel luminance, shadow density, print density.

## Gradation / 계조

- **Type**: tone term
- **Definition**: Continuous tonal steps from shadow to highlight.
- **KRetouchPro use**: Skin tone smoothness, highlight-to-shadow rolloff, curve adjustment.
- **Do not confuse with**: Contrast only. Gradation is about tonal transition quality.

## Gamma / 감마

- **Type**: tone curve term
- **Definition**: Slope or midtone response of a photographic characteristic curve.
- **KRetouchPro use**: Midtone curve adjustment and preview tone response.
- **Modern mapping**: Gamma correction or midtone curve slope.

## Contrast / 콘트라스트

- **Type**: tone term
- **Definition**: Difference between light and dark areas.
- **KRetouchPro use**: Contrast slider, local contrast, face contrast balancing.
- **Do not confuse with**: Sharpness. Contrast is tonal separation.

## ContrastGrade / 콘트라스트 등급

- **Type**: print reference term
- **Definition**: Print-paper contrast strength classification.
- **KRetouchPro use**: Reference concept for soft/normal/hard tone presets.
- **Modern mapping**: Contrast curve preset.

## HighContrast / 경조

- **Type**: contrast status
- **Definition**: Strong separation between highlights and shadows.
- **Visual cues**: Deep shadows, bright highlights, punchy image.
- **KRetouchPro use**: Tone warning and portrait softening controls.

## LowContrast / 연조

- **Type**: contrast status
- **Definition**: Soft tonal separation with gentle transitions.
- **Visual cues**: Flat or soft image, weak separation.
- **KRetouchPro use**: Add contrast, restore face separation, haze reduction.

## Brightness / 명도

- **Type**: color/tone term
- **Definition**: Relative brightness or darkness of a color or image area.
- **KRetouchPro use**: Brightness adjustment, skin luminosity matching.
- **Do not confuse with**: Exposure. Brightness may be display or pixel-level.

## MidTone / 중간 톤

- **Type**: tone range
- **Definition**: Tone range between deep shadows and bright highlights.
- **KRetouchPro use**: Skin tone lives mostly in midtones; important for portrait correction.

## MiddleGray / 중간회색

- **Type**: exposure reference
- **Definition**: Standard 18% gray reference used for exposure measurement.
- **KRetouchPro use**: White balance and exposure calibration analogy.
- **Modern mapping**: Neutral reference patch.

## GrayCard / 그레이 카드

- **Type**: calibration reference
- **Definition**: 18% reflectance gray card used to measure neutral exposure.
- **KRetouchPro use**: Neutral reference picker, white balance reference, exposure normalization.

## GrayScale / 그레이 스케일

- **Type**: tonal reference
- **Definition**: Step chart from white to black used for tone comparison.
- **KRetouchPro use**: Debug tone ramp, curve QA, export consistency check.

## ZoneSystem / 존 시스템

- **Type**: tone planning system
- **Definition**: System that divides tonal values into zones from black to white.
- **KRetouchPro use**: Helpful future concept for highlight/shadow warning and print-like tone preview.
- **Do not overbuild**: Use as reference, not a required feature.

## ReciprocityLaw / 상반 법칙

- **Type**: exposure relation
- **Definition**: Equivalent exposure can be produced by reciprocal shutter/aperture changes.
- **KRetouchPro use**: Reference only. Not directly needed for retouch engine.

## ReciprocityFailure / 상반칙 불궤

- **Type**: film exposure defect
- **Definition**: Film response failure during extremely short or long exposure.
- **KRetouchPro use**: Legacy reference only, unless simulating film look.

## ExposureLatitude / 관용도

- **Type**: capture tolerance
- **Definition**: Range of exposure error that can still be corrected acceptably.
- **KRetouchPro use**: Recovery limit concept for over/underexposed files.
- **Modern mapping**: Dynamic range and recoverable tonal range.

---

# 2. Color and Light Spectrum Terms

## VisibleLight / 가시광선

- **Type**: light spectrum term
- **Definition**: Wavelength range visible to humans, roughly 400-700 nm.
- **KRetouchPro use**: Base concept for RGB color and lighting interpretation.

## AdditiveColor / 가색법

- **Type**: color model
- **Definition**: Color mixing using light primaries red, green, and blue.
- **KRetouchPro use**: Screen preview, RGB channel operations.
- **Modern mapping**: RGB display color.

## SubtractiveColor / 감색법

- **Type**: color model
- **Definition**: Color mixing using cyan, magenta, and yellow dyes or inks.
- **KRetouchPro use**: Print preview analogy, color correction explanation.
- **Modern mapping**: CMY/CMYK print logic.

## PrimaryColor / 기본색

- **Type**: color concept
- **Definition**: Basic colors used to create other colors through mixing.
- **KRetouchPro use**: RGB/CMY channel naming and color math.

## ComplementaryColor / 보색

- **Type**: color relation
- **Definition**: Opposing color pair that neutralizes or balances color.
- **KRetouchPro use**: Color cast correction, skin redness/green cast balancing.

## ColorSensitivity / 감색성

- **Type**: film/color response
- **Definition**: How film or material responds to different colors of light.
- **KRetouchPro use**: Reference for sensor/profile response and channel sensitivity.

## ColorTemperature / 색온도

- **Type**: lighting color term
- **Definition**: Warm or cool quality of light, expressed in Kelvin.
- **Visual cues**: Low values look warm/red-yellow; high values look cool/blue.
- **KRetouchPro use**: White balance, studio light correction, skin color correction.

## KelvinTemperature / 캘빈도

- **Type**: color temperature unit
- **Definition**: Unit used to describe color temperature.
- **KRetouchPro use**: White balance UI or metadata display.

## ColorBalance / 색채균형

- **Type**: color correction term
- **Definition**: Balanced reproduction of colors under a given light source.
- **KRetouchPro use**: White balance, tint correction, skin neutralization.

## ColorAdaptation / 색순응

- **Type**: visual perception term
- **Definition**: Human vision adapts to lighting color and still perceives familiar colors.
- **KRetouchPro use**: Reminder that camera color cast may differ from human memory.
- **Do not confuse with**: Actual pixel neutrality.

## ColorReflection / 색반사

- **Type**: color contamination
- **Definition**: Colored surface reflects its color onto nearby subject.
- **KRetouchPro use**: Skin color cast detection near clothes, walls, or backgrounds.
- **Example use**: Red dress reflecting onto neck or chin.

## ColorCompensatingFilter / 색보정 필터

- **Type**: color correction reference
- **Definition**: Filter used to correct color balance.
- **KRetouchPro use**: Digital equivalent is channel/tint correction.

## ColorConversionFilter / 색온도 변환필터

- **Type**: color temperature correction reference
- **Definition**: Filter used to match film and light color temperature.
- **KRetouchPro use**: Digital white-balance preset analogy.

## NDFilter / ND 필터

- **Type**: optical filter
- **Definition**: Neutral-density filter that reduces light without changing color.
- **KRetouchPro use**: Reference only; may help explain long-exposure looks.

## UVFilter / UV 필터

- **Type**: optical filter
- **Definition**: Filter that absorbs ultraviolet haze and protects lens.
- **KRetouchPro use**: Haze reduction analogy, not direct portrait retouch term.

## CircularPolarizer / 원 편광 필터, CPL

- **Type**: optical filter
- **Definition**: Filter that reduces reflections and controls polarized light.
- **KRetouchPro use**: Reflection-control reference for glasses/skin/oily highlights.

## TungstenLight / 텅스텐 광

- **Type**: light source
- **Definition**: Warm incandescent light from tungsten filament.
- **KRetouchPro use**: Warm color cast correction.

## DaylightFilm / 일광용 필름

- **Type**: film color balance reference
- **Definition**: Film balanced for daylight around 5500-6000K.
- **KRetouchPro use**: Daylight white balance reference.

## TungstenFilm / 텅스텐 필름

- **Type**: film color balance reference
- **Definition**: Film balanced for tungsten light around 3200K.
- **KRetouchPro use**: Warm-light correction reference.

---

# 3. Sharpness, Blur, Texture, and Defect Terms

## Sharpness / 선명도

- **Type**: image detail term
- **Definition**: Perceived clarity of fine detail and edge definition.
- **KRetouchPro use**: Sharpen slider, preview clarity, hair/eye detail preservation.
- **Do not confuse with**: Contrast alone.

## Blur / 부러, Blur

- **Type**: image defect or effect
- **Definition**: Loss of sharpness caused by motion, camera shake, defocus, or intentional softening.
- **KRetouchPro use**: Blur slider, background blur, skin smoothing caution.
- **Do not confuse with**: Skin retouching. Skin smoothing should not blur eyes/mouth/hair.

## SoftFilter / 소프트필터

- **Type**: optical effect
- **Definition**: Filter producing soft-focus appearance.
- **KRetouchPro use**: Soft portrait effect reference.
- **Do not overuse**: Avoid making all retouch results look smeared.

## DutoFilter / 듀토

- **Type**: soft-focus optical filter
- **Definition**: Soft-focus filter with optical pattern producing real+ghost image effect.
- **KRetouchPro use**: Legacy portrait-softening reference.

## Reticulation / 망상효과

- **Type**: film defect
- **Definition**: Wrinkled emulsion pattern from sudden temperature change during processing.
- **KRetouchPro use**: Film-damage restoration reference only.

## NewtonRing / 뉴우톤 링

- **Type**: optical/contact defect
- **Definition**: Ring-shaped interference pattern caused by contact between film and glass.
- **KRetouchPro use**: Old photo scan defect classification.

## Vignette / 비네트

- **Type**: optical effect or intentional tone effect
- **Definition**: Darkening or exposure falloff toward image edges.
- **KRetouchPro use**: Edge darkening correction, intentional portrait focus effect.
- **Do not confuse with**: Background shadow.

## Flare / 플레어

- **Type**: optical artifact
- **Definition**: Veiling light or ghosting caused by internal lens reflections.
- **KRetouchPro use**: Low contrast haze correction, backlight artifact handling.

## Spotting / 스포팅

- **Type**: retouch/darkroom operation
- **Definition**: Removing small dust spots or white defects on print by painting or retouching.
- **KRetouchPro use**: Spot removal, dust cleanup, blemish retouch analogy.

## Etching / 에칭

- **Type**: retouch/darkroom operation
- **Definition**: Scraping or removing dark defects from image material.
- **KRetouchPro use**: Dark-spot removal analogy.
- **Do not confuse with**: Modern destructive pixel erasing.

## BlockedUp / 블럭 업

- **Type**: shadow/high-density defect
- **Definition**: Detail becomes unclear due to too much density or excessive dark mass.
- **KRetouchPro use**: Shadow detail loss warning.

## Irradiation / 이라데이션

- **Type**: film/light spread defect
- **Definition**: Light spreads in emulsion from overexposure and harms image boundaries.
- **KRetouchPro use**: Bloom/halation artifact analogy.

## Halftone / 망판

- **Type**: print reproduction term
- **Definition**: Image reproduced by dot pattern of varying size or spacing.
- **KRetouchPro use**: Scanned-print restoration and moire/dot-pattern cleanup.

---

# 4. Lens, Optics, Perspective, and Geometry Terms

## FocalLength / 초점거리

- **Type**: lens term
- **Definition**: Distance related to the lens's imaging angle and magnification.
- **KRetouchPro use**: Face perspective interpretation, portrait distortion warnings.

## WideAngleLens / 광각 렌즈

- **Type**: lens category
- **Definition**: Lens with wider view than normal, often shorter focal length.
- **Visual effect**: Expands space, exaggerates near objects.
- **KRetouchPro use**: Face distortion correction, body proportion caution.

## TelephotoLens / 망원렌즈

- **Type**: lens category
- **Definition**: Lens with narrow view and longer focal length.
- **Visual effect**: Compresses distance and perspective.
- **KRetouchPro use**: Portrait compression understanding.

## TelephotoEffect / 망원효과

- **Type**: perspective effect
- **Definition**: Distant subjects appear compressed and closer together.
- **KRetouchPro use**: Body/face proportion analysis.

## FisheyeLens / 어안렌즈

- **Type**: extreme wide-angle lens
- **Definition**: Lens with very wide angle, often around 180 degrees.
- **Visual effect**: Strong curved distortion.
- **KRetouchPro use**: Heavy distortion warning.

## MacroLens / 매크로 렌즈, 마이크로 렌즈

- **Type**: lens category
- **Definition**: Lens for close-up photography of small subjects.
- **KRetouchPro use**: Skin detail or close-up portrait context.

## CloseUp / 클로즈 업

- **Type**: framing/capture term
- **Definition**: Photographing a subject from close distance so it appears large.
- **KRetouchPro use**: Face close-up, skin detail, crop classification.

## Distortion / 왜곡

- **Type**: geometric defect/effect
- **Definition**: Straight lines or subject proportions appear bent or stretched.
- **KRetouchPro use**: Face/body geometry correction, lens profile correction.

## BarrelDistortion / 술통형 왜곡

- **Type**: lens distortion
- **Definition**: Straight lines bow outward from image center.
- **KRetouchPro use**: Wide-angle correction and face-edge caution.

## SphericalAberration / 구면수차

- **Type**: lens aberration
- **Definition**: Rays from lens center and edge focus differently, softening image.
- **KRetouchPro use**: Lens softness reference.

## Astigmatism / 비점수차

- **Type**: lens aberration
- **Definition**: Oblique rays do not focus correctly, causing directional blur.
- **KRetouchPro use**: Lens defect reference only.

## ChromaticAberration / 색수차

- **Type**: lens aberration
- **Definition**: Different wavelengths focus or magnify differently, creating color fringes.
- **KRetouchPro use**: Purple/green fringe correction, hair-edge cleanup.

## FieldCurvature / 상면만곡

- **Type**: lens aberration
- **Definition**: Image forms on curved surface instead of flat plane.
- **KRetouchPro use**: Edge softness reference.

## AsphericalLens / 비구면 렌즈

- **Type**: lens design
- **Definition**: Lens element designed to reduce spherical aberration and improve image quality.
- **KRetouchPro use**: Reference only.

## ApochromatLens / 아포크로매트 렌즈

- **Type**: lens design
- **Definition**: Lens corrected for chromatic aberration across multiple wavelengths.
- **KRetouchPro use**: Reference only.

## ImageCircle / 이미지서클

- **Type**: lens coverage term
- **Definition**: Circle of usable image projected by lens.
- **KRetouchPro use**: Reference for crop/edge quality.

## CircleOfConfusion / 착란원

- **Type**: focus/depth term
- **Definition**: Blur circle size that may still be perceived as acceptably sharp.
- **KRetouchPro use**: Depth-of-field and background blur logic reference.

## DepthOfField / 피사계 심도

- **Type**: focus range term
- **Definition**: Distance range that appears acceptably sharp.
- **KRetouchPro use**: Subject/background separation, blur simulation, portrait depth.

## DepthOfFocus / 초점심도

- **Type**: optical focus tolerance
- **Definition**: Acceptable focus tolerance around the film/sensor plane.
- **KRetouchPro use**: Reference only.

## HyperfocalDistance / 과초점거리

- **Type**: focus planning term
- **Definition**: Focus distance that maximizes depth of field toward infinity.
- **KRetouchPro use**: Reference only.

## PerspectiveControl / 쉬프트 렌즈, PC렌즈

- **Type**: perspective correction
- **Definition**: Lens movement used to correct perspective distortion.
- **KRetouchPro use**: Geometry correction analogy for verticals and body perspective.

## CameraMovement / 카메라 무브먼트

- **Type**: view camera operation
- **Definition**: Lens/film plane movements like tilt, shift, rise, fall, swing.
- **KRetouchPro use**: Perspective and geometric correction reference.

## Tilt / 틸트

- **Type**: camera/lens movement or motion effect
- **Definition**: Tilting lens/film plane, or camera motion depending on context.
- **KRetouchPro use**: Keep separate from Head Carrier Tilt. Head Carrier Tilt is portrait geometry, not camera tilt.

---

# 5. Lighting Terms for Portrait Interpretation

## MainLight / 주광, KeyLight / 키 라이트

- **Type**: portrait lighting term
- **Definition**: Primary light source defining face volume and texture.
- **KRetouchPro use**: Face light direction estimation, shadow/highlight interpretation.

## FillLight / 보조광

- **Type**: portrait lighting term
- **Definition**: Secondary light that lightens shadows created by the main light.
- **KRetouchPro use**: Shadow softness, portrait contrast analysis.

## SideLighting / 측면광

- **Type**: lighting direction
- **Definition**: Light coming from the left or right side of the subject.
- **Visual effect**: Strong form and facial dimensionality.
- **KRetouchPro use**: Face shadow detection and asymmetry caution.

## BroadLighting / 넓은 조명, BroadLight / 보드라이트

- **Type**: portrait lighting pattern
- **Definition**: Lighting where the camera-facing side of the face is brighter.
- **Visual effect**: Face appears broader and softer.
- **KRetouchPro use**: Portrait lighting annotation.

## ShortLighting / 숏라이트

- **Type**: portrait lighting pattern
- **Definition**: Lighting where the camera-away side of the face is brighter.
- **Visual effect**: Face appears narrower and more dimensional.
- **KRetouchPro use**: Portrait lighting annotation.

## ButterflyLighting / 나비 조명

- **Type**: portrait lighting pattern
- **Definition**: Frontal high light creating butterfly-like shadow under nose.
- **KRetouchPro use**: Face highlight/shadow interpretation.

## BounceLighting / 바운스 라이팅

- **Type**: lighting method
- **Definition**: Light reflected from wall, ceiling, or reflector before reaching subject.
- **Visual effect**: Softer light and weaker direct shadows.
- **KRetouchPro use**: Soft-light detection, shadow contrast expectation.

## Reflector / 반사판

- **Type**: lighting tool
- **Definition**: Surface used to reflect light into shadow areas.
- **KRetouchPro use**: Fill-light interpretation; catchlight and shadow lift.

## UmbrellaLight / 엄브렐러 라이트

- **Type**: studio modifier
- **Definition**: Umbrella reflector/diffuser that spreads light softly.
- **KRetouchPro use**: Soft studio light interpretation.

## Spotlight / 스포트라이트

- **Type**: lighting tool
- **Definition**: Narrow bright light focused on a specific area.
- **KRetouchPro use**: Local highlight warning or background spot effect.

## AvailableLight / 유용광

- **Type**: ambient lighting term
- **Definition**: Existing light available in the scene without adding artificial light.
- **KRetouchPro use**: Ambient portrait correction, low-light noise/skin handling.

## ArtificialLight / 인공광

- **Type**: light source category
- **Definition**: Human-made light such as lamps, flash, or studio light.
- **KRetouchPro use**: Color cast and white balance interpretation.

## BacklightDetection / 역광 자동 감지

- **Type**: camera/lighting detection
- **Definition**: Detection of bright background causing subject underexposure.
- **KRetouchPro use**: Portrait backlight correction and face lift detection.

## SynchroSun / 태양동조

- **Type**: flash-fill technique
- **Definition**: Using flash or reflector to fill shadows under strong direct sunlight.
- **KRetouchPro use**: Outdoor portrait shadow balancing reference.

## CatchLight / 캐치라이트

- **Type**: portrait eye highlight
- **Definition**: Reflection of light source in the eye.
- **KRetouchPro use**: Eye brightness, natural portrait detail preservation.
- **Do not confuse with**: Red-eye or specular skin highlight.

## RedEye / 적목현상

- **Type**: flash artifact
- **Definition**: Eyes appear red from flash reflection near lens axis.
- **KRetouchPro use**: Red-eye correction.

## GuideNumber / 가이드 넘버

- **Type**: flash power term
- **Definition**: Flash exposure coefficient based on aperture and distance.
- **KRetouchPro use**: Reference only; not needed for preview engine.

## Strobe / 스트로보

- **Type**: flash light
- **Definition**: Electronic flash producing short strong light.
- **KRetouchPro use**: Studio portrait lighting reference.

## RingStrobe / 링 스트로보

- **Type**: flash type
- **Definition**: Ring-shaped flash near lens axis, often used for macro or shadowless light.
- **KRetouchPro use**: Catchlight and flat-light reference.

---

# 6. Cropping, Framing, and Composition Terms

## ImageDomain / 이미지 도메인

- **Type**: working-image domain term
- **Definition**: Full pixel domain of the single current working image selected on the canvas.
- **KRetouchPro use**: Top-level mask domain reference where `1 = ImageDomain` and every mask is defined inside that selected working image.
- **Do not confuse with**: Crop box, face box, work area, or preview slot. `ImageDomain` is the full selected image itself.

## ImageDomainMask / 이미지 도메인 마스크

- **Type**: mask-domain term
- **Definition**: Full-mask representation of the entire current working image domain, expressed as `1`.
- **KRetouchPro use**: Base reference mask for formulas such as `PersonMask ⊂ 1` and `BackgroundMask = 1 - PersonMask`.
- **Do not confuse with**: `PersonMask`, `BackgroundMask`, or any local retouch work mask. `ImageDomainMask` is the untouched full-image mask.

## Cropping / 크로핑

- **Type**: framing operation
- **Definition**: Cutting image edges to improve composition or change frame.
- **KRetouchPro use**: Crop tool, export crop, framing labels.
- **Do not confuse with**: Body/portrait crop category. Cropping is the operation.

## Trimming / 트리밍

- **Type**: framing/print operation
- **Definition**: Adjusting composition by cutting or enlarging during print/output.
- **KRetouchPro use**: Similar to cropping; may be display alias.

## CloseUpShot / 클로즈 업

- **Type**: framing
- **Definition**: Subject is photographed close enough to fill much of the frame.
- **KRetouchPro use**: Face close-up, skin detail analysis.

## LowAngle / 앙각촬영

- **Type**: camera angle
- **Definition**: Camera shoots upward from below the subject.
- **Visual effect**: Subject can look taller, stronger, more imposing.
- **KRetouchPro use**: Face/body perspective warning.

## TunnelView / 터널 뷰

- **Type**: composition effect
- **Definition**: Subject framed by surrounding shapes like looking out from a tunnel.
- **KRetouchPro use**: Background/object framing reference only.

## Silhouette / 실루엣 사진

- **Type**: lighting/composition effect
- **Definition**: Subject appears as dark shape against brighter background.
- **KRetouchPro use**: Subject mask debugging, backlit portrait interpretation.

## Panning / 팬닝

- **Type**: motion technique
- **Definition**: Camera follows moving subject so subject is sharper and background streaks.
- **KRetouchPro use**: Motion blur classification reference only.

## ZoomInEffect / 줌인

- **Type**: motion/zoom effect
- **Definition**: Changing zoom during exposure creates radial blur.
- **KRetouchPro use**: Motion effect classification reference only.

---

# 7. Retouch and Darkroom Operation Analogies

## Dodging / 닷징

- **Type**: local tone operation
- **Definition**: Reducing exposure on selected print area to make it lighter.
- **KRetouchPro use**: Local brighten brush, shadow lift, face dodge.

## Burning / 버닝

- **Type**: local tone operation
- **Definition**: Adding exposure to selected print area to make it darker.
- **KRetouchPro use**: Local darken brush, highlight control, edge vignette.

## StraightPrint / 스트레이트 인화

- **Type**: print workflow reference
- **Definition**: Basic print without dodging or burning, used to inspect base exposure.
- **KRetouchPro use**: Base preview with no retouch; before/after reference.

## ContactPrint / 밀착인화

- **Type**: print workflow reference
- **Definition**: Printing film in direct contact with paper at same size.
- **KRetouchPro use**: Contact sheet/reference preview analogy.

## Printing / 인화

- **Type**: output process
- **Definition**: Making a positive photo image from negative or digital source.
- **KRetouchPro use**: Export/print output concept.

## SpotRemoval / 스포팅

- **Type**: retouch operation
- **Definition**: Removing dust spots, white marks, or small defects.
- **KRetouchPro use**: Blemish removal, old photo cleanup, dust cleanup.

## FarmerReducer / 파머감력제

- **Type**: darkroom density reduction reference
- **Definition**: Chemical reducer that lowers excessive density in film.
- **KRetouchPro use**: Legacy analogy for reducing over-dark density; reference only.

## Toning / 톤

- **Type**: tone/color operation
- **Definition**: In darkroom context, chemical color/tone change; in general photo context, brightness/darkness character.
- **KRetouchPro use**: Tone module naming. Keep meaning clear in UI.

---

# 8. Camera, Metering, and Exposure Control Terms

## ExposureMeter / 노출계

- **Type**: metering tool
- **Definition**: Device measuring light to determine exposure.
- **KRetouchPro use**: Reference for exposure analysis and histogram guidance.

## ReflectedLightMeter / 반사광식 노출계

- **Type**: metering method
- **Definition**: Measures light reflected from subject toward camera.
- **KRetouchPro use**: Explains why bright/dark subjects may be misread.

## IncidentLightMeter / 입사광식 노출계

- **Type**: metering method
- **Definition**: Measures light falling onto subject from light source.
- **KRetouchPro use**: Reference for more stable skin exposure logic.

## BuiltInMeter / 내장 노출계

- **Type**: camera metering tool
- **Definition**: Meter built into camera, often through-lens.
- **KRetouchPro use**: Metadata/reference only.

## TTLMetering / 티티엘 노출측정 방식

- **Type**: camera metering method
- **Definition**: Metering through the camera lens.
- **KRetouchPro use**: Reference only.

## SpotMeter / 스폿트노출계

- **Type**: metering method
- **Definition**: Narrow-angle meter for measuring a small image area.
- **KRetouchPro use**: Local sampling analogy for face/skin target measurement.

## AELock / AE Lock

- **Type**: camera exposure lock
- **Definition**: Holds measured exposure even when framing changes.
- **KRetouchPro use**: Reference for locking sampled skin/tone reference.

## Aperture / 조리개

- **Type**: lens exposure control
- **Definition**: Opening controlling light amount and depth of field.
- **KRetouchPro use**: Metadata and blur/depth interpretation.

## StopDown / 스톱다운

- **Type**: aperture operation
- **Definition**: Closing aperture to a smaller opening.
- **KRetouchPro use**: Reference only.

## AperturePriority / 조리개 우선식

- **Type**: camera exposure mode
- **Definition**: User sets aperture, camera sets shutter speed.
- **KRetouchPro use**: Metadata/reference only.

## ShutterPriority / 셔터 우선식

- **Type**: camera exposure mode
- **Definition**: User sets shutter speed, camera sets aperture.
- **KRetouchPro use**: Motion blur metadata/reference.

## AutomaticExposure / 자동노출

- **Type**: camera control
- **Definition**: Camera automatically sets shutter, aperture, or both.
- **KRetouchPro use**: Reference for exposure variation across batch images.

## ISO / ISO, ASA

- **Type**: sensitivity
- **Definition**: Film/sensor sensitivity scale.
- **KRetouchPro use**: Noise/low-light interpretation from metadata.

## ExposureIndex / E.I

- **Type**: exposure rating
- **Definition**: Working exposure index used by photographer, may differ from nominal film speed.
- **KRetouchPro use**: Legacy reference only.

## ExposureValue / EV값

- **Type**: exposure value
- **Definition**: Combined exposure value from aperture and shutter speed.
- **KRetouchPro use**: Metadata/reference for exposure comparison.

## Bracketing / 브라케팅

- **Type**: shooting method
- **Definition**: Taking multiple frames with varied exposure to avoid exposure error.
- **KRetouchPro use**: Batch selection and HDR/reference concept.

## InverseSquareLaw / 제곱 반비례법칙

- **Type**: lighting physics
- **Definition**: Light intensity decreases with square of distance from source.
- **KRetouchPro use**: Portrait lighting falloff interpretation.

---

# 9. Film, Print, and Legacy Reference Terms

## NegativeFilm / 네가티브 필름

- **Type**: film type
- **Definition**: Film where tones/colors are reversed and require printing to positive.
- **KRetouchPro use**: Old photo scan and film restoration reference.

## SlideFilm / 슬라이드, Transparency / 트랜스패런시

- **Type**: positive film
- **Definition**: Positive film image viewed directly by projection or light.
- **KRetouchPro use**: Color contrast and exposure latitude reference.

## ChromogenicFilm / 크로마제닉 필름

- **Type**: film process
- **Definition**: Black-and-white film processed like color negative, forming dye image.
- **KRetouchPro use**: Legacy reference only.

## InstantFilm / 즉석필름

- **Type**: film type
- **Definition**: Film that develops itself without separate darkroom processing.
- **KRetouchPro use**: Film-look reference only.

## Emulsion / 감광유제

- **Type**: film/print material
- **Definition**: Light-sensitive layer on film or paper.
- **KRetouchPro use**: Old photo damage/restoration vocabulary.

## LatentImage / 잠상

- **Type**: film process term
- **Definition**: Invisible image formed in emulsion before development.
- **KRetouchPro use**: Legacy reference only.

## Developer / 현상액

- **Type**: darkroom chemical
- **Definition**: Chemical solution that develops exposed image.
- **KRetouchPro use**: Legacy reference only.

## Fixer / 정착액

- **Type**: darkroom chemical
- **Definition**: Chemical solution that makes developed image no longer light-sensitive.
- **KRetouchPro use**: Legacy reference only.

## BarytaPaper / 바라이타지

- **Type**: print paper
- **Definition**: Paper with baryta coating that improves white base and surface quality.
- **KRetouchPro use**: Print-look reference.

## RCPaper / RC인화지

- **Type**: print paper
- **Definition**: Resin-coated photo paper with faster wash/dry workflow.
- **KRetouchPro use**: Print output reference only.

## GlossyPaper / 광택지

- **Type**: print surface
- **Definition**: Shiny photographic paper surface.
- **KRetouchPro use**: Output surface preset reference.

## Ferrotype / 광택인화

- **Type**: print finishing
- **Definition**: Gloss-enhancing drying/pressing process.
- **KRetouchPro use**: Legacy print reference only.

## Matte / 매트

- **Type**: print framing material
- **Definition**: Board frame placed around photo in a frame.
- **KRetouchPro use**: Print layout reference only.

---

# 10. KRetouchPro Implementation Notes

## Recommended Buckets

### ToneAndExposureTerms

- Exposure
- OverExposure
- UnderExposure
- Density
- Gradation
- Gamma
- Contrast
- Brightness
- MidTone
- GrayCard
- GrayScale
- ExposureLatitude

### ColorTerms

- VisibleLight
- AdditiveColor
- SubtractiveColor
- PrimaryColor
- ComplementaryColor
- ColorTemperature
- KelvinTemperature
- ColorBalance
- ColorAdaptation
- ColorReflection
- NDFilter
- UVFilter
- CircularPolarizer

### DetailAndArtifactTerms

- Sharpness
- Blur
- SoftFilter
- Vignette
- Flare
- ChromaticAberration
- Halftone
- Reticulation
- NewtonRing
- Irradiation
- BlockedUp

### LensAndGeometryTerms

- FocalLength
- WideAngleLens
- TelephotoLens
- TelephotoEffect
- FisheyeLens
- MacroLens
- CloseUp
- Distortion
- BarrelDistortion
- CircleOfConfusion
- DepthOfField
- PerspectiveControl
- CameraMovement

### PortraitLightingTerms

- MainLight
- KeyLight
- FillLight
- SideLighting
- BroadLighting
- ShortLighting
- ButterflyLighting
- BounceLighting
- Reflector
- UmbrellaLight
- AvailableLight
- ArtificialLight
- BacklightDetection
- CatchLight
- RedEye
- Strobe

### RetouchAnalogyTerms

- Dodging
- Burning
- Spotting
- Etching
- Cropping
- Trimming
- StraightPrint
- Printing
- Toning

## Product Behavior Rules

- Do not trigger automatic retouch just because a photographic term is detected.
- Use terms for labeling, diagnostics, help text, tool routing, and debug overlays.
- Keep darkroom terms as analogies unless the feature directly needs them.
- Keep optical terms separate from portrait body geometry.
- Do not confuse `Tilt` camera movement with `HeadCarrierTilt`.
- Do not confuse `Blur` with skin retouch; skin retouch must preserve important features.
- Do not treat body hair as blemish by default.
- Do not apply global contrast if only local face tone needs correction.

## Warning IDs

Use warning IDs when the term is ambiguous or unsafe:

- `term_context_uncertain`
- `lighting_pattern_uncertain`
- `skin_color_cast_from_clothing_possible`
- `wide_angle_face_distortion_possible`
- `highlight_clipping_possible`
- `shadow_blocked_possible`
- `chromatic_aberration_edge_possible`
- `vignette_detected_but_intent_uncertain`
- `blur_type_uncertain`
- `camera_tilt_not_head_tilt`

## Source Note

This dictionary was created from a user-provided Korean photographic glossary and reduced to terms that are likely useful for KRetouchPro product behavior, documentation, diagnostics, and future tool routing.
