# AGENTES.md — Contexto e continuidade do cliente Unity para L2J

Atualizado em 14/10/2026. Idioma de trabalho: PT-BR.

Este documento registra o contexto da sessão, as decisões, correções, evidências e pendências. Não significa que todos os problemas históricos foram resolvidos. Preserve as instruções AGENTS.md aplicáveis e as alterações anteriores do usuário.

## 1. Projetos relacionados e objetivo

| Papel | Local |
|---|---|
| Repositório atual | D:\Documentos\Projetos\Unity-Client-for-L2J |
| Projeto Unity | l2-unity |
| Servidor usado nos testes | D:\Documentos\Servers\L2J4Team_Unity |
| Contexto complementar | [AGENTES.md do servidor](D:/Documentos/Servers/L2J4Team_Unity/AGENTES.md) |
| Alternativa C1 mencionada pelo usuário | D:\Documentos\Servers\L2J_Mobius-master-L2J_Mobius_C1_HarbingersOfWar |

Objetivo: executar o cliente Unity, conectar ao servidor local e corrigir falhas reais, sem silenciar ou ocultar avisos.

O alvo efetivo da integração é o L2J4Team, um Interlude/aCis modificado. GameManager usa protocolo 746. O servidor aceita revisões 730–746. A alternativa Mobius C1 foi discutida como contexto; não há adaptação C1 implantada e validada nesta sessão. Reduzir o conteúdo visual de Interlude não adapta automaticamente os pacotes para C1.

O Unity é necessário para desenvolvimento e compilação. O executável gerado roda sem o Editor instalado.

## 2. Ambiente confirmado

- Unity 6.6, versão 6000.6.5f1, revisão 3ff58d469c8a.
- Editor: C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe.
- URP e Shader Graph: 17.6.0.
- ProBuilder: 6.1.2.
- Input System: 1.20.0.
- VFX Graph: 17.6.0.
- FMOD integrado em Assets/Plugins/FMOD.
- Windows, PowerShell; fuso do usuário America/Sao_Paulo.

Fontes da versão: [ProjectVersion.txt](l2-unity/ProjectSettings/ProjectVersion.txt) e [manifest.json](l2-unity/Packages/manifest.json).

Há numerosas alterações anteriores no repositório. Não restaurar arquivos indiscriminadamente, nem misturar correções novas com reversões do trabalho já feito.

## 3. Cena inicial e execução

Abrir [Menu.unity](l2-unity/Assets/Resources/Scenes/Menu.unity), e não a cena padrão Untitled.

Fluxo principal: Menu → l2_lobby → Game em cache → cena do mapa correspondente às coordenadas do personagem.

- Menu deve ser a primeira cena habilitada no build.
- A pasta Scenes/Menu pode estar vazia: o arquivo Menu.unity fica diretamente em Scenes.
- Game sozinho não contém todos os cenários do mundo.
- Durante importação inicial, indexação de assets e shaders podem demorar. Verificar o progresso e o Console antes de presumir travamento.

Referências: [SceneLoader.cs](l2-unity/Assets/Scripts/Game/Manager/SceneLoader.cs), [GameManager.cs](l2-unity/Assets/Scripts/Game/Manager/GameManager.cs) e [LoginCameraManager.cs](l2-unity/Assets/Scripts/Camera/LoginCameraManager.cs).

## 4. Histórico de migração para Unity recente

O usuário relatou e pediu correção de problemas nas seguintes áreas:

- Dependência local ausente com.antigravity.ide/package.json.
- APIs obsoletas do FMOD: GetInstanceID, TreeView, TreeViewItem, callbacks de hierarquia e conversão de identificadores.
- Colisão de nome System com Serializable em fmod_studio.cs.
- Passes URP que ainda sobrescreviam OnCameraSetup/Execute.
- APIs de busca de objetos, avisos de desenvolvimento e componentes do Atmospheric Height Fog.
- Parsing de dados, componentes de áudio de animação, janelas/UI, ocultação de membros e profundidade de serialização de ItemName.
- Crashes de workers de importação e referências de materiais/shaders.

