# 🎬 YTSpotifySync

> **Aplicativo Windows moderno (WinUI 3 / .NET 10) para gerenciar, sincronizar e baixar vídeos e transmissões ao vivo do seu canal no YouTube para o Spotify for Creators / Spotify Podcasts.**

![Plataforma](https://img.shields.io/badge/Plataforma-Windows%2010%20%7C%2011-blue)
![Framework](https://img.shields.io/badge/.NET-10.0-purple)
![UI](https://img.shields.io/badge/UI-WinUI%203%20(Windows%20App%20SDK)-0078D4)
![Padrão](https://img.shields.io/badge/Arquitetura-MVVM%20Toolkit-green)

---

## 🌟 Visão Geral

O **YTSpotifySync** foi desenvolvido para criadores de conteúdo que possuem um canal ativo no **YouTube** e um podcast correspondente no **Spotify**. O aplicativo identifica automaticamente todos os vídeos normais, transmissões ao vivo (Lives) e shorts que ainda **não foram publicados no Spotify**, permitindo baixá-los em alta qualidade (MP4) e direcionando o usuário diretamente para a tela de novo episódio do **Spotify for Creators**.

---

## 🚀 Principais Funcionalidades

- **Dashboard Inteligente:**
  - Contadores em tempo real de vídeos no YouTube, episódios no Spotify, itens pendentes e já sincronizados.
  - Sincronização paralela em lote com feedback visual de progresso.
- **Mecanismo de Comparação (SyncEngine):**
  - Normalização fonética e remoção de acentos/pontuação (`TitleNormalizer`).
  - Correspondência exata e por similaridade entre os títulos dos vídeos e episódios.
  - Filtros por status (*Todos*, *Apenas Pendentes*, *Já Sincronizados*).
  - Filtros por tipo de conteúdo (*Uploads*, *Lives/Ao Vivo*, *Shorts*).
  - Busca textual instantânea e seleção individual ou em massa.
- **Fila de Downloads (yt-dlp):**
  - Download sequencial ou em lote no formato MP4 (melhor vídeo + melhor áudio).
  - Acompanhamento de porcentagem de download em tempo real.
  - Download automático de miniaturas em alta resolução.
  - Botão de abertura rápida do arquivo e pasta no Windows Explorer.
  - Atalho com 1 clique para abrir a tela de publicação de novo episódio no **Spotify for Creators**.
- **Design Moderno:**
  - Interface nativa em **WinUI 3** com suporte ao material **Mica**, temas Claro e Escuro, animações suaves de transição e iconografia Segoe Fluent.
- **Configurações Persistentes:**
  - Armazenamento local seguro das credenciais em `%APPDATA%/YTSpotifySync/settings.json`.
  - Botões para teste de conexão individual com a API do YouTube e do Spotify.

---

## 🏗️ Arquitetura do Projeto

O projeto foi construído respeitando as melhores práticas do ecossistema .NET:

- **Linguagem & Runtime:** C# 13 / .NET 10 (`net10.0-windows10.0.26100.0`)
- **Padrão Arquitetural:** MVVM (Model-View-ViewModel) com `CommunityToolkit.Mvvm`
- **Injeção de Dependência:** `Microsoft.Extensions.DependencyInjection`
- **APIs de Terceiros:**
  - `Google.Apis.YouTube.v3` — Consulta em lote por playlist de uploads para preservação de cota diária.
  - `SpotifyAPI.Web` — Autenticação OAuth Client Credentials com paginação de episódios.
  - `yt-dlp` — Download de fluxos de vídeo/áudio e fusão via ffmpeg.

### Estrutura de Pastas

```text
YTSpotifySync/
├── YTSpotifySync.slnx
├── README.md
└── src/
    └── YTSpotifySync/
        ├── App.xaml / App.xaml.cs          # Ponto de entrada e Container de Injeção de Dependências
        ├── MainWindow.xaml / .cs           # Janela principal com NavigationView e Mica Backdrop
        ├── Helpers/
        │   ├── TitleNormalizer.cs          # Normalização de strings para cruzamento de títulos
        │   ├── CommonConverters.cs         # Conversores de dados para XAML (visibilidade, contadores, etc.)
        │   └── BoolToSeverityConverter.cs  # Conversão de status para InfoBar
        ├── Models/
        │   ├── Enums.cs                    # VideoType e SyncStatus
        │   ├── VideoItem.cs                # Modelo de vídeo do YouTube
        │   ├── EpisodeItem.cs              # Modelo de episódio do Spotify
        │   ├── SyncResult.cs               # Resultado do pareamento entre YouTube e Spotify
        │   └── DownloadTask.cs             # Tarefa de download gerenciada na fila
        ├── Services/
        │   ├── SettingsService.cs          # Persistência de configurações em JSON
        │   ├── YouTubeService.cs           # Cliente da YouTube Data API v3
        │   ├── SpotifyService.cs           # Cliente do Spotify Web API
        │   ├── SyncEngine.cs               # Comparador e classificador de conteúdo
        │   ├── DownloadService.cs          # Executor de processo do yt-dlp
        │   └── BrowserLauncher.cs          # Integração com navegador e Explorer
        ├── ViewModels/
        │   ├── DashboardViewModel.cs
        │   ├── ComparisonViewModel.cs
        │   ├── DownloadViewModel.cs
        │   └── SettingsViewModel.cs
        └── Views/
            ├── DashboardPage.xaml / .cs
            ├── ComparisonPage.xaml / .cs
            ├── DownloadPage.xaml / .cs
            └── SettingsPage.xaml / .cs
```

---

## ⚙️ Pré-Requisitos

1. **Sistema Operacional:** Windows 10 (versão 19041+) ou Windows 11.
2. **.NET 10 SDK:** Instalado no sistema.
3. **yt-dlp e ffmpeg:**
   - O aplicativo localiza o `yt-dlp.exe` automaticamente se estiver no `PATH` do sistema ou na pasta de downloads do usuário. Você também pode apontar o caminho diretamente na aba de **Configurações**.

---

## 🔑 Configuração das Chaves de API

No aplicativo, acesse a aba **Configurações** (ícone de engrenagem) e informe:

1. **YouTube Data API:**
   - **YouTube API Key:** Chave obtida no [Google Cloud Console](https://console.cloud.google.com/) com a *YouTube Data API v3* habilitada.
   - **ID do Canal:** ID do canal do YouTube (ex: `UCsF18bQeCiSw04faoERqGug`).
2. **Spotify Developer:**
   - **Client ID & Client Secret:** Obtidos no [Spotify Developer Dashboard](https://developer.spotify.com/dashboard).
   - **ID do Podcast (Show ID):** ID do seu show no Spotify (ex: `3Tx5j7earRSezANSH46i68`).

Clique em **Testar Conexão YouTube** e **Testar Conexão Spotify** para validar o acesso às APIs.

---

## 💻 Compilação e Execução

Para compilar a aplicação:

```powershell
dotnet build src/YTSpotifySync/YTSpotifySync.csproj
```

Para compilar a versão otimizada de Release:

```powershell
dotnet build src/YTSpotifySync/YTSpotifySync.csproj -c Release
```

Para executar a aplicação:

```powershell
dotnet run --project src/YTSpotifySync/YTSpotifySync.csproj
```

---

## 📄 Licença

Distribuído sob licença proprietária de uso pessoal.
