---
id: FEAT-04
title: Interface web
status: backlog
created: 2026-10-26
depends_on: [FEAT-01, FEAT-02, FEAT-03]
---

# FEAT-04 — Interface web

## Intention

Le traducteur fait son travail dans le navigateur, sans ouvrir un JSON. Le propriétaire y gère les langues, les membres et, s'il le veut, les textes source. L'interface est en anglais par défaut. Depuis le profil, on peut passer en français ; ce choix est enregistré sur l'appareil. C'est l'outil du propriétaire et de ses traducteurs, pas l'application traduite.

## Pages

### Installation et connexion

Décrites dans [01-comptes-et-session.md](01-comptes-et-session.md). Après connexion, la page d'accueil est la liste des projets visibles.

### Liste des projets

Chaque ligne montre le nom, la langue source, le nombre de langues cibles et l'avancement global. Le propriétaire voit un bouton « Nouveau projet ». Le membre ne le voit pas.

### Projet

En-tête : nom, langue source, avancement par langue cible.

Trois zones, dans cet ordre :

1. **Langues** — code, nom affiché, traduits / total, nombre de « à revoir ». Le propriétaire ajoute et retire. Le membre voit la liste de ses langues seulement.
2. **Fichiers** — nom du fichier et nombre de clés. Ouvrir un fichier entre dans l'éditeur.
3. **Membres** — nom, e-mail, langues assignées. Le propriétaire invite, modifie les langues et retire. Le membre ne voit pas cette zone.

### Éditeur

L'éditeur montre un fichier et une langue cible à la fois. La langue source est affichée en lecture à côté de la valeur éditable.

Chaque ligne comprend :

- le chemin de la clé ;
- la valeur source ;
- la valeur de la langue choisie ;
- le statut.

Filtres : toutes, à traduire, identiques, à revoir. Une recherche trouve un chemin de clé ou un morceau de la valeur source.

Le membre change de langue parmi celles qui lui sont assignées. Le propriétaire change parmi toutes les langues cibles, et peut basculer en édition de la source. En édition source, il n'y a pas de colonne traduite.

L'enregistrement est par clé. Un échec de jetons s'affiche sur la ligne, sans perdre la saisie en cours. Quitter la page avec une ligne non enregistrée demande confirmation.

Les groupes vides ne s'affichent pas comme des lignes éditables. `ACCOUNT` n'est pas une ligne. `ACCOUNT.LOGIN` l'est.

### Profil

Le profil permet de changer son nom, son mot de passe, et de créer ou révoquer un jeton CLI. Le jeton nouveau est montré une fois, avec un bouton pour le copier.

## Règles d'affichage

Les textes de traduction sont affichés tels quels, sans interpréter le HTML. Un `<` reste un `<`.

Les jetons `{{nom}}` dans la valeur source sont visuellement distincts du reste de la phrase, pour qu'on les recopie sans les traduire.

L'avancement d'une langue à jour affiche le total, par exemple `1280 / 1280`. Une langue sans clé affiche `0 / 0`.

## Critères d'acceptation

- [ ] AC-1 : Un membre ouvre un fichier et ne peut choisir que ses langues.
- [ ] AC-2 : Le filtre « à traduire » ne montre que les valeurs vides.
- [ ] AC-3 : Le filtre « identiques » ne montre que les valeurs égales à la source.
- [ ] AC-4 : La recherche `LOGIN` trouve `ACCOUNT.LOGIN` et une source qui contient ce mot.
- [ ] AC-5 : Un enregistrement refusé pour jeton manquant laisse le texte saisi dans le champ et explique le jeton attendu.
- [ ] AC-6 : Le propriétaire peut passer l'éditeur sur la source et enregistrer une nouvelle valeur.
- [ ] AC-7 : Le membre ne voit ni l'invitation, ni l'ajout de langue, ni l'édition source.
- [ ] AC-8 : Une valeur contenant `<b>` s'affiche comme du texte, pas comme du gras.

## Hors périmètre

- Raccourcis de traduction en masse.
- Aperçu dans l'application Kert.
- Thème sombre obligatoire. Le site suit le thème clair du navigateur en v1.
