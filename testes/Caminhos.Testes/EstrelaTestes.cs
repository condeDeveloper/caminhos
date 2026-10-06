using Conde.Caminhos;
using Xunit;

namespace Conde.Caminhos.Testes;

public class EstrelaTestes
{
    private const int Lado = 40;

    private static Grafo Grade(ulong semente = 11) => Grafo.Grade(Lado, 0.25, semente, out _);

    /// <summary>
    /// COM PALPITE ZERO, A ESTRELA E DIJKSTRA. Nao e parecido: e a mesma
    /// execucao, com os mesmos vertices expandidos.
    /// </summary>
    [Fact]
    public void ComPalpiteZeroAEstrelaEhDijkstra()
    {
        for (ulong semente = 1; semente <= 20; semente++)
        {
            var grafo = Grade(semente);
            var destino = Lado * Lado - 1;

            var estrela = Estrela.De(grafo, 0, destino, Estrela.Zero());
            var dijkstra = Dijkstra.De(grafo, 0);

            Assert.Equal(dijkstra.Distancia[destino], estrela.Distancia);
        }
    }

    /// <summary>
    /// A heuristica de QUARTEIRAO da a mesma resposta do Dijkstra gastando uma
    /// fracao dos vertices. E o unico ganho deste repositorio que nao custa nada:
    /// mesma resposta, menos trabalho.
    /// </summary>
    [Fact]
    public void OQuarteiraoDaAMesmaRespostaGastandoMuitoMenos()
    {
        for (ulong semente = 1; semente <= 20; semente++)
        {
            var grafo = Grade(semente);
            var destino = Lado * Lado - 1;

            var cega = Estrela.De(grafo, 0, destino, Estrela.Zero());
            var informada = Estrela.De(grafo, 0, destino, Estrela.Quarteirao(Lado, destino));

            if (cega.Distancia >= ForcaBruta.Infinito) continue;

            Assert.Equal(cega.Distancia, informada.Distancia);
            Assert.True(informada.Expandidos < cega.Expandidos, $"semente {semente}");
        }
    }

    /// <summary>
    /// A heuristica de quarteirao e ADMISSIVEL e CONSISTENTE na grade, e as duas
    /// coisas sao conferidas por caminhos diferentes: a admissibilidade contra a
    /// distancia real, a consistencia aresta por aresta.
    /// </summary>
    [Fact]
    public void OQuarteiraoEhAdmissivelEConsistente()
    {
        for (ulong semente = 1; semente <= 20; semente++)
        {
            var grafo = Grade(semente);
            var destino = Lado * Lado - 1;
            var heuristica = Estrela.Quarteirao(Lado, destino);

            Assert.Equal(0, Estrela.VerticesEmQueSuperestima(grafo, destino, heuristica));
            Assert.Equal(0, Estrela.ArestasEmQueEhInconsistente(grafo, heuristica));
        }
    }

    /// <summary>
    /// E a heuristica ZERO tambem e as duas coisas, trivialmente. Ela so nao ajuda
    /// em nada.
    /// </summary>
    [Fact]
    public void OPalpiteZeroEhAdmissivelEConsistenteETrivial()
    {
        var grafo = Grade();
        var destino = Lado * Lado - 1;

        Assert.Equal(0, Estrela.VerticesEmQueSuperestima(grafo, destino, Estrela.Zero()));
        Assert.Equal(0, Estrela.ArestasEmQueEhInconsistente(grafo, Estrela.Zero()));
    }

    /// <summary>
    /// O ACHADO: a heuristica EXAGERADA e muito mais rapida e devolve uma resposta
    /// errada, sem avisar que ela e errada.
    ///
    /// Isso nao e um bug: e a busca ponderada, usada de proposito quando uma
    /// resposta boa agora vale mais que a otima depois. O que quase ninguem mede e
    /// quanto ela erra.
    /// </summary>
    [Fact]
    public void AHeuristicaExageradaGanhaTempoEPerdeARespostaCerta()
    {
        var grafo = Grade();
        var destino = Lado * Lado - 1;

        var otima = Estrela.De(grafo, 0, destino, Estrela.Quarteirao(Lado, destino));
        var exagerada = Estrela.De(grafo, 0, destino, Estrela.Exagerada(Lado, destino, 3));

        Assert.True(exagerada.Expandidos < otima.Expandidos);
        Assert.True(exagerada.Distancia > otima.Distancia, "o exagero nao mudou a resposta nesta grade");

        // e ela nao avisa: o caminho existe e e um caminho de verdade
        Assert.NotEmpty(exagerada.Caminho);
        Assert.Equal(0, exagerada.Caminho[0]);
        Assert.Equal(destino, exagerada.Caminho[^1]);
    }

