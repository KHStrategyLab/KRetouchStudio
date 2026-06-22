# Portrait Body, Hair, and Clothing Location Dictionary

Last updated: 2026-06-15

## Purpose

This document defines body-part, body-hair, and clothing-location terms for KRetouchPro.

The purpose is not medical diagnosis.
The purpose is location notification, region naming, mask labeling, guide overlay naming, and future retouch-tool routing.

Use this document when the app needs to say:

- where a detected region is
- which body or clothing area a mask belongs to
- which parent region owns a feature
- whether a term is a physical body part, joint, surface, hair feature, clothing part, or shape/status tag

## Core Rules

### Region Rule

A region is an area with visible surface coverage.

Examples:

- `Hand`
- `Palm`
- `UpperArm`
- `Neck`
- `Waist`
- `Shirt`
- `Skirt`

### Joint Rule

A joint is a connection or bend point between two regions.

Examples:

- `WristJoint`
- `FingerBaseJoint`
- `ElbowJoint`
- `KneeJoint`
- `AnkleJoint`

### Surface Rule

A surface is one side or face of a region.

Examples:

- `Palm`
- `BackOfHand`
- `FrontNeck`
- `SideNeck`
- `BackNeck`
- `FootTop`
- `FootSole`

### Feature Rule

A feature is not always a separate body part.
It may be a visible shape, wrinkle, fat volume, hair distribution, seam, hem, fold, or clothing boundary.

Examples:

- `UpperArmSoftTissue`
- `NeckWrinkle`
- `TrapeziusBulge`
- `SleeveHem`
- `BeltLine`

### Parent Ownership Rule

Most small features should be owned by a parent region.

Examples:

- `NeckWrinkle` belongs to `Neck`
- `FingerHair` belongs to `Finger`
- `UpperArmSoftTissue` belongs to `UpperArm`
- `SleeveHem` belongs to `Sleeve`
- `Collar` belongs to `TopGarment`

### Do-Not-Confuse Rule

When two terms are visually close, keep them separate.

Examples:

- Neck is not face.
- Hair is not face skin.
- Trapezius is not the same as shoulder joint.
- Wrist is not the same as hand.
- Sleeve length is not the same as garment type.
- Belt line is not always anatomical waist.

---

# 1. Upper Limb Dictionary

## 1.1 Hand Region

### Hand / 손

- **Type**: body region
- **Parent**: UpperLimb
- **Definition**: Region from the wrist crease or wrist joint to the fingertips.
- **Location**: Distal end of the arm.
- **Upper boundary**: Wrist crease or visible wrist joint.
- **Lower boundary**: Fingertips.
- **Contains**: Palm, back of hand, fingers, thumb, hand edge, knuckles, fingernails.
- **Visual cues**: Five digits, palm/back surface, thumb angled away from the other fingers.
- **Do not confuse with**: Wrist or forearm.

### Palm / 손바닥

- **Type**: body surface
- **Parent**: Hand
- **Definition**: Inner surface of the hand.
- **Location**: From wrist crease to the base of the fingers on the palm side.
- **Contains**: Thenar area, hypothenar area, palm lines, finger-base creases.
- **Visual cues**: Softer inner surface, visible palm creases.
- **Hair rule**: Treat palm as generally hairless.

### BackOfHand / 손등

- **Type**: body surface
- **Parent**: Hand
- **Definition**: Outer surface of the hand opposite the palm.
- **Location**: From wrist to finger bases on the back side.
- **Visual cues**: Knuckles and tendons may be visible.
- **Hair rule**: May contain visible hand hair.

### HandEdge / 손날

- **Type**: boundary surface
- **Parent**: Hand
- **Definition**: Outer side edge of the hand along the little-finger side.
- **Location**: Lateral border from wrist to little finger.
- **Visual cues**: Edge used in a chopping-hand shape.
- **Do not confuse with**: Palm center or back of hand.

### ThenarArea / 엄지두덩

- **Type**: subregion
- **Parent**: Palm
- **Definition**: Thick padded area below the thumb on the palm.
- **Location**: Palm side under the thumb base.
- **Visual cues**: Rounded fleshy volume.
- **Use**: Useful for hand shape and palm orientation.

### HypothenarArea / 새끼두덩

- **Type**: subregion
- **Parent**: Palm
- **Definition**: Thick padded area below the little finger on the palm.
- **Location**: Palm side near the hand edge.
- **Visual cues**: Rounded volume on little-finger side.

### Fingernail / 손톱

- **Type**: nail feature
- **Parent**: Finger or Thumb
- **Definition**: Hard nail plate at the fingertip.
- **Location**: Dorsal side of distal finger segment.
- **Visual cues**: Smooth brighter plate, curved border.
- **Hair rule**: Hairless.
- **Do not confuse with**: Fingertip pad.

---

## 1.2 Finger Identity

### Thumb / 엄지손가락

- **Type**: digit
- **Parent**: Hand
- **Definition**: Shorter, thicker digit that angles away from the other four fingers.
- **Location**: Side of the hand, attached near the palm base.
- **Segments**: Thumb base, thumb middle joint, thumb distal segment.
- **Visual cues**: Opposable angle, fewer visible phalange-like segments than other fingers.
- **Do not confuse with**: Index finger.

### IndexFinger / 검지손가락

- **Type**: digit
- **Parent**: Hand
- **Definition**: Finger next to the thumb.
- **Location**: Second digit from thumb side.
- **Visual cues**: Often used for pointing; usually shorter than middle finger.
- **Segments**: Base segment, middle segment, distal segment.

### MiddleFinger / 중지손가락

- **Type**: digit
- **Parent**: Hand
- **Definition**: Central finger, usually the longest.
- **Location**: Middle of the four non-thumb fingers.
- **Visual cues**: Longest digit and near the hand axis.

### RingFinger / 약지손가락

- **Type**: digit
- **Parent**: Hand
- **Definition**: Finger between middle finger and little finger.
- **Location**: Fourth digit from thumb side.
- **Visual cues**: Usually shorter than middle finger and longer than little finger.

### LittleFinger / 새끼손가락

- **Type**: digit
- **Parent**: Hand
- **Definition**: Smallest finger at the outer edge of the hand.
- **Location**: Far side of the hand opposite the thumb.
- **Visual cues**: Shortest and narrowest finger; connected to hand edge.

---

## 1.3 Finger Segments and Joints

### FingerBase / 손가락 뿌리

- **Type**: subregion
- **Parent**: Finger
- **Definition**: Starting area where a finger separates from the palm or back of hand.
- **Location**: At the palm-to-finger transition.
- **Visual cues**: Finger web gaps, knuckle area on back side, crease on palm side.

### FingerBaseJoint / 손가락 뿌리 관절

- **Type**: joint
- **Parent**: Finger
- **Definition**: Main joint where a non-thumb finger bends from the hand.
- **Location**: At the base of index, middle, ring, or little finger.
- **Visual cues**: Prominent knuckle on back of hand when fist is bent.
- **Do not confuse with**: Wrist or finger middle joint.

### FingerMiddleJoint / 손가락 중간 관절

- **Type**: joint
- **Parent**: Finger
- **Definition**: First major bend joint after the finger base, moving toward the fingertip.
- **Location**: Lower-middle part of non-thumb fingers.
- **Visual cues**: Main folding crease when the finger bends.

### FingerTipJoint / 손가락 끝 관절

- **Type**: joint
- **Parent**: Finger
- **Definition**: Small bend joint near the fingertip.
- **Location**: Between middle segment and distal fingertip segment.
- **Visual cues**: Smaller crease close to fingernail.

### FingerTipSegment / 손끝마디

- **Type**: subregion
- **Parent**: Finger
- **Definition**: Distal segment from the fingertip joint to the fingertip.
- **Location**: End of the finger.
- **Contains**: Fingertip pad and fingernail.
- **Visual cues**: Rounded tip, nail on dorsal side.

### Fingertip / 손끝

- **Type**: terminal feature
- **Parent**: Finger
- **Definition**: Very end of a finger.
- **Location**: Distal endpoint of each finger.
- **Visual cues**: Rounded soft pad, nail nearby.

### FingerWeb / 손가락 사이

- **Type**: boundary feature
- **Parent**: Hand
- **Definition**: Valley-shaped skin connection between adjacent fingers.
- **Location**: Between finger bases.
- **Visual cues**: V-shaped gap when fingers are spread.

