using Conde.Caminhos;
using Xunit;

namespace Conde.Caminhos.Testes;

public class NegativosTestes
{
    /// <summary>
    /// O MENOR GRAFO em que o Dijkstra erra tem QUATRO vertices, e nao tres.
    ///
    /// Com tres o erro nao aparece, porque esta implementacao relaxa em quem ja
    /// saiu da fila: a distancia do vertice fechado ate melhora. O que nao
    /// acontece e ele ser EXPANDIDO de novo, entao o erro so vaza por uma aresta
    /// saindo dele. Eu tinha escrito o exemplo com tres vertices e ele passava.
    /// </summary>
    [Fact]
    public void OMenorErroDoDijkstraPrecisaDeQuatroVertices()
    {
        var comTres = new Grafo(3);
        comTres.Ligar(0, 1, 2);
        comTres.Ligar(0, 2, 3);
        comTres.Ligar(2, 1, -2);

        // com tres, o Dijkstra acerta
        Assert.Equal(ForcaBruta.De(comTres, 0), Dijkstra.De(comTres, 0).Distancia);

        // com o quarto vertice, ele erra
        var grafo = Negativos.MenorErroDoDijkstra();

        Assert.Equal(2, ForcaBruta.De(grafo, 0)[3]);
        Assert.Equal(3, Dijkstra.De(grafo, 0).Distancia[3]);
        Assert.Equal(2, BellmanFord.De(grafo, 0).Resultado.Distancia[3]);
        Assert.False(BellmanFord.De(grafo, 0).TemCicloNegativo);
    }

    /// <summary>
    /// O ACHADO QUE INVERTE A INTUICAO: somar uma constante para "consertar" o
    /// peso negativo erra SEIS VEZES MAIS que simplesmente usar o Dijkstra
    /// errado.
    ///
    /// A causa cabe numa frase: somar c em cada ARESTA soma c vezes o NUMERO DE
    /// ARESTAS do caminho, e caminhos diferentes tem numeros de arestas
    /// diferentes. O ajuste penaliza caminho longo em saltos e troca a resposta.
    /// </summary>
    [Fact]
    public void SomarConstanteErraMuitoMaisQueODijkstraDireto()
    {
        var c = Negativos.Medir(grafos: 500);

        Assert.True(c.DijkstraErrou > 0);
        Assert.True(c.SomarConstanteErrou > 4 * c.DijkstraErrou,
            $"Dijkstra errou {c.DijkstraErrou}, somar constante errou {c.SomarConstanteErrou}");
    }

    /// <summary>
    /// E somar constante erra ATE SEM PESO NEGATIVO: o problema nunca foi o sinal,
    /// foi o numero de saltos.
    /// </summary>
    [Fact]
    public void SomarConstanteErraAteSemPesoNegativo()
    {
        var grafo = Negativos.MenorErroDaConstante();

        Assert.False(grafo.TemNegativa);
        Assert.Equal(3, ForcaBruta.De(grafo, 0)[3]);
        Assert.Equal(3, Dijkstra.De(grafo, 0).Distancia[3]);

        // somando 10 em cada aresta, o caminho de tres saltos vira 33 e o de um
        // salto vira 14: o Dijkstra escolhe o outro
        Assert.Equal(4, Negativos.ComConstante(grafo, 0, 10)[3]);
        Assert.Equal(3, Negativos.ComConstante(grafo, 0, 0)[3]);
    }

    /// <summary>
    /// O conserto que FUNCIONA soma um valor por VERTICE em vez de um valor por
    /// aresta, e e exatamente isso que faz os termos do meio se cancelarem ao
    /// longo do caminho.
    /// </summary>
    [Fact]
    public void OConsertoQueFuncionaEhPorVertice()
    {
        for (ulong semente = 1; semente <= 200; semente++)
        {
            var grafo = Grafo.SortearComNegativas(8, 0.4, semente);
            var certo = ForcaBruta.De(grafo, 0);
            var potencial = BellmanFord.Potenciais(grafo)!;

            Assert.Equal(certo, Dijkstra.ComPotencial(grafo, 0, potencial).Distancia);
        }
    }