Não assumir que todo aviso desses grupos foi eliminado. Conferir o diff e os logs específicos antes de declarar uma correção histórica como concluída.

Referências relevantes:
- Assets/Plugins/FMOD/src e src/Editor.
- Assets/Plugins/Atmospheric Height Fog.
- [L2RenderGraphPass.cs](l2-unity/Assets/Scripts/Rendering/L2RenderGraphPass.cs).
- Rendering/L2FxCompositor/L2FxCompositorRenderPass.cs e L2SkyRenderPass.cs.
- Rendering/L2YebisPost/L2YebisPostRenderPass.cs.
- Rendering/Terrain/L2TerrainOverlayRenderPass.cs.
- Database/Model/ItemName.cs.
- UI/L2Window.cs e UI/Game/Merchant/MultiSellWindow.cs.
- Audio/Animation/CharacterAnimationAudioHandler.cs.

A renderização foi migrada para Render Graph. Não recolocar os overrides incompatíveis para contornar erros.

## 5. Crash após a logo: causa e correção confirmadas

Sintoma: player abria, mostrava a logo, carregava Game/level2 e fechava com erro nativo de leitura de dados serializados, incluindo Position out of bounds.

A investigação isolou grupos de MonoBehaviours em cópias diagnósticas. O componente Geodata foi identificado como causador do crash: caches de execução estavam participando da serialização.

Correções em [Geodata.cs](l2-unity/Assets/Scripts/Terrain/Geodata/Geodata.cs):
- _mapsToLoad, _mapsOrigin, _geodata e _loaded usam NonSerialized.
- A configuração necessária continua serializada.
- OnDestroy limpa o cache com verificação de nulo e libera a referência da instância correta.

Também foi marcado NonSerialized o cache _atkClipLengths em [BaseAnimationController.cs](l2-unity/Assets/Scripts/Animation/BaseAnimationController.cs).

Não tratar dicionários como invariavelmente não serializáveis nessa versão do Unity. Caches transitórios devem ser explicitamente excluídos.

Metadados corrigidos durante essa etapa:
- Data/Maps/17_25/Meta/17_25.t3d.meta: conflito removido preservando o GUID correto.
- Data/SysTextures/Icon/skill1191.png.meta: conteúdo vazio restaurado com o GUID original.

Validação: o build 20261014-081921 carregou lobby e Game e permaneceu aberto sem repetir o crash ou a divergência de layout investigada.

### Artefatos de investigação

Dentro de l2-unity:
- Logs/GeodataFixedBuild-Retry.log.
- Logs/GeodataFixedPlayer.log.
- Logs/MonoIsolationResults.json e Logs/IsolateMono.py.
- Builds/Diagnostics: cópias com grupos de componentes removidos, testes sem VFX e outros testes.

Essas cópias não são builds de distribuição. Alguns experimentos misturaram assemblies e dados antigos propositalmente. Não entregar tais executáveis, nem trocar apenas DLLs entre builds: dados serializados, metadados e assemblies precisam corresponder.

UnityPy foi usado temporariamente para inspeção de arquivos Unity, com dependências em TEMP/L2AssetInspection. Isso não é dependência de produção.

## 6. Dois defeitos visuais do login: ambos corrigidos

O usuário apontou dois problemas distintos: água ausente e um bloco rosa.

### Bloco rosa

A auditoria identificou Brushes/Brush4, próximo da câmera Login: seis faces sem referência de material. O cubo foi removido do prefab do lobby.

[L2BrushBuilder.cs](l2-unity/Assets/Scripts/Tools/Terrain/Builder/L2BrushBuilder.cs) passou a ignorar faces de construção sem referência de textura, emitindo aviso. Não transformar materiais texturados ausentes em faces invisíveis para esconder falhas.

Arquivo: [Brushes.prefab](l2-unity/Assets/Resources/Data/Maps/l2_lobby/Brushes.prefab).

### Água

