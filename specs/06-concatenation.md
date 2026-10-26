---
id: FEAT-06
title: Concaténation et contrôle des clés
status: backlog
created: 2026-10-26
depends_on: [FEAT-05]
---

# FEAT-06 — Concaténation et contrôle des clés

## Intention

L'application charge un JSON par langue, pas une vingtaine de petits fichiers. Après un `pull`, le propriétaire fusionne chaque dossier de locale. La commande signale aussi les langues qui ne portent pas exactement les clés de la langue source.

Elle travaille sur le disque. Elle n'appelle pas le serveur : le contrôle réseau est déjà `babelary status`.

## Commande

```text
babelary concat
```

Depuis le dossier qui contient `babelary.yml`, elle lit `base_path`. Chaque sous-dossier est une locale. Elle ignore les fichiers qui ne sont pas des dossiers de locale, et elle ignore un éventuel JSON déjà concaténé posé directement dans `base_path`.

Pour chaque locale, elle lit les `*.json` du dossier, dans l'ordre alphabétique des noms, et les fusionne à la racine.

```text
en-US/account.json  +  en-US/pet.json   →   en-US.json
fr-FR/account.json  +  fr-FR/pet.json   →   fr-FR.json
```

Le fichier produit est écrit dans `base_path`, à côté des dossiers : `base_path/fr-FR.json`. Il est destiné au build. Il n'est pas repoussé vers Babelary.

## Règles de fusion

Chaque petit fichier est un objet. Ses clés de premier niveau sont copiées dans l'objet fusionné.

Si deux fichiers d'une même locale définissent la même clé de premier niveau, par exemple deux fois `ACCOUNT`, la locale échoue. Aucun fichier de sortie n'est écrit pour elle. Le message cite les deux noms de fichiers et la clé.

Un JSON illisible échoue de la même façon, avec le chemin et l'erreur d'analyse.

Les feuilles non chaînes échouent. La concaténation ne sert pas à réparer un fichier : elle refuse de produire un bundle incomplet sans le dire.

## Contrôle des clés

La locale `source_locale` est la référence. Pour chaque autre locale, la commande compare les chemins de feuilles.

Elle affiche :

- les clés présentes dans la source et absentes de la locale ;
- les clés présentes dans la locale et absentes de la source ;
- les feuilles dont les jetons `{{nom}}` ne reprennent pas ceux de la source.

Une valeur vide est une clé présente. Elle n'est pas listée comme absente. Le trou de traduction se voit avec `babelary status`, pas avec la concaténation. La concaténation vérifie la forme des fichiers, pas le travail du traducteur.

## Code de sortie

Par défaut, la commande écrit les bundles des locales valides, affiche les écarts, et se termine par 0 même s'il y a des clés manquantes.

Avec `--strict`, elle se termine par 1 dès qu'une locale échoue à la fusion, ou qu'une locale cible n'a pas exactement les mêmes clés et les mêmes jetons que la source. Les bundles des locales valides sont quand même écrits, pour ne pas bloquer un build le temps de lire le rapport.

## Critères d'acceptation

- [ ] AC-1 : Deux fichiers d'une locale sans clé de premier niveau en commun produisent un seul JSON qui contient les deux objets.
- [ ] AC-2 : Deux fichiers qui déclarent tous les deux `ACCOUNT` n'écrivent pas de bundle pour cette locale et nomment les fichiers en cause.
- [ ] AC-3 : Une clé présente dans `en-US` et absente de `fr-FR` apparaît dans le rapport comme manquante pour `fr-FR`.
- [ ] AC-4 : Une clé en trop dans `ko-KR` apparaît comme clé supplémentaire.
- [ ] AC-5 : Une valeur vide `""` ne compte pas comme une clé manquante.
- [ ] AC-6 : Un jeton `{{name}}` présent dans la source et absent du bundle cible est signalé.
- [ ] AC-7 : `--strict` termine avec le code 1 quand le rapport n'est pas vide, et avec 0 quand chaque cible est alignée sur la source.
- [ ] AC-8 : Relancer la commande remplace les bundles déjà écrits et ne modifie pas les petits fichiers des dossiers de locale.

## Hors périmètre

- Concaténation côté serveur.
- Envoi du bundle vers l'application. Le build de l'application continue de lire le fichier produit en local.
