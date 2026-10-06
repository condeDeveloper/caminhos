using Conde.Caminhos;
using Xunit;

namespace Conde.Caminhos.Testes;

public class DijkstraTestes
{
    /// <summary>
    /// Com peso nao negativo ele acerta, em quinhentos grafos, em todos os
    /// vertices. E isso que faz o erro com peso negativo ser invisivel: o
    /// algoritmo e certo no caso em que quase todo mundo o usa.
    /// </summary>
    [Fact]
    public void ComPesoNaoNegativoEleAcertaSempre()
    {
        for (ulong semente = 1; semente <= 500; semente++)
        {
            var grafo = Grafo.Sortear(8, 0.4, semente);

            Assert.Equal(ForcaBruta.De(grafo, 0), Dijkstra.De(grafo, 0).Distancia);
        }
    }

    /// <summary>
    /// O vetor de antecessores tem que descrever o MESMO caminho que a tabela
    /// diz.
    ///
    /// Separar as duas coisas pega o erro em que a distancia esta certa e o
    /// caminho aponta para outro lugar, que passa por qualquer teste que so olhe
    /// distancia.
    /// </summary>
    [Fact]
    public void OCaminhoRefeitoPesaOQueATabelaDiz()
    {
        for (ulong semente = 1; semente <= 100; semente++)
        {
            var grafo = Grafo.Sortear(20, 0.2, semente);
            var resultado = Dijkstra.De(grafo, 0);

            for (var v = 0; v < grafo.Vertices; v++)
            {
                if (!resultado.Alcanca(v)) continue;

                Assert.Equal(resultado.Distancia[v], resultado.PesoDoCaminho(grafo, v));
                Assert.Equal(0, resultado.Caminho(v)[0]);
                Assert.Equal(v, resultado.Caminho(v)[^1]);
            }
        }
    }

    /// <summary>
    /// A resposta do Dijkstra passa nas duas conferencias que caracterizam a
    /// resposta certa, em grafos grandes demais para a forca bruta.
    /// </summary>
    [Fact]
    public void ARespostaPassaNasDuasConferenciasEmGrafoGrande()
    {
        for (ulong semente = 1; semente <= 20; semente++)
        {
            var grafo = Grafo.Sortear(200, 0.05, semente);
            var distancia = Dijkstra.De(grafo, 0).Distancia;

            Assert.Null(ForcaBruta.ViolaTriangular(grafo, distancia));
            Assert.True(ForcaBruta.TodaDistanciaEhAtingivel(grafo, 0, distancia));
        }
    }

    /// <summary>
    /// O ACHADO: com peso negativo ele erra e NAO AVISA.
    ///
    /// Nao ha excecao, nao ha valor especial, nao ha nada na saida que diga que
    /// aquela tabela nao e a resposta. Ela so e maior.
    /// </summary>
    [Fact]
    public void ComPesoNegativoEleErraSemAvisar()
    {
        var grafo = Negativos.MenorErroDoDijkstra();
        var resultado = Dijkstra.De(grafo, 0);

        Assert.Equal(2, ForcaBruta.De(grafo, 0)[3]);
        Assert.Equal(3, resultado.Distancia[3]);

        // a saida tem cara de resposta: ha um caminho, e ele e um caminho de
        // verdade, com arestas que existem
        Assert.True(resultado.Alcanca(3));
        Assert.NotEmpty(resultado.Caminho(3));
    }

    /// <summary>
    /// E o erro NAO aparece em todo grafo com peso negativo: ele aparece em uns 7
    /// por cento. Isso e o pior cenario possivel, porque um teste pequeno passa.
    /// </summary>
    [Fact]
    public void OErroAconteceEmUmaMinoriaDosGrafosComNegativa()
    {
        var errou = 0;
        var total = 0;

        for (ulong semente = 1; semente <= 500; semente++)
        {
            var grafo = Grafo.SortearComNegativas(8, 0.4, semente);
            if (!grafo.TemNegativa) continue;

            total++;
            if (Negativos.Diferentes(ForcaBruta.De(grafo, 0), Dijkstra.De(grafo, 0).Distancia) > 0) errou++;
        }

        Assert.True(total > 400);
        Assert.InRange((double)errou / total, 0.01, 0.3);
    }

    /// <summary>
    /// A versao com POTENCIAL devolve exatamente a mesma resposta que o
    /// Bellman-Ford, em grafos com peso negativo. E a peca que faz o Johnson
    /// funcionar.
    /// </summary>
    [Fact]
    public void ComPotencialEleAcertaAteComPesoNegativo()
    {
        for (ulong semente = 1; semente <= 200; semente++)
        {
            var grafo = Grafo.SortearComNegativas(10, 0.4, semente);
            var potencial = BellmanFord.Potenciais(grafo);

            Assert.NotNull(potencial);

            for (var origem = 0; origem < grafo.Vertices; origem++)
                Assert.Equal(
                    BellmanFord.De(grafo, origem).Resultado.Distancia,
                    Dijkstra.ComPotencial(grafo, origem, potencial!).Distancia);
        }
    }

    /// <summary>
    /// E o peso AJUSTADO pelos potenciais e sempre nao negativo, que e o motivo de
    /// o Dijkstra voltar a valer.
    /// </summary>
    [Fact]
    public void OPesoAjustadoPelosPotenciaisNuncaEhNegativo()
    {
        for (ulong semente = 1; semente <= 100; semente++)
        {
            var grafo = Grafo.SortearComNegativas(12, 0.4, semente);
            var potencial = BellmanFord.Potenciais(grafo)!;

            foreach (var a in grafo.Todas)
                Assert.True(a.Peso + potencial[a.De] - potencial[a.Para] >= 0,
                    $"a aresta {a.De}->{a.Para} ficou negativa depois do ajuste");
        }
    }

    [Fact]
    public void OMonteDevolveSempreOMenor()
    {
        var sorteio = new Sorteio(5);
        var monte = new Monte(4);
        var postos = new List<long>();

        for (var i = 0; i < 2_000; i++)
        {
            var peso = sorteio.Entre(-1_000, 1_000);
            monte.Por(i, peso);
            postos.Add(peso);
        }

        postos.Sort();

        Assert.Equal(2_000, monte.Quantos);
        foreach (var esperado in postos) Assert.Equal(esperado, monte.Tirar().Peso);
        Assert.Equal(0, monte.Quantos);
    }

    [Fact]
    public void OMonteVazioRecusa()
    {
        Assert.Throws<InvalidOperationException>(() => new Monte(4).Tirar());
    }
}
