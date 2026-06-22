# Face Visible Structure Master

## 1. Purpose

`Face Visible Structure Master` defines the visible-face structure vocabulary for KRetouchPro.

Its role is:
- keep the detector focused on what is actually visible in a 2D portrait photo
- separate direct detection targets from indirect support cues
- separate usable surface structure from reference-only internal anatomy
- provide one shared face-structure map for detection, masking, warping, and local retouch routing

This is not a medical anatomy document.
It is an engine-facing visible-structure definition.

## 2. Core Rule

All face structure should be classified into three layers:

### 2.1 Direct detection target

Visible structure that the engine may detect directly from pixels.

Examples:
- edge
- shadow band
- fold line
- dark pocket
- soft highlight boundary
- color region

### 2.2 Indirect support cue

Visible evidence that should support another detector, but should not always become the final boundary by itself.

Examples:
- specular highlight
- soft reflected light
- texture direction
- local shadow falloff
- surrounding skin tone continuity

### 2.3 Reference-only anatomy

Anatomical structure that is useful for naming or conceptual understanding, but is not a direct 2D detection target in the current engine.

Examples:
- lens
- retina
- vitreous body
- deep internal cartilage or bone terms that are not directly visible

## 3. Usage Rule

For every structure in this document, the engine should know:
- what can be seen
- what can be detected directly
- what can be used as support only
- what is reference only
- which tool family may consume it later

## 4. Forehead And Brow Region

### 4.1 Forehead skin

What is visible:
- broad skin plane above the brows
- tone gradient
- wrinkle bands
- hairline transition

Direct detection target:
- forehead skin region
- horizontal wrinkle band candidates
- forehead-to-hair boundary support

Indirect support cue:
- highlight rolloff across the forehead
- brow shadow that helps separate upper orbital structure

Reference-only anatomy:
- skull contour as a conceptual guide only

Tool / use cases:
- forehead tone cleanup
- forehead wrinkle cleanup
- hairline support
- upper crop support

### 4.2 Eyebrow

What is visible:
- hair band above the orbital region
- eyebrow top contour
- eyebrow bottom contour
- hair direction and density

Direct detection target:
- eyebrow body region
- brow lower edge
- brow tail direction

Indirect support cue:
- orbital shadow under the brow
- local skin average near brow boundary

Reference-only anatomy:
- deep brow bone shape as a conceptual support only

Tool / use cases:
- brow protection
- brow replacement
- brow density or shape routing

## 5. Eye Region

### 5.1 Upper eyelid

What is visible:
- upper lid contour
- lash attachment line
- upper fold shadow

Direct detection target:
- upper eyelid curve
- upper lash line
- upper fold band

Indirect support cue:
- soft lid shadow
- brow-to-lid tone falloff

Reference-only anatomy:
- internal eyelid tissue layers

Tool / use cases:
- eye shape routing
- fold cleanup
- eye-area protection masks

### 5.2 Lower eyelid

What is visible:
- lower lid curve
- lower lid edge
- fine under-eye fold

Direct detection target:
- lower eyelid curve
- lid margin

Indirect support cue:
- under-eye shadow band
- lower sclera brightness support

Reference-only anatomy:
- internal lower-lid tissue layers

Tool / use cases:
- eye bag routing
- under-eye cleanup
- eye-shape support

### 5.3 Eyelashes

What is visible:
- upper lash cluster
- lower lash cluster
- lash direction

Direct detection target:
- lash band as a dark contour support

Indirect support cue:
- local dark spikes near lid edges

Reference-only anatomy:
- follicle structure

Tool / use cases:
- lid-edge support
- eye protection masks

### 5.4 Sclera

What is visible:
- bright eye-white region around the iris
- small vessel texture in some cases

Direct detection target:
- sclera region
- iris boundary support

Indirect support cue:
- brightness separation from iris and eyelids

Reference-only anatomy:
- deeper eye globe structure

Tool / use cases:
- eye-white cleanup
- eye contrast routing

### 5.5 Iris

What is visible:
- colored circular eye region
- iris outer ring
- iris radial texture

Direct detection target:
- iris circular boundary
- iris visible mask

Indirect support cue:
- corneal highlight interruption
- pupil-centered roundness support

