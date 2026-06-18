# Privacidade e dependencias

Este app usa hooks globais do Windows para contar eventos de mouse e teclado. A partir desta versao ele nao grava quais teclas foram pressionadas, nao grava texto digitado e nao envia dados para a internet.

## O que o app salva

Arquivo local:

```text
%LOCALAPPDATA%\BongoCatTracker\stats.json
```

Campos salvos:

- `Clicks`: total de cliques.
- `Keys`: total de teclas pressionadas.

## O que o app nao salva

- Conteudo digitado.
- Nome das teclas.
- Janela ativa.
- Aplicativo usado.
- Historico por horario.
- Dados de rede.

## Bibliotecas e APIs usadas

Frameworks do .NET/Windows:

- .NET 8
- WPF (`Microsoft.WindowsDesktop.App`)
- Windows Forms apenas para o icone da bandeja (`NotifyIcon`)
- `System.Text.Json` para salvar os contadores
- APIs nativas do Windows via `user32.dll` e `kernel32.dll`:
  - `SetWindowsHookEx`
  - `CallNextHookEx`
  - `UnhookWindowsHookEx`
  - `GetModuleHandle`

Ferramenta de desenvolvimento, nao usada pelo `.exe` em runtime:

- Python + Pillow no script `tools/prepare_assets.py`, apenas para limpar os PNGs da gatinha.

## Limite importante

Este app pode ser auditado e limitado para nao registrar conteudo sensivel. Nenhum app consegue garantir que outro software do computador, especialmente malware, nao esteja capturando teclado ou mouse. Para isso, use Windows Security/Defender atualizado, remova programas suspeitos e rode verificacoes antimalware.
