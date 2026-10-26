---
id: VISION
title: Vision et modèle de fichiers
status: backlog
created: 2026-10-26
---

# Vision

## Intention

Babelary remplace un outil de traduction hébergé par un service sur le serveur du propriétaire. Les traducteurs n'ont pas accès au dépôt Git. Ils ouvrent le site, voient les clés, et remplissent les valeurs de leurs langues.

Le propriétaire garde le dépôt comme source des fichiers. Il envoie les textes source avec `push`, récupère les traductions avec `pull`, puis concatène chaque langue en un seul JSON pour l'application.

## Vocabulaire

| Mot | Sens |
|---|---|
| Instance | Le site Babelary déployé une fois. |
| Propriétaire d'instance | Le compte créé à l'installation. Seul ce compte crée des projets. |
| Projet | Une application à traduire, par exemple le front Kert. |
| Propriétaire | Le compte qui possède le projet. En v1, c'est le propriétaire d'instance. |
| Membre | Un traducteur invité sur un projet. |
| Langue source | La locale de référence, par défaut `en-US`. Elle définit les clés. |
| Langue cible | Une locale à traduire, par exemple `fr-FR` ou `ko-KR`. |
| Fichier | Un JSON nommé, par exemple `pet.json`. Le même nom existe dans chaque langue. |
| Clé | Le chemin d'une feuille, par exemple `PET.CREATION.TITLE`. |
| Valeur source | Le texte de la langue source pour cette clé. |
| Valeur traduite | Le texte d'une langue cible pour cette clé. |

## Modèle de fichiers

Le modèle reprend le rangement déjà utilisé dans l'application : un dossier par locale, plusieurs JSON par dossier.

```text
src/assets/i18n/
  en-US/
    account.json
    pet.json
  fr-FR/
    account.json
    pet.json
  ko-KR/
    account.json
    pet.json
```

Chaque fichier est un objet JSON. Les objets s'imbriquent. Seules les feuilles chaînes sont des clés traduisibles.

```json
{
  "ACCOUNT": {
    "LOGIN": "Username",
    "PASSWORD": "Password"
  }
}
```

La clé ci-dessus est `ACCOUNT.LOGIN` dans le fichier `account.json`.

La concaténation réunit tous les JSON d'une locale en un seul document, à la racine. L'application charge ensuite `fr-FR.json`, pas les fichiers séparés. Le détail est dans [06-concatenation.md](06-concatenation.md).

## Ce que le serveur garde

Le serveur garde le projet, les langues, les fichiers, les clés, les valeurs et les membres. Il ne garde pas le bundle concaténé : ce fichier est produit en local par le CLI, au moment du build de l'application.

## Hors périmètre v1

- Traduction automatique, suggestions par IA, mémoire de traduction.
- Quota, facturation, limite de mots.
- Formats autres que le JSON (YAML, Gettext, XLIFF, CSV).
- Feuilles qui ne sont pas des chaînes (nombres, booléens, tableaux).
- Relecture à deux niveaux, commentaires, captures d'écran, glossaire séparé.
- Plusieurs propriétaires par projet, équipes, organisations.
- Édition simultanée du même champ par deux personnes. Le dernier enregistrement gagne.
- Envoi d'e-mail obligatoire. L'invitation fonctionne par lien copié. Un SMTP peut être ajouté plus tard sans changer le lien.
