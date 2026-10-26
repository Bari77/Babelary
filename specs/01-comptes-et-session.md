---
id: FEAT-01
title: Comptes et session
status: backlog
created: 2026-10-26
depends_on: []
---

# FEAT-01 — Comptes et session

## Intention

Le site est privé. Le propriétaire installe l'instance, crée son compte, puis ouvre la porte aux traducteurs un par un. Le CLI s'authentifie avec un jeton, pas avec le mot de passe tapé dans le terminal à chaque commande.

## Installation

Tant qu'aucun compte n'existe, la seule page disponible est la création du propriétaire d'instance : nom, e-mail, mot de passe.

Dès que ce compte existe, cette page disparaît. Elle ne revient pas.

## Compte

Un compte a un nom, un e-mail unique et un mot de passe. L'e-mail sert d'identifiant de connexion.

Le mot de passe n'est jamais stocké en clair. La règle minimale est 12 caractères.

Il n'y a pas d'inscription publique. Un membre n'existe qu'après avoir ouvert une invitation et choisi son mot de passe. Le flux d'invitation est décrit dans [02-projets-membres-droits.md](02-projets-membres-droits.md).

## Session web

La connexion crée une session par cookie `HttpOnly`, `Secure`, `SameSite`. La déconnexion supprime la session côté serveur.

Une session inactive expire au bout de 30 jours. Le site redemande alors l'e-mail et le mot de passe.

## Jeton CLI

Chaque compte peut créer des jetons personnels depuis son profil. Un jeton est montré une seule fois, à la création. Ensuite l'interface n'affiche que son nom et sa date.

Le CLI envoie le jeton en `Authorization: Bearer`. Le jeton a les mêmes droits que le compte qui l'a créé.

Révoquer un jeton coupe les commandes qui l'utilisent. La session web du même compte reste ouverte.

## Critères d'acceptation

- [ ] AC-1 : Sur une instance vide, ouvrir le site affiche uniquement le formulaire du premier compte.
- [ ] AC-2 : Après création de ce compte, le formulaire d'installation n'est plus accessible.
- [ ] AC-3 : Un visiteur sans session qui ouvre une page du site arrive sur la connexion.
- [ ] AC-4 : Un e-mail déjà utilisé est refusé à la création comme à l'invitation.
- [ ] AC-5 : Un mot de passe de moins de 12 caractères est refusé.
- [ ] AC-6 : La déconnexion empêche de rouvrir une page privée avec l'ancienne session.
- [ ] AC-7 : Un jeton créé est affiché une fois, puis ne peut plus être relu.
- [ ] AC-8 : Un jeton révoqué reçoit un refus d'authentification à l'appel suivant.

## Hors périmètre

- OAuth, SSO, double facteur.
- Réinitialisation du mot de passe par e-mail. En v1, le propriétaire d'instance peut régénérer l'accès d'un membre en créant une nouvelle invitation pour le même e-mail.