### ThumbBaseJoint / 엄지 뿌리 관절

- **Type**: joint
- **Parent**: Thumb
- **Definition**: Deep joint where thumb attaches to the hand and rotates outward.
- **Location**: Side of palm near thenar area.
- **Visual cues**: Thumb angle changes around this point.
- **Do not confuse with**: Wrist joint.

### ThumbMiddleJoint / 엄지 중간 관절

- **Type**: joint
- **Parent**: Thumb
- **Definition**: Main visible bending joint of the thumb.
- **Location**: Middle of the thumb.
- **Visual cues**: Single prominent thumb fold.

### ThumbTipSegment / 엄지 끝마디

- **Type**: subregion
- **Parent**: Thumb
- **Definition**: Region from thumb middle joint to thumb tip.
- **Location**: Distal thumb.
- **Contains**: Thumb nail and thumb tip pad.

---

## 1.4 Wrist, Forearm, Elbow, Upper Arm, Shoulder

### Wrist / 손목

- **Type**: joint region
- **Parent**: UpperLimb
- **Definition**: Narrow connection between hand and forearm.
- **Location**: Between hand base and forearm.
- **Upper boundary**: Forearm begins where arm thickness becomes stable.
- **Lower boundary**: Hand begins at wrist crease or hand surface expansion.
- **Visual cues**: Wrist crease, narrow shape, bony protrusions.
- **Do not confuse with**: Hand or forearm.

### WristJoint / 손목 관절

- **Type**: joint
- **Parent**: Wrist
- **Definition**: Bend axis for hand movement relative to forearm.
- **Location**: Around wrist crease and carpal joint area.
- **Visual cues**: Hand flexion/extension center.

### Forearm / 전완, 아래팔

- **Type**: body region
- **Parent**: UpperLimb
- **Definition**: Arm region from elbow to wrist.
- **Location**: Distal half of arm between elbow and hand.
- **Upper boundary**: Elbow joint.
- **Lower boundary**: Wrist joint.
- **Visual cues**: Tapers toward wrist; may show forearm muscles or veins.
- **Hair rule**: May contain forearm hair.

### Elbow / 팔꿈치

- **Type**: joint region
- **Parent**: UpperLimb
- **Definition**: Bend region connecting upper arm and forearm.
- **Location**: Between upper arm and forearm.
- **Visual cues**: Back-side bony point, front-side fold crease.
- **Do not confuse with**: Arm center. Use actual bending axis.

### ElbowJoint / 팔꿈치 관절

- **Type**: joint
- **Parent**: Elbow
- **Definition**: Primary hinge joint for arm bending.
- **Location**: Around elbow bend axis.
- **Visual cues**: Arm direction changes at this point.

### InnerElbow / 팔꿈치 안쪽

- **Type**: joint surface
- **Parent**: Elbow
- **Definition**: Inner crease side of the elbow.
- **Location**: Front side when arm bends.
- **Visual cues**: Fold lines when bent.

### OuterElbow / 팔꿈치 바깥쪽

- **Type**: joint surface
- **Parent**: Elbow
- **Definition**: Outer bony side of the elbow.
- **Location**: Back side of elbow.
- **Visual cues**: Bony protrusion.

### UpperArm / 상완, 윗팔

- **Type**: body region
- **Parent**: UpperLimb
- **Definition**: Arm region from shoulder to elbow.
- **Location**: Proximal arm, attached to shoulder.
- **Upper boundary**: Shoulder/deltoid transition.
- **Lower boundary**: Elbow joint.
- **Visual cues**: Larger cylindrical or oval volume.
- **Hair rule**: May contain upper-arm hair.
- **Do not confuse with**: Forearm.

### UpperArmSoftTissue / 팔뚝살

- **Type**: shape/status feature
- **Parent**: UpperArm
- **Definition**: Soft tissue or fat volume visible on the lower or rear upper arm.
- **Location**: Back, lower, or outer-lower side of the upper arm.
- **Visual cues**: Rounded, sagging, or hanging volume when arm is down.
- **Use**: Shape-awareness, not a separate skeletal part.
- **Do not confuse with**: Elbow or sleeve.

### Shoulder / 어깨

- **Type**: body region
- **Parent**: UpperBody
- **Definition**: Upper side region where the arm begins from the torso.
- **Location**: From side of neck and clavicle area toward upper arm.
- **Contains**: Shoulder cap, shoulder line, shoulder joint area.
- **Visual cues**: Rounded deltoid volume, clothing shoulder seam often lies nearby.
- **Do not confuse with**: Trapezius.

### ShoulderJoint / 어깨 관절

- **Type**: joint
- **Parent**: Shoulder
- **Definition**: Arm rotation center where upper arm connects to torso.
- **Location**: Deep under outer shoulder cap.
- **Visual cues**: Arm direction and lift pivot.

### ShoulderLine / 어깨선

- **Type**: boundary line
- **Parent**: Shoulder
- **Definition**: Visual line from side neck toward outer shoulder and sleeve start.
- **Location**: Top contour of shoulder.
- **Visual cues**: Often overlaps garment shoulder seam.
- **Do not confuse with**: Actual garment seam. Clothing seam may be offset.

---

# 2. Neck, Torso, and Lower Limb Dictionary

## 2.1 Neck and Shoulder-Neck Area

### Neck / 목

- **Type**: body region
- **Parent**: UpperBody
- **Definition**: Vertical connector between head and torso.
- **Location**: From under jaw/chin to clavicle and shoulder base.
- **Upper boundary**: Under jawline and chin.
- **Lower boundary**: Clavicle, upper chest, shoulder base.
- **Contains**: Front neck, side neck, back neck, neck wrinkles, neck hair.
- **Do not confuse with**: Face or jawline.

### FrontNeck / 앞목

- **Type**: surface
- **Parent**: Neck
- **Definition**: Front-facing neck surface below chin and above clavicle.
- **Location**: Central anterior neck.
- **Visual cues**: Horizontal neck wrinkles often visible.

### SideNeck / 옆목

- **Type**: surface
- **Parent**: Neck
- **Definition**: Lateral neck surface from below ear toward clavicle/shoulder.
- **Location**: Left and right sides of neck.
- **Visual cues**: Oblique muscle lines may be visible.

### BackNeck / 뒷목

- **Type**: surface
- **Parent**: Neck
- **Definition**: Rear neck surface below hairline and above upper back.
- **Location**: Posterior neck.
- **Visual cues**: Neck nape, hairline, trapezius transition.

### NeckWrinkle / 목주름

- **Type**: wrinkle/status feature
- **Parent**: Neck
- **Definition**: Horizontal, curved, or oblique fold lines on neck skin.
- **Location**: Mostly front neck and side neck.
- **Visual cues**: Thin lines, folds, or skin creases.
- **Use**: Treat as a texture/status feature inside the neck region.
- **Do not confuse with**: Clothing collar or necklace.

### Trapezius / 승모근

- **Type**: muscle region / shape feature
- **Parent**: ShoulderNeckArea
- **Definition**: Sloped muscle area connecting back of neck to shoulder and upper back.
- **Location**: From back neck down and outward toward shoulder top.
- **Visual cues**: Sloped raised line from neck to shoulder; can make shoulders look raised.
- **Do not confuse with**: Shoulder joint or clothing shoulder seam.

### Clavicle / 쇄골

- **Type**: bone landmark / boundary feature
- **Parent**: UpperTorso
- **Definition**: Collarbone line under the neck and above upper chest.
- **Location**: Left and right horizontal/curved bones below neck.
- **Visual cues**: Visible shallow ridge or shadow.
- **Use**: Important boundary for neck, chest, and clothing neckline.

---

## 2.2 Torso and Waist

### UpperTorso / 상체

- **Type**: body region
- **Parent**: Body
- **Definition**: Body area from shoulders/chest to waist.
- **Location**: Above waist and below neck/shoulders.
- **Contains**: Chest, abdomen, back, waist region.

### Chest / 가슴

- **Type**: body region
- **Parent**: UpperTorso
- **Definition**: Front upper torso below neck and above abdomen.
- **Location**: From clavicle to lower rib area.
- **Visual cues**: Upper front torso surface.
- **Hair rule**: May contain chest hair.

### Abdomen / 복부, 배

