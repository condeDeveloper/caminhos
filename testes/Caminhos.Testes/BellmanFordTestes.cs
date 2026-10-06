using Conde.Caminhos;
using Xunit;

namespace Conde.Caminhos.Testes;

public class BellmanFordTestes
{
    /// <summary>
    /// Ele acerta com peso negativo, em quinhentos grafos, em todos os vertices.
    /// Esta e a diferenca inteira para o Dijkstra.
    /// </summary>
    [Fact]
    public void EleAcertaAteComPesoNegativo()
    {
        for (ulong semente = 1; semente <= 500; semente++)
        {
            var grafo = Grafo.SortearComNegativas(8, 0.4, semente);

            Assert.Equal(ForcaBruta.De(grafo, 0), BellmanFord.De(grafo, 0).Resultado.Distancia);
        }
    }

    [Fact]
    public void EleAcertaTambemSemPesoNegativo()
    {
        for (ulong semente = 1; semente <= 300; semente++)
        {
            var grafo = Grafo.Sortear(8, 0.4, semente);

            Assert.Equal(ForcaBruta.De(grafo, 0), BellmanFord.De(grafo, 0).Resultado.Distancia);
        }
    }

    /// <summary>
    /// O CICLO NEGATIVO e detectado, e esta e a pergunta que o Dijkstra nao
    /// responde de jeito nenhum.
    /// </summary>
    [Fact]
    public void OCicloNegativoEhDetectado()
    {
        var grafo = Grafo.ComCicloNegativo();

        Assert.True(BellmanFord.De(grafo, 0).TemCicloNegativo);

        // e o Dijkstra devolve um numero, sem avisar
        Assert.True(Dijkstra.De(grafo, 0).Alcanca(1));
    }

    [Fact]
    public void SemCicloNegativoEleNaoAcusa()
    {
        for (ulong semente = 1; semente <= 300; semente++)
        {
            Assert.False(BellmanFord.De(Grafo.SortearComNegativas(10, 0.4, semente), 0).TemCicloNegativo);
            Assert.False(BellmanFord.De(Grafo.Sortear(10, 0.4, semente), 0).TemCicloNegativo);
        }
    }

    /// <summary>
    /// O menor ciclo negativo que existe: duas arestas somando menos que zero.
    /// </summary>
    [Fact]
    public void OMenorCicloNegativoEhDeDuasArestas()
    {
        var grafo = new Grafo(2);
        grafo.Ligar(0, 1, 1);
        grafo.Ligar(1, 0, -2);

        Assert.True(BellmanFord.De(grafo, 0).TemCicloNegativo);

        // trocando o -2 por -1, a soma e zero e nao ha ciclo NEGATIVO
        var zero = new Grafo(2);
        zero.Ligar(0, 1, 1);
        zero.Ligar(1, 0, -1);

        Assert.False(BellmanFord.De(zero, 0).TemCicloNegativo);
    }

    /// <summary>
    /// O ACHADO: ele quase nunca precisa das n-1 passadas que o livro manda
    /// fazer.
    ///
    /// Com duzentos vertices, a conta do livro pede 199 passadas e as distancias
    /// param de mudar na setima. A saida antecipada nao e otimizacao esperta: e a
    /// diferenca entre vinte e oito vezes mais trabalho e o trabalho necessario.
    /// </summary>
    [Fact]
    public void EleQuaseNuncaPrecisaDasPassadasDoLivro()
    {
        foreach (var (vertices, densidade) in new[] { (50, 0.05), (50, 0.2), (200, 0.05), (200, 0.3) })
        {
            var grafo = Grafo.Sortear(vertices, densidade, 3);
            var usadas = BellmanFord.De(grafo, 0).PassadasUsadas;

            Assert.True(usadas < vertices / 5, $"com {vertices} vertices gastou {usadas} passadas");
            Assert.True(usadas >= 1);
        }
    }

    /// <summary>
    /// E a saida antecipada nao muda a resposta: ela so nao faz o trabalho que nao
    /// faria diferenca.
    /// </summary>
    [Fact]
    public void ASaidaAntecipadaNaoMudaARespostaa()
    {
        for (ulong semente = 1; semente <= 100; semente++)
        {
            var grafo = Grafo.SortearComNegativas(12, 0.3, semente);
            var saida = BellmanFord.De(grafo, 0);

            Assert.Equal(ForcaBruta.De(grafo, 0), saida.Resultado.Distancia);
            Assert.True(saida.PassadasUsadas <= grafo.Vertices - 1);
        }
    }

    /// <summary>
    /// Os POTENCIAIS obedecem a desigualdade triangular em toda aresta, que e
    /// exatamente o que faz o peso ajustado ser nao negativo.
    /// </summary>
    [Fact]
    public void OsPotenciaisObedecemATriangularEmTodaAresta()
    {
        for (ulong semente = 1; semente <= 100; semente++)
        {
            var grafo = Grafo.SortearComNegativas(12, 0.4, semente);
            var potencial = BellmanFord.Potenciais(grafo);

            Assert.NotNull(potencial);
            Assert.Equal(grafo.Vertices, potencial!.Length);

            foreach (var a in grafo.Todas)
                Assert.True(potencial[a.Para] <= potencial[a.De] + a.Peso);
        }
    }

    /// <summary>
    /// Com ciclo negativo nao existe potencial nenhum, e a funcao devolve nada em
    /// vez de um numero.
    /// </summary>
    [Fact]
    public void ComCicloNegativoNaoHaPotencial()
    {
        Assert.Null(BellmanFord.Potenciais(Grafo.ComCicloNegativo()));
    }

    /// <summary>
    /// Todo vertice alcanca pelo vertice extra, entao nenhum potencial fica
    /// infinito, nem num grafo desconexo.
    /// </summary>
    [Fact]
    public void OsPotenciaisExistemAteNoGrafoDesconexo()
    {
        var grafo = new Grafo(6);
        grafo.Ligar(0, 1, 5);
        grafo.Ligar(3, 4, -2);

        var potencial = BellmanFord.Potenciais(grafo);

        Assert.NotNull(potencial);
        Assert.All(potencial!, p => Assert.True(p < ForcaBruta.Infinito));
    }
}
