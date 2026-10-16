# Escopo de funcionalidades C1

Objetivo informado: limitar o cliente ao conteúdo e às funções necessárias para C1. Não tratar todas as extensões do Interlude como requisitos de implementação.

Referência externa consultada em 14/10/2026: [NetPro / RUSaCis — C1 Harbingers of War](https://rusacis.com/netpro/c1_harbingers_of_war.html). A página identifica protocolo 419.

**Decisão explícita do usuário: manter L2J4Team Interlude com funções C1.** Os números e formatos de pacotes efetivos permanecem Interlude 746. A tabela C1 delimita funcionalidades; não é a tabela de despacho que será enviada ao servidor atual.

## Conteúdo C1 e protocolo de rede são escolhas distintas

Manter L2J4Team Interlude com conteúdo C1 exige conservar seus números e formatos de pacotes. A tabela C1 serve para delimitar funcionalidades, não para substituir diretamente os opcodes.

Conectar a um servidor C1 de protocolo 419 exige outro perfil de protocolo: além de opcode, revisar o corpo e o handshake. Exemplo de colisão: 0x06 significa Attack no C1 e Die no servidor Interlude atual.

| Mensagem recebida | Referência C1 | Fonte L2J4Team atual |
|---|---|---|
| Attack | 0x06 | 0x05 |
| Die | 0x0B | 0x06 |
| Revive | 0x0C | 0x07 |
| StatusUpdate | 0x1A | 0x0E |
| CharacterSelectionInfo | 0x1F | 0x13 |
| CharacterSelected | 0x21 | 0x15 |
| NpcInfo | 0x22 | 0x16 |
| ChangeMoveType | 0x3E | 0x2E |
| UserInfo | 0x04 | 0x04 |

Mesmo quando o número coincide, isso não comprova formato idêntico.

## Prioridade de implementação

1. Login, seleção/criação/exclusão, entrada, saída e reinício.
2. Carregamento do mundo, personagens/NPCs/objetos, posição, movimento e teleporte.
3. Alvo, ataque, magia, HP/MP, morte/ressurreição, efeitos e tempos de habilidade.
4. Inventário, equipamento, loot, NPC/HTML, compra/venda, depósito e troca.
5. Chat, atalhos, aprendizagem de habilidades e quests presentes no datapack escolhido.
6. Party, clã, pets, lojas privadas, barcos e cerco, conforme o conteúdo C1 efetivamente habilitado.

Não são requisitos automáticos as funções posteriores, como subclasses, terceira profissão, CP, augments, Olympiad, Kamael e extensões customizadas do servidor. No perfil Interlude, alguns campos continuam sendo consumidos para manter alinhamento, mesmo sem interface para a função.

## Lacunas atuais que afetam funções C1

| Função | Mensagem no servidor atual | Situação do cliente |
|---|---|---|
| Objetos estáticos do mundo | StaticObjectInfo 0x99 | Sem binding; descarte observado |
| Falha/rejeição de ação | ActionFailed 0x25 | Apply vazio |
| Limpeza de alvo | TargetUnselected 0x2A | Apply vazio |
| Barra de ação/conjuração | SetupGauge 0x6D | Apply vazio |
| Tempo de recarga | SkillCoolTime 0xC1 | Apply vazio |
| Confirmação de criação | CharCreateOk 0x19 | Apply vazio |
| Atualizações de troca | TradeOwnAdd/TradeOtherAdd e confirmações | Apply vazio |
| Animação social | SocialAction 0x2D | Encaminha para método que só registra IGNORE |
| Sentar/levantar | ChangeWaitType 0x2F | Override apenas em UserWorldApply |

Essas funções constam do conjunto C1 e precisam de avaliação/implementação conforme seu uso. A seleção de raça, CharSelected, ChangeMoveType e gravação de eventos foram corrigidos nesta etapa. As demais lacunas não foram implementadas por esse inventário. Consultar os CSVs para os arquivos e rotas exatos.

## Atenção ao Mobius local

Foi feita apenas inspeção de leitura em `D:\Documentos\Servers\L2J_Mobius-master-L2J_Mobius_C1_HarbingersOfWar`. Seu AGENTS.md declara conteúdo C1 com protocolo adaptado para C2. A fonte `java/org/l2j/gameserver/network/ServerPackets.java` realmente define Attack=0x05 e Die=0x06, diferentes da referência C1 419. `CharSelectionInfo.java` também usa experiência int, enquanto o L2J4Team atual envia long.

Portanto, o nome da pasta e a revisão aceita não bastam para escolher os leitores. Se esse Mobius for escolhido, auditar seus fontes e configuração efetiva; não aplicar cegamente a tabela externa nem reutilizar todos os DTOs Interlude.

Nenhum servidor, versão enviada, opcode ou dado do banco foi alterado para C1 nesta etapa. A conexão atual continua Interlude conforme a escolha explícita do usuário. Não migrar para Mobius nem trocar opcodes para 419 sem nova instrução.
