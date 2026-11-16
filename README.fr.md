[English](README.md) · [中文](README.zh.md) · [हिन्दी](README.hi.md) · [Español](README.es.md) · [العربية](README.ar.md) · **Français** · [বাংলা](README.bn.md) · [Português](README.pt.md) · [Русский](README.ru.md) · [Bahasa Indonesia](README.id.md)

# IPTzTray

**Fuseau horaire Windows et DNS selon l’IP publique, même avec un VPN actif.**

Nom : IP + Tz (time zone, fuseau horaire) + Tray (zone de notification Windows).

## En bref

IPTzTray règle le fuseau horaire de Windows d’après l’IP publique : l’adresse par laquelle le trafic sort réellement. Vous activez un VPN dans un autre pays : le fuseau horaire passe à celui du pays de sortie. Vous revenez à l’IP de la maison : le fuseau horaire redevient celui du domicile. En option, le programme ferme aussi la fuite DNS hors du tunnel, qui permet au fournisseur de voir les noms des sites quand le VPN est actif.

Seul le fuseau horaire change ; l’horloge elle-même (l’heure UTC) n’est pas déplacée. L’écran affiche une autre heure locale, et les sites, programmes et services affichent la même.

En option, et c’est désactivé, le programme met aussi le pays Windows selon le pays de l’IP et remet le vôtre quand ce réglage est coupé. La langue d’affichage et la région du compte Microsoft ne changent pas.

Le programme supprime ainsi deux signes visibles d’un VPN : une heure locale qui ne correspond pas à l’IP, et des requêtes DNS qui contournent le tunnel. Les autres signes, comme une IP de centre de données ou la langue du système, il n’y touche pas.

Le programme voit l’IP de tout le système. Un proxy réglé seulement dans le navigateur lui échappe.

Gratuit pour tout usage. Les conditions pour les forks sont dans « Licence et nom ».

## Pourquoi

- Les sites, programmes et services affichent l’heure de la maison alors que le trafic sort déjà d’un autre pays.
- Le VPN est coupé, mais le fuseau horaire est resté étranger.
- Windows règle le fuseau horaire d’après le réseau Wi‑Fi le plus proche, et non d’après l’IP par laquelle passe le trafic.
- Avec le VPN actif, Windows continue d’interroger le DNS du routeur. La requête contourne le tunnel et le fournisseur voit les noms des sites.

Le réglage du fuseau horaire traite les trois premiers points ; la protection contre la fuite DNS, le quatrième.

- Cela peut probablement aider face au blocage de comptes Claude (Anthropic) et d’autres éditeurs d’outils de développement d’agents IA.

## Démarrer

