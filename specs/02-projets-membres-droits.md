---
id: FEAT-02
title: Projets, membres et droits
status: backlog
created: 2026-10-26
depends_on: [FEAT-01]
---

# FEAT-02 — Projets, membres et droits

## Intention

Un projet rassemble les fichiers d'une application. Le propriétaire le crée et y invite des traducteurs. Un traducteur ne voit que ses projets et ne modifie que les langues qu'on lui a confiées.

## Projet

Créer un projet demande un nom et une langue source. Le code de langue suit le format `xx-XX`, comme `en-US` ou `ko-KR`. Le code source ne se change plus une fois qu'une clé existe.

Le propriétaire d'instance est le propriétaire de chaque projet qu'il crée. Un membre ne peut pas créer de projet.

Supprimer un projet supprime ses langues, ses fichiers, ses clés, ses valeurs et ses invitations. L'action demande le nom du projet saisi de nouveau.

## Invitation

Le propriétaire invite par e-mail et choisit une ou plusieurs langues cibles. L'invitation produit un lien à copier. Le lien expire au bout de 7 jours et ne sert qu'une fois.

La personne ouvre le lien, voit le projet et les langues, choisit son nom et son mot de passe, puis entre dans le projet comme membre.

Si l'e-mail a déjà un compte, le lien rattache ce compte au projet sans recréer le mot de passe. La personne se connecte avec son mot de passe actuel.

Une invitation encore ouverte peut être annulée. Le lien cesse alors de fonctionner.

## Droits

| Action | Propriétaire | Membre |
|---|---|---|
| Voir le projet et l'avancement | oui | oui, langues assignées |
| Modifier une valeur traduite | oui, toutes les langues cibles | oui, langues assignées |
| Modifier la valeur source | oui | non |
| Ajouter ou retirer une langue | oui | non |
| Ajouter, renommer ou supprimer un fichier ou une clé | oui | non |
| Inviter, retirer un membre, changer ses langues | oui | non |
| Supprimer le projet | oui | non |
| Push des sources | oui | non |
| Push des traductions | oui | langues assignées seulement |
| Pull | oui | langues assignées, plus la source en lecture |

Retirer un membre coupe son accès au projet immédiatement, y compris pour ses jetons. Son compte reste utilisable s'il participe à un autre projet.

Changer les langues d'un membre prend effet à la prochaine requête.

## Critères d'acceptation

- [ ] AC-1 : Le propriétaire d'instance crée un projet avec un nom et une langue source `xx-XX`.
- [ ] AC-2 : Un membre qui appelle la création de projet reçoit un refus.
- [ ] AC-3 : Le lien d'invitation affiche le projet et les langues avant le choix du mot de passe.
- [ ] AC-4 : Un lien expiré, annulé ou déjà consommé affiche une page d'erreur sans créer de compte.
- [ ] AC-5 : Un membre ne voit dans la liste que les projets où il est invité.
- [ ] AC-6 : Un membre qui enregistre une valeur source, une langue, ou une traduction hors de ses langues reçoit un refus.
- [ ] AC-7 : Après retrait, l'ancienne session du membre ne lit plus ce projet.
- [ ] AC-8 : Supprimer un projet exige de retaper son nom, puis le projet n'apparaît plus nulle part.

## Hors périmètre

- Droits fins par fichier.
- Rôle relecteur distinct du traducteur.
- Transfert de propriété.
