# Auditoria do protocolo do servidor local — 14/10/2026

Servidor de referência: `D:\Documentos\Servers\L2J4Team_Unity`, fontes Java do fork Interlude. Cliente: `l2-unity`, protocolo configurado 746. A fonte deste servidor, incluindo suas modificações, prevalece sobre tabelas genéricas de Interlude.

## Escopo e evidência

- `server-to-client.csv`: todas as classes concretas com `writeImpl` nas pastas de pacotes Game e Login. Inclui opcode, subopcode FE, fonte Java, hash SHA-256, binding C#, DTO, operações de leitura/escrita e presença de Apply/OnParsed.
- `client-to-server.csv`: rotas dos diferentes estados do `L2GamePacketHandler`, incluindo subopcodes, classe Java, leitura no servidor e escritor correspondente no cliente.
- `login-client-to-server.csv`: quatro rotas do L2LoginPacketHandler, separadas dos pedidos Game, todas com escritor correspondente identificado.
- `inventory.json`: totais, estados e limitações do inventário.
- `world-application.csv`: 21 caminhos de encaminhamento direto/fila para WorldPacketApply e callbacks por tipo de entidade; ausência de override é distinguida de implementação existente.
- `audit_protocol.py`: reprodução do inventário, somente leitura dos fontes; não necessita dependências adicionais.
- `Assets/Editor/L2ProtocolAuditValidation.cs`: fixtures sintéticas segundo o servidor e avaliação nativa dos prefabs/Animator no Unity CLI. Resultado em `Logs/ProtocolAuditValidation.txt`.

Não são usados dados reais de contas nas fixtures. Os logs de execução não são copiados para esta pasta.

## Cobertura encontrada

| Medida | Resultado |
|---|---:|
| Classes escritoras do servidor | 272 |
| Chaves distintas de canal/opcode resolvidas | 267 |
| Chaves de bindings no cliente | 101 |
| Classes com binding e Apply preenchido | 85 |
| Classes com binding e Apply vazio | 12 |
| Classes sem binding no cliente | 174 |
| Pacote administrativo de opcode dinâmico | 1 |
| Bindings duplicados no cliente | 0 |
| Rotas de pedidos Game por estado | 210 |
| Rotas com escritor simples identificado no cliente | 44 |
| Rotas ausentes ou que exigem revisão de extensão | 166 |

Os totais de classes e rotas não equivalem a quantidades de opcodes únicos. Um opcode pode aparecer em mais de um estado ou classe. `AdminForgePacket` é um mecanismo administrativo arbitrário, não um opcode padrão ausente.

Foram encontradas 270 unidades de fonte com writeImpl; AbstractNpcInfo contém três escritores concretos (NpcInfo, SummonInfo e PcMorphInfo). As três variantes constam na matriz, totalizando 272 escritores.

## Significado dos estados

- `missing-client-binding`: o registro por atributos não tem receptor identificado para a chave enviada pelo servidor.
- `client-apply-empty`: Apply não executa comportamento. Consultar também OnParsed e outros efeitos de ciclo de vida antes de concluir a funcionalidade.
- `present-layout-and-runtime-unverified`: há leitor e aplicação, mas presença não comprova formato ou comportamento correto.
- `present-layout-unverified`: há escritor para o pedido simples; formato ainda exige validação.
- `missing-or-extended-review`: ausência identificada ou pedido estendido ainda sem correlação específica.

Traços de tipos não expandem loops nem escolhem ramos condicionais. Não comparar suas quantidades brutas como se fossem um único pacote. Métodos auxiliares também podem escrever campos. O dispatcher de pedidos Login consta em sua própria matriz; a presença dos quatro escritores não valida RSA, checksum nem todas as condições do handshake.

## Divergências confirmadas e corrigidas

### CharSelected — Game 0x15

`CharSelected.java` escreve SP após HP/MP; o cliente atribuía esse inteiro a CP. O cliente também tentava ler oito inteiros a mais no final. `PacketReader.ReadI` devolve zero ao esgotar o buffer, o que escondia essa divergência.

