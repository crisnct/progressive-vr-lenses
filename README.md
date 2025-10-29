# Progressive VR Lenses

Acest proiect conține planul tehnic și componentele de cod de bază pentru un simulator VR de lentile progresive construit în Unity folosind OpenXR. Documentația și codul skeleton descriu cum poate fi implementată o aplicație care să respecte cerințele utilizatorului privind configurarea parametrilor optici și redarea realistă a lentilelor progresive într-un mediu natural.

## Structură

- `docs/architecture.md` – descriere detaliată a arhitecturii, pipeline-ului optic și roadmap-ului MVP.
- `Assets/Scripts/Optics` – componente C# pentru modelarea lentilelor progresive și generarea hărților PAL.
- `Assets/Scripts/Runtime` – managerul runtime al simulării și integrarea cu pipeline-ul de randare.
- `Assets/Shaders` – compute shader HLSL pentru estomparea spațial-variabilă și simularea efectului "swim".
- `Assets/Resources` – date predefinite (ex. profiluri, bănci PSF) ce pot fi încărcate la runtime.

## Cerințe de dezvoltare

Proiectul este gândit pentru Unity 2022/2023 LTS cu URP și runtime OpenXR. Pentru a continua dezvoltarea, creează un proiect Unity și copiază directoarele `Assets/` și `docs/` în proiect. Urmează instrucțiunile din documentație pentru configurarea scenelor, UI-ului și shaderelor.