O objeto Water (1) usava uma superfície pequena e material de exemplo. Foi restaurado o shader nativo L2/World/WaterModulate2X, copiando as propriedades de Ertheia_water_sh.mat para o material específico do lobby.

A superfície Water (1) foi ampliada para 2048 × 2048 unidades, preservando a altura existente. As UVs do shader usam coordenadas do mundo; ampliar a superfície não amplia as ondas.

Arquivos:
- [l2_lobby.unity](l2-unity/Assets/Resources/Scenes/l2_lobby.unity).
- [lobby_ocean.mat](l2-unity/Assets/Resources/Data/Effects/lobby_water/lobby_ocean.mat).
- Assets/Resources/Data/Shaders/World/L2WaterModulate2X.shader e L2WaterCommon.hlsl.

A intervenção foi no oceano do lobby; não declarar que todos os materiais de água do jogo foram substituídos.

### Validação visual e limites

[L2LobbyVisualAudit.cs](l2-unity/Assets/Editor/L2LobbyVisualAudit.cs) oferece:
- Run: auditoria de materiais/câmeras e captura anterior.
- Repair: alteração de prefab, material e cena, mais captura posterior.
- RepairAndBuild: reparo seguido de build incremental.

Repair modifica assets; não é uma auditoria somente de leitura.

Artefatos: Logs/LobbyBefore.png, LobbyAfter.png, LobbyVisualAudit.txt, LobbyCapture.log e LobbyRepairBuild.log.

A comparação encontrou 96.892 pixels magenta antes e zero depois; a água ficou visível na captura posterior. A captura isolada do Editor não inicializa toda a iluminação de execução e mostra o cenário escuro. Não usá-la como prova de equivalência completa de iluminação em jogo.

Salvar a cena/prefab no Unity recente também atualizou formatos de serialização de meshes. Preserve GUIDs e confira essas alterações ao revisar o diff.

## 7. Build de referência e uso do CLI

Último build do cliente validado nesta sessão:

l2-unity/Builds/WindowsClean/20261014-124721/L2-Unity.exe

BuildSummary.txt: Succeeded, zero erros e 139 avisos. Esses avisos permanecem; não foram ocultados. O teste de inicialização carregou o lobby e Game, permaneceu responsivo e não apresentou as exceções/crashes pesquisados. Esse teste não equivale a validar todo combate, equipamento ou mapas.

Referências:
- [L2CleanWindowsBuild.cs](l2-unity/Assets/Editor/L2CleanWindowsBuild.cs).
- Logs/LobbyRepairBuild.log.
- Logs/LobbyFixedPlayer.log.
- Builds/WindowsClean/20261014-124721/BuildSummary.txt.

Build usa CleanBuildCache; BuildIncremental usa BuildOptions.None. Ambos criam pasta de saída datada e resumo.

Nesta instalação, o Unity Personal rejeitou -batchmode por ausência do entitlement com.unity.editor.headless, com saída 198. Usar o Editor normal por CLI, com -executeMethod e -quit. Não repetir a tentativa de headless como se já estivesse autorizada pela licença.

Exemplo com os caminhos atuais:

    $editorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe'
    $projectPath = 'D:\Documentos\Projetos\Unity-Client-for-L2J\l2-unity'
    $clientBuildArgs = @('-projectPath', $projectPath, '-executeMethod', 'L2CleanWindowsBuild.BuildIncremental', '-quit', '-logFile', "$projectPath\Logs\ClientBuild.log")
    $clientBuildProcess = Start-Process -FilePath $editorPath -ArgumentList $clientBuildArgs -WindowStyle Hidden -PassThru
    $clientBuildProcess.WaitForExit()

