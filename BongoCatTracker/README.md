# Bongo Cat Tracker

App local para Windows que fica sempre no topo e conta cliques e teclas globalmente.
A janela principal e um widget transparente com a gatinha; os controles ficam no icone da bandeja.

## Instalar ou executar

Build pronta na raiz do repositorio:

- `releases/CatTracker.exe`: arquivo unico pequeno. Requer .NET Desktop Runtime 8 instalado.
- `releases/CatTracker-win-x64-self-contained.zip`: pacote maior com runtime incluido.

Na raiz do repositorio:

```powershell
dotnet publish .\BongoCatTracker\BongoCatTracker.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
.\BongoCatTracker\bin\Release\net8.0-windows\win-x64\publish\BongoCatTracker.exe
```

Dentro desta pasta:

```powershell
dotnet publish .\BongoCatTracker.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
.\bin\Release\net8.0-windows\win-x64\publish\BongoCatTracker.exe
```

Modo desenvolvimento:

```powershell
dotnet run --project .\BongoCatTracker.csproj
```

## Requisitos

- Windows.
- .NET 8 SDK para compilar.
- .NET Desktop Runtime 8 para executar a build dependente de runtime.

## Uso

- Arraste a gatinha para mover o widget.
- Clique no `X` da imagem para fechar.
- Use o menu do icone da bandeja para mostrar, pausar, zerar ou sair.
- A gatinha troca de sprite quando recebe clique ou tecla.
- Os olhos mudam de cor em intervalos aleatorios.

## Dependencias

- .NET 8.
- WPF para a janela transparente.
- Windows Forms apenas para o icone de bandeja.
- `System.Text.Json` para salvar contadores.
- APIs nativas `user32.dll` e `kernel32.dll` para hooks globais.
- Python + Pillow apenas no script de desenvolvimento `tools/prepare_assets.py`.

## Privacidade

O app nao grava texto digitado nem nomes de teclas. Ele salva apenas totais agregados de cliques e teclas.

Os dados ficam em:

```text
%LOCALAPPDATA%\BongoCatTracker\stats.json
```

Veja tambem `PRIVACY.md`.

## Arquivos para auditar

- `NativeInputHook.cs`: hooks globais de mouse/teclado.
- `MainWindow.xaml.cs`: incrementa contadores e controla a UI.
- `StatsStore.cs`: leitura/escrita do arquivo local.
- `InputStats.cs`: campos persistidos.
- `BongoCatTracker.csproj`: dependencias e assets.

## Publicar

```powershell
dotnet publish .\BongoCatTracker.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```