Reference-only anatomy:
- internal iris muscle explanation only

Tool / use cases:
- iris clarity
- circle-lens style enhancement
- eye-color routing

### 5.6 Pupil

What is visible:
- darkest central round region

Direct detection target:
- pupil center
- pupil radius candidate

Indirect support cue:
- darkest local minimum after eye ROI prepass

Reference-only anatomy:
- light-path explanation only

Tool / use cases:
- pupil-centered eye routing
- iris detector stabilization

### 5.7 Tear duct / inner corner

What is visible:
- small bright or pink corner structure at the medial eye corner

Direct detection target:
- inner-corner notch / pocket

Indirect support cue:
- local brightness and skin-red transition

Reference-only anatomy:
- tear drainage anatomy

Tool / use cases:
- eye boundary completion
- inner-corner protection

### 5.8 Under-eye band

What is visible:
- soft crescent shadow or bag band under the lower eyelid

Direct detection target:
- under-eye shadow band

Indirect support cue:
- luminance drop below lower lid
- texture compression under eye

Reference-only anatomy:
- orbital fat explanation only

Tool / use cases:
- dark-circle cleanup
- eye-bag routing

### 5.9 Corneal highlight

What is visible:
- bright specular reflection on the eye surface

Direct detection target:
- usually not a primary boundary target

Indirect support cue:
- confirms eye orientation
- can support iris / pupil plausibility

Reference-only anatomy:
- cornea as a transparent optical layer

Tool / use cases:
- highlight preservation
- eye realism protection

## 6. Nose Region

### 6.1 Nose bridge

What is visible:
- vertical or slightly curved highlight plane
- side shadow falloff
- hard upper-center axis impression
- transition from the glabella area into the bridge

Direct detection target:
- bridge center band
- bridge side gradient
- bridge axis candidate
- visible upper bridge region

Indirect support cue:
- symmetric highlight / shadow balance
- nasal bone support impression
- upper lateral transition below the bone zone

Reference-only anatomy:
- nasal bone
- upper lateral cartilage

Tool / use cases:
- bridge shaping
- nose symmetry support
- bridge highlight protection

### 6.2 Septum support line

What is visible:
- central lower-nose support impression
- soft center divider above the philtrum

Direct detection target:
- usually not a hard visible contour target

Indirect support cue:
- center-axis continuity from bridge to tip
- nostril symmetry support
- philtrum alignment support

Reference-only anatomy:
- septal cartilage

Tool / use cases:
- lower nose symmetry interpretation
- nostril alignment support

### 6.3 Nose side planes

What is visible:
- left and right sloping planes from the bridge toward the tip and alar region
- soft side shadow break

Direct detection target:
- left side plane
- right side plane
- side-plane luminance break

Indirect support cue:
- bridge-to-tip volume continuity
- cheek-to-nose transition

Reference-only anatomy:
- upper lateral cartilage support interpretation

Tool / use cases:
- nose contour support
- bridge width reading

### 6.4 Nose tip

What is visible:
- rounded bright or mid-tone front plane
- tip highlight core
- soft lower edge before nostril shadow

Direct detection target:
- tip center region
- tip highlight core
- visible tip mass

Indirect support cue:
- nostril and alar geometry around the tip
- center-axis continuity from bridge

Reference-only anatomy:
- greater alar cartilage as a form explanation

Tool / use cases:
- tip protection
- nose-volume routing
- lower nose stabilization

### 6.5 Alar wings / nostril side walls

What is visible:
- left and right rounded side structures
- dark nostril pockets nearby
- soft outer flare width
- alar base height difference

Direct detection target:
- alar outer edge
- alar base width
- left alar body
- right alar body

Indirect support cue:
- nostril darkness
- skin-shadow transition
- tip adjacency
- lower sidewall curve

Reference-only anatomy:
- greater alar cartilage
- lesser alar cartilage

Tool / use cases:
- nostril asymmetry correction
- alar width shaping
- alar height balancing

### 6.6 Nostrils

What is visible:
- darkest small cavities under the nose tip
- left / right dark pocket
- nostril rim compression

Direct detection target:
- nostril dark pocket
- left/right nostril center candidate
- nostril lower boundary candidate

Indirect support cue:
- lower nose shadow support
- tip and ala symmetry
- septum-centered spacing