    /// <summary>
    /// E ela e inadmissivel E inconsistente, e as duas coisas sao medidas: ela
    /// superestima em milhares de vertices e viola a consistencia em milhares de
    /// arestas.
    /// </summary>
    [Fact]
    public void AExageradaSuperestimaEEhInconsistente()
    {
        var grafo = Grade();
        var destino = Lado * Lado - 1;
        var heuristica = Estrela.Exagerada(Lado, destino, 3);

        Assert.True(Estrela.VerticesEmQueSuperestima(grafo, destino, heuristica) > 100);
        Assert.True(Estrela.ArestasEmQueEhInconsistente(grafo, heuristica) > 100);
    }

    /// <summary>
    /// E a INCONSISTENCIA cobra o preco dela: vertices declarados prontos e
    /// corrigidos depois. Com heuristica consistente isso nunca acontece.
    /// </summary>
    [Fact]
    public void SoAInconsistenteReexpandeVertice()
    {
        var grafo = Grade();
        var destino = Lado * Lado - 1;

        Assert.Equal(0, Estrela.De(grafo, 0, destino, Estrela.Zero()).Reexpandidos);
        Assert.Equal(0, Estrela.De(grafo, 0, destino, Estrela.Quarteirao(Lado, destino)).Reexpandidos);

        var reexpansoes = 0L;
        for (ulong semente = 1; semente <= 20; semente++)
        {
            var outra = Grade(semente);
            reexpansoes += Estrela.De(outra, 0, destino, Estrela.Exagerada(Lado, destino, 3)).Reexpandidos;
        }

        Assert.True(reexpansoes > 0, "a heuristica inconsistente nunca reexpandiu nada");
    }

    /// <summary>
    /// Quanto maior o exagero, menos vertices e pior a resposta. As duas curvas
    /// andam juntas e em sentidos opostos.
    /// </summary>
    [Fact]
    public void QuantoMaiorOExageroPiorARespostaEMenosOTrabalho()
    {
        var grafo = Grade();
        var destino = Lado * Lado - 1;

        var otima = Estrela.De(grafo, 0, destino, Estrela.Quarteirao(Lado, destino));
        long expandidosAntes = otima.Expandidos;
        long distanciaAntes = otima.Distancia;

        foreach (var fator in new[] { 2, 3, 10 })
        {
            var saida = Estrela.De(grafo, 0, destino, Estrela.Exagerada(Lado, destino, fator));

            Assert.True(saida.Expandidos <= expandidosAntes, $"fator {fator} expandiu mais");
            Assert.True(saida.Distancia >= distanciaAntes, $"fator {fator} deu resposta melhor");
            Assert.True(saida.Distancia >= otima.Distancia, "nenhuma heuristica pode achar caminho menor que o otimo");

            expandidosAntes = saida.Expandidos;
            distanciaAntes = saida.Distancia;
        }
    }

    /// <summary>
    /// Nenhuma heuristica, nem a exagerada, devolve caminho MENOR que o otimo: o
    /// otimo e o otimo.
    /// </summary>
    [Fact]
    public void NenhumaHeuristicaAchaCaminhoMenorQueOOtimo()
    {
        for (ulong semente = 1; semente <= 20; semente++)
        {
            var grafo = Grade(semente);
            var destino = Lado * Lado - 1;
            var otima = Dijkstra.De(grafo, 0).Distancia[destino];

            foreach (var fator in new[] { 1, 2, 3, 5, 10 })
            {
                var saida = Estrela.De(grafo, 0, destino, Estrela.Exagerada(Lado, destino, fator));
                Assert.True(saida.Distancia >= otima, $"semente {semente}, fator {fator}");
            }
        }
    }

    /// <summary>
    /// E o caminho devolvido e um caminho de verdade: as arestas existem e o peso
    /// bate com a distancia.
    /// </summary>
    [Fact]
    public void OCaminhoDevolvidoEhUmCaminhoDeVerdade()
    {
        var grafo = Grade();
        var destino = Lado * Lado - 1;

        foreach (var heuristica in new[] { Estrela.Zero(), Estrela.Quarteirao(Lado, destino), Estrela.Exagerada(Lado, destino, 3) })
        {
            var saida = Estrela.De(grafo, 0, destino, heuristica);
            if (saida.Caminho.Count == 0) continue;

            long peso = 0;
            for (var i = 1; i < saida.Caminho.Count; i++)
            {
                var aresta = grafo.Saindo(saida.Caminho[i - 1]).Where(a => a.Para == saida.Caminho[i]).ToList();
                Assert.NotEmpty(aresta);
                peso += aresta.Min(a => a.Peso);
            }

            Assert.Equal(saida.Distancia, peso);
        }
    }
}
