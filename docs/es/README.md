# 🤖 Cuna de Hierro

## Un Mod de RimWorld BioTech

![RimWorld](https://img.shields.io/badge/RimWorld-1.6-8B4513?style=flat-square)
![BioTech DLC](https://img.shields.io/badge/BioTech_DLC-Requerido-6A0DAD?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-9.0-239120?style=flat-square&logo=csharp)
![Licencia](https://img.shields.io/badge/Licencia-MIT-blue?style=flat-square)

**Estación de acoplamiento de reparación automática para mecanoides de BioTech.**
Cuando están dañados, tus mecanoides buscarán la estación de forma autónoma y se repararán solos — sin necesidad de microgestión.

> 📖 **English documentation** available at [`../../README.md`](../../README.md)
> 📖 **Documentação em português** disponível em [`../pt/README.md`](../pt/README.md)

---

## ✨ Características

- **Reparación autónoma** — Los mecanoides detectan cuándo su salud cae por debajo de un umbral configurable y navegan a la estación disponible más cercana sin intervención del jugador
- **Consumo de recursos** — La reparación consume acero gestionado por `CompRefuelable`; los colonos reabastecen automáticamente cuando las reservas bajan del 25%
- **Requiere energía** — Necesita una conexión eléctrica activa; la estación se apaga limpiamente cuando se pierde el suministro
- **Expulsión manual** — Un botón de gizmo permite al jugador retirar forzosamente un mecanoide a mitad de reparación
- **Prioridad de estación** — Cada estación tiene una prioridad configurable (1–9); los mecanoides prefieren la estación con el número de prioridad más bajo
- **Totalmente configurable** — Todos los parámetros (umbral de salud, velocidad de reparación, coste de acero, rango de detección) son editables en el XML sin necesidad de recompilar
- **Bloqueado por investigación** — Desbloqueado por *Sistemas de Reparación de Mecanoides* (nivel Espacial, 1200 pts), requiere *Conceptos Básicos de Mecanoides* primero
- **Se puede averiar** — Requiere mantenimiento periódico, coherente con los edificios industriales del juego base
- **Seguro para guardar/cargar** — Todo el estado (ocupante, prioridad de estación, umbral de reparación) se serializa con `Scribe_References` y `Scribe_Values`
- **Sin pérdida de partes del cuerpo** — Solo repara lesiones activas; el daño permanente no se restaura (por diseño)
- **Sin dependencia de Harmony** — Toda la integración de IA se realiza mediante parches XML al ThinkTree, manteniendo el riesgo de compatibilidad bajo

---

## 🏗️ Descripción de la Arquitectura

El mod se construye alrededor de cuatro sistemas interconectados:

```text
┌──────────────────────────────────────────────────────────────────┐
│                    CAPA DE IA (ThinkTree)                        │
│                                                                  │
│  ThinkNode_ConditionalNeedsRepair                                │
│    └─ Comprueba: ¿es mecanoide? ¿del jugador? ¿salud<umbral?     │
│       └─ JobGiver_GoToIronCradle                                 │
│            └─ Emite: job IC_GoToIronCradle                       │
└──────────────────────────────┬───────────────────────────────────┘
                               │
                               ▼
┌──────────────────────────────────────────────────────────────────┐
│                       CAPA DE JOBS                               │
│                                                                  │
│  JobDriver_GoToIronCradle                                        │
│    1. GotoThing → caminar hasta InteractionCell                  │
│    2. dock (Instant) → TryAcceptOccupant → encolar job reparo    │
│                                                                  │
│  JobDriver_RepairAtIronCradle                                    │
│    - Espera (ToilCompleteMode.Never)                             │
│    - Termina cuando CurrentOccupant pasa a null                  │
└──────────────────────────────┬───────────────────────────────────┘
                               │
                               ▼
┌──────────────────────────────────────────────────────────────────┐
│                    CAPA DEL EDIFICIO                             │
│                                                                  │
│  Building_IronCradle                                             │
│    - Gestiona ocupante (TryAcceptOccupant / EjectOccupant)       │
│    - Tick: TryConsumeSteel cada repairTickInterval               │
│    - Acero gestionado por CompRefuelable (cap. 50 uds)           │
│    - Gizmos, InspectString, guardar/cargar                       │
│                                                                  │
│  CompIronCradle (ThingComp)                                      │
│    - CompTick: ApplyRepairTick cada repairTickInterval           │
│    - Cura todas las instancias Hediff_Injury activas             │
│    - Llama a OnRepairComplete cuando salud ≥ 99%                 │
│    - Almacena repairThreshold por instancia (ajustable)          │
└──────────────────────────────┬───────────────────────────────────┘
                               │
                               ▼
┌──────────────────────────────────────────────────────────────────┐
│                    CAPA DE REGISTRO                              │
│                                                                  │
│  IronCradleTracker (MapComponent)                                │
│    - Registro/baja en O(1) en SpawnSetup / DeSpawn               │
│    - Los ThinkNodes iteran esta lista en lugar de buscar el mapa │
│    - Declarado en MapComponentDefs.xml; instanciado por RW       │
└──────────────────────────────────────────────────────────────────┘
```

---

## 📁 Estructura de Carpetas

```text
IronCradle/
│
├── About/
│   └── About.xml                        ← Metadatos del mod, packageId (RexThar.IronCradle), dependencia BioTech
│
├── Assemblies/
│   └── IronCradle.dll                   ← Salida compilada (no editar manualmente)
│
├── 1.6/
│   ├── Defs/
│   │   ├── JobDefs/
│   │   │   └── JobDefs_IronCradle.xml       ← IC_GoToIronCradle + IC_RepairAtIronCradle
│   │   ├── MapComponentDefs/
│   │   │   └── MapComponentDefs.xml         ← Registra IronCradleTracker en RimWorld
│   │   ├── ResearchProjectDefs/
│   │   │   └── ResearchDefs.xml             ← IC_IronCradleResearch (Espacial, 1200 pts)
│   │   └── ThingDefs/
│   │       └── Buildings_IronCradle.xml     ← ThingDef IC_IronCradle: tamaño, coste, comps, investigación
│   │
│   ├── Languages/
│   │   ├── English/Keyed/
│   │   │   └── IronCradle.xml               ← Cadenas visibles para el jugador (locale base)
│   │   ├── Spanish (Español)/Keyed/
│   │   │   └── IronCradle.xml               ← Traducción al español (España)
│   │   ├── SpanishLatin (Español(Latinoamérica))/Keyed/
│   │   │   └── IronCradle.xml               ← Traducción al español (Latinoamérica)
│   │   └── Portuguese (Português)/Keyed/
│   │       └── IronCradle.xml               ← Traducción al portugués
│   │
│   └── Patches/
│       └── MechanoidThinkTree.xml           ← Inyecta nodo de reparación en MechanoidPlayerControlled
│
├── Source/
│   ├── IC_Mod.cs                            ← Bootstrap StaticConstructorOnStartup + verificación de textura
│   ├── IC_JobDefOf.cs                       ← Referencias estáticas de jobs [DefOf]
│   ├── Building_IronCradle.cs               ← Edificio principal: ocupante, acero por CompRefuelable, UI
│   ├── CompProperties_IronCradle.cs         ← CompProperties_IronCradle + CompIronCradle (tick de curación)
│   ├── JobDriver_GoToIronCradle.cs          ← Driver del job caminar-hacia-estación
│   ├── JobDriver_RepairAtIronCradle.cs      ← Driver del job reparación acoplada
│   ├── ThinkNode_ConditionalNeedsRepair.cs  ← Condicional IA + JobGiver_GoToIronCradle + RepairStationUtility
│   └── IronCradleTracker.cs                 ← Registro de estaciones como MapComponent
│
├── Textures/
│   └── Things/
│       └── Buildings/
│           └── IronCradle.png               ← Sprite del edificio 128×128 (debe añadirse)
│
├── docs/
│   ├── es/
│   │   └── README.md                        ← Este archivo — documentación en español
│   └── pt/
│       └── README.md                        ← Documentação completa em português
│
└── .vscode/
    ├── mod.csproj                           ← Archivo de proyecto (net480, RootNamespace y AssemblyName: IronCradle)
    ├── tasks.json                           ← Tareas de compilación (Windows + Linux)
    ├── launch.json                          ← Configuraciones de lanzamiento y depurador
    └── extensions.json                      ← Extensiones recomendadas para VS Code
```

---

## ⚙️ Referencia de Configuración

Todos los parámetros son ajustables directamente en `1.6/Defs/ThingDefs/Buildings_IronCradle.xml` dentro del bloque `<li Class="IronCradle.CompProperties_IronCradle">` — sin necesidad de recompilar.

| Propiedad | Por defecto | Descripción |
| --- | --- | --- |
| `repairHealthThreshold` | `0.5` | Fracción de salud inicial (0–1) por debajo de la cual el mecanoide busca reparación. Actúa como valor inicial por estación; el jugador puede sobreescribirlo en juego mediante el gizmo. |
| `repairSpeedPerTick` | `0.0005` | HP restaurados por tick de juego en cada lesión activa. |
| `steelPerRepairCycle` | `1` | Unidades de acero consumidas por intervalo de reparación. Con los valores por defecto, ~7,2 unidades/hora. |
| `repairTickInterval` | `500` | Ticks entre cada ciclo de consumo de acero y curación (~8,3 s a velocidad ×1). Controla tanto la granularidad de recursos como el coste de CPU. |
| `maxRepairRange` | `30` | Distancia máxima en celdas para que un mecanoide detecte esta estación y se desplace a ella. |

> **Consejo de ajuste:** `repairSpeedPerTick` y `repairTickInterval` están acoplados. Los HP efectivos curados por segundo son `repairSpeedPerTick × 60`.

---

## 🔬 Cómo Funciona la Reparación (Paso a Paso)

1. En cada tick de IA, `ThinkNode_ConditionalNeedsRepair.Satisfied()` evalúa cada mecanoide del jugador en este orden (las comprobaciones más baratas primero):
   - ¿Es un mecanoide? ¿Es del jugador?
   - ¿Está ya ejecutando un job de reparación (`IC_RepairAtIronCradle` o `IC_GoToIronCradle`)?
   - ¿Su salud ya está al 99% o más? *(pre-filtro barato para saltar mecanoides sanos)*
   - ¿Hay una estación alimentada, libre y alcanzable dentro de `maxRepairRange`? *(la más costosa — se ejecuta al final)*
   - ¿La salud está por debajo del `repairThreshold` configurado en esa estación específica?

2. Si todas las condiciones se cumplen, `JobGiver_GoToIronCradle` emite un job `IC_GoToIronCradle` apuntando a la estación válida más adecuada (menor `StationPriority`, luego más cercana), verificando antes que ningún otro pawn de la misma facción la tenga ya reservada.

3. `JobDriver_GoToIronCradle` lleva al mecanoide hasta la `InteractionCell` de la estación, después llama a `Building_IronCradle.TryAcceptOccupant()` y encola `IC_RepairAtIronCradle`. Si una condición de carrera rellena la estación entre el desplazamiento y el acoplamiento, el job termina como `Incompletable` y el mecanoide lo reintentará.

4. Cada `repairTickInterval` ticks mientras está acoplado:
   - **Tick del edificio:** `TryConsumeSteel()` descuenta de `CompRefuelable`. Si no hay suficiente acero, el mecanoide es expulsado y se notifica al jugador. Los colonos reabastecerán automáticamente cuando las reservas bajen del 25%.
   - **Tick del comp:** `ApplyRepairTick()` llama a `injury.Heal(repairSpeedPerTick)` sobre cada `Hediff_Injury` activa (no permanente). La curación se omite si el acero se agotó durante el tick del edificio.

5. Cuando `SummaryHealthPercent ≥ 0.99`, se activa `OnRepairComplete()`: el jugador recibe una carta positiva, se genera un efecto visual de luz sobre el mecanoide, `CurrentOccupant` se pone a `null`, y el `tickAction` de `JobDriver_RepairAtIronCradle` detecta el cambio y termina el job limpiamente.

6. Al cargar, `PostMapInit()` valida que el ocupante serializado sigue teniendo un job de reparación activo o en cola. Si no es así (p. ej. guardado editado externamente), la referencia al ocupante se limpia y se registra una advertencia, evitando que la estación quede bloqueada permanentemente.

---

## 🧱 Estadísticas del Edificio

| Estadística | Valor |
| --- | --- |
| DefName | `IC_IronCradle` |
| Tamaño | 2×2 celdas |
| HP máximos | 300 |
| Trabajo para construir | 4.000 ticks |
| Inflamabilidad | 50% |
| Belleza | −2 |
| Consumo eléctrico | 250W |
| Coste | 150 Acero + 4 Componentes Industriales + 1 Componente Espacial |
| Investigación | `IC_IronCradleResearch` — Sistemas de Reparación de Mecanoides (Espacial, 1200 pts) |
| Investigación previa requerida | Conceptos Básicos de Mecanoides (DLC BioTech) |
| Capacidad de acero (CompRefuelable) | 50 unidades |
| Umbral de reabastecimiento automático | 25% (12 unidades) |

---

## 🔧 Compilación

### Requisitos Previos

- .NET SDK para `net480` (Visual Studio 2022 / JetBrains Rider o CLI de `dotnet`)
- RimWorld 1.6 instalado vía Steam
- Paquete NuGet `Krafs.Rimworld.Ref` (se resuelve automáticamente al compilar)

### Visual Studio / Rider

1. Abre `.vscode/mod.csproj`.
2. Compilar → Release. El DLL se genera automáticamente en `1.6/Assemblies/` como `IronCradle.dll`.

### Línea de Comandos

```bash
cd .vscode
dotnet build -c Release
```

### VS Code

Usa la tarea **Build & Run** (`Ctrl+Shift+B`) definida en `.vscode/tasks.json`. Una configuración de lanzamiento separada **Attach Debugger** conecta Mono en el puerto `56000` para depuración en vivo.

### Rutas Predeterminadas de RimWorld

| SO | Ruta |
| --- | --- |
| Windows | `C:\Program Files (x86)\Steam\steamapps\common\RimWorld` |
| Linux | `~/.steam/steam/steamapps/common/RimWorld` |
| macOS | `~/Library/Application Support/Steam/steamapps/common/RimWorld` |

---

## 🖼️ Añadir la Textura

Coloca un PNG de **128 × 128 px** en:

```text
Textures/Things/Buildings/IronCradle.png
```

**Guía de estilo:** Sigue la estética de BioTech — paneles de acero oscuro con iluminación de acento en azul/verde azulado. El edificio ocupa 2×2 celdas; mantén el sprite visualmente centrado con un motivo sutil de brazo o cuna de acoplamiento. El ThingDef usa `Graphic_Single`, así que la textura no rota — diseña para una perspectiva cenital orientada al sur. El bootstrap en `IC_Mod.cs` registrará una advertencia si falta este archivo.

---

## 📦 Instalación

1. Copia la carpeta `IronCradle/` al directorio de mods de RimWorld:

    | SO | Directorio de Mods |
    | --- | --- |
    | Windows | `%APPDATA%\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Mods\` |
    | Linux | `~/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Mods/` |
    | macOS | `~/Library/Application Support/RimWorld/Mods/` |

2. Activa el mod en el juego. Asegúrate de que el **DLC BioTech** está activo.

3. Investiga *Conceptos Básicos de Mecanoides* y luego *Sistemas de Reparación de Mecanoides* para desbloquear el edificio.

---

## 🌍 Localización

Todas las cadenas visibles para el jugador se encuentran en `1.6/Languages/<Idioma>/Keyed/IronCradle.xml`. Todas las claves usan el prefijo `IC_` para evitar colisiones con otros mods. Las claves que contienen `{0}` son cadenas de formato que se rellenan en tiempo de ejecución con valores como la etiqueta corta del mecanoide.

| Idioma | Ruta |
| --- | --- |
| Inglés | `1.6/Languages/English/Keyed/` |
| Español (España) | `1.6/Languages/Spanish (Español)/Keyed/` |
| Español (Latinoamérica) | `1.6/Languages/SpanishLatin (Español(Latinoamérica))/Keyed/` |
| Portugués | `1.6/Languages/Portuguese (Português)/Keyed/` |

Para añadir una nueva traducción:

1. Crea `1.6/Languages/<NombreIdioma>/Keyed/IronCradle.xml`
2. Copia el archivo en inglés y reemplaza los valores (mantén las claves idénticas)
3. RimWorld usa el inglés automáticamente como respaldo para cualquier clave faltante

---

## ⚠️ Limitaciones Conocidas y Decisiones de Diseño

- **Sin regeneración de partes del cuerpo perdidas** — Las lesiones de tipo `HediffComp_GetsPermanent` se omiten intencionalmente en `ApplyRepairTick()`. La regeneración de miembros está fuera del alcance de este nivel de edificio.
- **Un solo ocupante por estación** — La estación admite exactamente un mecanoide a la vez. Coloca múltiples estaciones para escuadrones grandes.
- **Acero gestionado por CompRefuelable** — El gizmo de acero (capacidad y nivel actual) lo muestra automáticamente el comp. Los colonos reabastecen la estación igual que cualquier edificio recargable; no se necesita configuración manual.
- **El parche del ThinkTree apunta a `MechanoidPlayerControlled`** — Este es el árbol que usan los mecanoides propios del jugador en BioTech. Si Ludeon lo reestructura en una actualización futura, el XPath en `1.6/Patches/MechanoidThinkTree.xml` puede necesitar actualización. Síntoma: los mecanoides nunca buscan la estación de forma autónoma.
- **El parche de `MechanoidConstant` está comentado** — Un parche secundario para ese árbol está presente pero desactivado por defecto. Descoméntalo en `MechanoidThinkTree.xml` solo si necesitas compatibilidad con mods de terceros que usen ese árbol para mecanoides aliados.
- **Sin parches de Harmony** — Toda la integración de IA se realiza mediante parches XML al ThinkTree, manteniendo el riesgo de compatibilidad bajo.
- **El nombre del ensamblado coincide con el namespace** — Tanto `RootNamespace` como `AssemblyName` en `.vscode/mod.csproj` están definidos como `IronCradle`, igual que el namespace de C# en todo el código fuente.

---

## 📜 Licencia

MIT — libre de usar, modificar y distribuir con atribución.

---

Construido para RimWorld 1.6 · Requiere el DLC BioTech · Autor: RexThar