Reference-only anatomy:
- internal nasal cavity

Tool / use cases:
- nostril alignment
- lower nose detector stabilization
- nostril width / height comparison

### 6.7 Septum base and columella support

What is visible:
- soft central lower divider between nostrils
- short hanging center form above the philtrum

Direct detection target:
- rarely a full hard contour
- partial central divider support only

Indirect support cue:
- nostril spacing
- tip center lock
- philtrum connection

Reference-only anatomy:
- septal cartilage lower support
- columella anatomy as naming reference

Tool / use cases:
- lower-nose symmetry reading
- tip / philtrum center alignment

### 6.8 Philtrum

What is visible:
- vertical groove band between the nose base and upper lip
- central shallow valley
- left / right ridge pair in some lighting

Direct detection target:
- central groove shadow
- philtrum center path

Indirect support cue:
- upper lip center geometry
- nose-base symmetry support
- septum base continuity

Reference-only anatomy:
- deep muscular explanation only

Tool / use cases:
- upper-lip balance
- lip-center alignment
- nose-to-mouth center-axis support

### 6.9 Soft tissue outer contour

What is visible:
- visible skin envelope over the full nose
- smooth contour from bridge to tip to ala

Direct detection target:
- outer soft contour
- skin-plane boundary where visible

Indirect support cue:
- broad luminance falloff
- local texture continuity

Reference-only anatomy:
- soft tissue thickness map as conceptual support only

Tool / use cases:
- nose masking
- soft contour cleanup
- local retouch protection

### 6.10 Maxilla support interpretation

What is visible:
- not a direct visible nose structure by itself
- indirectly affects the side support under the alar and philtrum region

Direct detection target:
- none

Indirect support cue:
- lower side-plane support
- philtrum and upper-lip transition support

Reference-only anatomy:
- maxilla

Tool / use cases:
- naming reference only
- lower midface volume interpretation

## 7. Mouth Region

### 7.1 Structural reading order

What is visible:
- upper-lip center reads as a `W`-like crest
- lower-lip body reads as a `U`-like bowl
- the mouth is not a flat line but a clustered soft-volume structure

Direct detection target:
- mouth-width span
- upper-lip center crest candidate
- lower-lip bowl candidate

Indirect support cue:
- lip-to-skin contrast
- mouth slit continuity
- central highlight and shadow rhythm

Reference-only anatomy:
- construction-style drawing explanation only

Tool / use cases:
- mouth ROI entry
- lip-shape routing
- volume-aware lip masking

### 7.2 Mouth slit

What is visible:
- dark band where upper and lower lips meet
- center-depth line
- local compression at left and right ends

Direct detection target:
- mouth center line
- left and right mouth corner limit
- visible slit polyline candidate

Indirect support cue:
- lip contrast around the slit
- upper-lip and lower-lip contact rhythm

Reference-only anatomy:
- oral cavity depth explanation only

Tool / use cases:
- mouth-corner detection
- lip shape separation
- open/closed mouth routing

### 7.3 Cupid's bow / upper-lip crest

What is visible:
- the upper-lip center reads as a `W`-like crest
- two center peaks with a shallow dip between them

Direct detection target:
- left crest peak
- right crest peak
- center dip

Indirect support cue:
- philtrum continuity
- upper-lip shadow falloff

Reference-only anatomy:
- idealized drawing construction cue

Tool / use cases:
- upper-lip contour control
- lip-center symmetry
- lip tint edge routing

### 7.4 Upper lip

What is visible:
- darker lip plane
- cupid-bow shape
- central to lateral soft-volume breakup
- reduced highlight compared with the lower lip

Direct detection target:
- upper lip outer contour
- cupid-bow center peaks
- upper-lip body mask
- upper-lip lower edge against the mouth slit

Indirect support cue:
- philtrum shadow
- mouth slit relation
- orbicularis-oris curvature support

Reference-only anatomy:
- internal soft tissue
- orbicularis-oris muscular explanation only

Tool / use cases:
- lip contour
- upper-lip volumizing
- lip tint routing
- upper-lip shape correction

### 7.5 Lower lip

What is visible:
- broader brighter lip plane
- central highlight band
- fuller front mass
- softer outer edge into surrounding skin

