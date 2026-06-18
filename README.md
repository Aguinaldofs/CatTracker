# CatTracker

Widget local para Windows inspirado em Bongo Cat. Ele fica sobre a tela, reage a cliques e teclas, mostra contadores visiveis e pode ser fechado pelo `X` da propria imagem ou pelo icone da bandeja.

## Instalar ou executar

### Opcao 1: baixar pronto

Baixe um destes arquivos na pasta `releases/`:

- [`CatTracker.exe`](releases/CatTracker.exe): arquivo unico pequeno. Requer .NET Desktop Runtime 8 instalado.
- [`CatTracker-win-x64-self-contained.zip`](releases/CatTracker-win-x64-self-contained.zip): pacote maior com o runtime necessario. Extraia o `.zip` e execute `BongoCatTracker.exe`.

### Opcao 2: compilar localmente

Clone o repositorio:

```powershell
git clone https://github.com/Aguinaldofs/CatTracker.git
cd CatTracker
```

Publique o executavel:

```powershell
dotnet publish .\BongoCatTracker\BongoCatTracker.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

Execute:

```powershell
.\BongoCatTracker\bin\Release\net8.0-windows\win-x64\publish\BongoCatTracker.exe
```

### Opcao 3: rodar em modo desenvolvimento

```powershell
dotnet run --project .\BongoCatTracker\BongoCatTracker.csproj
```

## Requisitos

- Windows.
- .NET 8 SDK para compilar ou rodar em modo desenvolvimento.
- .NET Desktop Runtime 8 para executar a build `--self-contained false`.

## Como usar

- Arraste a gatinha para mover o widget.
- Clique no `X` da imagem para fechar.
- Use o icone da bandeja para mostrar, pausar/retomar, zerar ou sair.
- O contador visivel mostra apenas totais de `Cliques` e `Teclas`.
- Os olhos mudam de cor em intervalos aleatorios: verde, amarelo, amarelo-esverdeado ou um olho de cada cor.

## Dependencias e APIs usadas

Runtime do app:

- .NET 8.
- WPF (`Microsoft.WindowsDesktop.App`) para a janela transparente.
- Windows Forms apenas para o icone da bandeja (`NotifyIcon`).
- `System.Text.Json` para salvar os contadores locais.
- APIs nativas do Windows via P/Invoke:
  - `user32.dll`: `SetWindowsHookEx`, `CallNextHookEx`, `UnhookWindowsHookEx`.
  - `kernel32.dll`: `GetModuleHandle`.

Ferramenta de desenvolvimento:

- Python + Pillow em `BongoCatTracker/tools/prepare_assets.py`, usado somente para converter os PNGs da pasta `ori/` em sprites com transparencia real. Isso nao roda no executavel final.

## Privacidade

O app usa hooks globais do Windows para contar eventos de mouse e teclado. Isso e necessario para que a gatinha reaja mesmo quando outro programa esta em foco.

Ele nao armazena trackers, texto digitado ou nomes de teclas. A versao atual salva apenas contadores agregados:

```json
{
  "Clicks": 123,
  "Keys": 456
}
```

Arquivo local salvo:

```text
%LOCALAPPDATA%\BongoCatTracker\stats.json
```

O app nao salva:

- conteudo digitado
- nome das teclas
- janela ativa
- aplicativo usado
- historico por horario
- eventos individuais
- dados de rede

Ele tambem nao tem codigo de rede, servidor, analytics ou telemetria.

## Arquivos para auditar se nao tiver confianca

Olhe estes arquivos primeiro:

- `BongoCatTracker/NativeInputHook.cs`: instala os hooks globais e emite apenas eventos agregados.
- `BongoCatTracker/MainWindow.xaml.cs`: incrementa `Clicks` e `Keys`, anima sprites, salva contadores e controla a bandeja.
- `BongoCatTracker/StatsStore.cs`: mostra exatamente onde e como o `stats.json` e salvo.
- `BongoCatTracker/InputStats.cs`: define os unicos campos persistidos.
- `BongoCatTracker/BongoCatTracker.csproj`: lista o tipo de app e assets incluidos, sem pacotes externos de runtime.
- `BongoCatTracker/PRIVACY.md`: resumo da politica de privacidade do app.

Uma busca util para verificar que nao ha rede no codigo:

```powershell
rg "HttpClient|WebClient|Socket|TcpClient|UdpClient|analytics|telemetry|Track|Upload|PostAsync|SendAsync" BongoCatTracker
```

## Limite importante

Este projeto consegue limitar e explicar o que o CatTracker faz. Ele nao consegue garantir que outro software do Windows, extensoes, drivers ou malware nao capturem teclado e mouse. Para isso, mantenha o Windows Security/Defender ativo, revise programas instalados e rode verificacoes antimalware.
