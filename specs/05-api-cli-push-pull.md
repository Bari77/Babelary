---
id: FEAT-05
title: API et CLI push / pull
status: backlog
created: 2026-10-26
depends_on: [FEAT-02, FEAT-03]
---

# FEAT-05 — API et CLI

## Intention

Le propriétaire synchronise le dépôt sans ouvrir le site. Le CLI envoie les JSON source, récupère les JSON traduits dans les dossiers de langues, et peut déposer une traduction faite dans un fichier local. Les droits du compte s'appliquent tels quels.

## Configuration locale

Un fichier `babelary.yml` ou `babelary.json`, à la racine du module qui contient les traductions, décrit le projet. Si les deux sont dans le même dossier, le JSON l'emporte. Le jeton n'y figure pas.

```yaml
base_url: https://babelary.exemple
project: kert-front
source_locale: en-US
base_path: ./src/locale
files: "{locale}/{name}.json"
```

Le jeton est la variable d'environnement `BABELARY_TOKEN`. S'il manque, le CLI s'arrête avant tout appel réseau et le dit.

`project` est l'identifiant stable du projet, choisi à la création, distinct du nom affiché. Il ne change pas.

`files` est le chemin relatif de chaque fichier. `{locale}` est obligatoire, une seule fois. `{name}` est le nom sans extension, `{file}` le nom complet, `{source}` la langue source. Un segment `*` vaut `{file}`, un segment `*.json` vaut `{name}.json`. Le motif se termine par `.json`, ou contient `{file}`.

`layout: folder` est le raccourci de `{locale}/{name}.json`. `layout: locale` est le raccourci de `{locale}.json`. `files` l'emporte sur `layout`. Si ni l'un ni l'autre n'est écrit, le CLI reprend le motif enregistré sur le projet. Le même champ se règle dans les paramètres du site.

Le serveur garde le nom de fichier (`home.json`, `en-US.json`). Le motif dit seulement où le placer sur le disque et dans l'archive. `{locale}/{name}.json` exporte `fr-FR/home.json`. `{locale}.json` exporte `fr-FR.json` à partir de `en-US.json`. `{name}.{locale}.json` exporte `home.fr-FR.json`.

## Commandes

### `babelary push sources`

Lit les fichiers de `base_path` dont le chemin correspond au motif pour la langue source. Pour chaque fichier :

- crée le fichier côté serveur s'il n'existe pas ;
- crée les clés nouvelles avec une valeur vide dans chaque langue cible ;
- met à jour les valeurs source des clés déjà là ;
- supprime les clés de ce fichier qui ne sont plus dans le JSON local.

Un fichier du serveur qui n'est pas dans ce push n'est pas touché. Pour supprimer un fichier entier, la commande est `babelary push sources --delete-missing-files`. Sans ce drapeau, un fichier oublié reste en place.

La commande affiche le nombre de clés créées, modifiées et supprimées, par fichier. Elle refuse un JSON invalide ou une feuille non chaîne, et n'envoie alors rien de ce fichier.

### `babelary push translations`

Lit, pour chaque langue cible, les fichiers de `base_path` qui correspondent au motif. Pour chaque fichier et chaque clé déjà connue :

- enregistre la valeur locale si elle n'est pas vide ;
- laisse la valeur serveur intacte si la valeur locale est vide, pour ne pas effacer le travail d'un traducteur avec un fichier encore troué ;
- refuse la clé si les jetons ne correspondent pas, et continue les autres clés ;
- ignore une clé inconnue du serveur.

La langue source n'est pas lue par cette commande. Un membre ne met à jour que ses langues. Les autres dossiers sont ignorés avec un avertissement, pas une erreur globale.

`--overwrite-empty` permet à une valeur locale vide d'effacer la traduction serveur. Ce n'est pas le défaut.

### `babelary pull`

Écrit, pour chaque langue, le chemin donné par le motif sous `base_path`. Le JSON reconstruit les groupes à partir des chemins de clés. L'ordre des clés suit l'ordre du fichier source.

Les valeurs « à traduire » sont écrites comme des chaînes vides, afin que le fichier ait les mêmes clés que la source.

La langue source est aussi écrite, pour que le dépôt puisse récupérer une clé ajoutée depuis l'interface. Les fichiers locaux existants sont remplacés. Un dossier de langue inconnu du projet n'est pas effacé.

### `babelary status`

Interroge le serveur et affiche, par langue cible, le total, le nombre traduit, le nombre à revoir, et la liste des clés à traduire. Aucun fichier local n'est modifié. Le code de sortie vaut 1 s'il reste au moins une clé à traduire, 0 sinon.

## Règles communes

Le CLI parle en HTTPS à `base_url`. Un échec d'authentification s'arrête tout de suite avec un message qui ne contient pas le jeton.

Les commandes sont rejouables. Pousser deux fois le même dossier ne crée pas de doublon et ne change pas les statuts si le texte est le même.

Le nom de fichier stocké sert de jointure entre le disque et le serveur. Avec `{locale}/{name}.json`, `en-US/pet.json` met à jour `pet.json` du projet. Si `files` ou `layout` est écrit, `push sources` enregistre ce motif sur le projet.

## Critères d'acceptation

- [ ] AC-1 : `push sources` sur un projet vide crée les fichiers, les clés et les valeurs source.
- [ ] AC-2 : Une clé retirée du JSON local est retirée du serveur, traductions comprises, seulement pour les fichiers présents dans le push.
- [ ] AC-3 : Sans `--delete-missing-files`, un fichier serveur absent du disque reste en place.
- [ ] AC-4 : `push translations` n'efface pas une traduction serveur avec une chaîne locale vide, sauf `--overwrite-empty`.
- [ ] AC-5 : `push translations` d'un membre ignore les langues non assignées et l'écrit dans la sortie.
- [ ] AC-6 : `push translations` ne modifie aucune valeur source.
- [ ] AC-7 : `pull` produit un dossier par langue cible, avec les mêmes feuilles que la source, vides si non traduites.
- [ ] AC-8 : `status` se termine par le code 1 tant qu'une langue cible a une valeur vide.
- [ ] AC-9 : Un jeton de membre qui lance `push sources` reçoit un refus, et aucun fichier n'est modifié.
- [ ] AC-10 : La sortie du CLI ne contient jamais la valeur de `BABELARY_TOKEN`.

## Hors périmètre

- Mode watch.
- Résolution de conflit à trois branches. Le push translations applique la copie locale selon les règles ci-dessus, sans merge.
- Téléchargement d'un ZIP depuis l'interface. Le pull du CLI suffit en v1.
