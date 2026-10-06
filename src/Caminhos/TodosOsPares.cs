namespace Conde.Caminhos;

/// <summary>
/// FLOYD-WARSHALL e JOHNSON: a distancia entre TODOS os pares.
///
/// As duas respondem a mesma pergunta por caminhos completamente diferentes, e
/// qual delas ganha depende so da DENSIDADE do grafo:
///
///   Floyd-Warshall e tres lacos encaixados, sempre n ao cubo, sem olhar quantas
///   arestas existem. Ele e cinco linhas e nao tem estrutura de dados nenhuma.
///
///   Johnson reajusta os pesos com Bellman-Ford e roda um Dijkstra por vertice,
///   que custa n vezes (m + n log n). Num grafo esparso isso e muito menos; num
///   grafo denso e mais, porque m vira n ao quadrado e o log sobra.
///
/// O cruzamento das duas curvas e uma das medidas deste repositorio, e ele nao
/// esta onde a intuicao coloca.
/// </summary>
public static class TodosOsPares
{
    public sealed record Saida(long[][] Distancia, long Operacoes, bool TemCicloNegativo);

    /// <summary>
    /// FLOYD-WARSHALL: para cada vertice do meio k, ver se passar por ele encurta
    /// o caminho de i para j.
    ///
    /// A ordem dos lacos importa e quase todo mundo erra na primeira vez: o k
    /// precisa ser o laco de FORA. Com o k por dentro, o algoritmo so considera
    /// caminhos com um vertice intermediario, e devolve um numero plausivel e
    /// errado.
    /// </summary>
    public static Saida Floyd(Grafo grafo)
    {
        ArgumentNullException.ThrowIfNull(grafo);

        var n = grafo.Vertices;
        var d = new long[n][];

        for (var i = 0; i < n; i++)
        {
            d[i] = new long[n];
            Array.Fill(d[i], ForcaBruta.Infinito);
            d[i][i] = 0;
        }

        foreach (var a in grafo.Todas)
            if (a.Peso < d[a.De][a.Para]) d[a.De][a.Para] = a.Peso;

        long operacoes = 0;

        for (var k = 0; k < n; k++)
            for (var i = 0; i < n; i++)
            {
                if (d[i][k] >= ForcaBruta.Infinito) continue;

                for (var j = 0; j < n; j++)
                {
                    operacoes++;
                    if (d[k][j] >= ForcaBruta.Infinito) continue;

                    var soma = d[i][k] + d[k][j];
                    if (soma < d[i][j]) d[i][j] = soma;
                }
            }

        var ciclo = false;
        for (var i = 0; i < n; i++) if (d[i][i] < 0) ciclo = true;

        return new Saida(d, operacoes, ciclo);
    }

    /// <summary>
    /// A versao com o K NO LUGAR ERRADO, de proposito.
    ///
    /// Ela existe porque o erro e silencioso: ela devolve uma matriz inteira de
    /// numeros plausiveis, todos maiores ou iguais aos certos, e so uma comparacao
    /// com a resposta certa mostra. Num grafo pequeno ela costuma ate acertar, que
    /// e o que faz o bug chegar em producao.
    /// </summary>
    public static long[][] FloydComKPorDentro(Grafo grafo)
    {
        ArgumentNullException.ThrowIfNull(grafo);

        var n = grafo.Vertices;
        var d = new long[n][];

        for (var i = 0; i < n; i++)
        {
            d[i] = new long[n];
            Array.Fill(d[i], ForcaBruta.Infinito);
            d[i][i] = 0;
        }

        foreach (var a in grafo.Todas)
            if (a.Peso < d[a.De][a.Para]) d[a.De][a.Para] = a.Peso;

        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++)
                for (var k = 0; k < n; k++)
                {
                    if (d[i][k] >= ForcaBruta.Infinito || d[k][j] >= ForcaBruta.Infinito) continue;

                    var soma = d[i][k] + d[k][j];
                    if (soma < d[i][j]) d[i][j] = soma;
                }

        return d;
    }

    /// <summary>
    /// JOHNSON: reajustar os pesos para nao negativos e rodar um Dijkstra por
    /// vertice.
    ///
    /// O reajuste e a parte bonita: somar o potencial da origem e subtrair o do
    /// destino muda o peso de cada aresta e NAO muda qual caminho e o minimo,
    /// porque os potenciais do meio se cancelam ao longo do caminho. So sobram o
    /// da ponta e o do fim.
    /// </summary>
    public static Saida Johnson(Grafo grafo)
    {
        ArgumentNullException.ThrowIfNull(grafo);

        var potencial = BellmanFord.Potenciais(grafo);
        var n = grafo.Vertices;

        if (potencial is null)
        {
            var vazio = new long[n][];
            for (var i = 0; i < n; i++) vazio[i] = [];

            return new Saida(vazio, 0, true);
        }

        var d = new long[n][];
        long operacoes = (long)grafo.Vertices * grafo.Arestas;

        for (var origem = 0; origem < n; origem++)
        {
            var resultado = Dijkstra.ComPotencial(grafo, origem, potencial);
            d[origem] = resultado.Distancia;
            operacoes += resultado.Relaxamentos + resultado.Retiradas;
        }

        return new Saida(d, operacoes, false);
    }

    /// <summary>n Dijkstras diretos, que so vale com peso nao negativo.</summary>
    public static Saida DijkstraPorVertice(Grafo grafo)
    {
        ArgumentNullException.ThrowIfNull(grafo);

        var n = grafo.Vertices;
        var d = new long[n][];
        long operacoes = 0;

        for (var origem = 0; origem < n; origem++)
        {
            var resultado = Dijkstra.De(grafo, origem);
            d[origem] = resultado.Distancia;
            operacoes += resultado.Relaxamentos + resultado.Retiradas;
        }

        return new Saida(d, operacoes, false);
    }

    /// <summary>Duas matrizes de distancia sao iguais.</summary>
    public static bool Iguais(long[][] uma, long[][] outra)
    {
        ArgumentNullException.ThrowIfNull(uma);
        ArgumentNullException.ThrowIfNull(outra);

        if (uma.Length != outra.Length) return false;

        for (var i = 0; i < uma.Length; i++)
        {
            if (uma[i].Length != outra[i].Length) return false;
            for (var j = 0; j < uma[i].Length; j++) if (uma[i][j] != outra[i][j]) return false;
        }

        return true;
    }

    /// <summary>Em quantas posicoes duas matrizes discordam.</summary>
    public static int Discordancias(long[][] uma, long[][] outra)
    {
        var quantas = 0;

        for (var i = 0; i < uma.Length; i++)
            for (var j = 0; j < uma[i].Length; j++)
                if (uma[i][j] != outra[i][j]) quantas++;

        return quantas;
    }
}