    [Fact]
    public void DiferentesContaOsVerticesQueDiscordam()
    {
        Assert.Equal(0, Negativos.Diferentes([1, 2, 3], [1, 2, 3]));
        Assert.Equal(2, Negativos.Diferentes([1, 2, 3], [1, 5, 9]));
    }

    /// <summary>
    /// O erro do Dijkstra e sempre PARA MAIS: ele nunca acha um caminho que nao
    /// existe, ele so deixa de achar um que existe.
    /// </summary>
    [Fact]
    public void OErroDoDijkstraEhSempreParaMais()
    {
        for (ulong semente = 1; semente <= 300; semente++)
        {
            var grafo = Grafo.SortearComNegativas(8, 0.4, semente);
            var certo = ForcaBruta.De(grafo, 0);
            var dijkstra = Dijkstra.De(grafo, 0).Distancia;

            for (var v = 0; v < grafo.Vertices; v++)
                Assert.True(dijkstra[v] >= certo[v], $"semente {semente}, vertice {v}");
        }
    }

    /// <summary>
    /// O ACHADO QUE EU NAO ESPERAVA: com peso negativo, a TABELA e o VETOR DE
    /// ANTECESSORES do Dijkstra param de concordar entre si.
    ///
    /// No grafo minimo, a tabela diz que o vertice 3 custa 3, e o caminho que o
    /// proprio Dijkstra devolve, refeito pelos antecessores, pesa 2. Os
    /// antecessores foram corrigidos quando o vertice 1 melhorou; a distancia do
    /// 3, que ja tinha sido calculada a partir do valor velho, nao foi.
    ///
    /// Isso e um sinal de graca: com peso nao negativo as duas coisas nunca
    /// discordam (o teste ao lado confere em cem grafos), entao a discordancia
    /// sozinha ja denuncia que a resposta esta errada. Quase ninguem confere,
    /// porque quase ninguem olha o vetor de antecessores.
    /// </summary>
    [Fact]
    public void ComNegativaATabelaEOsAntecessoresDiscordam()
    {
        var grafo = Negativos.MenorErroDoDijkstra();
        var resultado = Dijkstra.De(grafo, 0);

        Assert.Equal(3, resultado.Distancia[3]);
        Assert.Equal([0, 2, 1, 3], resultado.Caminho(3));
        Assert.Equal(2, resultado.PesoDoCaminho(grafo, 3));

        Assert.NotEqual(resultado.Distancia[3], resultado.PesoDoCaminho(grafo, 3));
    }

    /// <summary>
    /// E a discordancia e um detector BARATO e INCOMPLETO, nesta ordem.
    ///
    /// Em trezentos grafos sorteados com peso negativo, o Dijkstra devolveu uma
    /// tabela errada em 21 e a discordancia apareceu em 19. Ela custa o numero de
    /// arestas, contra o exponencial da forca bruta, e deixa passar alguns casos:
    /// quando a correcao tardia nao chega a mexer no vetor de antecessores do
    /// vertice afetado, as duas saidas continuam consistentes entre si e as duas
    /// continuam erradas.
    ///
    /// Eu tinha escrito este teste afirmando que a discordancia pegava TODOS os
    /// casos, e a medida disse 19 de 21.
    /// </summary>
    [Fact]
    public void ADiscordanciaEhUmDetectorBaratoEIncompleto()
    {
        var discordou = 0;
        var errou = 0;

        for (ulong semente = 1; semente <= 300; semente++)
        {
            var grafo = Grafo.SortearComNegativas(8, 0.4, semente);
            var resultado = Dijkstra.De(grafo, 0);

            var temDiscordancia = Enumerable.Range(0, grafo.Vertices)
                .Any(v => resultado.Alcanca(v) && resultado.Distancia[v] != resultado.PesoDoCaminho(grafo, v));

            if (temDiscordancia) discordou++;
            if (Negativos.Diferentes(ForcaBruta.De(grafo, 0), resultado.Distancia) > 0) errou++;
        }

        Assert.True(errou > 0);
        Assert.True(discordou > 0.8 * errou, $"discordou em {discordou}, errou em {errou}");
        Assert.True(discordou <= errou, "a discordancia apareceu onde a tabela estava certa");
    }
}
