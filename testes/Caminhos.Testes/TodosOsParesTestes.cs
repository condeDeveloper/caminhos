using Conde.Caminhos;
using Xunit;

namespace Conde.Caminhos.Testes;

public class TodosOsParesTestes
{
    [Fact]
    public void OFloydConcordaComAForcaBrutaDeCadaOrigem()
    {
        for (ulong semente = 1; semente <= 200; semente++)
        {
            var grafo = Grafo.Sortear(8, 0.4, semente);
            var floyd = TodosOsPares.Floyd(grafo).Distancia;

            for (var origem = 0; origem < grafo.Vertices; origem++)
                Assert.Equal(ForcaBruta.De(grafo, origem), floyd[origem]);
        }
    }

    [Fact]
    public void OFloydEOJohnsonConcordamEntreSi()
    {
        for (ulong semente = 1; semente <= 100; semente++)
        {
            var grafo = Grafo.SortearComNegativas(10, 0.4, semente);

            var floyd = TodosOsPares.Floyd(grafo);
            var johnson = TodosOsPares.Johnson(grafo);

            Assert.False(floyd.TemCicloNegativo);
            Assert.False(johnson.TemCicloNegativo);
            Assert.True(TodosOsPares.Iguais(floyd.Distancia, johnson.Distancia),
                $"semente {semente}: {TodosOsPares.Discordancias(floyd.Distancia, johnson.Distancia)} posicoes");
        }
    }

    [Fact]
    public void OsTresConcordamEmGrafoSemPesoNegativo()
    {
        for (ulong semente = 1; semente <= 100; semente++)
        {
            var grafo = Grafo.Sortear(15, 0.3, semente);

            var floyd = TodosOsPares.Floyd(grafo).Distancia;
            var johnson = TodosOsPares.Johnson(grafo).Distancia;
            var dijkstras = TodosOsPares.DijkstraPorVertice(grafo).Distancia;

            Assert.True(TodosOsPares.Iguais(floyd, johnson));
            Assert.True(TodosOsPares.Iguais(floyd, dijkstras));
        }
    }

    /// <summary>
    /// O ACHADO QUE PEGA TODO MUNDO UMA VEZ: com o k no laco de DENTRO, o
    /// Floyd-Warshall devolve uma matriz inteira de numeros plausiveis e errados.
    ///
    /// Ele nao quebra, nao estoura, nao devolve negativo: ele devolve uma matriz
    /// em que algumas distancias sao maiores do que a verdadeira, porque so
    /// considerou caminhos com poucos vertices intermediarios.
    /// </summary>
    [Fact]
    public void OKNoLacoDeDentroDaUmaMatrizPlausivelEErrada()
    {
        var errou = 0;
        var total = 0;

        for (ulong semente = 1; semente <= 200; semente++)
        {
            var grafo = Grafo.Sortear(12, 0.15, semente);

            var certo = TodosOsPares.Floyd(grafo).Distancia;
            var errado = TodosOsPares.FloydComKPorDentro(grafo);

            total++;
            var discordancias = TodosOsPares.Discordancias(certo, errado);
            if (discordancias > 0) errou++;

            // o erro e sempre PARA MAIS: a versao errada nunca acha um caminho
            // que nao existe, ela so deixa de achar caminhos que existem
            for (var i = 0; i < grafo.Vertices; i++)
                for (var j = 0; j < grafo.Vertices; j++)
                    Assert.True(errado[i][j] >= certo[i][j]);
        }

        Assert.True((double)errou / total > 0.5, $"a versao errada so errou em {errou} de {total}");
    }

    /// <summary>
    /// E em grafo pequeno ela costuma ATE ACERTAR, que e o que faz o bug chegar em
    /// producao.
    /// </summary>
    [Fact]
    public void EmGrafoPequenoAVersaoErradaCostumaAcertar()
    {
        var acertou = 0;

        for (ulong semente = 1; semente <= 100; semente++)
        {
            var grafo = Grafo.Sortear(4, 0.5, semente);

            if (TodosOsPares.Iguais(TodosOsPares.Floyd(grafo).Distancia, TodosOsPares.FloydComKPorDentro(grafo)))
                acertou++;
        }

        Assert.True(acertou > 50, $"a versao errada so acertou em {acertou} de 100 grafos de 4 vertices");
    }

    /// <summary>
    /// O ACHADO DO CRUZAMENTO: o Johnson ganha no grafo esparso e PERDE no denso,
    /// e a virada esta por volta de metade das arestas possiveis.
    ///
    /// O Floyd gasta o mesmo no grafo vazio e no completo, porque ele nao olha
    /// quantas arestas existem.
    /// </summary>
    [Fact]
    public void OJohnsonGanhaNoEsparsoEPerdeNoDenso()
    {
        var esparso = Grafo.Sortear(60, 0.05, 7);
        var denso = Grafo.Sortear(60, 1.0, 7);

        Assert.True(TodosOsPares.Johnson(esparso).Operacoes < TodosOsPares.Floyd(esparso).Operacoes / 2);
        Assert.True(TodosOsPares.Johnson(denso).Operacoes > TodosOsPares.Floyd(denso).Operacoes);
    }

    /// <summary>
    /// E o custo do Floyd nao muda com a densidade: ele e n ao cubo, sempre.
    /// </summary>
    [Fact]
    public void OCustoDoFloydNaoDependeDasArestas()
    {
        var vazio = TodosOsPares.Floyd(Grafo.Sortear(40, 0.02, 7)).Operacoes;
        var cheio = TodosOsPares.Floyd(Grafo.Sortear(40, 1.0, 7)).Operacoes;

        Assert.True(cheio <= 40L * 40 * 40);
        Assert.True(vazio <= cheio);

        // e com o grafo completo ele gasta exatamente n ao cubo
        Assert.Equal(40L * 40 * 40, cheio);
    }

    [Fact]
    public void ComCicloNegativoOsDoisAvisam()
    {
        var grafo = Grafo.ComCicloNegativo();

        Assert.True(TodosOsPares.Floyd(grafo).TemCicloNegativo);
        Assert.True(TodosOsPares.Johnson(grafo).TemCicloNegativo);
    }

    [Fact]
    public void ADiagonalEhZeroSemCicloNegativo()
    {
        var grafo = Grafo.Sortear(20, 0.3, 4);
        var d = TodosOsPares.Floyd(grafo).Distancia;

        for (var i = 0; i < 20; i++) Assert.Equal(0, d[i][i]);
    }

    [Fact]
    public void MatrizesDeTamanhosDiferentesNaoSaoIguais()
    {
        Assert.False(TodosOsPares.Iguais([[1, 2]], [[1, 2], [3, 4]]));
        Assert.False(TodosOsPares.Iguais([[1, 2]], [[1, 2, 3]]));
    }
}