Direct detection target:
- lower lip outer contour
- lower lip central mass
- lower-lip highlight zone
- lower-lip lower boundary candidate

Indirect support cue:
- lower-lip highlight
- shadow under lip
- lower-lip side fade toward the corners

Reference-only anatomy:
- internal soft tissue

Tool / use cases:
- lower-lip volumizing
- tint / gloss routing
- lower-lip highlight preservation

### 7.6 Mouth corners

What is visible:
- dark compressed endpoints at left and right mouth edges
- small inward pocket
- soft overlap between lip mass and cheek tissue

Direct detection target:
- left mouth corner
- right mouth corner
- corner depth notch

Indirect support cue:
- small triangular shadow pocket
- lip-muscle overlap
- nasolabial extension support

Reference-only anatomy:
- deep oral muscle anatomy

Tool / use cases:
- smile lift
- mouth width support
- corner asymmetry correction

### 7.7 Philtrum

What is visible:
- vertical groove band between nose base and upper lip
- shallow center valley with left/right ridge support in some lighting

Direct detection target:
- central groove shadow
- philtrum center path

Indirect support cue:
- upper lip center geometry
- nose-base symmetry support
- cupid-bow alignment support

Reference-only anatomy:
- deep muscular explanation only

Tool / use cases:
- upper-lip balance
- lip-center alignment
- nose-to-mouth center-axis support

### 7.8 Lips proper / vermilion body

What is visible:
- the actual colored lip surface distinct from surrounding skin
- the upper and lower lip masses read as separate visible color/texture zones

Direct detection target:
- lip-surface ownership
- upper/lower lip split

Indirect support cue:
- saturation difference against surrounding skin
- mouth slit separation

Reference-only anatomy:
- formal lip-anatomy naming layer

Tool / use cases:
- lip tint masking
- gloss routing
- lip-only texture processing

### 7.9 Orbicularis-oris support ring

What is visible:
- not a sharp contour by itself
- appears indirectly as radial curvature and skin-flow around the lips

Direct detection target:
- none as a hard visible boundary

Indirect support cue:
- circular skin-flow support around the mouth
- radial wrinkle direction
- corner compression support

Reference-only anatomy:
- orbicularis oris muscle

Tool / use cases:
- volume interpretation
- wrinkle routing around the mouth
- natural lip blend support

### 7.10 Nasolabial extension toward mouth

What is visible:
- fold extension from the nose side toward the mouth corner
- cheek-to-corner compression band

Direct detection target:
- local fold-shadow candidate near the corner approach

Indirect support cue:
- mouth corner depth
- cheek volume drop

Reference-only anatomy:
- extended nasolabial fold interpretation

Tool / use cases:
- smile-line cleanup
- mouth-corner context support

### 7.11 Lower-lip to chin transition

What is visible:
- soft shadow under the lower lip
- transition from lip mass into the chin plane

Direct detection target:
- under-lip shadow band
- lower-lip stop line

Indirect support cue:
- chin volume
- lower-lip highlight balance

Reference-only anatomy:
- lower-mouth soft tissue explanation only

Tool / use cases:
- lower-lip separation
- lip-to-chin contour support

### 7.12 Mandible support interpretation

What is visible:
- not directly visible as a lip structure
- indirectly supports the lower-lip and chin foundation

Direct detection target:
- none

Indirect support cue:
- lower-face support under the mouth
- chin transition stability

Reference-only anatomy:
- mandible

Tool / use cases:
- naming reference only
- lower-mouth volume interpretation

## 8. Cheek And Midface Region

### 8.1 Nasolabial fold

What is visible:
- diagonal or curved fold from nose side toward mouth corner

Direct detection target:
- fold line
- fold shadow band

Indirect support cue:
- cheek volume drop
- mouth-corner compression

Reference-only anatomy:
- facial fat pad explanation only

Tool / use cases:
- wrinkle cleanup
- age-line reduction

### 8.2 Cheek plane

What is visible:
- broad lateral face surface
- highlight and shadow volume

Direct detection target:
- cheek region ownership
- cheek shading band

Indirect support cue:
- facial center-to-side luminance falloff

Reference-only anatomy:
- zygomatic bone explanation only