- Verificar se o projeto já está aberto e se há UnityLockfile. Não encerrar o Editor do usuário indiscriminadamente.
- Caches/licenciamento do Editor escrevem fora do repositório; no ambiente Codex isso exigiu execução com permissões apropriadas.
- Builds podem passar longos períodos lendo assets sem atualizar o último trecho do log. Conferir atividade do processo e progresso; não cancelar por troca de foco do Codex.
- Esperar Succeeded e conferir o novo executável, não executar por engano um build antigo.
- O build 20261013-212728 e executáveis antigos ainda tiveram o crash: não os indicar como corrigidos.
- Não publicar logs brutos de licenciamento: podem conter tokens e identificadores sensíveis.

## 8. Conexão local e Guard

Configuração efetiva:
- Login: 127.0.0.1:2106.
- Game: endereço anunciado pelo servidor, agora 127.0.0.1:7777.
- Comunicação interna Game/Login: 127.0.0.1:9014.
- Protocolo: 746.

Networking.prefab define login local. Menu.unity contém override da porta do GameClient para 7777; ServerSelectWindow também usa o endereço/porta recebidos do servidor. O valor 11000 no prefab base do GameClient não representa a porta efetiva da cena Menu.

Servidor: AllowGuardSystem=False e StreamCryptEnabled=False em Protect.ini. A criptografia padrão necessária ao protocolo continua presente; Guard.jar não foi removido.

O servidor tinha uma chamada incondicional de HWID no EnterWorld. A flag false evitava inicializar o gerenciador, mas a chamada acessava uma coleção nula. Foram corrigidas condições em EnterWorld, desconexão, despacho de resposta do Guard e comando administrativo de HWID.

Ver o [contexto do servidor](D:/Documentos/Servers/L2J4Team_Unity/AGENTES.md) para a compilação e instalação dessa correção.

## 9. Personagem no céu: causa confirmada e decisão pendente

Evidência dos logs e de consulta somente de leitura ao banco:
- Login anterior: mapa 17_25, coordenadas (-84037, 242782, -3730).
- Login atual: mapa 22_21, coordenadas (69145, 118971, -3472).
- Erros: prefab 22_21 não encontrado e cena 22_21 ausente do build.

Os registros consultados pertencem a personagens distintos, com posições salvas diferentes. O primeiro carregou uma cidade porque a região estava incluída.

Mapas presentes em Assets/Resources/Data/Maps e Resources/Scenes:
- 16_24, 16_25, 17_24 e 17_25: região da Ilha Falante.
- l2_lobby: cenário de login.

O mapa 22_21 não está apenas desabilitado no build: os assets dele também estão ausentes. Adicionar somente seu nome à lista de cenas não cria o terreno.

Consulta feita com prepared statement, sem alterar dados. Nenhuma posição de personagem foi modificada. Não registrar nomes, contas, IDs de personagens ou credenciais neste documento.

Opções em aberto:
1. Preparar testes em uma região incluída, como Ilha Falante.
2. Obter/importar os assets da região 22_21 e configurar cena, materiais e carregamento.

O usuário ainda não escolheu uma dessas alternativas. Não simular o mapa ausente com um plano genérico ou deslocar silenciosamente personagens.

Falha de fluxo a revisar: LoadSWMapRoutine conclui a entrada após LoadSceneAdditiveIfNeeded mesmo quando o mapa falhou. Uma correção futura deve verificar o carregamento real e apresentar erro claro/recuperação; ainda não foi aplicada.

Referências: SceneLoader.cs, Utils/VectorUtils.cs e Terrain/MapLoader.cs.

## 10. Outras pendências observadas no login em jogo

Estas foram observadas; conferir a atualização de auditoria abaixo antes de tratá-las como abertas:

- Os caminhos absolutos de outro ambiente em UnityReplaySession, UnityChainLog, ReplayAttackTrace, NpcCombatTrace, DropMeshLifetimeLog, DocExtractorParticleSnapshotLogger e DocExtractorDropAdenaCoinLogger foram corrigidos para Application.persistentDataPath na auditoria de 14/10. O teste de combate conectado ainda é necessário.
- NewMagicSkillsState.HandleEvent apresenta NullReferenceException no fluxo de magia.
- O item 17 aparece como equipamento não encontrado; investigar o catálogo e a interpretação de munição/mão esquerda antes de adicionar item fictício.
- Há entidades de NPC não encontradas após tentativas assíncronas.