- **Type**: body region
- **Parent**: Torso
- **Definition**: Front torso below chest and above pelvis.
- **Location**: Between lower rib area and pelvis.
- **Visual cues**: Belly surface, navel area.
- **Hair rule**: May contain abdominal hair.

### Back / 등

- **Type**: body region
- **Parent**: Torso
- **Definition**: Rear torso surface from shoulders to waist/pelvis.
- **Location**: Posterior torso.
- **Hair rule**: May contain back hair.

### Waist / 허리

- **Type**: body region / boundary zone
- **Parent**: Torso
- **Definition**: Middle torso area between lower ribs and upper pelvis.
- **Location**: Narrowing area of body side contour.
- **Upper boundary**: Lower rib area.
- **Lower boundary**: Upper pelvis / iliac crest area.
- **Visual cues**: Side indentation or transition between torso and pelvis.
- **Do not confuse with**: Entire back or belt line.

### WaistFold / 허리 접힘

- **Type**: fold/status feature
- **Parent**: Waist
- **Definition**: Skin or clothing fold around waist.
- **Location**: Front, side, or back waist.
- **Visual cues**: Horizontal compression lines.
- **Do not confuse with**: Belt line.

### Pelvis / 골반

- **Type**: body region
- **Parent**: LowerBody
- **Definition**: Broad lower torso structure connecting waist to legs.
- **Location**: Below waist, above thighs.
- **Visual cues**: Wider body base, hip curve.
- **Do not confuse with**: Waist.

### HipJoint / 고관절

- **Type**: joint
- **Parent**: Pelvis
- **Definition**: Deep joint where thigh connects to pelvis.
- **Location**: Side/front of pelvis, often covered by clothing.
- **Visual cues**: Leg movement origin, not always directly visible.

---

## 2.3 Lower Limb

### Thigh / 허벅지, 대퇴부

- **Type**: body region
- **Parent**: LowerLimb
- **Definition**: Upper leg from hip/groin to knee.
- **Location**: Between pelvis and knee.
- **Visual cues**: Large upper leg volume.
- **Hair rule**: May contain thigh hair.

### Knee / 무릎

- **Type**: joint region
- **Parent**: LowerLimb
- **Definition**: Bend region connecting thigh and lower leg.
- **Location**: Mid-leg joint.
- **Visual cues**: Kneecap at front, popliteal fold at back.
- **Do not confuse with**: Simple middle of leg. Use actual joint structure.

### KneeJoint / 무릎 관절

- **Type**: joint
- **Parent**: Knee
- **Definition**: Primary hinge joint for leg bending.
- **Location**: Around kneecap and joint line.
- **Visual cues**: Leg direction changes here.

### Kneecap / 슬개골, 무릎뼈

- **Type**: bone landmark
- **Parent**: Knee
- **Definition**: Front bony plate of the knee.
- **Location**: Anterior knee center.
- **Visual cues**: Rounded or angular front knee highlight/shadow.

### PoplitealArea / 오금

- **Type**: joint surface
- **Parent**: Knee
- **Definition**: Back side fold area of the knee.
- **Location**: Posterior knee.
- **Visual cues**: Crease when knee bends.

### KneeWrinkle / 무릎 주름

- **Type**: wrinkle/status feature
- **Parent**: Knee
- **Definition**: Fine lines or folds around the knee surface.
- **Location**: Around front or back knee.
- **Use**: Texture/status tag, not separate body part.

### LowerLeg / 종아리, 하퇴부

- **Type**: body region
- **Parent**: LowerLimb
- **Definition**: Leg region from knee to ankle.
- **Location**: Below knee and above ankle.
- **Visual cues**: Calf volume at rear, shin line at front.
- **Hair rule**: May contain lower-leg hair.

### Calf / 종아리 후면

- **Type**: subregion
- **Parent**: LowerLeg
- **Definition**: Rear muscular volume of lower leg.
- **Location**: Back of lower leg.
- **Visual cues**: Rounded calf shape.

### Shin / 정강이

- **Type**: subregion
- **Parent**: LowerLeg
- **Definition**: Front bony line of lower leg.
- **Location**: Front lower leg.
- **Visual cues**: Vertical ridge or flat front surface.

### Ankle / 발목

- **Type**: joint region
- **Parent**: LowerLimb
- **Definition**: Narrow connection between lower leg and foot.
- **Location**: Between calf/shin and foot.
- **Visual cues**: Malleolus bone protrusions.
- **Do not confuse with**: Foot or lower leg.

### AnkleJoint / 발목 관절

- **Type**: joint
- **Parent**: Ankle
- **Definition**: Bend joint between foot and lower leg.
- **Location**: Around ankle bones.
- **Visual cues**: Foot angle changes from this region.

### Foot / 발

- **Type**: body region
- **Parent**: LowerLimb
- **Definition**: Region from ankle to toe tips.
- **Location**: Distal lower limb.
- **Contains**: Foot top, sole, toes, toenails.
- **Hair rule**: Foot top and toes may contain hair; sole is generally hairless.

### FootTop / 발등

- **Type**: surface
- **Parent**: Foot
- **Definition**: Top surface of the foot.
- **Location**: From ankle front to toe bases.
- **Hair rule**: May contain foot hair.

### FootSole / 발바닥

- **Type**: surface
- **Parent**: Foot
- **Definition**: Bottom surface of the foot.
- **Location**: Under foot.
- **Hair rule**: Treat as generally hairless.

### Toe / 발가락

- **Type**: digit
- **Parent**: Foot
- **Definition**: Five distal digits of the foot.
- **Location**: Front end of foot.
- **Visual cues**: Shorter digits than fingers, often partially covered by footwear.
- **Hair rule**: May contain toe hair.

### BigToe / 엄지발가락

- **Type**: digit
- **Parent**: Foot
- **Definition**: Largest toe on the inner side of the foot.
- **Location**: Medial front foot.
- **Visual cues**: Widest toe.

### LittleToe / 새끼발가락

- **Type**: digit
- **Parent**: Foot
- **Definition**: Smallest toe on the outer side of the foot.
- **Location**: Lateral front foot.
- **Visual cues**: Small and often angled outward.

### Toenail / 발톱

- **Type**: nail feature
- **Parent**: Toe
- **Definition**: Nail plate on top of toe tip.
- **Location**: Distal toe top.
- **Hair rule**: Hairless.

---

# 3. Body Hair Dictionary

## 3.1 Hair Classification Rules

### HairAsIndependentRegion

Treat these as independently nameable regions when visible:

- `ScalpHair`
- `Eyebrow`
- `Eyelash`
- `Beard`
- `PubicHair`

### HairAsParentFeature

Treat these as feature tags owned by a body parent:

- `ArmHair`
- `ForearmHair`
- `HandBackHair`
- `FingerHair`
- `LegHair`
- `ChestHair`
- `AbdominalHair`
- `BackHair`
- `NeckHair`
- `EarHair`
- `NoseHair`
- `FootTopHair`
- `ToeHair`

### GenerallyHairlessSurfaces

Default hairless surfaces:

- Palm
- Foot sole
- Lip surface
- Fingernails
- Toenails

---

## 3.2 Head and Face Hair

### ScalpHair / 머리카락

- **Type**: independent hair region
- **Parent**: Head
- **Definition**: Hair growing from the scalp.
- **Location**: From hairline over top, sides, and back of head.
- **Contains**: Front hair, side hair, back hair, crown hair, hairline, baby hair.
- **Do not confuse with**: Face skin.

### Hairline / 헤어라인

- **Type**: boundary feature
- **Parent**: ScalpHair
- **Definition**: Boundary where forehead/skin meets scalp hair.
- **Location**: Upper forehead and side temple boundary.
- **Use**: Important for face/hair separation.

### BabyHair / 잔머리

- **Type**: hair feature
- **Parent**: ScalpHair
- **Definition**: Short loose hair around hairline, temples, ears, or nape.
- **Location**: Forehead edge, temple, ear front, back neck.
- **Use**: Edge-aware masking and background separation.

### Eyebrow / 눈썹

- **Type**: independent hair region
- **Parent**: Face
- **Definition**: Arc-shaped hair region above the eye.
- **Location**: Above each eye socket.
- **Contains**: Brow head, brow body, brow tail.
- **Do not confuse with**: Eyelid or eye shadow.

### BrowHead / 눈썹 앞머리

- **Type**: subregion
- **Parent**: Eyebrow
- **Definition**: Inner start of eyebrow near nose bridge.
- **Location**: Medial eyebrow end.

