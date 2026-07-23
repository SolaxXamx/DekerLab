# RocketStats

Application Windows moderne pour afficher les statistiques Rocket League d'un joueur.

## Fonctionnalités

- **Interface moderne** avec thème sombre/clair
- **Tableau de bord** avec vue d'ensemble des statistiques
- **Profil joueur** avec avatar, niveau et progression
- **Rangs** pour 1v1, 2v2 et 3v3
- **Statistiques détaillées** (buts, passes, arrêts, MVP, etc.)
- **Graphiques** d'évolution du MMR et des victoires
- **Paramètres** personnalisables
- **Recherche de joueurs** par pseudo Epic
- **Mise à jour automatique** des données

## Technologies

- **Langage**: C#
- **Framework**: .NET 8 Windows Forms
- **Compatible**: Windows 10 et Windows 11 (32/64 bits)

## Structure du projet

```
RocketStats/
├── Program.cs                 # Point d'entrée
├── RocketStatsForm.cs        # Classe principale du formulaire
├── RocketStatsForm.*.cs      # Parties du formulaire
├── Models.cs                 # Modèles de données
├── Controls/                 # Contrôles personnalisés
│   ├── RoundedPanel.cs
│   ├── RoundedButton.cs
│   ├── RoundedTextBox.cs
│   ├── RoundedPictureBox.cs
│   ├── GradientPanel.cs
│   ├── ProgressCircle.cs
│   ├── StatCard.cs
│   ├── RankCard.cs
│   ├── GraphPanel.cs
│   ├── LoadingSpinner.cs
│   └── SideBarButton.cs
├── Utilities/                # Classes utilitaires
│   ├── ThemeColors.cs
│   ├── RankIcons.cs
│   ├── ImageHelper.cs
│   ├── DataScraper.cs
│   └── TextBoxExtensions.cs
├── Resources/                # Ressources
│   ├── app_icon.ico
│   ├── app_icon.png
│   └── Images/
├── Scripts/                 # Scripts batch
│   ├── Run.bat
│   ├── Build.bat
│   └── Publish.bat
├── RocketStats.csproj       # Fichier projet
├── app.manifest             # Manifest pour DPI awareness
└── README.md
```

## Compilation

### Prérequis
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Compiler
```bash
# Méthode 1: Utiliser le script
double-cliquez sur Scripts/Build.bat

# Méthode 2: En ligne de commande
cd RocketStats
dotnet build RocketStats.csproj -c Release
```

### Exécuter
```bash
# Méthode 1: Utiliser le script
double-cliquez sur Scripts/Run.bat

# Méthode 2: En ligne de commande
cd RocketStats/bin/Debug/net8.0-windows
RocketStats.exe
```

### Publier
```bash
# Pour publier en auto-contained (x64)
dotnet publish RocketStats.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Le résultat sera dans bin/Release/net8.0-windows/win-x64/publish/
```

## Fonctionnement

1. **Recherche d'un joueur**: Entrez le pseudo Epic dans la barre de recherche et cliquez sur "Rechercher"
2. **Actualisation**: Cliquez sur "Actualiser" pour mettre à jour les données
3. **Navigation**: Utilisez la barre latérale pour naviguer entre les différentes vues
4. **Paramètres**: Personnalisez l'application dans la section Paramètres

## Données

Actuellement, l'application utilise des **données simulées** pour la démonstration. Pour une version de production, vous devez implémenter la classe `DataScraper` pour récupérer les vraies données depuis le site officiel de Rocket League ou Tracker Network.

### Intégration avec Playwright (optionnel)

Pour récupérer les vraies données:

1. Installez le package Microsoft.Playwright:
```bash
dotnet add package Microsoft.Playwright
```

2. Modifiez la méthode `ScrapePlayerData` dans `DataScraper.cs` pour utiliser Playwright

## Personnalisation

Vous pouvez personnaliser:
- **Thème**: Sombre ou Clair
- **Couleur principale**: À implémenter avec un sélecteur de couleur
- **Intervalle d'actualisation**: En minutes
- **Chargement automatique**: Charger le dernier joueur au démarrage
- **Animations**: Activer/désactiver les animations

## Résolution des problèmes

### Problème: L'application ne s'affiche pas correctement
- **Solution**: Vérifiez que votre système supporte .NET 8 et que les pilotes graphiques sont à jour

### Problème: Les données ne se chargent pas
- **Solution**: Vérifiez votre connexion Internet et que le pseudo Epic est correct

### Problème: Erreur de compilation
- **Solution**: Assurez-vous d'avoir installé le .NET 8 SDK

## Contribution

Les contributions sont les bienvenues! Ouvrez une Pull Request pour proposer des améliorations.

## Licence

Ce projet est sous licence MIT. Voir le fichier LICENSE pour plus de détails.

---

**RocketStats** - Votre compagnon pour les statistiques Rocket League
