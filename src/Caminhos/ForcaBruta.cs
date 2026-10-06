namespace Conde.Caminhos;

/// <summary>
/// O JUIZ deste repositorio: TODOS os caminhos simples, enumerados um a um.
///
/// Nao e um algoritmo melhor nem uma implementacao de referencia: e a definicao.
/// Caminho minimo e o menor peso entre todos os caminhos, e esta classe enumera
/// todos os caminhos.
///
/// Ela e exponencial e nao da para melhorar, e isso nao e defeito: ela so roda em
/// grafos de menos de dez vertices, que e exatamente o tamanho em que um
/// algoritmo errado ainda parece certo. Dijkstra com peso negativo erra em grafos
/// pequenos do mesmo jeito que erra em grafos grandes, e no pequeno da para
/// provar.
///
/// E o enumerador tem uma propriedade que nenhum algoritmo de caminho minimo tem:
/// ele funciona com peso negativo, com ciclo negativo e com grafo desconexo, sem
/// mudar uma linha. O ciclo negativo nao o atrapalha porque caminho SIMPLES nao
/// repete vertice.
/// </summary>
public static class ForcaBruta
{
    public const long Infinito = long.MaxValue / 4;

    /// <summary>
    /// A distancia minima de uma origem para todos, pela definicao.
    ///
    /// Note que com ciclo negativo isto NAO devolve menos infinito: ele devolve o
    /// melhor caminho SIMPLES, que e um numero bem definido. As duas perguntas sao
    /// diferentes e a diferenca so aparece quando ha ciclo negativo, que e por isso
    /// que as medidas com ciclo negativo sao tratadas em separado.
    /// </summary>
    public static long[] De(Grafo grafo, int origem)
    {
        ArgumentNullException.ThrowIfNull(grafo);
        if (grafo.Vertices > 12) throw new ArgumentException("a forca bruta so cabe em grafos minusculos", nameof(grafo));

        var melhor = new long[grafo.Vertices];
        Array.Fill(melhor, Infinito);
        melhor[origem] = 0;

        var noCaminho = new bool[grafo.Vertices];
        noCaminho[origem] = true;

        Andar(grafo, origem, 0, melhor, noCaminho);

        return melhor;
    }

    private static void Andar(Grafo grafo, int aqui, long acumulado, long[] melhor, bool[] noCaminho)
    {
        foreach (var aresta in grafo.Saindo(aqui))
        {
            if (noCaminho[aresta.Para]) continue;

            var soma = acumulado + aresta.Peso;
            if (soma < melhor[aresta.Para]) melhor[aresta.Para] = soma;

            noCaminho[aresta.Para] = true;
            Andar(grafo, aresta.Para, soma, melhor, noCaminho);
            noCaminho[aresta.Para] = false;
        }
    }

    /// <summary>
    /// Quantos caminhos simples existem de uma origem ate um destino.
    ///
    /// Este numero esta no README por um motivo: ele e o que torna o juiz
    /// honesto. Num grafo completo de dez vertices sao mais de cem mil caminhos
    /// entre dois vertices, e o juiz olhou todos.
    /// </summary>
    public static long QuantosCaminhos(Grafo grafo, int origem, int destino)
    {
        ArgumentNullException.ThrowIfNull(grafo);

        var noCaminho = new bool[grafo.Vertices];
        noCaminho[origem] = true;

        return Contar(grafo, origem, destino, noCaminho);
    }

    private static long Contar(Grafo grafo, int aqui, int destino, bool[] noCaminho)
    {
        if (aqui == destino) return 1;

        long quantos = 0;

        foreach (var aresta in grafo.Saindo(aqui))
        {
            if (noCaminho[aresta.Para]) continue;

            noCaminho[aresta.Para] = true;
            quantos += Contar(grafo, aresta.Para, destino, noCaminho);
            noCaminho[aresta.Para] = false;
        }

        return quantos;
    }

    /// <summary>
    /// A DESIGUALDADE TRIANGULAR, que e a propriedade que toda tabela de
    /// distancias correta tem que ter.
    ///
    /// Para toda aresta de u para v com peso w: d(v) menor ou igual a d(u) + w.
    /// Se isso vale em todas as arestas e d(origem) e zero, a tabela e otima.
    ///
    /// Ela e util porque e barata: conferir uma resposta custa o numero de
    /// arestas, enquanto calcular custa muito mais. Num grafo grande, em que o
    /// juiz exponencial nao cabe, ela e o juiz que sobra.
    /// </summary>
    public static (int De, int Para)? ViolaTriangular(Grafo grafo, long[] distancia)
    {
        ArgumentNullException.ThrowIfNull(grafo);
        ArgumentNullException.ThrowIfNull(distancia);

        foreach (var a in grafo.Todas)
        {
            if (distancia[a.De] >= Infinito) continue;
            if (distancia[a.Para] > distancia[a.De] + a.Peso) return (a.De, a.Para);
        }

        return null;
    }

    /// <summary>
    /// A outra metade: toda distancia finita precisa ser ATINGIVEL por algum
    /// caminho de verdade.
    ///
    /// Sem isso, uma tabela toda de zeros passaria na desigualdade triangular com
    /// pesos nao negativos. As duas conferencias juntas caracterizam a resposta
    /// certa.
    /// </summary>
    public static bool TodaDistanciaEhAtingivel(Grafo grafo, int origem, long[] distancia)
    {
        ArgumentNullException.ThrowIfNull(grafo);
        ArgumentNullException.ThrowIfNull(distancia);

        var alcancado = new long[grafo.Vertices];
        Array.Fill(alcancado, Infinito);
        alcancado[origem] = 0;

        // relaxamento ate parar de mudar, que alcanca qualquer tabela valida
        for (var passo = 0; passo < grafo.Vertices; passo++)
            foreach (var a in grafo.Todas)
                if (alcancado[a.De] < Infinito && alcancado[a.De] + a.Peso < alcancado[a.Para])
                    alcancado[a.Para] = alcancado[a.De] + a.Peso;

        for (var v = 0; v < grafo.Vertices; v++)
        {
            if (distancia[v] >= Infinito && alcancado[v] >= Infinito) continue;
            if (distancia[v] != alcancado[v]) return false;
        }

        return true;
    }
}