### BrowBody / 눈썹 몸통

- **Type**: subregion
- **Parent**: Eyebrow
- **Definition**: Main central eyebrow body.
- **Location**: Middle of eyebrow.

### BrowTail / 눈썹 꼬리

- **Type**: subregion
- **Parent**: Eyebrow
- **Definition**: Outer tapered end of eyebrow.
- **Location**: Lateral eyebrow end.

### Eyelash / 속눈썹

- **Type**: independent hair feature
- **Parent**: Eye
- **Definition**: Short hairs along eyelid edges.
- **Location**: Upper and lower eyelid margins.
- **Contains**: Upper eyelash, lower eyelash.
- **Do not confuse with**: Eyeliner or eye shadow.

### Sideburn / 구레나룻

- **Type**: hair region
- **Parent**: FaceHair or ScalpHair
- **Definition**: Hair descending in front of the ear from temple area.
- **Location**: Temple below hairline, in front of ear.
- **Use**: Boundary between scalp hair and beard.

### Beard / 수염

- **Type**: independent hair region
- **Parent**: Face
- **Definition**: Facial hair on upper lip, chin, jaw, cheeks, and lower face.
- **Location**: Lower face.
- **Contains**: Mustache, chin beard, under-chin beard, cheek beard.
- **Do not confuse with**: Skin blemish or shadow.

### Mustache / 콧수염

- **Type**: beard subregion
- **Parent**: Beard
- **Definition**: Facial hair above upper lip and below nose.
- **Location**: Upper lip area.
- **Contains**: Philtrum mustache if needed.

### PhiltrumMustache / 인중수염

- **Type**: beard subregion
- **Parent**: Mustache
- **Definition**: Central mustache hair on the philtrum.
- **Location**: Between nose base and upper lip center.

### ChinBeard / 턱수염

- **Type**: beard subregion
- **Parent**: Beard
- **Definition**: Hair on chin front and chin tip.
- **Location**: Under lower lip to chin tip.

### UnderChinBeard / 턱밑수염

- **Type**: beard subregion
- **Parent**: Beard
- **Definition**: Hair under the chin near neck start.
- **Location**: Below jaw/chin boundary.
- **Do not confuse with**: Neck hair.

### CheekBeard / 뺨수염

- **Type**: beard subregion
- **Parent**: Beard
- **Definition**: Facial hair on lower cheek or side face.
- **Location**: Mouth side to cheek lower area.

### NoseHair / 콧털

- **Type**: hair feature
- **Parent**: Nose
- **Definition**: Hair visible at nostril opening.
- **Location**: Nostril entrance.
- **Use**: Only mark when visible externally.

### EarHair / 귀털

- **Type**: hair feature
- **Parent**: Ear
- **Definition**: Hair visible around ear opening or outer ear edge.
- **Location**: Ear canal entrance or auricle edge.
- **Use**: Ear detail tag.

---

## 3.3 Torso and Limb Hair

### NeckHair / 목 털

- **Type**: hair feature
- **Parent**: Neck
- **Definition**: Hair visible on neck skin.
- **Location**: Under jaw, side neck, back neck, or nape.
- **Do not confuse with**: Under-chin beard.

### ArmpitHair / 겨드랑이 털

- **Type**: hair feature
- **Parent**: Armpit
- **Definition**: Hair in the underarm hollow.
- **Location**: Between upper arm and torso.
- **Use**: Body hair feature, not clothing shadow.

### ChestHair / 가슴 털

- **Type**: hair feature
- **Parent**: Chest
- **Definition**: Hair on front upper torso.
- **Location**: Below clavicle, chest center, and around nipple area.
- **Contains**: Areola hair if visible.

### AreolaHair / 유륜 주변 털

- **Type**: hair feature
- **Parent**: Chest
- **Definition**: Hair around areola/nipple.
- **Location**: Around areola boundary.
- **Use**: Fine body hair detail when necessary.

### AbdominalHair / 배 털

- **Type**: hair feature
- **Parent**: Abdomen
- **Definition**: Hair on front abdomen.
- **Location**: Between lower chest and pelvis, often around navel or center line.

### WaistHair / 허리 털

- **Type**: hair feature
- **Parent**: Waist
- **Definition**: Hair around waist side or lower back boundary.
- **Location**: Waist side or rear lower torso.

### BackHair / 등 털

- **Type**: hair feature
- **Parent**: Back
- **Definition**: Hair on rear torso.
- **Location**: Upper back, shoulder blade area, central back, lower back.

### ButtockHair / 엉덩이 털

- **Type**: hair feature
- **Parent**: Buttock
- **Definition**: Hair on buttock surface or lower gluteal area.
- **Location**: Buttock region.
- **Use**: Only when relevant to visible body region.

### PubicHair / 음모

- **Type**: independent hair region
- **Parent**: LowerTorso
- **Definition**: Hair around pubic and groin area.
- **Location**: Pubic area and inner groin.
- **Use**: Sensitive region. Only use when task scope explicitly requires body-region labeling.

### UpperArmHair / 상완 털

- **Type**: hair feature
- **Parent**: UpperArm
- **Definition**: Hair on upper arm.
- **Location**: Shoulder to elbow region.

### ForearmHair / 전완 털

- **Type**: hair feature
- **Parent**: Forearm
- **Definition**: Hair on forearm.
- **Location**: Elbow to wrist region.
- **Visual cues**: Common visible arm hair.

### HandBackHair / 손등 털

- **Type**: hair feature
- **Parent**: BackOfHand
- **Definition**: Hair on back of hand.
- **Location**: Wrist below to finger bases.

### FingerHair / 손가락 털

- **Type**: hair feature
- **Parent**: Finger
- **Definition**: Hair on dorsal finger segments.
- **Location**: Usually between finger base and middle joints, on back side.
- **Do not confuse with**: Finger wrinkles.

### ThighHair / 허벅지 털

- **Type**: hair feature
- **Parent**: Thigh
- **Definition**: Hair on upper leg.
- **Location**: Hip/groin to knee.

### KneeHair / 무릎 주변 털

- **Type**: hair feature
- **Parent**: Knee
- **Definition**: Hair around knee surface.
- **Location**: Around kneecap or knee sides.

### LowerLegHair / 종아리 털

- **Type**: hair feature
- **Parent**: LowerLeg
- **Definition**: Hair on lower leg.
- **Location**: Knee to ankle, front/side/back.

### FootTopHair / 발등 털

- **Type**: hair feature
- **Parent**: FootTop
- **Definition**: Hair on top of foot.
- **Location**: Ankle front to toe bases.

### ToeHair / 발가락 털

- **Type**: hair feature
- **Parent**: Toe
- **Definition**: Hair on top of toes.
- **Location**: Dorsal toe surface.

---

# 4. Clothing Dictionary

## 4.1 Clothing Classification Rules

### Garment Type vs Garment Attribute

Garment type and garment attributes must be separate.

Examples:

- `Shirt` is a garment type.
- `ShortSleeve` is a sleeve-length attribute.
- `VNeck` is a neckline attribute.
- `HighWaist` is a waist-position attribute.
- `MidiLength` is a skirt-length attribute.

### Body Boundary Reference

Clothing length should be judged relative to body landmarks.

Primary references:

- Neck base
- Clavicle
- Shoulder line
- Upper-arm midpoint
- Elbow
- Forearm midpoint
- Wrist
- Natural waist
- Belt line
- Hip line
- Upper thigh
- Mid thigh
- Knee
- Mid calf
- Ankle
- Foot top

### Clothing Layer Rule

A jacket can sit over a shirt.
A shirt can sit over skin.
A belt can sit on top of pants or skirt.

Do not overwrite body region labels with clothing labels.
Use clothing masks as separate overlay regions.

---

## 4.2 Upper Garment Types

### TopGarment / 상의

- **Type**: clothing region
- **Parent**: Clothing
- **Definition**: Any garment covering upper torso.
- **Location**: From neck/shoulder area down to waist, hip, or below.
- **Contains**: Shirt, T-shirt, blouse, knit, jacket, sleeve, neckline, hem.
- **Do not confuse with**: Body torso.

### Shirt / 셔츠

- **Type**: upper garment
- **Parent**: TopGarment
- **Definition**: Structured upper garment, usually with front opening and collar.
- **Location**: Neck to torso.
- **Visual cues**: Buttons, placket, collar, cuffs, shirt hem.
- **Attributes**: Sleeve length, collar type, hem length.

