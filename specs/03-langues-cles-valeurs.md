---
id: FEAT-03
title: Langues, clés et valeurs
status: backlog
created: 2026-10-26
depends_on: [FEAT-02]
---

# FEAT-03 — Langues, clés et valeurs

## Intention

La langue source tient la liste des clés. Chaque langue cible tient une valeur par clé. Ajouter une langue prépare les trous, sans recopier l'anglais comme si c'était déjà traduit. Une chaîne volontairement identique à la source reste une vraie traduction.

## Langue cible

Le propriétaire ajoute une langue par son code `xx-XX` et un nom affiché, par exemple `ko-KR` et `한국어`. Le code est unique dans le projet. Il ne peut pas être celui de la langue source.

Ajouter une langue crée une valeur vide pour chaque clé déjà présente. Retirer une langue supprime ses valeurs. L'action demande une confirmation.

## Fichiers et clés

Un fichier a un nom stable, `pet.json`. Les noms suivent `nom.json`, sans chemin de dossier.

Une clé est le chemin des objets jusqu'à une feuille chaîne, séparé par des points. `PET.TABS.FORM` est une clé. `PET` et `PET.TABS` n'en sont pas : ce sont des groupes.

Créer une clé se fait en envoyant le JSON source, ou depuis l'interface du propriétaire. Une clé nouvelle naît avec la valeur source et une valeur vide dans chaque langue cible.

Supprimer une clé supprime ses traductions. Renommer une clé est une suppression suivie d'une création : les traductions ne suivent pas le nouveau nom.

## Valeurs

Une valeur est une chaîne, y compris vide. La chaîne vide dans une langue cible signifie « non traduit ».

La chaîne source vide est autorisée. Elle compte comme une clé à traduire.

Les espaces en début et fin sont conservés. Ils font partie du texte affiché par l'application.

## Statuts d'une valeur cible

| Statut | Condition |
|---|---|
| À traduire | Valeur vide. |
| Traduit | Valeur non vide, et la source n'a pas changé depuis l'enregistrement. |
| Identique | Valeur non vide et égale à la source, caractère pour caractère. C'est un cas de « traduit », affiché à part pour qu'on puisse le relire. |
| À revoir | Valeur non vide, enregistrée contre une source qui a changé depuis. |

« Identique » n'est pas un trou. L'avancement le compte comme fait. Le filtre de l'interface permet quand même de les revoir, pour les noms propres laissés tels quels comme pour les oublis.

Quand la source change, les traductions non vides de cette clé passent à « à revoir ». Leur texte n'est pas effacé.

## Jetons

Un jeton est `{{nom}}` ou, dans un catalogue Angular, `{$nom}`. Le nom contient des lettres, des chiffres, `_` et, pour `{{nom}}`, `.`.

La liste des jetons de la traduction doit être la même que celle de la source, avec le même nombre d'occurrences. L'ordre peut changer.

`Bonjour {{name}}` et `{{name}}, bonjour` sont compatibles. `Bonjour {{nom}}` ne l'est pas si la source dit `{{name}}`.

Une sauvegarde qui casse les jetons est refusée. La valeur précédente reste en place. La valeur source, elle, peut changer les jetons : les traductions passent alors à « à revoir », et le prochain enregistrement traducteur devra reprendre les nouveaux jetons.

## Avancement

Pour une langue cible :

```text
traduites / total
```

`traduites` compte les statuts Traduit, Identique et À revoir. `total` est le nombre de clés du projet. « À traduire » est le reste.

L'avancement du projet est la somme des langues cibles.

## Critères d'acceptation

- [ ] AC-1 : Ajouter `ko-KR` crée une valeur vide pour chaque clé, sans copier la source.
- [ ] AC-2 : Une feuille qui n'est pas une chaîne est refusée à l'import comme à la saisie.
- [ ] AC-3 : Enregistrer une traduction égale à la source la classe « identique » et l'inclut dans l'avancement.
- [ ] AC-4 : Vider une traduction la reclasse « à traduire ».
- [ ] AC-5 : Modifier la source d'une clé passe ses traductions non vides à « à revoir » sans les effacer.
- [ ] AC-6 : `{{name}}` retiré ou renommé dans la traduction est refusé, avec le jeton attendu indiqué.
- [ ] AC-7 : Deux occurrences de `{{day}}` dans la source en exigent deux dans la traduction.
- [ ] AC-8 : Retirer une langue cible retire ses valeurs et la retire de l'avancement.
- [ ] AC-9 : Supprimer une clé la retire de toutes les langues.

## Hors périmètre

- Pluriels ICU, genres, formes au singulier et au pluriel.
- Suggestion de traduction.
- Historique des anciennes valeurs. Le statut « à revoir » suffit en v1.
