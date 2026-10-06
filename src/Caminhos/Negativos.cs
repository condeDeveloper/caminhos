namespace Conde.Caminhos;

/// <summary>
/// O PESO NEGATIVO, que e onde o algoritmo mais usado do mundo erra em silencio.
///
/// Peso negativo nao e exotico. Ele aparece em toda conta em que o peso pode ser
/// GANHO: arbitragem de cambio, rota com trecho subsidiado, escalonamento com
/// restricao de diferenca, fluxo com custo. E a reacao mais comum a ele e uma das
/// duas daqui, e as duas estao erradas.
/// </summary>
public static class Negativos
{
    public sealed record Comparacao(int Grafos, int DijkstraErrou, int PiorErro, int SomarConstanteErrou, int PiorErroDaConstante);

    /// <summary>
    /// A medida: rodar Dijkstra e o juiz no mesmo grafo e contar as diferencas.
    ///
    /// O ponto nao e que o Dijkstra erra: isso esta em todo livro. O ponto e que
    /// ele NAO AVISA. Nao ha excecao, nao ha valor especial, nao ha nada na saida
    /// que diga que aquela tabela nao e a resposta. Ela so e maior.
    /// </summary>
    public static Comparacao Medir(int vertices = 8, double densidade = 0.4, int grafos = 500)
    {
        var errou = 0;
        var pior = 0;
        var errouConstante = 0;
        var piorConstante = 0;

        for (var i = 0; i < grafos; i++)
        {
            var grafo = Grafo.SortearComNegativas(vertices, densidade, (ulong)(i + 1));
            if (!grafo.TemNegativa) continue;

            var certo = ForcaBruta.De(grafo, 0);
            var dijkstra = Dijkstra.De(grafo, 0).Distancia;

            var diferentes = Diferentes(certo, dijkstra);
            if (diferentes > 0)
            {
                errou++;
                if (diferentes > pior) pior = diferentes;
            }

            var somado = PorSomarConstante(grafo, 0);
            var diferentesConstante = Diferentes(certo, somado);
            if (diferentesConstante > 0)
            {
                errouConstante++;
                if (diferentesConstante > piorConstante) piorConstante = diferentesConstante;
            }
        }

        return new Comparacao(grafos, errou, pior, errouConstante, piorConstante);
    }

    /// <summary>
    /// O CONSERTO QUE TODO MUNDO TENTA: somar uma constante em todas as arestas
    /// para elas ficarem nao negativas, rodar Dijkstra e descontar depois.
    ///
    /// Ele e errado, e o motivo cabe numa frase: somar c em cada ARESTA soma c
    /// vezes o NUMERO DE ARESTAS no caminho, e caminhos diferentes tem numeros de
    /// arestas diferentes. O ajuste penaliza caminho longo em numero de saltos, e
    /// o caminho minimo passa a ser outro.
    ///
    /// Esta funcao faz exatamente isso, descontando c vezes o numero de arestas do
    /// caminho que o Dijkstra devolveu, que e a versao mais caridosa da tentativa.
    /// Ela ainda erra.
    ///
    /// O conserto que FUNCIONA e o de Johnson, e ele soma um valor diferente por
    /// VERTICE em vez de um valor igual por aresta: somar h(u) e subtrair h(v) faz
    /// os termos do meio se cancelarem ao longo do caminho.
    /// </summary>
    public static long[] PorSomarConstante(Grafo grafo, int origem)
    {
        ArgumentNullException.ThrowIfNull(grafo);

        var menor = grafo.Todas.Select(a => a.Peso).DefaultIfEmpty(0).Min();

        return ComConstante(grafo, origem, menor < 0 ? -menor : 0);
    }

    /// <summary>
    /// A mesma tentativa com uma constante escolhida a mao, para o teste poder
    /// mostrar que o problema nao e o peso negativo: e a constante.
    /// </summary>
    public static long[] ComConstante(Grafo grafo, int origem, long constante)
    {
        ArgumentNullException.ThrowIfNull(grafo);

        var ajustado = new Grafo(grafo.Vertices);
        foreach (var a in grafo.Todas) ajustado.Ligar(a.De, a.Para, a.Peso + constante);

        var resultado = Dijkstra.De(ajustado, origem);
        var distancia = new long[grafo.Vertices];

        for (var v = 0; v < grafo.Vertices; v++)
        {
            if (resultado.Distancia[v] >= ForcaBruta.Infinito)
            {
                distancia[v] = ForcaBruta.Infinito;
                continue;
            }

            var saltos = Math.Max(0, resultado.Caminho(v).Count - 1);
            distancia[v] = resultado.Distancia[v] - constante * saltos;
        }

        return distancia;
    }

    /// <summary>Em quantos vertices as duas tabelas discordam.</summary>
    public static int Diferentes(long[] certo, long[] dado)
    {
        ArgumentNullException.ThrowIfNull(certo);
        ArgumentNullException.ThrowIfNull(dado);

        var quantos = 0;
        for (var v = 0; v < certo.Length; v++) if (certo[v] != dado[v]) quantos++;

        return quantos;
    }

    /// <summary>
    /// O MENOR GRAFO em que o Dijkstra erra: quatro vertices e quatro arestas.
    ///
    ///   0 -> 1 com peso  2
    ///   0 -> 2 com peso  3
    ///   2 -> 1 com peso -2
    ///   1 -> 3 com peso  1
    ///
    /// O Dijkstra tira o 1 da fila com distancia 2, declara pronto e relaxa a
    /// aresta dele, dando 3 para o vertice 3. Depois ele chega no 2 e descobre que
    /// o 1 custava 1, nao 2. A tabela do 1 ate melhora, porque esta implementacao
    /// relaxa em quem ja saiu; o que nao acontece e o vertice 1 ser EXPANDIDO de
    /// novo, e o 3 fica com 3 quando o certo e 2.
    ///
    /// Com tres vertices o erro nao aparece: o vertice corrigido precisa ter
    /// aresta saindo, senao a correcao tardia chega a tempo. Eu tinha escrito o
    /// exemplo com tres, e ele passava.
    /// </summary>
    public static Grafo MenorErroDoDijkstra()
    {
        var grafo = new Grafo(4);

        grafo.Ligar(0, 1, 2);
        grafo.Ligar(0, 2, 3);
        grafo.Ligar(2, 1, -2);
        grafo.Ligar(1, 3, 1);

        return grafo;
    }

    /// <summary>
    /// O menor grafo em que SOMAR CONSTANTE erra, e ele nem precisa de peso
    /// negativo: ele so precisa de dois caminhos com numeros de arestas
    /// diferentes.
    ///
    ///   0 -> 1 -> 2 -> 3, cada um com peso 1   (tres arestas, peso 3)
    ///   0 -> 3 com peso 4                      (uma aresta, peso 4)
    ///
    /// O caminho curto em peso e o longo em saltos. Somando 10 em cada aresta, o
    /// primeiro vira 33 e o segundo vira 14, e o Dijkstra devolve o outro caminho.
    /// </summary>
    public static Grafo MenorErroDaConstante()
    {
        var grafo = new Grafo(4);

        grafo.Ligar(0, 1, 1);
        grafo.Ligar(1, 2, 1);
        grafo.Ligar(2, 3, 1);
        grafo.Ligar(0, 3, 4);

        return grafo;
    }
}
