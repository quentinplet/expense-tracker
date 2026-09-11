# 💶 Trézo

> Un suivi de budget clair : chaque flux bancaire classé, chaque catégorie plafonnée, chaque mois comparable au précédent.

![Angular](https://img.shields.io/badge/Angular-21-DD0031?logo=angular&logoColor=white)
![PrimeNG](<https://img.shields.io/badge/PrimeNG-21%20(Sakai)-6366F1?logo=primeng&logoColor=white>)
![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![EF Core](https://img.shields.io/badge/EF%20Core-10-512BD4?logo=nuget&logoColor=white)
![Postgres](https://img.shields.io/badge/PostgreSQL-Npgsql-336791?logo=postgresql&logoColor=white)
![i18n](https://img.shields.io/badge/i18n-FR%20%7C%20EN-informational)
![Tests](https://img.shields.io/badge/tests-100%20passing-brightgreen?logo=xunit&logoColor=white)
![CI/CD](https://github.com/quentinplet/expense-tracker/actions/workflows/ci-cd.yml/badge.svg)
![License](https://img.shields.io/badge/license-MIT-green)

Application web de suivi budgétaire personnel : transactions classées par catégorie,
budgets mensuels (par catégorie ou global) avec reconduction automatique, charges
récurrentes générées en tâche de fond, import CSV de relevé bancaire, notifications
in-app, dashboard avec graphiques, et interface bilingue français/anglais avec
bascule instantanée.

![Dashboard](docs/screenshots/dashboard.png)

---

## 🔗 Démo live

**[calm-glacier-08876b503.5.azurestaticapps.net](https://calm-glacier-08876b503.5.azurestaticapps.net)**

Inscription libre — créez votre propre compte pour tester l'application.

> L'API et le front tournent sur des paliers gratuits Azure (App Service F1) et une base
> Neon gratuite : le premier appel après une période d'inactivité peut prendre quelques
> secondes le temps que l'instance se réveille.

---

## ✨ Fonctionnalités

- **Transactions** — CRUD complet, filtres (compte/catégorie/type/période), tri,
  pagination serveur, sélection multiple, saisie rapide en dialog modal
- **Catégories personnelles** — chaque utilisateur reçoit sa propre copie d'un jeu de
  catégories de départ à l'inscription ; création/renommage/suppression libres, avec
  réassignation automatique des transactions vers une catégorie « Other » verrouillée
- **Budgets** — plafond mensuel par catégorie ou budget global (toutes catégories de
  dépense), barre de progression, reconduction automatique d'un mois sur l'autre
  (`AutoRenew`) via une tâche planifiée
- **Charges récurrentes** — dépenses ou revenus périodiques (hebdo/mensuel/annuel),
  génération automatique de la transaction à échéance, rattrapage des occurrences
  manquées après un arrêt du service
- **Import CSV** — upload d'un relevé bancaire, détection auto de l'encodage/séparateur
  et mapping des colonnes, prévisualisation ligne par ligne, dédoublonnage par hash
  recalculé côté serveur, détection de conflit avec les charges récurrentes
- **Notifications in-app** — budget franchissant 90 %/100 %, charge récurrente générée,
  vérifié après chaque écriture de transaction sans jamais faire échouer celle-ci
- **Dashboard** — net cumulé, répartition des dépenses par catégorie (mois ou 12
  derniers mois glissants), courbe d'évolution, budgets et charges à venir, actions
  rapides, transactions récentes
- **Compte utilisateur** — inscription avec jeu de catégories de départ personnel,
  gestion du profil (nom, mot de passe, email), suppression de compte
- **Bilingue FR/EN** — bascule instantanée sans rechargement (ngx-translate côté
  Angular, `IStringLocalizer` côté API pour les messages d'erreur), montants et dates
  formatés selon la locale active
- **Authentification** — ASP.NET Core Identity + JWT (access token court + refresh
  token en cookie httpOnly), isolation stricte des données par utilisateur

Hors périmètre pour l'instant : multi-devise, comptes bancaires multiples avec solde
réel, page Rapports détaillée, synchronisation bancaire automatique.

---

## 🖼️ Captures d'écran

|                     Transactions                     |                    Saisie rapide                     |
| :---------------------------------------------------: | :---------------------------------------------------: |
| ![Transactions](docs/screenshots/transactions.png) | ![Saisie rapide](docs/screenshots/quick-add.png) |

|                Budgets                 |                  Catégories                  |
| :-------------------------------------: | :-------------------------------------------: |
| ![Budgets](docs/screenshots/budgets.png) | ![Catégories](docs/screenshots/categories.png) |

---

## 🤔 Pourquoi ce projet

Ce projet met l'accent sur des chantiers techniques concrets et démontrables :

| Chantier                                                            | Ce que ça démontre                                                                                   |
| ------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| Auth JWT + isolation multi-tenant                                   | Sécurité applicative, autorisation au niveau des données, refresh token                              |
| Architecture en couches (Controller → Service/Repository → EF Core) | Séparation des responsabilités, DTOs, injection de dépendances                                       |
| Jobs planifiés idempotents (`BackgroundService`)                    | Reconduction de budgets et génération de charges récurrentes, sans doublon même après un redémarrage |
| Import CSV avec dédoublonnage                                       | Parsing robuste (encodage, séparateur FR), idempotence, workflow de prévisualisation                 |
| Bilinguisme FR/EN de bout en bout                                   | i18n complète — front et back — souvent absente des projets portfolio                                |
| Tests unitaires (xUnit + Moq)                                       | 100 tests sur les règles métier critiques — isolation multi-tenant, agrégations, jobs idempotents    |
| CI/CD GitHub Actions → Azure                                        | Pipeline build + test + déploiement automatique sur push                                             |

---

## 🏗️ Stack technique

| Couche      | Techno                                                                                          |
| ----------- | ----------------------------------------------------------------------------------------------- |
| Frontend    | Angular 21 (standalone, signaux), PrimeNG 21 (thème Sakai), Tailwind 4, Chart.js, ngx-translate |
| Backend     | ASP.NET Core 10 Web API, ASP.NET Core Identity + JWT                                            |
| Données     | PostgreSQL (Npgsql), Entity Framework Core 10, migrations versionnées                           |
| i18n        | ngx-translate (front) + `IStringLocalizer` (back)                                               |
| Hébergement | Azure Static Web Apps (front) + Azure App Service (API) + Neon (Postgres serverless)            |
| CI/CD       | GitHub Actions — build, test, déploiement automatique sur `main`                                |

### Architecture backend

Un seul projet ASP.NET Core, quatre couches à sens unique :

```
Controller ──▶ Service / Repository ──▶ DbContext (EF Core) ──▶ PostgreSQL
```

Isolation multi-tenant imposée par la signature des méthodes de repository
(`GetByIdAsync(id, userId, ct)`) plutôt que par convention — le compilateur empêche
d'oublier le filtre `UserId`.

---

## 🧪 Tests

100 tests unitaires backend (`API.Tests`, xUnit + Moq — tout est mocké, aucune base de
données requise) : `dotnet test expense-tracker.slnx`. Couvrent notamment :

- **Isolation multi-tenant** — chaque contrôleur refuse l'accès à une ressource
  n'appartenant pas à l'utilisateur courant (403), jamais un simple oubli de filtre
- **Règles métier** — cohérence `Category.Type`/`Transaction.Type`, immuabilité de
  `CategoryId`/`Month` sur un budget, cascade de réaffectation à la suppression d'une
  catégorie (transactions, budgets, charges récurrentes)
- **Agrégations** — `Spent`/`Remaining` d'un budget, répartition par catégorie du
  dashboard (mois vs 12 derniers mois glissants), courbe d'évolution bornée
- **Jobs planifiés** — reconduction automatique des budgets et génération des charges
  récurrentes, y compris le rattrapage de plusieurs échéances manquées en un seul
  passage, de façon idempotente (rejouer le job ne crée jamais de doublon)
- **Notifications** — seuils de budget à 90 %/100 % indépendants, jamais dupliqués

Exécutés automatiquement en CI à chaque push/PR (voir [CI/CD](#-cicd)).

---

## 🚀 Démarrer en local

### Prérequis

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org)
- [Docker](https://www.docker.com/) (pour PostgreSQL)
- `dotnet-ef` : `dotnet tool install --global dotnet-ef`

### Base de données

```bash
docker compose up -d
```

### Backend (`/API`)

```bash
cd API
dotnet ef database update --project .
dotnet run --project .
# API disponible sur https://localhost:5001
```

Au démarrage, l'API applique les migrations en attente puis initialise deux comptes
de démo (`john` / `admin`, mot de passe `Pa$$w0rd`) avec des données associées,
utiles pour tester en local.

### Frontend (`/client`)

```bash
cd client
npm install
npm start
# App disponible sur https://localhost:4200
```

Le frontend attend un certificat local auto-signé (`client/ssl/*.pem`) pour servir en
HTTPS — nécessaire pour matcher les origines autorisées par le CORS de l'API.

---

## 🔄 CI/CD

Un pipeline GitHub Actions unique (`.github/workflows/ci-cd.yml`) build et teste l'API
(100 tests xUnit, rapport détaillé publié dans l'onglet Summary du run) et le client à
chaque push/PR, puis déploie automatiquement sur push vers `main` : l'API sur Azure App
Service, le front sur Azure Static Web Apps.

---

## 🗺️ Roadmap

Prochains chantiers : page Rapports (comparaisons mensuelles détaillées), comptes
bancaires multiples avec solde réel.

---

## 📄 Licence

[MIT](LICENSE)