Windows 10 ou 11, 64 bits. Rien à installer : le programme est un seul fichier, [IpTzTray.exe](https://github.com/kubsite1/iptztray/releases/latest/download/IpTzTray.exe), de 250 Ko au plus. Toutes les versions sont sur la page [Releases](https://github.com/kubsite1/iptztray/releases).

1. Téléchargez et lancez `IpTzTray.exe`.
2. Autorisez les droits d’administrateur : sans eux, Windows ne laisse pas changer le fuseau horaire.
3. Une icône apparaît dans la zone de notification ; ensuite, le fuseau horaire s’ajuste tout seul.

Au premier lancement, le programme mémorise le fuseau horaire actuel comme fuseau du domicile et active son propre démarrage à l’ouverture de session. À chaque lancement avec les droits d’administrateur, il désactive le réglage automatique du fuseau horaire de Windows (le service `tzautoupdate`) ; sinon Windows règle le fuseau horaire d’après le Wi‑Fi le plus proche et sa valeur diverge de celle du programme.

Si vous refusez les droits, l’icône apparaît, mais le fuseau horaire ne change pas. Le menu garde « Exécuter en administrateur ».

À la fermeture du programme et à la fin de la session Windows (déconnexion ou arrêt), le fuseau horaire du domicile revient, même si le changement auto est en pause. Cela se désactive dans les paramètres.

## Icône

L’icône affiche le décalage du fuseau horaire actuel de Windows : `+3`, `-5`, `+5:30`.

<p>
<img src="screenshots/icon-match.png" width="56" alt="Vert">
<img src="screenshots/icon-mismatch.png" width="56" alt="Orange">
<img src="screenshots/icon-wait.png" width="56" alt="Gris">
<img src="screenshots/icon-error.png" width="56" alt="Rouge">
</p>

- **Vert** — le fuseau horaire de Windows correspond à celui de l’IP.
- **Orange** — ne correspond pas : le VPN est déjà actif mais le fuseau horaire est encore l’ancien, ou le changement auto est en pause.
- **Gris** — l’IP n’a pas encore été vérifiée.
- **Rouge** — impossible de connaître l’IP ou le fuseau horaire. Le fuseau horaire ne change pas.

Double-clic sur l’icône : vérifier maintenant. Une seconde copie du programme ne démarre pas : la première reste dans la zone de notification.

## Fuite DNS

La vérification est désactivée par défaut. Elle s’active dans les paramètres.

Le programme regarde si Windows interroge le DNS non seulement par le tunnel, mais aussi sur une autre carte réseau active, le plus souvent le routeur. Les cartes virtuelles, Bluetooth et TAP sont exclues. La vérification consiste en une requête d’un nom aléatoire `.invalid` vers ce DNS. S’il répond alors que la requête DNS parallèle est active dans Windows, les noms des sites peuvent sortir hors du VPN.

![Fenêtre de fuite DNS](screenshots/dns.png)

Quand une fuite est trouvée, le programme affiche cette fenêtre. La fermer ou choisir « Pas maintenant » ne change rien. « Corriger », tant que le VPN est actif :

- désactive la requête DNS parallèle de Windows ;
- mémorise le DNS de la carte latérale et met à sa place le DNS du tunnel ;
- bloque les requêtes sortantes vers l’ancien DNS et vers les DNS publics connus, y compris DNS-over-TLS et DNS-over-HTTPS ;
- envoie tous les noms au DNS du tunnel.

Sur le réseau de la maison, tout revient comme avant.

Modes :

- **Ne pas vérifier** — retire la protection déjà active et remet les réglages précédents.
- **Une fois au démarrage** — demande seulement s’il y a une fuite à ce moment-là. Un VPN activé plus tard n’est pas vérifié automatiquement.
- **Surveiller et corriger tout seul** — corrige sans demander à chaque activation du VPN et remet les réglages à la maison.

Pour cette fonction, le VPN est considéré comme actif quand la route principale passe par une carte tunnel, WireGuard, Amnezia ou TAP. Le changement de fuseau horaire n’en dépend pas : il ne regarde que l’IP publique. Si la carte du VPN ne ressemble à aucune de celles-ci, le fuseau horaire change quand même, mais le programme peut ne pas corriger le DNS tout seul.

Après la correction, le programme redémarre le service DNS. Si internet ne répond plus, redémarrez l’ordinateur. Parfois, la modification ne prend effet qu’après un redémarrage.

Un test de fuite externe peut encore montrer le DNS du VPN lui-même, par exemple Cloudflare dans le pays de sortie. C’est le DNS du tunnel, pas celui du routeur de la maison.

## Référence

### Menu

![Menu](screenshots/menu.png)

Trois lignes grises en haut : lieu, IP avec le nom IANA du fuseau horaire, fuseau horaire actuel de Windows. Si le fuseau horaire actuel ne correspond pas à celui de l’IP, la troisième ligne montre les deux. Une ligne trop longue est coupée ; le texte complet est dans l’info-bulle.

- **Vérifier maintenant** — une vérification supplémentaire de l’IP, tout de suite.
- **Pause du changement auto** — les vérifications continuent, le fuseau horaire ne change pas. « Reprendre le changement auto » lève la pause et ajuste aussitôt le fuseau horaire.
- **Revenir au fuseau domicile** — met le fuseau horaire du domicile mémorisé et active la pause ; sinon la vérification suivante de l’IP le changerait aussitôt.
- **Retirer l’épingle** — visible seulement si l’IP actuelle a une épingle (voir « Paramètres »).
- **Corriger la fuite DNS** — visible seulement si une fuite a déjà été trouvée.
- **Exécuter en administrateur** — visible seulement sans droits d’administrateur.
- **Copier l’IP** — copie l’IP publique dans le presse-papiers.
- **Paramètres**, **À propos**, **Quitter**.

### Paramètres

![Paramètres](screenshots/settings.png)

- **À la fermeture, revenir au fuseau domicile** — activé par défaut.
- **Mémoriser le fuseau actuel** — fait aussitôt du fuseau horaire actuel celui du domicile et mémorise le pays Windows actuel.
- **Changer le pays selon l’IP** — désactivé. Met le pays Windows selon le pays de l’IP publique. Les droits d’administrateur ne sont pas nécessaires.
- **À l’arrêt ou au changement, revenir à votre pays** — activé. Votre pays revient quand la case est décochée, en pause, avec « Revenir au fuseau domicile » et à la fermeture.
- **Démarrer à l’ouverture de session** — une tâche du Planificateur de tâches nommée `IpTzTray`, avec les droits d’administrateur, sans redemander les droits. Décocher la case supprime la tâche.
- **Combien attendre la réponse du service** — 5, 10 ou 20 secondes, 5 par défaut. C’est la limite de chaque étape : séparément pour la vérification de l’IP et pour la demande du fuseau horaire.
- **Fuite DNS** — les trois modes de la section « Fuite DNS ».
- **Langue** — par défaut, la langue de Windows. Si elle n’est pas dans la liste, l’anglais. « Enregistrer » applique la langue aussitôt, sans redémarrage.

Langues de l’interface : English, 中文, हिन्दी, Español, العربية, Français, বাংলা, Português, Русский, Bahasa Indonesia. Les noms des fuseaux horaires viennent de Windows, ils sont donc dans la langue du système.

En bas de la fenêtre : le chemin du dossier `%AppData%\IpTzTray` et le bouton « Ouvrir ». Le dossier contient `settings.json` et le journal `log.txt`. Le journal est écrit en russe. Quand le fichier grossit, la partie précédente est gardée dans `log.txt.old`.

**Épingle** — votre propre fuseau horaire pour une IP précise. Tant qu’elle existe, le programme met ce fuseau horaire pour cette IP et n’interroge pas le service de géolocalisation. Cette version n’a pas de fenêtre pour épingler. On peut l’écrire dans `settings.json`, programme fermé, dans le champ `Pins` :

```json
"Pins": { "203.0.113.10": "Eastern Standard Time" }
```

Le fuseau horaire s’indique par son nom Windows. `tzutil /l` affiche la liste des noms.

### Ligne de commande

- `IpTzTray.exe --check` — affiche l’IP et le fuseau horaire, ne change rien.
- `IpTzTray.exe --dns` — indique si une fuite DNS est visible, ne change rien.
- `IpTzTray.exe --dry` — zone de notification en mode aperçu : fuseau horaire et DNS inchangés, réglages écrits dans un dossier temporaire.
- `IpTzTray.exe --map Europe/Amsterdam` — affiche le fuseau horaire Windows pour un nom IANA.
- `IpTzTray.exe --selftest` — vérifie la table des fuseaux horaires, la lecture des réponses et l’enregistrement des réglages ; ne change pas le système.

## Fonctionnement

- **IP publique** : demandée en même temps à trois services : Cloudflare (`1.1.1.1`), ipinfo.io et ip-api.com. L’adresse donnée par au moins deux est retenue. La réponse d’un seul service n’est acceptée que si les autres se taisent. Si les réponses divergent, la vérification recommence. Les adresses privées sont écartées.
- **Fréquence** : tant que l’IP ne change pas, environ toutes les 10 secondes. Après un changement de réseau, première vérification au bout de 5 secondes, puis toutes les 5 secondes pendant environ 25 secondes, puis de nouveau toutes les 10 secondes.
- **Fuseau horaire** : demandé tour à tour à ip-api.com, ipwho.is, ipinfo.io et geojs.io, seulement quand l’IP a changé. Le nom IANA est converti en fuseau horaire Windows avec la table Unicode CLDR et appliqué avec `tzutil`.
- **Fuseau horaire du domicile** : le fuseau horaire de Windows au premier lancement, avant le premier réglage. Si un fuseau étranger était déjà en place, c’est lui qui est mémorisé. On le corrige avec « Mémoriser le fuseau actuel ».
- **Votre pays** : le pays Windows au premier lancement. Le même bouton le met aussi à jour.
- **Ce qui sort** : les services de détection de l’IP voient votre IP publique à chaque vérification. Le service de géolocalisation ne reçoit l’IP que quand l’adresse change.

## Limites

- Un proxy réglé seulement dans le navigateur n’est pas détecté : le programme voit l’IP de tout le système.
- Dans la base de géolocalisation, la ville du centre de données du VPN ne correspond parfois pas au bon fuseau horaire. Le programme met celui que le service a renvoyé.
- Sans droits d’administrateur, ni le fuseau horaire ni le DNS ne changent. Le pays Windows peut quand même changer.
- La région du compte Microsoft ne change pas. Une IP épinglée n’a pas de code pays, donc le programme ne change pas le pays pour elle.

## Compilation

C#, Windows Forms, le .NET Framework fourni avec Windows, x64. Seul le compilateur `csc` est nécessaire, il est fourni avec Windows. Visual Studio n’est pas nécessaire.

```
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Résultat : `publish\IpTzTray.exe` et l’icône `publish\app.ico`.

- `src` — code source.
- `data/windowsZones.xml` — table des fuseaux horaires Unicode CLDR.
- `build.ps1` — script de compilation.
- `app.manifest` — manifeste Windows.

Le programme a été écrit et compilé avec Grok 4.7.

## Licence et nom

Gratuit pour tout usage, y compris commercial.

Les forks et les versions modifiées ont deux conditions : ne pas retirer la mention de l’auteur et ne pas utiliser le nom IPTzTray. Texte complet dans [LICENSE](LICENSE).

Auteur : hello@kubsite.ru  
Page du programme : https://github.com/kubsite1/iptztray

Merci pour le soutien — l’agrégateur de messageries [wamm.chat](https://wamm.chat).