### TShirt / 티셔츠

- **Type**: upper garment
- **Parent**: TopGarment
- **Definition**: Pull-over upper garment with no front button opening by default.
- **Location**: Neck to torso.
- **Visual cues**: Knit fabric, simple neckline, short or long sleeves.
- **Attributes**: Round neck, V-neck, sleeve length.

### Blouse / 블라우스

- **Type**: upper garment
- **Parent**: TopGarment
- **Definition**: Soft or decorative upper garment, often lighter material.
- **Location**: Neck to torso.
- **Visual cues**: Soft drape, decorative neckline, cuffs, ribbon, buttons.
- **Do not confuse with**: Shirt if fabric and shape are soft/decorative.

### KnitTop / 니트

- **Type**: upper garment
- **Parent**: TopGarment
- **Definition**: Knitted-fabric upper garment.
- **Location**: Neck to torso.
- **Visual cues**: Knit texture, elasticity, ribbed edges.
- **Attributes**: Neckline and sleeve length.

### Jacket / 재킷

- **Type**: outer upper garment
- **Parent**: TopGarment
- **Definition**: Structured outer garment worn over another top.
- **Location**: Shoulder to waist, hip, or below.
- **Visual cues**: Lapel, front opening, seams, structured shoulders.
- **Do not confuse with**: Shirt layer underneath.

### Cardigan / 카디건

- **Type**: outer upper garment
- **Parent**: TopGarment
- **Definition**: Front-opening knit outer garment.
- **Location**: Shoulder to torso.
- **Visual cues**: Knit texture, front opening, buttons or no buttons.

### Hoodie / 후드티

- **Type**: upper garment
- **Parent**: TopGarment
- **Definition**: Sweatshirt-like garment with hood.
- **Location**: Upper body, hood may cover back neck/head.
- **Visual cues**: Hood, drawstrings, casual fabric.

---

## 4.3 Neckline and Collar Attributes

### Neckline / 넥라인

- **Type**: clothing boundary
- **Parent**: TopGarment
- **Definition**: Opening shape around neck and upper chest.
- **Location**: Between garment and neck/chest skin.
- **Use**: Parent category for neck opening shapes.

### RoundNeck / 라운드넥

- **Type**: neckline attribute
- **Parent**: Neckline
- **Definition**: Rounded neck opening.
- **Location**: Around base of neck.
- **Visual cues**: Smooth U or circular curve.

### VNeck / V넥, 브이넥

- **Type**: neckline attribute
- **Parent**: Neckline
- **Definition**: Neckline with V-shaped front drop.
- **Location**: Front neck toward upper chest center.
- **Visual cues**: Sharp or soft V point.
- **Variants**: Shallow V-neck, deep V-neck.

### SquareNeck / 스퀘어넥

- **Type**: neckline attribute
- **Parent**: Neckline
- **Definition**: Square or rectangular neck opening.
- **Location**: Front chest and clavicle area.
- **Visual cues**: Straight horizontal/vertical edges.

### BoatNeck / 보트넥

- **Type**: neckline attribute
- **Parent**: Neckline
- **Definition**: Wide shallow neckline running across collarbone area.
- **Location**: Wide left-right opening near clavicle.
- **Visual cues**: Horizontal, broad neckline.

### TurtleNeck / 터틀넥

- **Type**: collar/neckline attribute
- **Parent**: Neckline
- **Definition**: High collar covering much of the neck.
- **Location**: Neck region.
- **Visual cues**: Fabric wraps upward around neck.
- **Do not confuse with**: Natural neck skin.

### ShirtCollar / 셔츠 칼라

- **Type**: collar attribute
- **Parent**: Shirt
- **Definition**: Folded collar structure around neck.
- **Location**: Neck opening of shirt.
- **Visual cues**: Folded fabric points and collar stand.

### Lapel / 라펠

- **Type**: jacket feature
- **Parent**: Jacket
- **Definition**: Folded front collar area of jacket.
- **Location**: Chest front, from collar down toward button area.
- **Visual cues**: Angular folded edges.

---

## 4.4 Sleeves

### Sleeve / 소매

- **Type**: clothing region
- **Parent**: TopGarment
- **Definition**: Garment part covering arm.
- **Location**: From shoulder seam or armhole to sleeve hem.
- **Contains**: Sleeve body, sleeve hem, cuff.
- **Use**: Parent for sleeve length and sleeve shape.

### Sleeveless / 민소매

- **Type**: sleeve-length attribute
- **Parent**: Sleeve
- **Definition**: No sleeve or minimal sleeve; arm mostly exposed.
- **Location**: Garment ends near shoulder/armhole.
- **Body reference**: Does not cover upper arm.

### CapSleeve / 캡소매

- **Type**: sleeve-length attribute
- **Parent**: Sleeve
- **Definition**: Very short sleeve that lightly covers shoulder tip.
- **Location**: Shoulder cap only.
- **Body reference**: Ends near uppermost upper arm.

### ShortSleeve / 반팔

- **Type**: sleeve-length attribute
- **Parent**: Sleeve
- **Definition**: Sleeve ending above elbow, usually around upper-arm middle.
- **Location**: Shoulder to upper arm.
- **Body reference**: Ends above elbow.

### HalfSleeve / 5부 소매

- **Type**: sleeve-length attribute
- **Parent**: Sleeve
- **Definition**: Sleeve ending around elbow.
- **Location**: Upper arm lower area to elbow.
- **Body reference**: Ends near elbow joint.

### ThreeQuarterSleeve / 7부 소매

- **Type**: sleeve-length attribute
- **Parent**: Sleeve
- **Definition**: Sleeve ending below elbow and above wrist.
- **Location**: Forearm mid region.
- **Body reference**: Ends around mid-forearm.

### LongSleeve / 긴팔

- **Type**: sleeve-length attribute
- **Parent**: Sleeve
- **Definition**: Sleeve reaching wrist.
- **Location**: Shoulder to wrist.
- **Body reference**: Covers most or all arm to wrist.

### SleeveHem / 소매단

- **Type**: clothing boundary
- **Parent**: Sleeve
- **Definition**: End edge of sleeve.
- **Location**: Sleeve distal boundary.
- **Use**: Main reference for sleeve length classification.

### Cuff / 커프스

- **Type**: clothing feature
- **Parent**: Sleeve
- **Definition**: Structured sleeve-end band.
- **Location**: Wrist or sleeve end.
- **Visual cues**: Buttoned band, seam, folded or ribbed edge.

### PuffSleeve / 퍼프소매

- **Type**: sleeve-shape attribute
- **Parent**: Sleeve
- **Definition**: Sleeve with inflated volume around shoulder or arm.
- **Location**: Shoulder to upper arm.
- **Visual cues**: Bulging fabric volume.
- **Do not confuse with**: Upper-arm body volume.

---

## 4.5 Top Length and Hem

### TopHem / 상의 밑단

- **Type**: clothing boundary
- **Parent**: TopGarment
- **Definition**: Lowest edge of upper garment.
- **Location**: Around torso lower boundary.
- **Use**: Main reference for top length classification.

### CropTopLength / 크롭 길이 상의

- **Type**: top-length attribute
- **Parent**: TopGarment
- **Definition**: Top ending above or near natural waist.
- **Location**: Lower ribs to above waist.
- **Body reference**: May expose abdomen.

### RegularTopLength / 기본 길이 상의

- **Type**: top-length attribute
- **Parent**: TopGarment
- **Definition**: Top ending around waist or upper hip.
- **Location**: Waist to upper pelvis.
- **Body reference**: Common shirt/T-shirt length.

### LongTopLength / 롱 길이 상의

- **Type**: top-length attribute
- **Parent**: TopGarment
- **Definition**: Top extending over hip or toward upper thigh.
- **Location**: Lower pelvis to upper thigh.
- **Use**: Tunic, long shirt, long knit.

---

## 4.6 Waistline and Belt Line

### NaturalWaistLine / 자연 허리선

- **Type**: body reference line
- **Parent**: Waist
- **Definition**: Natural narrow area between ribs and pelvis.
- **Location**: Anatomical waist.
- **Use**: Body reference for clothing waist height.

### GarmentWaistLine / 의상 허리선