Tool / use cases:
- shading
- face slimming
- skin cleanup

### 8.3 Cheekbone region

What is visible:
- side highlight ridge or side shadow break

Direct detection target:
- cheekbone band candidate

Indirect support cue:
- eye-to-mouth side-plane transition

Reference-only anatomy:
- zygomatic arch anatomy

Tool / use cases:
- contour shaping
- cheek suppression routing

## 9. Jawline, Chin, And Lower Face Region

### 9.1 Jawline

What is visible:
- lower face contour from side cheek toward chin

Direct detection target:
- face-to-neck contour band
- lower-face side boundary

Indirect support cue:
- under-jaw shadow
- neck skin start

Reference-only anatomy:
- mandible anatomy as conceptual support only

Tool / use cases:
- face shape
- jawline trim
- symmetry correction

### 9.2 Chin

What is visible:
- front lower-face mass
- lower highlight and under-shadow separation

Direct detection target:
- chin center
- chin lower edge band

Indirect support cue:
- mouth-center axis
- jawline continuity

Reference-only anatomy:
- chin bone explanation only

Tool / use cases:
- chin alignment
- lower-face crop support
- jaw / neck work-area anchor

### 9.3 Under-jaw band

What is visible:
- shadow or compressed transition directly below the jawline

Direct detection target:
- under-jaw shadow band

Indirect support cue:
- neck skin tone continuity
- clothing-start guide relation

Reference-only anatomy:
- deep submental anatomy

Tool / use cases:
- double-chin detection
- neck transition routing

### 9.4 Double-chin region

What is visible:
- second lower curve or soft skin bulge below the jawline

Direct detection target:
- lower soft bulge candidate
- submental mask region

Indirect support cue:
- under-jaw shadow stacking
- neck-to-clothing separation

Reference-only anatomy:
- fat and muscle explanation only

Tool / use cases:
- double-chin reduction
- lower-face cleanup

## 10. Neck And Clothing Entry Region

### 10.1 Neck skin

What is visible:
- vertical skin band below the chin
- wrinkle lines
- tone gradients

Direct detection target:
- visible neck skin region
- neck wrinkle band

Indirect support cue:
- jawline lower boundary
- clothing-start line

Reference-only anatomy:
- deep neck muscle explanation

Tool / use cases:
- neck wrinkle cleanup
- skin continuity
- lower-face support

### 10.2 Clothing start

What is visible:
- neck-to-clothing boundary
- collar or garment onset

Direct detection target:
- lower skin stop line
- clothing boundary guide

Indirect support cue:
- color break
- texture break
- shadow seam

Reference-only anatomy:
- none

Tool / use cases:
- crop stabilization
- neck / clothing separation
- lower protection masks

## 11. Glasses And Optical Accessory Layer

### 11.1 Glasses overview

What is visible:
- glasses appear as a separate worn object placed over the eye and nose region
- they are not part of the eye contour itself
- they may contain both opaque frame structure and transparent lens structure

Direct detection target:
- whole glasses ownership candidate
- left / right lens placement
- bridge placement

Indirect support cue:
- eye axis
- brow-to-rim spacing
- nose bridge alignment

Reference-only anatomy:
- optical function explanation only
- fashion / personality description only

Tool / use cases:
- accessory protection
- glasses-aware eye detection
- glasses-aware face retouch routing

### 11.2 Frame outer contour

What is visible:
- hard visible rim around the lens area in framed glasses
- left and right frame masses

Direct detection target:
- frame outer contour
- left frame body
- right frame body

Indirect support cue:
- strong contrast against skin
- specular edge on metal or plastic

Reference-only anatomy:
- material category as conceptual support only

Tool / use cases:
- accessory mask
- frame cleanup
- frame preservation during skin work

### 11.3 Lens boundary

What is visible:
- transparent lens edge
- oval / round / rectangular boundary depending on frame type

Direct detection target:
- left lens boundary
- right lens boundary

Indirect support cue:
- weak edge continuity
- glare interruption
- eye distortion through lens

Reference-only anatomy:
- optical correction explanation only

Tool / use cases:
- lens-aware eye protection
- glare routing
- lens-only mask support

### 11.4 Bridge

What is visible:
- central connector between left and right lens regions
- rigid cross-piece over the upper nose

