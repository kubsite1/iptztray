[English](README.md) · [中文](README.zh.md) · [हिन्दी](README.hi.md) · [Español](README.es.md) · [العربية](README.ar.md) · [Français](README.fr.md) · [বাংলা](README.bn.md) · **Português** · [Русский](README.ru.md) · [Bahasa Indonesia](README.id.md)

# IPTzTray

**Fuso horário do Windows e DNS de acordo com o IP público, inclusive com a VPN ligada.**

Nome: IP + Tz (time zone, fuso horário) + Tray (bandeja do Windows).

## Resumo

O IPTzTray define o fuso horário do Windows pelo IP público: o endereço pelo qual o tráfego realmente sai. Ligou uma VPN de outro país — o fuso horário muda para o do país de saída. Voltou ao IP de casa — o fuso horário volta a ser o de casa. Opcionalmente, o programa também fecha o vazamento de DNS por fora do túnel, que deixa o provedor ver os nomes dos sites com a VPN ligada.

Só o fuso horário muda; o relógio em si (a hora UTC) não é alterado. A tela mostra outra hora local, e sites, programas e serviços mostram a mesma hora.

Opcionalmente, e isto vem desligado, o programa também define o país do Windows pelo país do IP e devolve o seu quando esse ajuste é desligado. O idioma da interface e a região da conta Microsoft não mudam.

Assim o programa remove dois sinais visíveis de VPN: hora local que não confere com o IP e consultas DNS por fora do túnel. Outros sinais, como um IP de data center ou o idioma do sistema, ele não altera.

O programa vê o IP do sistema inteiro. Um proxy configurado só no navegador passa despercebido.

Grátis para qualquer uso. As condições para forks estão em “Licença e nome”.

## Para quê

- Sites, programas e serviços mostram a hora de casa, embora o tráfego já saia de outro país.
- A VPN foi desligada, mas o fuso horário continuou estrangeiro.
- O Windows define o fuso horário pela rede Wi‑Fi mais próxima, e não pelo IP por onde passa o tráfego.
- Com a VPN ligada, o Windows continua consultando o DNS do roteador. A consulta sai por fora do túnel e o provedor vê os nomes dos sites.

Os três primeiros itens são resolvidos pelo ajuste do fuso horário; o quarto, pela proteção contra vazamento de DNS.

- Provavelmente, isso também pode ajudar contra o bloqueio de contas da Claude (Anthropic) e de outros fornecedores de desenvolvimento de agentes de IA.

## Como começar