- **Type**: clothing boundary
- **Parent**: Garment
- **Definition**: Waist seam or intended waist position of garment.
- **Location**: Around torso or lower garment top.
- **Do not confuse with**: Natural waist. It may sit higher or lower.

### BeltLine / 벨트라인

- **Type**: clothing boundary / accessory line
- **Parent**: LowerGarment or Belt
- **Definition**: Horizontal line where belt sits or where lower garment begins.
- **Location**: Waist, high waist, or upper pelvis depending on style.
- **Visual cues**: Belt, waistband, pants/skirt top edge.
- **Use**: Main boundary between upper garment and lower garment.

### HighWaist / 하이웨이스트

- **Type**: waist-position attribute
- **Parent**: LowerGarment
- **Definition**: Lower garment waistline sits above natural waist or near navel.
- **Location**: Upper waist/abdomen area.
- **Visual cues**: Longer visible leg proportion.

### MidWaist / 미드웨이스트

- **Type**: waist-position attribute
- **Parent**: LowerGarment
- **Definition**: Waistline sits near natural waist.
- **Location**: Natural waist area.

### LowWaist / 로우웨이스트

- **Type**: waist-position attribute
- **Parent**: LowerGarment
- **Definition**: Waistline sits below natural waist near upper pelvis.
- **Location**: Pelvis/hip top.
- **Visual cues**: Longer visible torso, lower waistband.

### Belt / 벨트

- **Type**: clothing accessory
- **Parent**: Clothing
- **Definition**: Strap-like accessory around waist or belt line.
- **Location**: Around waist or lower garment top.
- **Visual cues**: Buckle, strap, belt loops.

---

## 4.7 Lower Garment Types

### LowerGarment / 하의

- **Type**: clothing region
- **Parent**: Clothing
- **Definition**: Garment covering lower body below waist or pelvis.
- **Location**: From belt line/waistband to hem.
- **Contains**: Pants, skirt, shorts, leggings.
- **Do not confuse with**: Body legs.

### Pants / 바지

- **Type**: lower garment
- **Parent**: LowerGarment
- **Definition**: Lower garment with separate left and right leg tubes.
- **Location**: Waist/belt line to leg hems.
- **Visual cues**: Two separated leg openings.
- **Attributes**: Pants length, waist position, fit.

### Shorts / 반바지

- **Type**: lower garment
- **Parent**: Pants
- **Definition**: Pants ending above knee.
- **Location**: Waist to thigh or above knee.
- **Visual cues**: Short leg tubes.

### Skirt / 치마

- **Type**: lower garment
- **Parent**: LowerGarment
- **Definition**: Lower garment not separated into two leg tubes.
- **Location**: Waist/belt line to skirt hem.
- **Visual cues**: Continuous fabric around lower body.
- **Attributes**: Skirt length and skirt shape.

### Leggings / 레깅스

- **Type**: lower garment
- **Parent**: LowerGarment
- **Definition**: Tight stretch lower garment following leg shape.
- **Location**: Waist to ankle, calf, or foot depending on style.
- **Visual cues**: Close-fitting fabric.
- **Do not confuse with**: Bare legs.

---

## 4.8 Pants Length Attributes

### VeryShortPantsLength / 매우 짧은 바지

- **Type**: pants-length attribute
- **Parent**: Pants
- **Definition**: Pants ending at upper thigh.
- **Body reference**: Upper thigh.
- **Visual cues**: Large leg exposure.

### ShortPantsLength / 반바지 길이

- **Type**: pants-length attribute
- **Parent**: Pants
- **Definition**: Pants ending around mid-thigh to above knee.
- **Body reference**: Mid thigh to above knee.

### KneePantsLength / 무릎 길이 바지

- **Type**: pants-length attribute
- **Parent**: Pants
- **Definition**: Pants ending around knee line.
- **Body reference**: Knee.

### SevenEighthPantsLength / 7부 바지

- **Type**: pants-length attribute
- **Parent**: Pants
- **Definition**: Pants ending below knee and around mid-calf.
- **Body reference**: Upper to mid lower leg.

### AnkleCropPantsLength / 9부 바지

- **Type**: pants-length attribute
- **Parent**: Pants
- **Definition**: Pants ending above ankle.
- **Body reference**: Lower calf to above ankle.
- **Visual cues**: Ankle visible.

### LongPantsLength / 긴바지

- **Type**: pants-length attribute
- **Parent**: Pants
- **Definition**: Pants reaching ankle.
- **Body reference**: Ankle.

### MaxiPantsLength / 맥시 길이 바지

- **Type**: pants-length attribute
- **Parent**: Pants
- **Definition**: Pants extending below ankle or onto shoe/foot top.
- **Body reference**: Below ankle to foot top.

---

## 4.9 Skirt Length Attributes

### MiniSkirtLength / 미니 스커트

- **Type**: skirt-length attribute
- **Parent**: Skirt
- **Definition**: Skirt ending at upper thigh or high above knee.
- **Body reference**: Upper thigh.
- **Visual cues**: Large leg exposure.

### ShortSkirtLength / 숏 스커트

- **Type**: skirt-length attribute
- **Parent**: Skirt
- **Definition**: Skirt ending around mid-thigh.
- **Body reference**: Mid thigh.

### AboveKneeSkirtLength / 무릎 위 길이 치마

- **Type**: skirt-length attribute
- **Parent**: Skirt
- **Definition**: Skirt ending below mid-thigh but above knee.
- **Body reference**: Lower thigh to above knee.

### KneeSkirtLength / 무릎 길이 치마

- **Type**: skirt-length attribute
- **Parent**: Skirt
- **Definition**: Skirt ending around knee line.
- **Body reference**: Knee.

### MidiSkirtLength / 미디 스커트

- **Type**: skirt-length attribute
- **Parent**: Skirt
- **Definition**: Skirt ending below knee and around calf.
- **Body reference**: Upper to mid lower leg.

### LongSkirtLength / 롱 스커트

- **Type**: skirt-length attribute
- **Parent**: Skirt
- **Definition**: Skirt ending around lower calf to above ankle.
- **Body reference**: Lower calf to ankle.

### MaxiSkirtLength / 맥시 스커트

- **Type**: skirt-length attribute
- **Parent**: Skirt
- **Definition**: Skirt reaching ankle, foot top, or floor-close length.
- **Body reference**: Ankle to foot top.

---

## 4.10 Skirt Shape Attributes

### ALineSkirt / A라인 스커트

- **Type**: skirt-shape attribute
- **Parent**: Skirt
- **Definition**: Skirt that widens gradually from waist to hem.
- **Visual cues**: Narrow top, wider bottom.

### PleatedSkirt / 플리츠 스커트

- **Type**: skirt-shape attribute
- **Parent**: Skirt
- **Definition**: Skirt with repeated folds or pleats.
- **Visual cues**: Many vertical fold lines.

### TightSkirt / 타이트 스커트

- **Type**: skirt-shape attribute
- **Parent**: Skirt
- **Definition**: Skirt closely following hip and thigh shape.
- **Visual cues**: Narrow silhouette.

### FlareSkirt / 플레어 스커트

- **Type**: skirt-shape attribute
- **Parent**: Skirt
- **Definition**: Skirt with stronger outward spread toward hem.
- **Visual cues**: Wider volume and fabric flare than simple A-line.

---

## 4.11 Clothing Boundaries and Reference Lines

### ShoulderSeam / 어깨 봉제선

- **Type**: clothing boundary
- **Parent**: TopGarment
- **Definition**: Garment seam along shoulder.
- **Location**: Near body shoulder line.
- **Do not confuse with**: Anatomical shoulder line.

### Armhole / 암홀

- **Type**: clothing boundary
- **Parent**: TopGarment
- **Definition**: Opening or seam where sleeve attaches to body garment.
- **Location**: Around shoulder and armpit.
- **Use**: Important for sleeveless/cap sleeve classification.

### Placket / 앞여밈

- **Type**: clothing feature
- **Parent**: Shirt or Jacket
- **Definition**: Front opening strip, often with buttons.
- **Location**: Center front of garment.

### Button / 단추

- **Type**: clothing feature
- **Parent**: Garment
- **Definition**: Small fastening element.
- **Location**: Placket, cuff, waistband, or jacket front.

### Waistband / 허리밴드

- **Type**: clothing boundary
- **Parent**: LowerGarment
- **Definition**: Top band of pants, skirt, shorts, or leggings.
- **Location**: Around waist or pelvis.
- **Use**: Main visible belt-line reference.

