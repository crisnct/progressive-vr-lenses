# Progressive VR Lenses – Arhitectură și Plan de Implementare

Acest document descrie arhitectura tehnică propusă pentru aplicația VR care simulează lentilele progresive conform specificațiilor furnizate. Soluția este organizată în module principale care pot fi dezvoltate incremental în Unity folosind OpenXR și compute shaders HLSL.

## 1. Prezentare generală

Aplicația oferă o experiență VR în care utilizatorul configurează o pereche de lentile progresive (PAL) și observă impactul asupra vederii într-un mediu natural. Sistemul se compune din trei fluxuri majore:

1. **Configurarea parametrilor optici** – interfață UI pentru introducerea rețetei optice, geometriei ramei și parametrilor headset-ului.
2. **Generarea hărților PAL** – transformarea parametrilor într-o hartă 2D care descrie variația locală de putere, astigmatism și magnificație.
3. **Simularea vizuală în VR** – post-procesare pe GPU ce aplică defocus spațial variabil, astigmatism și efectul "swim" peste imaginea scenei naturale.

## 2. Module software

### 2.1 PAL Designer

- **Input**: SPH, CYL, AXIS (OD/OS), ADD, coridor (lungime + profil), fitting height, PD/mono-PD, inset near, pantoscopic tilt, wrap angle, vertex distance, tip de design, parametrii ramei, IPD headset, FOV, rezoluție, distorsiune HMD, eye-tracking (opțional).
- **Output**: Texturi float32 per ochi ce codifică `Sph`, `Cyl`, `Axis`, `Magnification` și meta-date (ScriptableObject `PalLensProfile`).
- **Proces**:
  1. Normalizare parametri (convertire mm → metri, grade).
  2. Generare curba de putere pe coridor (distance → near) folosind design `hard/soft/office`.
  3. Modelare astigmatism periferic (lobii laterali) și blending.
  4. Maparea pe grilă 2D conform fitting height și PD.
  5. Calcul vector de magnificație pentru warp.

### 2.2 Optics Runtime

- Banca PSF pre-calculată (12–20 kernel-uri 2D, combinații Sph/Cyl).
- Compute shader pentru blur spațial variabil + aplicare warp.
- Integrare cu Scriptable Render Pass (URP) pentru aplicarea post-procesării după randarea scenei.
- Gaze-contingent update dacă este disponibil eye-tracking.

### 2.3 Nature Scene & Interaction

- Scenă Unity cu teren, vegetație, markere near/intermediate/distance.
- XR Interaction Toolkit pentru UI world-space și manipularea obiectelor de test.
- Modul Calibration pentru determinarea IPD/FOV/pixeli pe grad.

## 3. Flux de date

1. Utilizatorul introduce parametrii în UI → `ProfileService` salvează profilul.
2. `PalMapGenerator` procesează profilul și produce texturi PAL per ochi.
3. Texturile sunt trimise compute shader-ului `PalBlur.compute` prin `PalSimulationController`.
4. Shader-ul aplică defocus variabil și warp peste color buffer-ul scenei.
5. Rezultatul este afișat în headset prin OpenXR cu suport single-pass instanced.

## 4. Interfață utilizator

- Panou principal cu secțiuni pentru Rețetă, Design, Montură, Headset.
- Slider-e/picker-e pentru parametri numeric (sph/cyl/axis, ADD, coridor, tilt etc.).
- Preset-uri (office/outdoor/sport) ce setează default-urile coridorului și profilului de putere.
- Funcții Save/Load profil (JSON) în `Assets/Resources/Profiles` sau folder persistent.
- Overlays QA: hărți iso-power, indicator zone near/intermediate/distance.

## 5. Pipeline grafic

1. **Preprocesare**: generarea texturilor PAL + selectarea PSF-urilor relevante.
2. **Render natural scene**: URP forward rendering.
3. **PAL Blur Pass (compute)**:
   - Împarte color buffer-ul în tile-uri 32×32 px.
   - Pentru fiecare tile, citește valorile PAL și distanța planului scenă.
   - Interpolează PSF-ul (defocus + astigmatism) și aplică convoluția.
   - Calculează factorul de magnificație și aplică warp subtil (vector field).
4. **Composite**: combină rezultatul cu overlay debug dacă este activ.

## 6. Date și calibrări

- `psf_bank.asset`: ScriptableObject ce conține kernel-urile precompute.
- `PalLensProfile`: ScriptableObject cu parametri + texturi generate.
- Calibrare px/deg: `CalibrationService` execută un flow ghidat în VR.
- Mapping pupilă-lentilă: se folosește fitting height, PD, vertex distance pentru plasarea centrului optic.

## 7. Roadmap MVP

1. **Milestone 1** (2–3 săptămâni hobby):
   - Scenă natură simplă + randare VR
   - UI profile cu parametrii principali (Rx, ADD, coridor, fitting height, PD)
   - Generare hartă PAL sintetică (fără eye-tracking)
   - Compute pass cu blur spațial variabil (Gaussian aproximativ)
2. **Milestone 2**:
   - Warp/magnificație și efect "swim"
   - Preset-uri de design și salvare profil JSON
   - Wizard calibrare IPD/pixeli pe grad
3. **Milestone 3**:
   - Eye-tracking, foveated rendering
   - Optimizări performanță (temporal reprojection, downsampling periferic)
   - Validare și QA overlays, export rapoarte

## 8. Extensii viitoare

- Aberații cromatice minore, shading volumetric pentru natură, integrare cu Varjo/Vision Pro, plugin telemetry.
- Comparare cu simulatoare comerciale pentru calibrare subiectivă.