Para os logs, usar localização de escrita apropriada ao usuário atual, como Application.persistentDataPath, preservando o diagnóstico. Não simplesmente capturar e ignorar exceções.

Referências:
- Game/ChainLog/UnityReplaySession.cs.
- Game/ChainLog/UnityChainLog.cs.
- Controller/StateMachine/State/NewMagicSkillsState.cs.
- Logs do player em %USERPROFILE%\AppData\LocalLow\DefaultCompany\L2-Unity\Player.log e Player-prev.log.

Os logs são rotacionados ao reabrir o cliente. Preservar evidências relevantes antes de novos testes, sem expor credenciais ou dados pessoais.

## 11. Procedimento para continuar

1. Ler este documento e o equivalente do servidor.
2. Identificar qual build/processo o usuário está usando.
3. Diferenciar falha de conexão/Guard, falha de pacote, falta de assets e falha de renderização.
4. Para cenário ausente, confirmar coordenadas, mapa calculado, presença dos assets e inclusão da cena no build.
5. Resolver a escolha pendente de região de testes/importação antes de alterar coordenadas.
6. Corrigir os caminhos de logs e os erros de magia com reprodução e validação apropriadas.
7. Recompilar pelo CLI autorizado e testar o artefato completo.
8. Atualizar ambos os documentos quando mudar protocolo, build de referência, configuração local ou resultado de validação.

Regras da sessão: respostas em PT-BR; diagnóstico, correção, riscos quando pertinentes e arquivos alterados; correções reais, sem silenciar avisos; não introduzir dependências desnecessárias; não solicitar novamente autorização já concedida; não apagar alterações anteriores. Não criar novos chats, automações ou subagentes por iniciativa própria.

Scripts auxiliares em l2-unity/Logs são ferramentas de manutenção desta sessão, não parte do produto: ConfigureLocalServer.ps1, PatchDisabledGuard.ps1 e ReadCharacterLocations.java. O primeiro altera a instalação do servidor e recompila; o segundo aplica substituições específicas e não deve ser reaplicado cegamente; o terceiro somente consulta posições. Conferir o conteúdo e o estado atual antes de reutilizá-los.

## 12. Auditoria de opcodes do fork Interlude — 14/10/2026

Relatório e fontes exatas: [auditoria](l2-unity/docs/protocol-audit/README.md), [servidor → cliente](l2-unity/docs/protocol-audit/server-to-client.csv), [cliente → servidor](l2-unity/docs/protocol-audit/client-to-server.csv), [totais](l2-unity/docs/protocol-audit/inventory.json), [testes nativos](l2-unity/docs/protocol-audit/validation-20261014.txt).

Inventário cobre 272 escritores Game/Login em 270 arquivos (inclui três variantes concretas em AbstractNpcInfo) e 210 rotas do dispatcher Game por estado. São 267 chaves resolvidas no servidor e 101 bindings no cliente; 85 escritores possuem binding/Apply, 12 têm Apply vazio, 174 não têm binding e um pacote é administrativo/dinâmico. Não confundir classes ou rotas com opcodes únicos, nem presença de leitor com compatibilidade comprovada. world-application.csv documenta 21 encaminhamentos e diferencia callbacks herdados vazios de overrides. SocialAction é explicitamente ignorado na aplicação; ChangeWaitType só possui override em UserWorldApply e Revive não possui override para NPC/Monster.

Correções:
- CharSelected 0x15: oito inteiros lidos a mais foram removidos; SP estava sendo atribuído a CP. DTO agora valida o tamanho restante do formato deste servidor, usa sexo/raça enviados e não acessa UI na thread de leitura. Aparência/máximos adicionais são completados durante Apply.
- ChangeMoveType 0x2E: faltava enum, leitor e aplicação. Agora atualiza a identidade e usa Entity.UpdateMoveType. Pacotes com tamanho/modo inválidos são rejeitados.
- Registros de combate/efeitos usam pasta gravável do aplicativo. Falhas não foram ocultadas.
- Lobby: destino inicial acompanha a colocação; chegada considera XZ e deslocamento limitado ao destino.

