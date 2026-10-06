namespace Conde.Caminhos;

/// <summary>
/// A HEURISTICA: o palpite de quanto falta para chegar.
///
/// Ela e o que separa a busca cega da busca informada, e ela tem duas
/// propriedades que todo mundo confunde:
///
///   ADMISSIVEL: nunca superestima o que falta. Isso garante a resposta OTIMA.
///   CONSISTENTE: h(u) nao passa de w(u,v) + h(v), para toda aresta. Isso garante
///                que nenhum vertice precisa ser reexpandido.
///
/// Consistente implica admissivel, e a volta e falsa. Uma heuristica admissivel e
/// inconsistente da a resposta certa gastando mais do que deveria, porque o
/// algoritmo descobre caminhos melhores para vertices que ja tinha fechado.
/// </summary>
public abstract record Heuristica(string Nome)
{
    public abstract long Falta(int vertice);

    public sealed override string ToString() => Nome;
}

/// <summary>
/// A busca A ESTRELA: Dijkstra com um palpite somado na prioridade.
///
/// A unica diferenca para o Dijkstra e qual numero entra na fila: em vez da
/// distancia ja percorrida, a distancia mais o palpite do que falta. Com palpite
/// zero, A estrela E Dijkstra, literalmente a mesma execucao.
///
/// E e por isso que ele ganha: ele gasta a fila nos vertices que parecem levar ao
/// destino, em vez de crescer um circulo em volta da origem.
/// </summary>
public static class Estrela
{
    public sealed record Saida(long Distancia, List<int> Caminho, long Expandidos, long Reexpandidos);

    public static Saida De(Grafo grafo, int origem, int destino, Heuristica heuristica)
    {
        ArgumentNullException.ThrowIfNull(grafo);
        ArgumentNullException.ThrowIfNull(heuristica);

        var distancia = new long[grafo.Vertices];
        var anterior = new int[grafo.Vertices];
        var fechado = new bool[grafo.Vertices];

        Array.Fill(distancia, ForcaBruta.Infinito);
        Array.Fill(anterior, -1);
        distancia[origem] = 0;

        var fila = new Monte(grafo.Vertices);
        fila.Por(origem, heuristica.Falta(origem));

        long expandidos = 0;
        long reexpandidos = 0;

        while (fila.Quantos > 0)
        {
            var (vertice, _) = fila.Tirar();

            if (fechado[vertice])
            {
                // so conta como reexpansao se a distancia melhorou de verdade
                // depois de o vertice ter sido fechado: e esse o preco da
                // heuristica inconsistente
                continue;
            }

            fechado[vertice] = true;
            expandidos++;

            if (vertice == destino) break;

            foreach (var aresta in grafo.Saindo(vertice))
            {
                var soma = distancia[vertice] + aresta.Peso;
                if (soma >= distancia[aresta.Para]) continue;

                if (fechado[aresta.Para])
                {
                    // o vertice ja tinha sido declarado pronto e nao estava:
                    // isso e exatamente o que a consistencia impede
                    fechado[aresta.Para] = false;
                    reexpandidos++;
                }

                distancia[aresta.Para] = soma;
                anterior[aresta.Para] = vertice;
                fila.Por(aresta.Para, soma + heuristica.Falta(aresta.Para));
            }
        }

        var caminho = new List<int>();
        if (distancia[destino] < ForcaBruta.Infinito)
        {
            for (var v = destino; v != -1; v = anterior[v]) caminho.Add(v);
            caminho.Reverse();
        }

        return new Saida(distancia[destino], caminho, expandidos, reexpandidos);
    }

    /// <summary>A heuristica que nao palpita nada: com ela, A estrela e Dijkstra.</summary>
    public static Heuristica Zero() => new SemPalpite();

    /// <summary>
    /// A distancia de quarteirao ate o destino numa grade, que e a heuristica
    /// ADMISSIVEL e CONSISTENTE do problema: na grade so da para andar em quatro
    /// direcoes, com custo um, entao nenhum caminho pode ser mais curto que isso.
    /// </summary>
    public static Heuristica Quarteirao(int lado, int destino) => new PorQuarteirao(lado, destino);

    /// <summary>
    /// A MESMA heuristica multiplicada por um fator maior que um.
    ///
    /// Ela deixa de ser admissivel e passa a chutar alto: o algoritmo acredita que
    /// falta mais do que falta, corre para o destino e DEIXA DE OLHAR caminhos que
    /// seriam melhores. Ela e muito mais rapida, e a resposta pode estar errada.
    ///
    /// Isso nao e um erro de quem escreveu: e a "busca ponderada", usada de
    /// proposito quando uma resposta boa agora vale mais que a otima depois. O que
    /// quase ninguem mede e quanto ela erra.
    /// </summary>
    public static Heuristica Exagerada(int lado, int destino, int fator) => new Multiplicada(lado, destino, fator);

    private sealed record SemPalpite() : Heuristica("zero (e Dijkstra)")
    {
        public override long Falta(int vertice) => 0;
    }

    private sealed record PorQuarteirao(int Lado, int Destino) : Heuristica("quarteirao")
    {
        public override long Falta(int vertice) =>
            Math.Abs(vertice / Lado - Destino / Lado) + Math.Abs(vertice % Lado - Destino % Lado);
    }

    private sealed record Multiplicada(int Lado, int Destino, int Fator) : Heuristica($"quarteirao vezes {Fator}")
    {
        public override long Falta(int vertice) =>
            Fator * (Math.Abs(vertice / Lado - Destino / Lado) + Math.Abs(vertice % Lado - Destino % Lado));
    }

    /// <summary>
    /// A heuristica e ADMISSIVEL: ela nunca estima mais do que a distancia real
    /// ate o destino.
    ///
    /// Conferido contra o Dijkstra rodado de tras para a frente, que da a distancia
    /// exata de cada vertice ate o destino.
    /// </summary>
    public static int VerticesEmQueSuperestima(Grafo grafo, int destino, Heuristica heuristica)
    {
        ArgumentNullException.ThrowIfNull(grafo);
        ArgumentNullException.ThrowIfNull(heuristica);

        var real = Dijkstra.De(grafo.Invertido(), destino).Distancia;
        var quantos = 0;

        for (var v = 0; v < grafo.Vertices; v++)
        {
            if (real[v] >= ForcaBruta.Infinito) continue;
            if (heuristica.Falta(v) > real[v]) quantos++;
        }

        return quantos;
    }

    /// <summary>
    /// A heuristica e CONSISTENTE: para toda aresta, h(u) nao passa de w + h(v).
    ///
    /// Esta e uma conferencia LOCAL, aresta por aresta, e nao precisa saber a
    /// resposta. E a diferenca entre as duas propriedades fica visivel aqui: a
    /// admissibilidade precisa da distancia real, a consistencia nao precisa de
    /// nada.
    /// </summary>
    public static int ArestasEmQueEhInconsistente(Grafo grafo, Heuristica heuristica)
    {
        ArgumentNullException.ThrowIfNull(grafo);
        ArgumentNullException.ThrowIfNull(heuristica);

        return grafo.Todas.Count(a => heuristica.Falta(a.De) > a.Peso + heuristica.Falta(a.Para));
    }
}
