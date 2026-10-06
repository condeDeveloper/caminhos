using Conde.Caminhos;
using Xunit;

namespace Conde.Caminhos.Testes;

/// <summary>
/// O juiz tambem precisa de juiz.
///
/// Todo numero deste repositorio e "o algoritmo concorda com a forca bruta". Se a
/// enumeracao tivesse um erro, todos os algoritmos pareceriam certos ou errados
/// pelo motivo errado. Por isso ela e conferida contra casos de resposta
/// conhecida, contra as duas propriedades que caracterizam a resposta certa e
/// contra o Bellman-Ford em grafos onde os dois valem.
/// </summary>
public class ForcaBrutaTestes
{
    [Fact]
    public void NoCaminhoDeUmaLinhaAResposaEhASoma()
    {
        var grafo = new Grafo(5);
        for (var i = 0; i < 4; i++) grafo.Ligar(i, i + 1, i + 1);

        var d = ForcaBruta.De(grafo, 0);

        Assert.Equal(0, d[0]);
        Assert.Equal(1, d[1]);
        Assert.Equal(3, d[2]);
        Assert.Equal(6, d[3]);
        Assert.Equal(10, d[4]);
    }

    [Fact]
    public void OQueNaoTemCaminhoFicaInfinito()
    {
        var grafo = new Grafo(3);
        grafo.Ligar(0, 1, 5);

        var d = ForcaBruta.De(grafo, 0);

        Assert.Equal(5, d[1]);
        Assert.Equal(ForcaBruta.Infinito, d[2]);
    }

    /// <summary>
    /// A forca bruta acha o caminho LONGO quando ele e o barato, que e o caso em
    /// que o atalho caro engana.
    /// </summary>
    [Fact]
    public void OCaminhoLongoGanhaQuandoEleEhOBarato()
    {
        var grafo = Negativos.MenorErroDaConstante();

        Assert.Equal(3, ForcaBruta.De(grafo, 0)[3]);
    }

    /// <summary>
    /// Com peso NEGATIVO ela continua certa, sem mudar uma linha. E o unico
    /// "algoritmo" daqui que nao precisa saber se ha peso negativo.
    /// </summary>
    [Fact]
    public void ComPesoNegativoElaContinuaCerta()
    {
        var grafo = Negativos.MenorErroDoDijkstra();

        Assert.Equal(1, ForcaBruta.De(grafo, 0)[1]);
        Assert.Equal(2, ForcaBruta.De(grafo, 0)[3]);
    }

    /// <summary>
    /// Em grafos SEM peso negativo, ela concorda com o Dijkstra e com o
    /// Bellman-Ford em todos os vertices, em quinhentos grafos.
    /// </summary>
    [Fact]
    public void ElaConcordaComOsDoisAlgoritmosEmGrafoSemNegativa()
    {
        for (ulong semente = 1; semente <= 500; semente++)
        {
            var grafo = Grafo.Sortear(7, 0.4, semente);
            var certo = ForcaBruta.De(grafo, 0);

            Assert.Equal(certo, Dijkstra.De(grafo, 0).Distancia);
            Assert.Equal(certo, BellmanFord.De(grafo, 0).Resultado.Distancia);
        }
    }

    /// <summary>
    /// E com peso negativo, ela concorda com o Bellman-Ford e NAO com o Dijkstra.
    /// </summary>
    [Fact]
    public void ComNegativaElaConcordaSoComOBellmanFord()
    {
        var dijkstraErrou = 0;

        for (ulong semente = 1; semente <= 300; semente++)
        {
            var grafo = Grafo.SortearComNegativas(8, 0.5, semente);
            var certo = ForcaBruta.De(grafo, 0);

            Assert.Equal(certo, BellmanFord.De(grafo, 0).Resultado.Distancia);
            if (!certo.SequenceEqual(Dijkstra.De(grafo, 0).Distancia)) dijkstraErrou++;
        }

        Assert.True(dijkstraErrou > 10, $"o Dijkstra so errou em {dijkstraErrou} de 300");
    }

    /// <summary>
    /// A contagem de caminhos: num grafo completo de n vertices, o numero de
    /// caminhos simples de um vertice a outro tem formula fechada, que e a soma
    /// das permutacoes dos vertices do meio.
    /// </summary>
    [Theory]
    [InlineData(3, 2)]
    [InlineData(4, 5)]
    [InlineData(5, 16)]
    [InlineData(6, 65)]
    [InlineData(7, 326)]
    public void AContagemDeCaminhosBateComAFormula(int vertices, long esperado)
    {
        var grafo = new Grafo(vertices);
        for (var i = 0; i < vertices; i++)
            for (var j = 0; j < vertices; j++)
                if (i != j) grafo.Ligar(i, j, 1);

        Assert.Equal(esperado, ForcaBruta.QuantosCaminhos(grafo, 0, vertices - 1));
    }

    /// <summary>
    /// A DESIGUALDADE TRIANGULAR: a resposta certa nunca a viola, e uma tabela com
    /// qualquer valor errado para mais viola.
    /// </summary>
    [Fact]
    public void ARespostaCertaNuncaViolaATriangular()
    {
        for (ulong semente = 1; semente <= 200; semente++)
        {
            var grafo = Grafo.Sortear(10, 0.4, semente);
            var certo = ForcaBruta.De(grafo, 0);

            Assert.Null(ForcaBruta.ViolaTriangular(grafo, certo));

            // estragar uma distancia para mais tem que ser pego
            var estragado = (long[])certo.Clone();
            var alvo = Enumerable.Range(1, 9).FirstOrDefault(v => certo[v] < ForcaBruta.Infinito, -1);
            if (alvo < 0) continue;

            estragado[alvo] += 1;
            var violacao = ForcaBruta.ViolaTriangular(grafo, estragado);
            var atingivel = ForcaBruta.TodaDistanciaEhAtingivel(grafo, 0, estragado);

            Assert.True(violacao is not null || !atingivel, "estragar a tabela passou nas duas conferencias");
        }
    }

    /// <summary>
    /// A triangular SOZINHA nao basta, e por isso as duas conferencias andam
    /// juntas: uma tabela toda de zeros passa nela com pesos nao negativos.
    /// </summary>
    [Fact]
    public void ATriangularSozinhaNaoBasta()
    {
        var grafo = Grafo.Sortear(10, 0.5, 2);
        var zeros = new long[10];

        Assert.Null(ForcaBruta.ViolaTriangular(grafo, zeros));
        Assert.False(ForcaBruta.TodaDistanciaEhAtingivel(grafo, 0, zeros));
    }

    [Fact]
    public void GrafoGrandeDemaisParaAForcaBrutaEhRecusado()
    {
        Assert.Throws<ArgumentException>(() => ForcaBruta.De(new Grafo(13), 0));
    }
}