Validação: fixtures CharSelectionInfo com 0/2 personagens; CharSelected com experiência longa e SP diferente de CP; ChangeMoveType caminhada/corrida e truncamento. Todos passaram no Unity CLI. Avaliação dos prefabs ativos confirma estados/clips de locomoção e correspondência de nomes dos ossos do torso padrão em MFighter/MMagic. ExecuteMethod pode terminar com código zero mesmo ao lançar exceção; verificar o conteúdo do relatório e BuildSummary.

Erro de seleção de raça confirmado: CharSelectionInfo ignorava a raça do pacote e usava uma tabela parcial por classe, que retorna DarkElf para classes como Hawkeye 9 e Sagittarius 92. Os logs mostram equipamento MDarkElf na seleção e MFighter na entrada. Corrigido para usar a raça explícita e o resolvedor comum de corpo; magos de terceira profissão do servidor local também foram classificados. Fixtures de nove combinações de raça/classe, incluindo classes C1 e avançadas, passaram no Unity CLI. Não afirmar a resolução visual completa antes de testar o novo player; [LobbyPose] registra estado/clip/flag death e ligações das malhas uma vez por personagem. Não inventar pacote Die nem alterar HP/posição do banco para esconder a pose.

Pendências principais: StaticObjectInfo 0x99, mensagens FE, aplicações vazias de criação/ação/alvo/gauge/cooldown/henna/macro/troca, demais classes sem binding e parsers que aceitam truncamento retornando zero. Não adicionar handlers vazios para simular suporte. login-client-to-server.csv contém as quatro rotas de pedidos Login com escritores identificados; RSA/checksum/handshake completos e validação dinâmica de todas as funcionalidades não estão comprovados por esse inventário.

Ferramentas: docs/protocol-audit/audit_protocol.py reproduz o inventário sem dependências; Assets/Editor/L2ProtocolAuditValidation.cs executa testes e pode chamar build incremental somente após validação. Nenhuma alteração Java/DB/configuração do servidor foi feita nesta auditoria.

## 13. Decisão de escopo C1 — explícita do usuário

