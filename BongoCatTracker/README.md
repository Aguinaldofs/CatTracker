# Bongo Cat Tracker

App local para Windows que fica sempre no topo e conta cliques e teclas globalmente.
A janela principal e um widget transparente com a gatinha; os controles ficam no icone da bandeja.

## Rodar

Use o executavel publicado:

```powershell
.\bin\Release\net8.0-windows\win-x64\publish\BongoCatTracker.exe
```

Ou rode em modo desenvolvimento:

```powershell
dotnet run --project .\BongoCatTracker.csproj
```

## Uso

- Arraste a gatinha para mover o widget.
- Use o menu do icone da bandeja para mostrar, pausar, zerar ou sair.
- A gatinha troca de sprite quando recebe clique ou tecla.

## Privacidade

O app nao grava texto digitado nem nomes de teclas. Ele salva apenas totais agregados de cliques e teclas.

Os dados ficam em:

```text
%LOCALAPPDATA%\BongoCatTracker\stats.json
```

Veja tambem `PRIVACY.md`.

## Publicar

```powershell
dotnet publish .\BongoCatTracker.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```