O DTO agora segue exatamente o escritor local, valida os 260 bytes restantes após nome/ID/título, preserva SP e lê quatro inteiros reservados no final. Dados de aparência e máximos, ausentes desse pacote, são obtidos da seleção durante Apply. O parser não acessa mais a janela Unity na thread de leitura.

Fixture de 278 bytes: campos distintos, experiência acima de 32 bits, SP diferente de CP, sexo e relógio; consumo completo. `CharSelectionInfo` também foi validado com zero e dois personagens, selecionado no último slot, doubles de HP/MP, 34 inteiros de paperdoll, enchant byte e augmentation int. Isso valida leitura dos campos testados, não todas as operações da interface.

### ChangeMoveType — Game 0x2E

O servidor envia ID, modo 0/1 e inteiro reservado. O cliente não possuía opcode/binding e os logs mostram descarte de pacotes de 13 bytes com 0x2E, incluindo o opcode.

Foram adicionados enum, DTO e binding. Apply atualiza `Identity.IsRunning` e chama `Entity.UpdateMoveType`, mantendo o caminho existente para o controlador do jogador. O DTO rejeita tamanho incorreto e modo inválido. Fixtures cobrem caminhada, corrida e truncamento; aplicação visual precisa de teste conectado.

### Eventos de combate interrompidos por registro

Os logs mostram `UnauthorizedAccessException` em gravações sob um caminho absoluto de outro computador. Replay, ChainLog e demais registradores agora usam `Application.persistentDataPath`, preservando os registros. Não foi adicionado catch para esconder falhas.

### Movimento no lobby

O destino podia conservar a posição anterior ao posicionamento do prefab. PlacePawn agora inicializa o destino com o ponto de colocação. A distância de chegada usa XZ, consistente com o deslocamento horizontal, e o passo não ultrapassa o destino.

## Seleção de raça incorreta no lobby: erro confirmado, validação visual pendente

A captura enviada pelo usuário pertence à seleção de personagens, antes da entrada no mundo. Os registros do cliente mostram `wait_bow → walk_bow → wait_bow`, com HasState verdadeiro. Isso não prova a pose renderizada nem identifica morte enviada pelo servidor.

O teste nativo ativa os prefabs (originalmente inativos), executa estados de espera/caminhada em MFighter e MMagic e observa o esqueleto animado. Os estados existem e selecionam os clips correspondentes. A ordem dos nomes dos ossos do torso padrão corresponde ao esqueleto: 70 em MFighter e 86 em MMagic.

Na revisão posterior, foi confirmado que CharSelectionInfo ignorava a raça explicitamente enviada e usava MapClassId.GetCharacterRace. Essa tabela só reconhece algumas classes iniciais e devolve DarkElf para as demais: por exemplo, Hawkeye 9 (C1) e Sagittarius 92 (Interlude) humanos eram convertidos para Dark Elf. Os registros da seleção mostram equipamento MDarkElf enquanto a entrada no jogo usa MFighter. O DTO foi corrigido para usar a raça recebida e o resolvedor comum de corpo; a classificação de magos foi estendida às terceiras profissões existentes no servidor local. Fixtures de nove combinações nas cinco raças passaram. A resolução visual com o equipamento completo ainda precisa de login no executável novo.

`L2LobbyPoseDiagnostics` registra uma vez por personagem no lobby, após um segundo: controlador, estado, clip, flag death, orientação e ligação das malhas ao Animator. Não registra nome de jogador, conta ou ID real. Usar os registros `[LobbyPose]` do novo executável para confrontar a captura. Não declarar a falha visual resolvida somente porque o Animator isolado funciona.

## Lacunas prioritárias

