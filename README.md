# caminhos

Caminho mínimo do zero em C# e .NET 8: Dijkstra, Bellman-Ford, Floyd-Warshall,
Johnson e busca A estrela. O juiz é a **enumeração de todos os caminhos simples**,
que não é um algoritmo melhor: é a definição.

```
$ dotnet medidor.dll juiz

  vertices   grafos   caminhos olhados   dijkstra errou   bellman-ford errou
         5      300                760                4                    0
         6      300              1,590                7                    0
         7      300              4,020               14                    0
         8      300             12,663               32                    0
```

A força bruta é exponencial e só roda em grafos de menos de dez vértices, e isso
não é limitação: é onde um algoritmo errado ainda parece certo. Dijkstra com peso
negativo erra em grafo pequeno do mesmo jeito que erra em grafo grande, e no
pequeno dá para provar.

Nenhuma medida aqui usa relógio. Todas são contagem de relaxamentos, de retiradas
da fila, de vértices expandidos ou de respostas erradas.

## O Dijkstra erra com peso negativo, e não avisa

```
$ dotnet medidor.dll negativo

  O MENOR GRAFO EM QUE O DIJKSTRA ERRA: 4 VERTICES, 4 ARESTAS

    0 -> 1 com peso  2
    0 -> 2 com peso  3
    2 -> 1 com peso -2
    1 -> 3 com peso  1

    a resposta certa para o 3:     2
    o Dijkstra devolveu:           3
    o Bellman-Ford:                2
```

Não há exceção, não há valor especial, não há nada na saída que diga que aquela
tabela não é a resposta. Ela só é maior.

E peso negativo não é exótico: ele aparece em toda conta em que o peso pode ser
**ganho**. Arbitragem de câmbio, rota com trecho subsidiado, escalonamento com
restrição de diferença, fluxo com custo.

## O conserto que todo mundo tenta é pior que o problema

```
  jeito              grafos   errou em   pior caso (vertices errados)
  Dijkstra direto        500         35                             5
  somar constante        500        252                             6
```

Somar uma constante em todas as arestas para elas ficarem não negativas erra
**sete vezes mais** que simplesmente usar o Dijkstra errado. A razão cabe numa
frase: somar c em cada **aresta** soma c vezes o **número de arestas** do caminho,
e caminhos diferentes têm números de arestas diferentes.

E ele erra até sem peso negativo:

```
    0 -> 1 -> 2 -> 3, cada aresta com peso 1   (tres saltos, peso 3)
    0 -> 3 com peso 4                          (um salto, peso 4)

    a resposta certa:                 3
    somando 10 em cada aresta:        4
```

O conserto que **funciona** é o de Johnson, e a diferença é onde a soma entra:
por **vértice**, com w(u,v) + h(u) - h(v). Os potenciais do meio se cancelam ao
longo do caminho e só sobram os das pontas, então o caminho mínimo é o mesmo e
todo peso fica não negativo.

## Onde Floyd-Warshall e Johnson se cruzam

```
$ dotnet medidor.dll densidade

  densidade   arestas        floyd      johnson   n dijkstras      vencedor
       0.02        64       12,060        4,571           731       johnson
       0.10       332      144,360       45,439        25,519       johnson
       0.30     1,066      203,400      136,854        72,894       johnson
       0.60     2,155      213,660      269,313       140,013         floyd
       1.00     3,540      216,000      437,028       224,628         floyd
```

O Floyd-Warshall gasta exatamente n ao cubo no grafo vazio e no grafo completo:
ele não olha quantas arestas existem. O Johnson depende de m, então ganha no
esparso e perde no denso, e a virada está por volta de metade das arestas
possíveis.

## O Bellman-Ford quase nunca precisa das passadas que o livro manda

```
  vertices   densidade   passadas usadas   passadas do livro
        50        0.05                 6                  49
       200        0.05                 7                 199
       200        0.30                 5                 199
```

Com duzentos vértices, a conta pede 199 passadas e as distâncias param de mudar
na sétima. A saída antecipada não é otimização esperta: é a diferença entre vinte
e oito vezes mais trabalho e o trabalho necessário.

## A busca com palpite, e o que o palpite custa

```
$ dotnet medidor.dll estrela

  heuristica             distancia   expandidos   reexpandidos   superestima em   inconsistente em
  zero (e Dijkstra)            118        2,662              0                0                  0
  quarteirao                   118          510              0                0                  0
  quarteirao vezes 3           146          211              2            2,659              3,968
```

Com palpite zero, A estrela **é** Dijkstra: não é parecido, é a mesma execução. A
heurística de quarteirão dá a mesma resposta gastando um quinto dos vértices, e
esse é o único ganho deste repositório que não custa nada.

A exagerada custa:

```
  fator   distancia   expandidos   erro
      1         118          510    0.0 %
      2         128          253    8.5 %
      3         146          211   23.7 %
     10         148          193   25.4 %
```