Manter L2J4Team Interlude com funções C1. Não migrar para Mobius nem trocar a revisão/opcodes para C1 419. A rede efetiva continua Interlude 746; a [referência C1](https://rusacis.com/netpro/c1_harbingers_of_war.html) delimita funcionalidades. Ver [escopo C1](l2-unity/docs/protocol-audit/c1-scope.md). As 174 classes sem binding não são todas requisitos de implementação: distinguir funções C1 e extensões posteriores/customizadas. Não desativar funções que existiam em C1 automaticamente, nem truncar campos do protocolo Interlude que continuam necessários ao alinhamento.

Inspeção somente de leitura do Mobius local mostrou adaptação C2: ServerPackets define Attack 0x05 e Die 0x06; CharSelectionInfo usa exp int, enquanto L2J4Team usa long. Não deduzir o formato somente pelo nome de pasta ou revisão permitida. Esse Mobius não é o servidor escolhido.

Login: os limites de conta/senha do bloco RSA foram corrigidos para 14/16 bytes, conforme os offsets 94/108 do servidor. Testes de fronteira ASCII/UTF-8 passaram sem autenticar com credenciais reais. CharSelected Apply também preserva o rosto selecionado.

Build 20261014-161948 foi interrompido para incluir o erro de raça descoberto após compilar seu código; não entregar esse diretório. Preservados os backups e o marcador antigo do Editor em Logs/Recovery-20261014-1648. Depois de remover a pasta vazia de recuperação, a execução CLI com -force-d3d11 abriu o projeto e passou os testes atualizados.

Build atualizado: l2-unity/Builds/WindowsClean/20261014-165517/L2-Unity.exe. BuildSummary: Succeeded, zero erros, 139 avisos. Substitui o build 124721 como referência das correções desta auditoria; não há afirmação de que todos os avisos foram resolvidos. Startup de teste com Direct3D 11: mais de um minuto aberto/responsivo, modelos e cenário do menu carregados, zero exceções/crash no log separado Logs/ProtocolAuditSmoke.log. Somente o processo de teste foi encerrado. Não houve login com credenciais reais; conferir a pose equipada e os registros [LobbyPose] no próximo login desta versão.

## 14. Nomes e cabelo do humano fighter — 14/10/2026

Dois problemas distintos foram reproduzidos e corrigidos. Âncoras de nomes usavam deslocamento negativo e preferiam sempre o CharacterController, mesmo quando menor que a colisão do npcgrp. Agora usam o maior topo entre colisor e dados de colisão, mais 0,08 m; respeitam a transformação do colisor filho. Atualizado também o override do menu. Não usar ossos animados para fazer o texto oscilar com o passo.

CacheHair gravava style*6+color*2, mas GetHair consultava style*8+color*2. Corrigido o stride e removida a compensação especial da humana fighter. Teste nativo validou os 406 prefabs existentes contra seus nomes/variantes exatos.

As manchas persistiam no penteado zero mesmo com o cache correto: a peça frontal ah usava atlas com layout de outra versão. Extraídos do cliente C2 local D:/Jogos/L2 C2 (Splendor)/systextures/MFighter.utx os 12 atlas originais das famílias m000/m001/m002, quatro cores cada. PNGs substituídos preservando GUIDs, sem redesenhar pixels. Corrigidas também as referências dos materiais (havia cor apontando para recurso diferente). Camada bh conservada. FinalBlend original confirmado: FrameBufferBlending=2, AlphaRef=5, AlphaTest/TwoSided=True; materiais usam SrcAlpha/OneMinusSrcAlpha, cutoff 5/255, ZWrite=1, fila 3000.

L2CharacterVisualAudit.Run passou: âncora com colisor filho deslocado/escalado, precedência da colisão NPC, 406 variantes e 12 materiais. Renderização final conferida tanto em material neutro quanto no shader real com iluminação controlada: sem as manchas de pele/preto na testa. Evidências em l2-unity/docs/character-visual/; detalhes e hashes em docs/character-visual-audit.md e character-visual-atlas-provenance.json. Leitor exploratório restrito a pacote C2 123/23 ficou em Logs/CharacterVisual/read_utx.py; não tratá-lo como extrator geral (especialmente referências de exports em outros pacotes). Nenhum executável de extração externo foi instalado; download oficial do UE Viewer retornou 404.

A roupa assimétrica é o fallback existente do item 21/MFighter_m001_t02_u quando a armadura IL equipada está ausente. Não foram adicionados assets das armaduras IL nem alterados servidor/opcodes para corrigir estes problemas visuais. O build 20261014-230021 ficou incompleto, sem BuildSummary; não entregar esse diretório.

## 15. Rebuild solicitado em 15/10/2026

Novo Unity CLI iniciado em 15/10 às 10:04, método L2CleanWindowsBuild.BuildIncremental, com -force-d3d11 e sem -batchmode. Log: l2-unity/Logs/Rebuild-20261015.log. Build concluído: Succeeded, zero erros e 139 avisos. Executável de referência: l2-unity/Builds/WindowsClean/20261015-100947/L2-Unity.exe. Nenhuma alteração adicional no código.

Teste de abertura com log separado Logs/RebuildSmoke-20261015.log: mais de 85 segundos aberto/responsivo, modelos e menu carregados, zero correspondências para exceções/erro fatal/crash/erro de shader no log. Sem autenticação com credenciais reais; não houve teste de login ou de nomes dentro da cidade nesta execução. Somente a instância criada para esse teste foi encerrada, após conferir seu caminho e argumento de log. Esse build substitui 20261014-165517 como referência atual e inclui as correções da seção 14. Os 139 avisos não foram silenciados nem declarados resolvidos.

