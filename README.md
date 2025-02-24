# Skill Test

## Requisiti e Completati (Tutti i requisiti richiesti sono stati completati)

- Un avatar che si muove grazie agli input dell'utente.
- Un suono di background.
- Un suono spazializzato legato ad un oggetto 3D in scena.
- Un menù di pausa.
- Un elemento di Interattività.
- Cambio colore dell'editor in fase di Play.
- Script custom di movimento dell'avatar:
  - L'avatar si deve muovere tramite i tasti WASD.
  - Salto tramite spacebar.
- La camera deve seguire l'avatar in modo adeguato.
- Una traccia audio di background in loop.
- Prevedere un tasto di apertura/chiusura di una UI.
- Il menu di UI deve contenere almeno:
  - Un button che attiva/disattiva l'audio in scena.
  - Un'immagine che rappresenta lo stato dell'audio (attivato/disattivato).
  - Una lista di tracce che cambia l'audio di background.
  - Uno slider che influenza il volume generale dell'audio.
- L'avatar deve cambiare animazione in base al tipo di stato in cui si trova (Idle, Camminata ed eventuale salto).
- Un oggetto 3D che emette una traccia audio spazializzata. L'audio deve fare play/pause al click sull'oggetto.
- Bake delle luci:
  - Limite massimo di risoluzione 1024px per un massimo di una lightmap.
  - Lightmap Non direzionali.
  - Gli oggetti dinamici dovrebbero essere illuminati da Light Probe.
  - In caso di baking, sostituire lo skybox di default con uno a piacimento.
- Usare il New Input System per muovere l'avatar.
- Usare le Cinemachine per la gestione della camera.
  - Creare una camera orbitale che si muove al drag dell'utente.
  - La camera deve rimanere sempre tra un minimo ed un massimo sull'asse Y.
  - Il range dovrebbe essere facilmente editabile da Editor.
- Movimento dell'avatar anche tramite punta e clicca.
- Creare un variant dell'avatar che abbia un Navmesh Agent.
  - L'Agent deve muoversi randomicamente per la scena.
  - Scansare degli obstacles.
  - Fermarsi per X secondi prima di andare al punto successivo.
  - Collegare l'animator dell'Agent in base allo stato in cui si trova (Idle/Camminata).

### Feature aggiuntive

- Sistema di interazione generale: il player può interagire premendo il tasto E quando si trova nel range.
- Sistema di focus: quando il player interagisce con un NPC, la camera guarda l'NPC; se l'interazione termina, la camera torna a guardare il player.
- Sistema di riproduzione audio casuale: alla chiamata della funzione viene istanziato un gameobject con un suono randomico scelto da una lista, con volume e pitch randomici entro un certo range.
- Implementazione del sistema di occlusion culling integrato di Unity.
- Sistema di IK per gli avatar: gli avatar ruotano la testa in direzione di altri avatar se sono nel loro range visivo.
- Stesso codice di movimento per NPC e player, con alcune differenze:
  - Il player ha un codice che gestisce gli input e li manda al codice di movimento.
  - Gli NPC hanno un codice che gestisce la loro logica di movimento (muoversi nella scena, destinazioni, pause).
- Sistema di dialogo per gli NPC:
  - I dialoghi possono essere configurati nell'inspector.
  - Ogni dialogo può attivare un evento.
  - I dialoghi vengono mostrati in una UI dedicata.
- Possibilità di vedere gli avatar dietro i muri.

### Assets utilizzati

- [Grid Prototype Materials](https://assetstore.unity.com/packages/2d/textures-materials/grid-prototype-materials-214264)
- [VFolders](https://assetstore.unity.com/packages/tools/utilities/vfolders-249825)
- [VHierarchy](https://assetstore.unity.com/packages/tools/utilities/vhierarchy-249759)
- [KayKit Adventurers Character Pack](https://assetstore.unity.com/packages/3d/characters/humanoids/humans/kaykit-adventurers-character-pack-for-unity-290679)
- [Low Poly Fantasy Medieval Village](https://assetstore.unity.com/packages/3d/environments/fantasy/low-poly-fantasy-medieval-village-163701)
- [Game Input Controller Icons (Free)](https://assetstore.unity.com/packages/2d/gui/icons/game-input-controller-icons-free-285953)
- [Fancy Mobile GUI - UI Pack](https://assetstore.unity.com/packages/2d/gui/icons/fancy-mobile-gui-ui-pack-for-beginners-238024)

### Build
  
- [Build online](https://michelecolella.github.io/Little-Paladins-WebGL-Build)