1. `StaticObjectInfo` 0x99: ausência também observada nos descartes do cliente; afeta objetos estáticos do mundo.
2. Aplicações vazias: ActionFailed, CharCreateOk, HennaInfo, SendMacroList, SetupGauge, SkillCoolTime, TargetUnselected e cinco mensagens de troca. Os CSVs apontam os arquivos exatos. Não marcar como implementadas apenas porque o pacote foi lido.
3. Pacotes FE: completar correlação de subopcode e comportamento; os logs apresentam descartes FE, sem informação suficiente para identificar todos os subopcodes.
4. Demais 174 classes ausentes: consultar matriz por funcionalidade; não adicionar receptores vazios nem simplesmente descartar para simular compatibilidade.
5. `PacketReader`: vários métodos ainda aceitam truncamento devolvendo zero ou string vazia. A migração global para leitura estrita exige revisar os DTOs restantes e testes de regressão; não foi aplicada indiscriminadamente nesta etapa.
6. O registro de Game/PlayerInfo em OnParsed de CharSelected ainda é dependência do fluxo existente; migrar a preparação inteira para a fila principal requer revisão dos consumidores de UserInfo.
7. Persistem outras falhas nos logs, incluindo magia, NPCs assíncronos e duplicidade de registros. Não atribuí-las automaticamente aos opcodes sem reprodução.
8. `SocialAction` 0x2D possui binding, mas WorldPacketApply apenas registra IGNORE: não aplica animação social. `ChangeWaitType` 0x2F só tem override em UserWorldApply, e `Revive` 0x07 não tem override em Npc/MonsterWorldApply. Esses casos constam na matriz de aplicação; uma função vazia herdada não equivale a suporte.
9. `RequestAuthLogin`: servidor lê conta em 14 bytes (offset 94) e senha em 16 bytes (offset 108). O cliente permitia 15 bytes de conta, sobrescrevendo o último com a senha, e limitava a senha a 15 bytes. Os limites foram corrigidos para 14/16; testes de fronteira ASCII/UTF-8 passaram, sem tentativas com credenciais reais.

## Objetivo C1 definido pelo usuário

Consultar [escopo C1](c1-scope.md). O usuário escolheu explicitamente manter L2J4Team Interlude com funções C1. Conexão, números e formatos de pacotes continuam Interlude 746; funções posteriores não são requisitos obrigatórios. Não migrar para Mobius/protocolo 419. O Mobius local foi inspecionado apenas como comparação e seus opcodes/corpos não correspondem integralmente à referência C1 419.

A tentativa de build `20261014-161948` foi interrompida para incluir a correção de raça identificada depois que seu código já havia sido compilado. Ela não é um artefato de entrega. Os backups de recuperação foram preservados em Logs/Recovery-20261014-1648. A nova validação passou também para classes C1/avançadas, cinco raças e os limites UTF-8 de login.

## Build e teste de inicialização

Entrega: `Builds/WindowsClean/20261014-165517/L2-Unity.exe`. Unity CLI 6000.6.5f1, Editor normal com -force-d3d11, sem -batchmode. BuildSummary confirma Succeeded, zero erros e 139 avisos. Os avisos não foram ocultados nem todos resolvidos; um exemplo é a transição AnyState → spwait02 em Npc_Basic sem condição/Exit Time.

O executável foi iniciado em teste com Direct3D 11 e log separado `Logs/ProtocolAuditSmoke.log`. Permaneceu aberto e responsivo por mais de um minuto, carregou os modelos/cenário do menu e não registrou exceções ou crash. Apenas o processo desse teste foi encerrado depois. Nenhuma autenticação com credenciais reais ou teste conectado da pose foi feito. O teste isolado dos prefabs/Animator cobre MFighter e MMagic; as cinco raças foram testadas na resolução de corpo, não em todas as animações/equipamentos.

Fonte de verdade dos resultados: validation-20261014.txt, BuildSummary.txt e logs locais da execução. Compatibilidade funcional C1 completa ainda exige concluir as lacunas priorizadas em c1-scope.md.

## Reprodução

Executar `audit_protocol.py` com Python 3. No Unity CLI, abrir este projeto com `-executeMethod L2ProtocolAuditValidation.Run -quit -logFile <arquivo>` para validar. `L2ProtocolAuditValidation.ValidateAndBuild` valida e então gera um player Windows incremental em uma nova pasta datada. Neste ambiente, a licença Personal não permite execução headless com `-batchmode`; utilizar o Editor CLI normal.

Conferir o relatório de validação e `BuildSummary.txt`: código de saída zero do Editor, isoladamente, não prova sucesso de executeMethod. Testar o executável novo; nenhuma autenticação automática com credenciais do usuário foi feita.