Direct detection target:
- bridge segment
- bridge center point

Indirect support cue:
- nose bridge axis
- left/right lens symmetry

Reference-only anatomy:
- none

Tool / use cases:
- glasses alignment
- center-axis support
- bridge shadow interpretation

### 11.5 Nose pad

What is visible:
- small contact structure on the nose side for some glasses
- local contrast break on the nasal side plane

Direct detection target:
- left nose pad
- right nose pad

Indirect support cue:
- tiny cast shadow
- local skin interruption

Reference-only anatomy:
- none

Tool / use cases:
- glasses support-point detection
- nose/glasses separation

### 11.6 Hinge and temple start

What is visible:
- side joint near the outer lens edge
- start of the glasses arm toward the ear

Direct detection target:
- hinge point
- temple start direction

Indirect support cue:
- frame continuation
- side-head accessory flow

Reference-only anatomy:
- none

Tool / use cases:
- accessory completeness
- side-frame preservation

### 11.7 Lens glare patch

What is visible:
- bright white or colored reflection on the lens surface
- soft or hard highlight patch that may partially hide the eye

Direct detection target:
- glare patch region

Indirect support cue:
- lens location confirmation
- surface angle clue

Reference-only anatomy:
- none

Tool / use cases:
- glare removal
- eye detector blocking mask
- highlight preservation logic

### 11.8 Lens occlusion and eye distortion zone

What is visible:
- eye detail partially reduced, blurred, shifted, or optically distorted behind the lens

Direct detection target:
- occlusion zone
- distortion-affected eye region

Indirect support cue:
- frame boundary
- glare patch
- eye-size mismatch through lens

Reference-only anatomy:
- optical correction theory only

Tool / use cases:
- glasses-aware iris / pupil routing
- avoid false eye-edge detection
- restoration planning

### 11.9 Frame cast shadow

What is visible:
- soft or narrow shadow from the frame, bridge, or temple on skin

Direct detection target:
- frame shadow band

Indirect support cue:
- light direction
- frame-to-skin contact relation

Reference-only anatomy:
- none

Tool / use cases:
- distinguish shadow from wrinkle
- preserve natural frame depth

### 11.10 Material interpretation

What is visible:
- metal frames tend to read as thin, sharp, high-contrast lines
- plastic / horn-rim frames tend to read as thicker, softer, darker masses
- rimless glasses rely more on lens edge, nose pad, and hinge evidence

Direct detection target:
- none as a single hard contour

Indirect support cue:
- highlight shape
- edge thickness
- local reflectance behavior

Reference-only anatomy:
- none

Tool / use cases:
- frame-type interpretation
- detector tuning hints

## 12. Skin, Shadow, And Boundary Support Layer

### 12.1 Visible skin field

What is visible:
- continuous skin-colored region across face and neck

Direct detection target:
- average-skin candidate region

Indirect support cue:
- Y / Cr / Cb clustering
- local luminance continuity

Reference-only anatomy:
- none

Tool / use cases:
- skin masks
- safe retouch masks
- detector support

### 12.2 Shadow bands

What is visible:
- compressed dark structure at folds, edges, or recessed areas

Direct detection target:
- fold-shadow candidate
- under-structure contrast band

Indirect support cue:
- local gradient direction

Reference-only anatomy:
- none

Tool / use cases:
- wrinkle routing
- contour routing
- local volume shaping

### 12.3 Specular highlights

What is visible:
- sharp bright reflection on skin, lips, or eye surface

Direct detection target:
- usually not a primary contour target

Indirect support cue:
- surface curvature
- material / moisture clue

Reference-only anatomy:
- none

Tool / use cases:
- highlight preservation
- gloss-aware masking

## 13. Engine Interpretation Rule

Short version:

- detect visible surface first
- use support cues second
- keep internal anatomy as naming reference only

Meaning:
- what the photo clearly shows may become a direct detector target
- what the photo only suggests should remain a support cue
- what the photo does not visibly expose should not be forced into a hard detector rule

## 14. Current Practical Direction

This master document should support:
- shared face vocabulary
- future detector contracts
- future mask contracts
- tool routing
- local proxy work-area design

This is the approved visible-face-structure direction for KRetouchPro at the current stage.
