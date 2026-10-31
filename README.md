# Babelary

Bureau de traduction auto-hébergé. Les traducteurs ouvrent le site, voient les clés et remplissent les langues qu'on leur a confiées. Ils n'ont pas accès au dépôt Git. Le propriétaire pousse les sources avec le CLI, récupère les traductions, puis concatène chaque langue en un JSON pour l'application.

Une instance, hébergée par le propriétaire du serveur. Le premier compte créé à l'installation est le seul à pouvoir créer des projets. Les membres arrivent par une invitation. Le format historique est le JSON imbriqué. Un catalogue Angular (`locale` + `translations`, identifiants à points, jetons `{$nom}`) est conservé tel quel. Les locales suivent `xx-XX`.

Le détail est dans les specs.

| Spec | Sujet |
|---|---|
| [specs/00-vision.md](specs/00-vision.md) | Produit, vocabulaire, fichiers |
| [specs/01-comptes-et-session.md](specs/01-comptes-et-session.md) | Installation, session, jeton CLI |
| [specs/02-projets-membres-droits.md](specs/02-projets-membres-droits.md) | Projets, membres, droits |
| [specs/03-langues-cles-valeurs.md](specs/03-langues-cles-valeurs.md) | Langues, clés, statuts, jetons `{{ }}` |
| [specs/04-interface-web.md](specs/04-interface-web.md) | Écrans |
| [specs/05-api-cli-push-pull.md](specs/05-api-cli-push-pull.md) | API et commandes push, pull, status |
| [specs/06-concatenation.md](specs/06-concatenation.md) | Fusion des JSON et contrôle des clés |

## Stack

| Rôle | Choix |
|---|---|
| API | .NET 10, ASP.NET Core, couches `Api` / `Application` / `Core` / `Infrastructure` |
| Données | PostgreSQL 17, EF Core, migrations |
| Session web | Cookie `HttpOnly`, 30 jours. Jeton Bearer pour le CLI |
| Interface | Angular 21, standalone, signals, Angular Material, Bun |
| Textes de l'outil | `Front/public/i18n/fr-FR.json` |
| CLI | `Back/Cli`, commande `babelary` |
| Dev | Podman Compose |
| Images | CI GitHub : `${REGISTRY}/babelary/api` et `${REGISTRY}/babelary/web` |

Même façon de découper le code que Kert'App : actions `api/v1/{Contrôleur}/{action}`, query string, `BaseService`, stores, exceptions `ERR_*`. PostgreSQL remplace SQL Server parce que l'instance est unique et auto-hébergée. Les mots de passe passent par `PasswordHasher` (PBKDF2). L'interface suit le BackOffice (Material), pas l'app mobile Ionic.

L'installation crée le propriétaire d'instance. La connexion pose un cookie de session, le profil change le nom et le mot de passe, et crée ou révoque un jeton CLI. L'interface utilise le thème sombre d'Angular Material. Les projets, langues et commandes du CLI restent à écrire.

## Lancer en local

[Podman](https://podman.io/) 5 ou plus, avec la commande `podman compose`.

```sh
podman compose up --build
```

Le fichier de dev attache les conteneurs au réseau de la machine Podman. Windows voit alors les ports sur localhost.

- Interface : http://localhost:4200
- API : http://localhost:8080/api/v1/Health/get
- PostgreSQL : `localhost:5432`, base `babelary`, utilisateur `babelary`, mot de passe `babelary`

Ce mot de passe ne sert qu'au bureau de développement. Le serveur de production le reçoit par variable d'environnement.

`Ctrl+C` arrête les conteneurs. Les données Postgres restent dans le volume `postgres-data`.

## Images et serveur

Les workflows `.github/workflows/back-cd.yml` et `front-cd.yml` construisent les `Dockerfile` au push sur `develop` (`staging`) et `main` (`latest`), puis poussent la version lue dans `Back/CHANGELOG.md` ou `Front/CHANGELOG.md`.

Variables du dépôt, comme sur Kert'App :

| Nom | Rôle |
|---|---|
| `REGISTRY` | Hôte du registry, par exemple `registry.exemple/babel` |
| `REGISTRY_USERNAME` | Secret |
| `REGISTRY_PASSWORD` | Secret |

Sur le serveur, un fichier `.env` à côté de `compose.prod.yaml` :

```sh
REGISTRY=registry.exemple/babel
TAG=latest
POSTGRES_USER=babelary
POSTGRES_PASSWORD=...
POSTGRES_DB=babelary
WEB_PORT=8080
```

```sh
podman compose -f compose.prod.yaml pull
podman compose -f compose.prod.yaml up -d
```

Nginx publie le site et proxifie `/api/` vers l'API. L'API n'a pas de port public. Le TLS se termine devant ce Nginx.

## Dépôt

```text
Back/     API, métier, EF Core, CLI
Front/    Angular
specs/    comportement v1
compose.yaml
compose.prod.yaml
```

```sh
dotnet test Back/Babelary.Back.slnx
dotnet run --project Back/Cli/Cli.csproj -- --help
bun run --cwd Front start
```