### LowerGarmentHem / 하의 밑단

- **Type**: clothing boundary
- **Parent**: LowerGarment
- **Definition**: Lowest edge of pants, skirt, shorts, or leggings.
- **Location**: Distal garment edge.
- **Use**: Main length classification reference.

---

# 5. Recommended Dictionary Buckets

## Body Regions

- Hand
- Palm
- BackOfHand
- Wrist
- Forearm
- Elbow
- UpperArm
- Shoulder
- Neck
- Chest
- Abdomen
- Back
- Waist
- Pelvis
- Thigh
- Knee
- LowerLeg
- Ankle
- Foot
- Toe

## Body Surfaces

- Palm
- BackOfHand
- FrontNeck
- SideNeck
- BackNeck
- FootTop
- FootSole
- InnerElbow
- OuterElbow
- PoplitealArea

## Digits

- Thumb
- IndexFinger
- MiddleFinger
- RingFinger
- LittleFinger
- Toe
- BigToe
- LittleToe

## Joints

- WristJoint
- FingerBaseJoint
- FingerMiddleJoint
- FingerTipJoint
- ThumbBaseJoint
- ThumbMiddleJoint
- ElbowJoint
- ShoulderJoint
- HipJoint
- KneeJoint
- AnkleJoint

## Shape and Texture Features

- UpperArmSoftTissue
- NeckWrinkle
- KneeWrinkle
- WaistFold
- Trapezius
- Clavicle

## Hair Regions and Hair Features

- ScalpHair
- Hairline
- BabyHair
- Eyebrow
- Eyelash
- Sideburn
- Beard
- Mustache
- ChinBeard
- UnderChinBeard
- CheekBeard
- NoseHair
- EarHair
- NeckHair
- ArmpitHair
- ChestHair
- AbdominalHair
- BackHair
- UpperArmHair
- ForearmHair
- HandBackHair
- FingerHair
- ThighHair
- LowerLegHair
- FootTopHair
- ToeHair

## Clothing Types

- TopGarment
- Shirt
- TShirt
- Blouse
- KnitTop
- Jacket
- Cardigan
- Hoodie
- LowerGarment
- Pants
- Shorts
- Skirt
- Leggings
- Belt

## Clothing Attributes

- RoundNeck
- VNeck
- SquareNeck
- BoatNeck
- TurtleNeck
- ShirtCollar
- Sleeveless
- CapSleeve
- ShortSleeve
- HalfSleeve
- ThreeQuarterSleeve
- LongSleeve
- PuffSleeve
- CropTopLength
- RegularTopLength
- LongTopLength
- HighWaist
- MidWaist
- LowWaist
- VeryShortPantsLength
- ShortPantsLength
- KneePantsLength
- SevenEighthPantsLength
- AnkleCropPantsLength
- LongPantsLength
- MaxiPantsLength
- MiniSkirtLength
- ShortSkirtLength
- AboveKneeSkirtLength
- KneeSkirtLength
- MidiSkirtLength
- LongSkirtLength
- MaxiSkirtLength
- ALineSkirt
- PleatedSkirt
- TightSkirt
- FlareSkirt

## Clothing Boundaries

- Neckline
- Sleeve
- SleeveHem
- Cuff
- TopHem
- NaturalWaistLine
- GarmentWaistLine
- BeltLine
- Waistband
- LowerGarmentHem
- ShoulderSeam
- Armhole
- Placket
- Button

---

# 6. Implementation Notes for Codex

## Naming Direction

Use stable English IDs for code and logs.
Keep Korean labels as display aliases.

Recommended shape:

```text
Id: UpperArmSoftTissue
KoreanLabel: 팔뚝살
Type: ShapeFeature
Parent: UpperArm
```

## Region Detection Priority

For future detection or manual labeling, prefer this order:

1. Identify parent body or clothing region.
2. Identify visible boundary or joint reference.
3. Add feature or attribute tag.
4. Do not treat attributes as separate physical regions unless needed for masks.

## Mask Ownership Rule

A mask should have one clear owner.

Examples:

- `FingerHairMask` belongs to `Finger`.
- `NeckWrinkleMask` belongs to `Neck`.
- `SleeveHemMask` belongs to `Sleeve`.
- `BeltLineMask` belongs to `LowerGarment` or `Belt`.

## Retouch Tool Routing Rule

Do not use these terms to trigger automatic beautification.
Use them only to route local tools, debug overlays, manual guides, and location notifications.

## Warning Rule

If a region cannot be identified safely, use warning output instead of guessing.

Examples:

- `finger_identity_uncertain`
- `neckline_hidden_by_hair`
- `beltline_hidden_by_top`
- `sleeve_hem_occluded`
- `knee_region_occluded`
- `hair_region_not_separable`

## Do-Not-Do Rules

- Do not merge body skin and clothing into one mask.
- Do not treat body hair as skin blemish by default.
- Do not treat wrinkles and hair as the same texture class.
- Do not use clothing length labels without a body reference line.
- Do not let garment boundaries overwrite anatomical boundaries.
- Do not run full-body automatic correction just because a body part was detected.



---

# 7. Portrait Framing and Crop Coverage Dictionary

## Purpose

This section defines portrait crop, visible body coverage, and pose/orientation terms.

These labels are for location notification and framing classification only.
They are not body masks and must not overwrite body, hair, or clothing regions.

## 7.1 Core Framing Rules

### Framing Label Rule

A framing label describes how much of the person is visible in the photo.
It does not mean every visible pixel belongs to that body region.

Examples:

- `UpperBodyShot` means the crop shows upper-body portrait coverage.
- Clothing, hair, background, hands, and props still need separate masks.

### Lowest Visible Reference Rule

Classify framing by the lowest reliably visible body reference.

Primary reference order:

1. chin
2. neck
3. shoulders
4. chest / bust line
5. waist
6. belt line
7. hips / pelvis
8. upper thigh
9. knee
10. calf / lower leg
11. ankle
12. feet

### Studio Framing Rule: UpperBody vs HalfBody

Use photographic framing meaning, not casual anatomical wording.

- `FullBodyShot / 전신` means the full person from head to feet.
- `HalfBodyShot / 반신` means roughly half of the full body in portrait framing. In studio use, it commonly reaches the waist, belt line, pelvis, or upper thigh.
- `UpperBodyShot / 상반신` means the upper half of a half-body portrait. It is shorter than half-body.
- In this dictionary, `UpperBodyShot / 상반신` should stop around head, neck, shoulders, and chest/upper torso.
- If the crop reaches natural waist, belt line, pelvis, or upper thigh, classify it as `HalfBodyShot / 반신`, not `UpperBodyShot / 상반신`.
- Casual Korean may call the body below the waist `하반신`, but this document uses `LowerBodyShot / 하반신 컷` only as a lower-body-focused crop label, not as the opposite half of `UpperBodyShot`.

### Occlusion Rule

If clothing, hair, hands, props, or crop edges hide a body reference, classify only by the visible coverage.
Do not infer invisible lower body parts unless the task explicitly needs estimated body layout.

---

## 7.2 Portrait Crop Terms

### FaceCloseUp / 얼굴 클로즈업

- **Type**: portrait framing
- **Parent**: ImageFraming
- **Definition**: Crop focused mainly on the face.
- **Visible range**: Face fills most of the frame; hairline, chin, or part of neck may be cropped.
- **Lower reference**: Chin or upper neck.
- **Use**: Face detail, skin, eyes, nose, mouth, eyebrow, facial-hair labels.
- **Do not confuse with**: Headshot if full head and shoulders are visible.

### Headshot / 두상

- **Type**: portrait framing
- **Parent**: ImageFraming
- **Definition**: Crop showing the head and usually a small amount of neck.
- **Visible range**: Full head, face, hair, and neck start.
- **Lower reference**: Neck or shoulder start.
- **Use**: ID/profile face framing, hairline, face contour, neck start.
- **Do not confuse with**: Head-and-shoulders if shoulder line is clearly visible.

### HeadAndShouldersShot / 두상+어깨

- **Type**: portrait framing
- **Parent**: ImageFraming
- **Definition**: Crop showing head, neck, and shoulders.
- **Visible range**: Head to shoulder line or upper chest.
- **Lower reference**: Shoulder line or upper chest.
- **Use**: ID photo, profile portrait, studio portrait.
- **Do not confuse with**: Bust shot if chest area is more fully included.