Metade do trabalho por 8,5 por cento de erro, e nada na saída diz que a resposta
não é a mínima: o caminho existe, as arestas existem, o peso bate.

## O que as medidas me corrigiram

**O menor grafo em que o Dijkstra erra tem quatro vértices, e eu tinha escrito
com três.** Com três ele acerta, e o motivo é uma propriedade da implementação
que eu não tinha notado: o Dijkstra com fila preguiçosa continua relaxando em
vértices que já saíram, então a distância do vértice fechado até melhora. O que
não acontece é ele ser **expandido** de novo, então o erro só vaza por uma aresta
saindo dele. Precisa de um quarto vértice para o erro aparecer. O exemplo de três
vértices passava no teste.

**Com peso negativo, a tabela e o vetor de antecessores do Dijkstra param de
concordar entre si.** No grafo mínimo, a tabela diz que o vértice 3 custa 3 e o
caminho que o próprio Dijkstra devolve pesa 2. Os antecessores foram corrigidos
quando o vértice 1 melhorou; a distância do 3, calculada a partir do valor velho,
não foi. Com peso não negativo isso nunca acontece, então a discordância sozinha
denuncia o problema sem precisar da resposta certa. E ela é **incompleta**: em 300
grafos, a tabela errou em 21 e a discordância apareceu em 19. Eu tinha escrito o
teste afirmando que ela pegava todos.

**O meu gerador de grafos com peso negativo tinha ciclo negativo, e o
Bellman-Ford me avisou.** A primeira versão só deixava negativas as arestas que
iam de um índice menor para um maior, achando que as arestas de volta, grandes e
positivas, pagariam a conta. Não pagam: quatro arestas de -50 seguidas de uma de
+100 fecham um ciclo de -100. Em grafos de oito vértices quase nunca aparecia; em
dez, apareceu.

O conserto é o próprio truque de Johnson, de trás para a frente: começar com
pesos todos positivos e aplicar w(u,v) + h(u) - h(v) com potenciais sorteados.
Esse ajuste **não muda** o peso de nenhum ciclo e muda o peso de cada aresta. A
mesma identidade que o algoritmo usa para tirar os negativos serve para pôr.

**O Floyd-Warshall com o k no laço de dentro devolve uma matriz inteira de
números plausíveis.** O erro é sempre **para mais**: a versão errada nunca acha um
caminho que não existe, ela só deixa de achar caminhos que existem. E em grafos de
quatro vértices ela acerta na maioria das vezes, que é o que faz o bug chegar em
produção.

**A desigualdade triangular sozinha não prova nada.** Uma tabela toda de zeros
passa nela com pesos não negativos. A conferência que caracteriza a resposta
certa são as duas juntas: nenhuma aresta violada **e** toda distância atingível
por um caminho de verdade.

## Por que o juiz é a enumeração

Comparar Dijkstra com Bellman-Ford diz qual dos dois está diferente e não diz
qual está certo. A enumeração de todos os caminhos simples é a definição do
problema, e ela tem uma propriedade que nenhum algoritmo de caminho mínimo tem:
funciona com peso negativo, com ciclo negativo e com grafo desconexo, sem mudar
uma linha. O ciclo negativo não a atrapalha porque caminho **simples** não repete
vértice.

E ela também tem juiz: a contagem de caminhos num grafo completo é conferida
contra a fórmula fechada, e as duas propriedades que caracterizam a resposta
(triangular e atingibilidade) são verificadas em cada grafo.

Os pesos são inteiros de propósito. Caminho mínimo é um problema cuja resposta é
uma **soma**, e somar número com vírgula introduz uma diferença na última casa que
faz dois caminhos empatados desempatarem de um jeito em cada máquina.

O sorteio é de um gerador congruente escrito aqui, e não do sorteio da
biblioteca, cuja documentação não promete a mesma sequência entre versões nem
entre sistemas. A integração contínua roda em três: com o gerador próprio, a
semente 7 produz exatamente o mesmo grafo nos três.

## As peças

| arquivo | o que faz |
| --- | --- |
| `Grafo.cs` | o grafo, o sorteador determinístico e as grades |
| `ForcaBruta.cs` | todos os caminhos simples e as duas conferências: o juiz |
| `Dijkstra.cs` | o algoritmo, o monte binário e a versão com potencial |
| `BellmanFord.cs` | caminho com peso negativo, ciclo negativo e os potenciais |
| `TodosOsPares.cs` | Floyd-Warshall, Johnson e a versão com o k no lugar errado |
| `Estrela.cs` | A estrela, as heurísticas e as duas propriedades delas |
| `Negativos.cs` | os grafos mínimos de erro e o conserto que não funciona |
| `Medidas.cs` | as tabelas que o medidor imprime e que os testes conferem |

## Como rodar

```
dotnet test testes/Caminhos.Testes/Caminhos.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- tudo
```

As medidas aceitam `juiz`, `negativo`, `densidade`, `estrela`, `ciclo` e `tudo`.

## Licença

MIT.
