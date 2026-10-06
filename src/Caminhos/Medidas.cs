namespace Conde.Caminhos;

/// <summary>
/// As MEDIDAS: as tabelas que o medidor imprime e que os testes conferem.
///
/// Elas ficam aqui, e nao no medidor, para o teste poder afirmar sobre o MESMO
/// numero que aparece no texto. Tabela de README que ninguem testa envelhece
/// errado em silencio.
///
/// Nenhuma delas usa relogio. Todas sao contagem de relaxamentos, de retiradas da
/// fila, de vertices expandidos ou de respostas erradas.
/// </summary>
public static class Medidas
{
    public sealed record LinhaJuiz(int Vertices, int Grafos, long Caminhos, int DijkstraErrou, int BellmanErrou);

    /// <summary>
    /// O JUIZ contra os dois algoritmos, em grafos com peso negativo.
    ///
    /// A coluna dos caminhos existe para o juiz nao ser uma palavra: ela diz
    /// quantos caminhos simples a forca bruta olhou para dar a resposta.
    /// </summary>
    public static List<LinhaJuiz> Juiz(int grafos = 300)
    {
        var linhas = new List<LinhaJuiz>();

        foreach (var vertices in new[] { 5, 6, 7, 8 })
        {
            long caminhos = 0;
            var dijkstra = 0;
            var bellman = 0;

            for (var i = 0; i < grafos; i++)
            {
                var grafo = Grafo.SortearComNegativas(vertices, 0.5, (ulong)(i + 1));
                var certo = ForcaBruta.De(grafo, 0);

                caminhos += ForcaBruta.QuantosCaminhos(grafo, 0, vertices - 1);

                if (Negativos.Diferentes(certo, Dijkstra.De(grafo, 0).Distancia) > 0) dijkstra++;

                var saida = BellmanFord.De(grafo, 0);
                if (!saida.TemCicloNegativo && Negativos.Diferentes(certo, saida.Resultado.Distancia) > 0) bellman++;
            }

            linhas.Add(new LinhaJuiz(vertices, grafos, caminhos, dijkstra, bellman));
        }

        return linhas;
    }

    public sealed record LinhaNegativa(string Jeito, int Grafos, int Errou, int PiorErro);

    /// <summary>As duas tentativas que nao funcionam, com o tamanho do estrago.</summary>
    public static List<LinhaNegativa> Negativas(int grafos = 500)
    {
        var c = Negativos.Medir(grafos: grafos);

        return
        [
            new LinhaNegativa("Dijkstra direto", c.Grafos, c.DijkstraErrou, c.PiorErro),
            new LinhaNegativa("somar constante", c.Grafos, c.SomarConstanteErrou, c.PiorErroDaConstante),
        ];
    }

    public sealed record LinhaDensidade(double Densidade, int Vertices, int Arestas, long Floyd, long Johnson, long NDijkstras);

    /// <summary>
    /// O CRUZAMENTO: Floyd-Warshall contra Johnson, conforme o grafo engorda.
    ///
    /// O Floyd nao olha quantas arestas existem: ele e n ao cubo no grafo vazio e
    /// no grafo completo. O Johnson depende de m, entao ele ganha no esparso e
    /// perde no denso.
    /// </summary>
    public static List<LinhaDensidade> Densidades(int vertices = 60)
    {
        var linhas = new List<LinhaDensidade>();

        foreach (var densidade in new[] { 0.02, 0.05, 0.1, 0.3, 0.6, 1.0 })
        {
            var grafo = Grafo.Sortear(vertices, densidade, 7);

            linhas.Add(new LinhaDensidade(
                densidade,
                vertices,
                grafo.Arestas,
                TodosOsPares.Floyd(grafo).Operacoes,
                TodosOsPares.Johnson(grafo).Operacoes,
                TodosOsPares.DijkstraPorVertice(grafo).Operacoes));
        }

        return linhas;
    }

    public sealed record LinhaEstrela(string Heuristica, long Distancia, long Expandidos, long Reexpandidos, int Superestima, int Inconsistente);

