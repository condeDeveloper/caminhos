namespace Conde.Caminhos;

/// <summary>
/// BELLMAN-FORD: relaxar TODAS as arestas, n-1 vezes.
///
/// Ele e o algoritmo que ninguem escreve porque "e mais lento", e ele responde
/// duas perguntas que o Dijkstra nao responde:
///
///   1. caminho minimo com peso NEGATIVO;
///   2. existe ciclo negativo.
///
/// A ideia e uma linha: depois de k passadas, toda distancia que usa no maximo k
/// arestas ja esta certa. Um caminho simples tem no maximo n-1 arestas, entao n-1
/// passadas bastam. E se a n-esima passada ainda melhora alguma coisa, existe um
/// caminho com n arestas melhor que todos os de n-1, o que so e possivel com
/// ciclo negativo.
///
/// Essa segunda parte nao e um extra: detectar ciclo negativo e o que torna
/// possivel usar caminho minimo em arbitragem de moeda, em escalonamento com
/// restricao de diferenca e em qualquer problema em que "peso" pode ser ganho.
/// </summary>
public static class BellmanFord
{
    public sealed record Saida(Resultado Resultado, bool TemCicloNegativo, int PassadasUsadas);

    public static Saida De(Grafo grafo, int origem)
    {
        ArgumentNullException.ThrowIfNull(grafo);

        var distancia = new long[grafo.Vertices];
        var anterior = new int[grafo.Vertices];

        Array.Fill(distancia, ForcaBruta.Infinito);
        Array.Fill(anterior, -1);
        distancia[origem] = 0;

        long relaxamentos = 0;
        var passadas = 0;
        var mudou = true;

        // a saida antecipada nao e detalhe: num grafo em que as distancias ja
        // assentaram na terceira passada, as outras n-4 nao fazem nada
        for (var passo = 0; passo < grafo.Vertices - 1 && mudou; passo++)
        {
            mudou = false;
            passadas++;

            foreach (var a in grafo.Todas)
            {
                relaxamentos++;
                if (distancia[a.De] >= ForcaBruta.Infinito) continue;

                var soma = distancia[a.De] + a.Peso;
                if (soma >= distancia[a.Para]) continue;

                distancia[a.Para] = soma;
                anterior[a.Para] = a.De;
                mudou = true;
            }
        }

        // a passada extra: se ela ainda melhora, ha ciclo negativo
        var cicloNegativo = false;

        foreach (var a in grafo.Todas)
        {
            if (distancia[a.De] >= ForcaBruta.Infinito) continue;
            if (distancia[a.De] + a.Peso < distancia[a.Para]) { cicloNegativo = true; break; }
        }

        return new Saida(
            new Resultado(distancia, anterior, relaxamentos, 0),
            cicloNegativo,
            passadas);
    }

    /// <summary>
    /// Os POTENCIAIS para o algoritmo de Johnson: a distancia a partir de um
    /// vertice novo ligado a todos com peso zero.
    ///
    /// O vertice extra garante que todo vertice e alcancavel, e as distancias dele
    /// sao potenciais validos: para toda aresta, h(u) + w(u,v) nao e menor que
    /// h(v), que e exatamente a desigualdade triangular, que e exatamente o que faz
    /// o peso ajustado ser nao negativo.
    ///
    /// Devolve null se ha ciclo negativo, porque ai nao existe potencial nenhum.
    /// </summary>
    public static long[]? Potenciais(Grafo grafo)
    {
        ArgumentNullException.ThrowIfNull(grafo);

        var comExtra = new Grafo(grafo.Vertices + 1);
        foreach (var a in grafo.Todas) comExtra.Ligar(a.De, a.Para, a.Peso);
        for (var v = 0; v < grafo.Vertices; v++) comExtra.Ligar(grafo.Vertices, v, 0);

        var saida = De(comExtra, grafo.Vertices);
        if (saida.TemCicloNegativo) return null;

        return saida.Resultado.Distancia[..grafo.Vertices];
    }
}