Windows 10 ou 11, 64 bits. Não precisa instalar nada: o programa é um único arquivo, [IpTzTray.exe](https://github.com/kubsite1/iptztray/releases/latest/download/IpTzTray.exe), de até 250 KB. Todas as versões estão na página [Releases](https://github.com/kubsite1/iptztray/releases).

1. Baixe e execute o `IpTzTray.exe`.
2. Permita os direitos de administrador: sem eles o Windows não deixa mudar o fuso horário.
3. Um ícone aparece na bandeja; a partir daí o fuso horário se ajusta sozinho.

Na primeira execução o programa memoriza o fuso horário atual como o de casa e ativa a própria inicialização ao entrar no Windows. Em cada execução com direitos de administrador, ele desativa o ajuste automático de fuso horário do Windows (o serviço `tzautoupdate`); caso contrário, o Windows define o fuso horário pelo Wi‑Fi mais próximo e o valor diverge do programa.

Se os direitos forem negados, o ícone aparece, mas o fuso horário não muda. No menu fica “Executar como administrador”.

Ao sair do programa e ao encerrar a sessão do Windows (sair da conta ou desligar), o fuso horário de casa volta, mesmo com a troca automática em pausa. Isso pode ser desativado nas configurações.

## Ícone

O ícone mostra o deslocamento do fuso horário atual do Windows: `+3`, `-5`, `+5:30`.

<p>
<img src="screenshots/icon-match.png" width="56" alt="Verde">
<img src="screenshots/icon-mismatch.png" width="56" alt="Laranja">
<img src="screenshots/icon-wait.png" width="56" alt="Cinza">
<img src="screenshots/icon-error.png" width="56" alt="Vermelho">
</p>

- **Verde** — o fuso horário do Windows confere com o do IP.
- **Laranja** — não confere: a VPN já está ligada, mas o fuso horário ainda é o antigo, ou a troca automática está em pausa.
- **Cinza** — o IP ainda não foi verificado.
- **Vermelho** — não foi possível saber o IP ou o fuso horário. O fuso horário não muda.

Clique duplo no ícone: verificar agora. Uma segunda cópia do programa não inicia: na bandeja fica a primeira.

## Vazamento de DNS

A verificação vem desligada. Ela é ativada nas configurações.

O programa verifica se o Windows consulta o DNS não só pelo túnel, mas também em outro adaptador ativo, quase sempre o roteador. Adaptadores virtuais, Bluetooth e TAP ficam de fora. A verificação é uma consulta de um nome aleatório `.invalid` a esse DNS. Se ele responder enquanto a consulta DNS paralela do Windows estiver ligada, os nomes dos sites podem sair por fora da VPN.

![Janela de vazamento de DNS](screenshots/dns.png)

Quando há vazamento, o programa mostra esta janela. Fechar a janela e “Agora não” não mudam nada. “Corrigir”, enquanto a VPN estiver ligada:

- desliga a consulta DNS paralela do Windows;
- memoriza o DNS do adaptador lateral e coloca no lugar o DNS do túnel;
- bloqueia consultas de saída para o DNS antigo e para DNS públicos conhecidos, inclusive DNS-over-TLS e DNS-over-HTTPS;
- envia todos os nomes para o DNS do túnel.

Na rede de casa, tudo volta a ser como antes.

Modos:

- **Não verificar** — remove a proteção já ligada e restaura as configurações anteriores.
- **Uma vez ao iniciar** — pergunta só se houver vazamento naquele momento. Uma VPN ligada depois não é verificada sozinha.
- **Vigiar e corrigir sozinho** — corrige sem perguntar sempre que a VPN é ligada e restaura as configurações em casa.

Para esta função, a VPN conta como ligada quando a rota principal passa por um adaptador de túnel, WireGuard, Amnezia ou TAP. A troca de fuso horário não depende disso: ela olha só o IP público. Se o adaptador da VPN não se parecer com nenhum desses, o fuso horário muda mesmo assim, mas o programa pode não corrigir o DNS sozinho.

Depois da correção, o programa reinicia o serviço de DNS. Se a internet cair, reinicie o computador. Às vezes a mudança só passa a valer depois de reiniciar.

Num teste de vazamento externo pode continuar aparecendo o DNS da própria VPN, por exemplo Cloudflare no país de saída. Esse é o DNS do túnel, não o do roteador de casa.

## Referência

### Menu

![Menu](screenshots/menu.png)

Três linhas cinzas no topo: local, IP com o nome IANA do fuso horário, fuso horário atual do Windows. Se o fuso horário atual não conferir com o do IP, a terceira linha mostra os dois. Uma linha longa é cortada; o texto completo fica na dica ao passar o mouse.

- **Verificar agora** — uma verificação extra do IP na hora.
- **Pausar a troca automática** — as verificações continuam, o fuso horário não muda. “Continuar a troca automática” tira a pausa e ajusta o fuso horário na hora.
- **Voltar ao fuso de casa** — define o fuso horário de casa memorizado e ativa a pausa; senão a próxima verificação do IP o mudaria na hora.
- **Remover a fixação** — aparece só se o IP atual tiver uma fixação (veja “Configurações”).
- **Corrigir vazamento de DNS** — aparece só se um vazamento já foi encontrado.
- **Executar como administrador** — aparece só sem direitos de administrador.
- **Copiar IP** — copia o IP público para a área de transferência.
- **Configurações**, **Sobre**, **Sair**.

### Configurações

![Configurações](screenshots/settings.png)

- **Ao sair, voltar ao fuso de casa** — ligado por padrão.
- **Memorizar o fuso atual** — torna o fuso horário atual o de casa na hora e memoriza o país atual do Windows.
- **Mudar o país conforme o IP** — desligado. Define o país do Windows pelo país do IP público. Direitos de administrador não são necessários.
- **Ao desligar ou mudar, voltar ao seu país** — ligado. O seu país volta ao desmarcar a caixa, na pausa, com “Voltar ao fuso de casa” e ao sair.
- **Iniciar ao entrar no Windows** — uma tarefa do Agendador de Tarefas chamada `IpTzTray`, com direitos de administrador, sem pedir os direitos de novo. Desmarcar a caixa apaga a tarefa.
- **Quanto esperar a resposta do serviço** — 5, 10 ou 20 segundos, 5 por padrão. É o limite de cada etapa: um para verificar o IP e outro para pedir o fuso horário.
- **Vazamento de DNS** — os três modos da seção “Vazamento de DNS”.
- **Idioma** — por padrão, o idioma do Windows. Se ele não estiver na lista, inglês. “Salvar” aplica o idioma na hora, sem reiniciar.

Idiomas da interface: English, 中文, हिन्दी, Español, العربية, Français, বাংলা, Português, Русский, Bahasa Indonesia. Os nomes dos fusos horários vêm do Windows, por isso aparecem no idioma do sistema.

Na parte de baixo da janela: o caminho da pasta `%AppData%\IpTzTray` e o botão “Abrir”. Na pasta ficam `settings.json` e o registro `log.txt`. O registro é escrito em russo. Quando o arquivo cresce, a parte anterior é guardada em `log.txt.old`.

**Fixação** — um fuso horário próprio para um IP específico. Enquanto ela existir, o programa define esse fuso horário para esse IP e não consulta o serviço de geolocalização. Esta versão não tem janela para fixar. Dá para escrever a fixação em `settings.json`, com o programa fechado, no campo `Pins`:

```json
"Pins": { "203.0.113.10": "Eastern Standard Time" }
```

O fuso horário é indicado pelo nome do Windows. `tzutil /l` mostra a lista de nomes.

### Linha de comando

- `IpTzTray.exe --check` — mostra o IP e o fuso horário, não muda nada.
- `IpTzTray.exe --dns` — mostra se há vazamento de DNS visível, não muda nada.
- `IpTzTray.exe --dry` — bandeja em modo de prévia: fuso horário e DNS não mudam, as configurações vão para uma pasta temporária.
- `IpTzTray.exe --map Europe/Amsterdam` — mostra o fuso horário do Windows para um nome IANA.
- `IpTzTray.exe --selftest` — verifica a tabela de fusos horários, a leitura das respostas e o salvamento das configurações; não muda o sistema.

## Como funciona

- **IP público**: consultado ao mesmo tempo em três serviços: Cloudflare (`1.1.1.1`), ipinfo.io e ip-api.com. Vale o endereço informado por pelo menos dois. A resposta de um único serviço só é aceita se os outros não responderem. Se as respostas divergirem, a verificação se repete. Endereços privados são descartados.
- **Frequência**: enquanto o IP não muda, a cada 10 segundos, mais ou menos. Depois de uma mudança de rede, a primeira verificação vem em 5 segundos, depois a cada 5 segundos por cerca de 25 segundos, e então de novo a cada 10 segundos.
- **Fuso horário**: consultado em ordem em ip-api.com, ipwho.is, ipinfo.io e geojs.io, só quando o IP muda. O nome IANA é convertido em fuso horário do Windows pela tabela Unicode CLDR e definido com `tzutil`.
- **Fuso horário de casa**: o fuso horário do Windows na primeira execução, antes do primeiro ajuste. Se nessa hora já estava um fuso estrangeiro, é ele que fica memorizado. Corrige-se com “Memorizar o fuso atual”.
- **O seu país**: o país do Windows na primeira execução. O mesmo botão também o atualiza.
- **O que sai**: os serviços que detectam o IP veem seu IP público a cada verificação. O serviço de geolocalização recebe o IP só quando o endereço muda.

## Limitações

- Um proxy configurado só no navegador não é detectado: o programa vê o IP do sistema inteiro.
- A cidade do data center da VPN na base de geolocalização às vezes não confere com o fuso horário certo. Fica o fuso horário que o serviço devolveu.
- Sem direitos de administrador, nem o fuso horário nem o DNS mudam. O país do Windows ainda pode mudar.
- A região da conta Microsoft não muda. Um IP fixado não tem código de país, então o programa não muda o país para ele.

## Compilação

C#, Windows Forms, o .NET Framework que já vem no Windows, x64. Só é preciso o compilador `csc`, que vem com o Windows. Não precisa de Visual Studio.

```
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Resultado: `publish\IpTzTray.exe` e o ícone `publish\app.ico`.

- `src` — código-fonte.
- `data/windowsZones.xml` — tabela de fusos horários Unicode CLDR.
- `build.ps1` — script de compilação.
- `app.manifest` — manifesto do Windows.

O programa foi escrito e compilado com o Grok 4.7.

## Licença e nome

Grátis para qualquer uso, inclusive comercial.

Forks e versões modificadas têm duas condições: não remover a autoria e não usar o nome IPTzTray. Texto completo em [LICENSE](LICENSE).

Autor: hello@kubsite.ru  
Página do programa: https://github.com/kubsite1/iptztray

Agradecemos o apoio do agregador de mensagens [wamm.chat](https://wamm.chat).