    /// <summary>
    /// A BUSCA COM PALPITE numa grade, com as tres heuristicas.
    ///
    /// A coluna da distancia e a que importa: as tres respondem, e nem todas
    /// respondem a mesma coisa.
    /// </summary>
    public static List<LinhaEstrela> Estrelas(int lado = 60, double paredes = 0.25, ulong semente = 11)
    {
        var grafo = Grafo.Grade(lado, paredes, semente, out _);
        var destino = lado * lado - 1;
        var linhas = new List<LinhaEstrela>();

        var heuristicas = new[]
        {
            Estrela.Zero(),
            Estrela.Quarteirao(lado, destino),
            Estrela.Exagerada(lado, destino, 3),
        };

        foreach (var heuristica in heuristicas)
        {
            var saida = Estrela.De(grafo, 0, destino, heuristica);

            linhas.Add(new LinhaEstrela(
                heuristica.Nome,
                saida.Distancia,
                saida.Expandidos,
                saida.Reexpandidos,
                Estrela.VerticesEmQueSuperestima(grafo, destino, heuristica),
                Estrela.ArestasEmQueEhInconsistente(grafo, heuristica)));
        }

        return linhas;
    }

    public sealed record LinhaExagero(int Fator, long Distancia, long Expandidos, double ErroPorCento);

    /// <summary>
    /// O PRECO DO EXAGERO: quanto a busca ponderada economiza e quanto ela erra.
    ///
    /// Esta e a tabela que quase nunca aparece junto: as duas colunas lado a lado.
    /// </summary>
    public static List<LinhaExagero> Exageros(int lado = 60, double paredes = 0.25, ulong semente = 11)
    {
        var grafo = Grafo.Grade(lado, paredes, semente, out _);
        var destino = lado * lado - 1;
        var otima = Estrela.De(grafo, 0, destino, Estrela.Quarteirao(lado, destino)).Distancia;

        var linhas = new List<LinhaExagero>();

        foreach (var fator in new[] { 1, 2, 3, 5, 10 })
        {
            var saida = Estrela.De(grafo, 0, destino, Estrela.Exagerada(lado, destino, fator));

            linhas.Add(new LinhaExagero(
                fator,
                saida.Distancia,
                saida.Expandidos,
                otima == 0 ? 0 : 100.0 * (saida.Distancia - otima) / otima));
        }

        return linhas;
    }

    public sealed record LinhaCiclo(string Algoritmo, string Resposta);

    /// <summary>
    /// O que cada algoritmo faz quando a pergunta NAO TEM RESPOSTA: um detecta o
    /// ciclo negativo, outro devolve um numero.
    /// </summary>
    public static List<LinhaCiclo> Ciclos()
    {
        var grafo = Grafo.ComCicloNegativo();
        var bellman = BellmanFord.De(grafo, 0);
        var floyd = TodosOsPares.Floyd(grafo);
        var dijkstra = Dijkstra.De(grafo, 0);

        return
        [
            new LinhaCiclo("Dijkstra", $"devolveu {dijkstra.Distancia[1]} para o vertice 1, sem avisar"),
            new LinhaCiclo("Bellman-Ford", bellman.TemCicloNegativo ? "detectou o ciclo negativo" : "nao detectou"),
            new LinhaCiclo("Floyd-Warshall", floyd.TemCicloNegativo ? "detectou o ciclo negativo" : "nao detectou"),
        ];
    }

    public sealed record LinhaPassadas(int Vertices, double Densidade, int PassadasUsadas, int PassadasDoLivro);

    /// <summary>
    /// Quantas passadas o Bellman-Ford REALMENTE precisa, contra as n-1 que o
    /// livro manda fazer.
    /// </summary>
    public static List<LinhaPassadas> Passadas()
    {
        var linhas = new List<LinhaPassadas>();

        foreach (var (vertices, densidade) in new[] { (50, 0.05), (50, 0.2), (50, 0.5), (200, 0.05), (200, 0.3) })
        {
            var grafo = Grafo.Sortear(vertices, densidade, 3);
            var saida = BellmanFord.De(grafo, 0);

            linhas.Add(new LinhaPassadas(vertices, densidade, saida.PassadasUsadas, vertices - 1));
        }

        return linhas;
    }
}