### BustShot / 흉상

- **Type**: portrait framing
- **Parent**: ImageFraming
- **Definition**: Crop showing head, shoulders, and chest area.
- **Visible range**: Head to chest or upper torso.
- **Lower reference**: Chest, sternum area, bust line, or upper rib area.
- **Use**: Portrait retouch, neckline, collar, jacket, chest-level framing.
- **Do not confuse with**: Half-body shot if waist, belt line, hips, or upper thigh are visible.

### UpperBodyShot / 상반신

- **Type**: portrait framing
- **Parent**: ImageFraming
- **Definition**: Studio portrait crop showing the upper half of a half-body portrait, not the full anatomical upper half of the body.
- **Visible range**: Head, neck, shoulders, and chest/upper torso.
- **Lower reference**: Chest, bust line, or upper torso area.
- **Studio rule**: If the crop reaches the natural waist, belt line, pelvis, or upper thigh, it is already `HalfBodyShot / 반신`, not `UpperBodyShot / 상반신`.
- **Use**: ID photo, profile portrait, studio portrait, neckline, collar, shoulder, and upper-chest labels.
- **Do not confuse with**: Waist-up, half-body, or casual anatomical upper-body wording.

### WaistUpShot / 허리 위

- **Type**: portrait framing
- **Parent**: ImageFraming
- **Definition**: Crop ending around the natural waist.
- **Visible range**: Head to natural waist.
- **Lower reference**: Natural waistline.
- **Studio rule**: Treat waist-up as a longer crop than `UpperBodyShot / 상반신`; depending on context it may be grouped near `HalfBodyShot / 반신`.
- **Use**: Clothing top, waist fold, belt-near crop, hand-on-waist pose.
- **Do not confuse with**: UpperBodyShot if the image stops around chest/upper torso.

### HalfBodyShot / 반신

- **Type**: portrait framing
- **Parent**: ImageFraming
- **Definition**: Studio portrait crop representing roughly half of the full body.
- **Visible range**: Head down to natural waist, belt line, hip line, pelvis, or upper thigh depending on crop style.
- **Lower reference**: Natural waist, belt line, hip line, pelvis, or upper thigh.
- **Studio rule**: This is longer than `UpperBodyShot / 상반신`. If waist or belt line is visible, prefer `HalfBodyShot / 반신`.
- **Use**: Portrait or fashion image where upper garment and lower garment top may be visible.
- **Do not confuse with**: UpperBodyShot. If only head, shoulders, and chest/upper torso are visible, use `UpperBodyShot` instead.

### ThighUpShot / 허벅지 위

- **Type**: portrait framing
- **Parent**: ImageFraming
- **Definition**: Crop showing head to upper or mid-thigh.
- **Visible range**: Head to thigh area.
- **Lower reference**: Upper or mid-thigh.
- **Use**: Fashion, jacket length, skirt/pants top, hand position.
- **Do not confuse with**: Knee-up shot if knees are visible.

### KneeUpShot / 무릎 위

- **Type**: portrait framing
- **Parent**: ImageFraming
- **Definition**: Crop showing head to knee area or just above knees.
- **Visible range**: Head to knee or above-knee region.
- **Lower reference**: Knee line.
- **Use**: Dress length, skirt length, pants length, leg visibility.

### ThreeQuarterBodyShot / 3/4신

- **Type**: portrait framing
- **Parent**: ImageFraming
- **Definition**: Crop showing most of the body but not necessarily the feet.
- **Visible range**: Head to lower leg, calf, or ankle area.
- **Lower reference**: Calf, lower leg, or ankle.
- **Use**: Long clothing, standing pose, lower-body alignment.
- **Do not confuse with**: Full body if feet are cropped.

### FullBodyShot / 전신

- **Type**: portrait framing
- **Parent**: ImageFraming
- **Definition**: Crop showing the whole person from head to feet.
- **Visible range**: Head, torso, legs, feet.
- **Lower reference**: Feet included or at least full foot boundary visible.
- **Use**: Full-body alignment, clothing length, pose, shoes.
- **Do not confuse with**: Three-quarter body if ankles or feet are cropped.

### FeetIncludedFullBodyShot / 발 포함 전신

- **Type**: portrait framing
- **Parent**: ImageFraming
- **Definition**: Full-body crop where feet are clearly visible and not cropped.
- **Visible range**: Head to complete feet.
- **Lower reference**: Full foot outline.
- **Use**: Best full-body reference for pose, height, garment length, shoe/foot labels.

---

## 7.3 Partial Detail Framing Terms

### HandsOnlyShot / 손 중심 컷

- **Type**: partial framing
- **Parent**: ImageFraming
- **Definition**: Crop focused on hands.
- **Visible range**: Hand, fingers, wrist, sometimes forearm.
- **Use**: Hand, nail, finger joint, hand hair labels.

### FeetOnlyShot / 발 중심 컷

- **Type**: partial framing
- **Parent**: ImageFraming
- **Definition**: Crop focused on feet.
- **Visible range**: Foot, toes, ankle, sometimes lower leg.
- **Use**: Footwear, toes, ankle, foot hair labels.

### TorsoOnlyShot / 몸통 중심 컷

- **Type**: partial framing
- **Parent**: ImageFraming
- **Definition**: Crop focused on torso without full head or full legs.
- **Visible range**: Chest, abdomen, waist, clothing.
- **Use**: Clothing, belt line, waist fold, garment boundary labels.

### LowerBodyShot / 하반신 컷

- **Type**: partial framing
- **Parent**: ImageFraming
- **Definition**: Crop focused on the lower body area, commonly understood as below the waist in casual use.
- **Visible range**: Waist, hips, legs, pants, skirt, shoes.
- **Studio rule**: Do not use this as the strict opposite of `UpperBodyShot / 상반신`. In this document, `UpperBodyShot` follows studio portrait framing and means the upper half of a half-body crop.
- **Use**: Lower garment, skirt length, pants length, knee, leg hair labels.

---

## 7.4 Orientation and Pose Coverage Terms

### FrontFacingPortrait / 정면 인물

- **Type**: orientation label
- **Parent**: ImageFraming
- **Definition**: Subject faces the camera mostly frontally.
- **Use**: Symmetry, face alignment, clothing centerline.

### SideFacingPortrait / 측면 인물

- **Type**: orientation label
- **Parent**: ImageFraming
- **Definition**: Subject is shown mostly from the side.
- **Use**: Side neck, profile, body outline, clothing silhouette.

### ThreeQuarterFacingPortrait / 반측면 인물

- **Type**: orientation label
- **Parent**: ImageFraming
- **Definition**: Subject is turned between frontal and side view.
- **Use**: Common portrait angle, asymmetric visibility handling.

### SeatedPortrait / 앉은 인물

- **Type**: pose label
- **Parent**: ImageFraming
- **Definition**: Subject is sitting.
- **Use**: Lap, bent knees, folded clothing, hidden lower body.

### StandingPortrait / 선 인물

- **Type**: pose label
- **Parent**: ImageFraming
- **Definition**: Subject is standing.
- **Use**: Full-body alignment, clothing length, leg visibility.

---

## 7.5 Framing Implementation Notes

### Recommended Framing Buckets

- `FaceCloseUp`
- `Headshot`
- `HeadAndShouldersShot`
- `BustShot`
- `UpperBodyShot`
- `WaistUpShot`
- `HalfBodyShot`
- `ThighUpShot`
- `KneeUpShot`
- `ThreeQuarterBodyShot`
- `FullBodyShot`
- `FeetIncludedFullBodyShot`

### Warning Labels

Use warning labels instead of guessing.

Examples:

- `framing_lower_body_occluded`
- `feet_cut_off`
- `waistline_hidden_by_clothing`
- `head_top_cropped`
- `shoulder_line_occluded`
- `hands_occlude_torso`
- `seated_pose_hides_leg_length`

### Do-Not-Do Rules

- Do not use framing labels as body masks.
- Do not infer full body if feet are cropped.
- Do not call a crop full-body when ankles or feet are missing.
- Do not confuse `UpperBodyShot` with `HalfBodyShot`; `UpperBodyShot` stops around chest/upper torso, while `HalfBodyShot` can extend to waist, hips, pelvis, or upper thigh.
- Do not use clothing hem position alone to determine body framing.
