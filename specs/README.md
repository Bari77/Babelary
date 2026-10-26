# Babelary — specs

Bureau de traduction auto-hébergé. Il garde les clés JSON et leur valeur dans chaque langue, invite des traducteurs, et se synchronise avec le dépôt par un CLI.

Pas de traduction automatique. Pas de quota de mots. Le dépôt Git reste la copie de travail du propriétaire ; Babelary est l'endroit où les traducteurs écrivent.

## Ordre de lecture

| Spec | Sujet |
|---|---|
| [00-vision.md](00-vision.md) | Produit, vocabulaire, modèle de fichiers, hors périmètre |
| [01-comptes-et-session.md](01-comptes-et-session.md) | Installation, compte, session, jeton CLI |
| [02-projets-membres-droits.md](02-projets-membres-droits.md) | Projets, propriétaire, membre traducteur |
| [03-langues-cles-valeurs.md](03-langues-cles-valeurs.md) | Langues, clés, valeurs, statuts, jetons `{{ }}` |
| [04-interface-web.md](04-interface-web.md) | Écrans du propriétaire et du traducteur |
| [05-api-cli-push-pull.md](05-api-cli-push-pull.md) | API et commandes push, pull, status |
| [06-concatenation.md](06-concatenation.md) | Fusion des JSON d'une langue et contrôle des clés |

## Décisions déjà prises

- Une instance, hébergée par le propriétaire du serveur.
- Deux rôles par projet : **propriétaire** et **membre**.
- Le premier compte, créé à l'installation, est le seul à pouvoir créer des projets.
- Les membres arrivent par une invitation. L'inscription publique est fermée.
- Format unique en v1 : JSON imbriqué, feuilles de type chaîne.
- La langue source est la référence des clés. Les autres langues s'alignent sur elle.
- Une valeur identique à la source est une traduction valide, pas un trou.
- Le CLI pousse et tire des fichiers. La concaténation se fait en local, après le pull.
