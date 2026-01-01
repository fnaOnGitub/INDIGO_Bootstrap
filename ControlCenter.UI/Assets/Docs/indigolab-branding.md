# IndigoLab Branding Guide

Guida completa al branding e all'identità visiva del **Indigo Control Center**.

## 🎨 Palette Colori

Il Control Center utilizza una palette professionale e tecnica, senza colori eccessivamente saturi o cartoon.

### Colori Principali

| Nome | Colore | Hex | Utilizzo |
|------|--------|-----|----------|
| **Indigo Principale** | ![#4B0082](https://via.placeholder.com/20/4B0082/4B0082) | `#4B0082` | Titoli, icone primarie, bordi |
| **Indigo Scuro** | ![#2E004F](https://via.placeholder.com/20/2E004F/2E004F) | `#2E004F` | Bordi card, header, sottotitoli |
| **Bianco** | ![#FFFFFF](https://via.placeholder.com/20/FFFFFF/FFFFFF) | `#FFFFFF` | Background card e contenuti |
| **Grigio Chiaro** | ![#F3F3F3](https://via.placeholder.com/20/F3F3F3/F3F3F3) | `#F3F3F3` | Background neutro, pulsanti default |

### Colori Accento

| Nome | Colore | Hex | Utilizzo |
|------|--------|-----|----------|
| **Arancione Acceso** | ![#FF7A00](https://via.placeholder.com/20/FF7A00/FF7A00) | `#FF7A00` | Pulsanti importanti, call-to-action |
| **Ciano Tenue** | ![#00B7C3](https://via.placeholder.com/20/00B7C3/00B7C3) | `#00B7C3` | Dettagli tecnici, icone secondarie |

## 📝 Risorse XAML

### SolidColorBrush Disponibili

```xml
<!-- Colori Primari -->
<StaticResource ResourceKey="IndigoPrimaryBrush"/>
<StaticResource ResourceKey="IndigoDarkBrush"/>
<StaticResource ResourceKey="IndigoWhiteBrush"/>
<StaticResource ResourceKey="IndigoLightGrayBrush"/>

<!-- Colori Accento -->
<StaticResource ResourceKey="IndigoOrangeBrush"/>
<StaticResource ResourceKey="IndigoCyanBrush"/>

<!-- Alias Accenti -->
<StaticResource ResourceKey="AccentColorBrush"/>         <!-- = IndigoPrimaryBrush -->
<StaticResource ResourceKey="SecondaryAccentBrush"/>     <!-- = IndigoOrangeBrush -->
<StaticResource ResourceKey="TertiaryAccentBrush"/>      <!-- = IndigoCyanBrush -->
```

## 🎯 Stili TextBlock

### TitleTextBlockStyle

Utilizzato per i titoli principali delle pagine.

**Proprietà:**
- FontSize: 32px
- FontWeight: SemiBold
- Foreground: IndigoPrimaryBrush
- TextWrapping: NoWrap

**Esempio:**

```xml
<TextBlock Text="Dashboard" Style="{StaticResource TitleTextBlockStyle}"/>
```

### SubtitleTextBlockStyle

Utilizzato per sottotitoli e descrizioni.

**Proprietà:**
- FontSize: 16px
- FontWeight: Normal
- Foreground: IndigoDarkBrush
- Opacity: 0.85
- TextWrapping: Wrap

**Esempio:**

```xml
<TextBlock Text="Monitoraggio cluster in tempo reale" 
           Style="{StaticResource SubtitleTextBlockStyle}"/>
```

### BodyStrongTextBlockStyle

Utilizzato per testo enfatizzato nel body.

**Proprietà:**
- FontSize: 14px
- FontWeight: SemiBold
- Foreground: TextFillColorPrimaryBrush (si adatta al tema)
- TextWrapping: NoWrap

**Esempio:**

```xml
<TextBlock Text="agent-orchestrator" 
           Style="{StaticResource BodyStrongTextBlockStyle}"/>
```

## 📦 Stili Card

### IndigoCardStyle

Stile predefinito per card e contenitori.

**Proprietà:**
- Background: IndigoWhiteBrush
- BorderBrush: IndigoDarkBrush
- BorderThickness: 1
- CornerRadius: 8
- Padding: 16

**Esempio:**

```xml
<Border Style="{StaticResource IndigoCardStyle}">
    <!-- Contenuto card -->
</Border>
```

**Esempio inline (senza style):**

```xml
<Grid Background="{StaticResource IndigoWhiteBrush}"
      BorderBrush="{StaticResource IndigoDarkBrush}"
      BorderThickness="1"
      CornerRadius="8"
      Padding="16">
    <!-- Contenuto -->
</Grid>
```

## 🔘 Stili Button

### IndigoButtonStyle

Pulsante standard con stile Indigo.

**Proprietà:**
- Background: IndigoLightGrayBrush
- Foreground: Testo standard (si adatta al tema)
- BorderBrush: IndigoPrimaryBrush
- BorderThickness: 1
- Padding: 12,8
- CornerRadius: 4

**Esempio:**

```xml
<Button Content="Refresh" 
        Style="{StaticResource IndigoButtonStyle}"/>
```

### IndigoAccentButtonStyle

Pulsante con accento arancione per call-to-action.

**Proprietà:**
- Background: IndigoOrangeBrush
- Foreground: IndigoWhiteBrush

**Esempio:**

```xml
<Button Content="Deploy" 
        Style="{StaticResource IndigoAccentButtonStyle}"/>
```

## 🔄 Stili ProgressRing e ProgressBar

### IndigoProgressRingStyle

**Proprietà:**
- Foreground: IndigoPrimaryBrush

**Esempio:**

```xml
<ProgressRing Style="{StaticResource IndigoProgressRingStyle}"
              IsActive="True"
              Width="40"
              Height="40"/>
```

### IndigoProgressBarStyle

**Proprietà:**
- Foreground: IndigoPrimaryBrush

**Esempio:**

```xml
<ProgressBar Style="{StaticResource IndigoProgressBarStyle}"
             Value="50"
             Maximum="100"/>
```

## 🧭 Iconografia

### Linee Guida Icone

- ✅ **Usa SOLO SymbolIcon o FontIcon Fluent** (monocromatici)
- ❌ **NO icone colorate, cartoon o personalizzate**
- ✅ Icone con colore `IndigoPrimaryBrush` per primarie
- ✅ Icone con colore `SecondaryAccentBrush` per accento
- ✅ Icone con colore `TertiaryAccentBrush` per dettagli tecnici

### Esempi

```xml
<!-- Icona primaria (Indigo) -->
<SymbolIcon Symbol="Home" 
            Foreground="{StaticResource IndigoPrimaryBrush}"/>

<!-- Icona accento (Arancione) -->
<SymbolIcon Symbol="Accept" 
            Foreground="{StaticResource SecondaryAccentBrush}"/>

<!-- Icona tecnica (Ciano) -->
<FontIcon Glyph="&#xE8A5;" 
          Foreground="{StaticResource TertiaryAccentBrush}"/>
```

### Icone per NavigationView

```xml
<NavigationViewItem Content="Dashboard">
    <NavigationViewItem.Icon>
        <SymbolIcon Symbol="Home"/>
    </NavigationViewItem.Icon>
</NavigationViewItem>
```

## 🎨 Look & Feel

### Principi di Design

1. **Professionale**: Design pulito, senza elementi giocosi
2. **Tecnico**: Enfasi su funzionalità e dati
3. **Monocromatico**: Icone Fluent standard senza colori personalizzati
4. **Contrasto**: Testo scuro su sfondo chiaro per leggibilità
5. **Consistenza**: Usa sempre gli stili predefiniti

### DO ✅

- Usa `IndigoWhiteBrush` per background card
- Usa `IndigoDarkBrush` per bordi
- Usa SymbolIcon standard Fluent
- Usa colori accento con parsimonia
- Mantieni CornerRadius a 8 per card
- Usa font `SemiBold` per titoli

### DON'T ❌

- Non usare colori saturi o cartoon
- Non usare icone colorate personalizzate
- Non usare sfumature o gradienti
- Non usare animazioni eccessive
- Non usare font decorativi
- Non usare emoji nelle interfacce

## 📐 Spaziatura e Layout

### Padding Standard

- **Card**: 16px
- **Contenuti**: 20px
- **Header**: 24px

### Margin Standard

- **Tra card**: 12px
- **Tra sezioni**: 16px
- **Page margins**: 24px

### CornerRadius Standard

- **Card**: 8px
- **Button**: 4px
- **Modal**: 8px

## 🌓 Temi Light/Dark

Il Control Center supporta automaticamente i temi di Windows.

### Colori Adattivi

I seguenti colori si adattano automaticamente:
- `TextFillColorPrimaryBrush`
- `TextFillColorSecondaryBrush`
- `ControlFillColorDefaultBrush`

I colori IndigoLab sono statici ma si integrano bene con entrambi i temi.

## 📱 Esempi Completi

### Pagina Standard

```xml
<Grid Padding="24">
    <StackPanel Spacing="16">
        <TextBlock Text="Page Title" 
                   Style="{StaticResource TitleTextBlockStyle}"/>
        <TextBlock Text="Page subtitle" 
                   Style="{StaticResource SubtitleTextBlockStyle}"/>
        
        <!-- Card -->
        <Border Background="{StaticResource IndigoWhiteBrush}"
                BorderBrush="{StaticResource IndigoDarkBrush}"
                BorderThickness="1"
                CornerRadius="8"
                Padding="16">
            <!-- Content -->
        </Border>
    </StackPanel>
</Grid>
```

### Card con Icona

```xml
<Grid Background="{StaticResource IndigoWhiteBrush}"
      BorderBrush="{StaticResource IndigoDarkBrush}"
      BorderThickness="1"
      CornerRadius="8"
      Padding="16">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
    
    <SymbolIcon Grid.Column="0" 
                Symbol="Accept" 
                Foreground="{StaticResource SecondaryAccentBrush}"
                FontSize="24"
                Margin="0,0,12,0"/>
    
    <TextBlock Grid.Column="1" 
               Text="Feature Name"
               Style="{StaticResource BodyStrongTextBlockStyle}"/>
</Grid>
```

## 🔗 Riferimenti

- [Microsoft Fluent Design System](https://www.microsoft.com/design/fluent/)
- [WinUI 3 Controls Gallery](https://learn.microsoft.com/en-us/windows/apps/design/controls/)
- [Color Contrast Checker (WCAG)](https://webaim.org/resources/contrastchecker/)

---

**© 2025 IndigoLab. Tutti i diritti riservati.**
