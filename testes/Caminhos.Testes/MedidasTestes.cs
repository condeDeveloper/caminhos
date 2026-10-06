using Conde.Caminhos;
using Xunit;

namespace Conde.Caminhos.Testes;

/// <summary>
/// Os testes das TABELAS que o medidor imprime.
///
/// Eles existem porque numero em README que ninguem confere envelhece errado em
/// silencio: alguem troca uma constante, a tabela muda e o texto continua dizendo
/// a frase antiga. Aqui o teste afirma sobre o mesmo objeto que vira linha
/// impressa.
/// </summary>
public class MedidasTestes
{
    /// <summary>
    /// A linha principal: o Bellman-Ford nunca erra e o Dijkstra erra em todos os
    /// tamanhos de grafo, com o juiz tendo olhado milhares de caminhos.
    /// </summary>
    [Fact]
    public void OJuizSeparaOsDoisAlgoritmos()
    {
        var linhas = Medidas.Juiz();

        Assert.Equal(4, linhas.Count);

        foreach (var l in linhas)
        {
            Assert.Equal(0, l.BellmanErrou);
            Assert.True(l.DijkstraErrou > 0, $"com {l.Vertices} vertices o Dijkstra nao errou nenhuma vez");
            Assert.True(l.Caminhos > l.Grafos, "o juiz olhou menos de um caminho por grafo");
        }

        // mais vertices, mais caminhos para o juiz olhar
        for (var i = 1; i < linhas.Count; i++)
            Assert.True(linhas[i].Caminhos > linhas[i - 1].Caminhos);
    }

    /// <summary>
    /// A frase do texto: somar constante erra muito mais que o Dijkstra direto.
    /// </summary>
    [Fact]
    public void SomarConstanteEhPiorQueNaoConsertarNada()
    {
        var linhas = Medidas.Negativas();

        Assert.Equal(2, linhas.Count);

        var dijkstra = linhas.Single(l => l.Jeito == "Dijkstra direto");
        var constante = linhas.Single(l => l.Jeito == "somar constante");

        Assert.True(dijkstra.Errou > 0);
        Assert.True(constante.Errou > 4 * dijkstra.Errou);
        Assert.True(constante.PiorErro >= dijkstra.PiorErro);
    }

    /// <summary>
    /// A frase do texto: o Johnson ganha no esparso e perde no denso, e o Floyd
    /// nao muda com a densidade.
    /// </summary>
    [Fact]
    public void OCruzamentoEstaEntreOEsparsoEODenso()
    {
        var linhas = Medidas.Densidades();

        Assert.Equal(6, linhas.Count);

        var primeiro = linhas[0];
        var ultimo = linhas[^1];

        Assert.True(primeiro.Johnson < primeiro.Floyd, "no grafo mais esparso o Johnson nao ganhou");
        Assert.True(ultimo.Johnson > ultimo.Floyd, "no grafo completo o Johnson nao perdeu");

        // o Floyd nao passa de n ao cubo, em nenhuma densidade
        foreach (var l in linhas) Assert.True(l.Floyd <= (long)l.Vertices * l.Vertices * l.Vertices);

        // e o numero de arestas cresce com a densidade pedida
        for (var i = 1; i < linhas.Count; i++) Assert.True(linhas[i].Arestas > linhas[i - 1].Arestas);
    }

    /// <summary>
    /// A frase do texto: o Bellman-Ford usa uma fracao das passadas que o livro
    /// manda.
    /// </summary>
    [Fact]
    public void OBellmanFordUsaUmaFracaoDasPassadasDoLivro()
    {
        var linhas = Medidas.Passadas();

        Assert.Equal(5, linhas.Count);

        foreach (var l in linhas)
        {
            Assert.True(l.PassadasUsadas >= 1);
            Assert.True(l.PassadasUsadas * 5 < l.PassadasDoLivro,
                $"com {l.Vertices} vertices gastou {l.PassadasUsadas} de {l.PassadasDoLivro}");
        }
    }

    /// <summary>
    /// A frase do texto: a heuristica certa da a mesma resposta gastando muito
    /// menos, e a exagerada gasta menos ainda e muda a resposta.
    /// </summary>
    [Fact]
    public void AsTresHeuristicasSeComportamComoOTextoDiz()
    {
        var linhas = Medidas.Estrelas();

        Assert.Equal(3, linhas.Count);

        var cega = linhas[0];
        var boa = linhas[1];
        var exagerada = linhas[2];

        Assert.Equal(cega.Distancia, boa.Distancia);
        Assert.True(boa.Expandidos < cega.Expandidos / 3);

        Assert.True(exagerada.Distancia > boa.Distancia);
        Assert.True(exagerada.Expandidos < boa.Expandidos);

        Assert.Equal(0, cega.Superestima);
        Assert.Equal(0, boa.Superestima);
        Assert.True(exagerada.Superestima > 100);

        Assert.Equal(0, cega.Inconsistente);
        Assert.Equal(0, boa.Inconsistente);
        Assert.True(exagerada.Inconsistente > 100);
    }

    /// <summary>
    /// As duas colunas lado a lado: quanto o exagero economiza e quanto ele erra.
    /// </summary>
    [Fact]
    public void OExageroEconomizaEErraAoMesmoTempo()
    {
        var linhas = Medidas.Exageros();

        Assert.Equal(5, linhas.Count);
        Assert.Equal(0, linhas[0].ErroPorCento, 6);

        for (var i = 1; i < linhas.Count; i++)
        {
            Assert.True(linhas[i].Expandidos <= linhas[i - 1].Expandidos);
            Assert.True(linhas[i].ErroPorCento >= linhas[i - 1].ErroPorCento);
        }

        Assert.True(linhas[^1].ErroPorCento > 10);
    }

    /// <summary>
    /// E quando a pergunta nao tem resposta: dois detectam, um devolve um numero.
    /// </summary>
    [Fact]
    public void ComCicloNegativoSoOsDoisHonestosAvisam()
    {
        var linhas = Medidas.Ciclos();

        Assert.Equal(3, linhas.Count);
        Assert.Contains("sem avisar", linhas.Single(l => l.Algoritmo == "Dijkstra").Resposta, StringComparison.Ordinal);
        Assert.Contains("detectou", linhas.Single(l => l.Algoritmo == "Bellman-Ford").Resposta, StringComparison.Ordinal);
        Assert.Contains("detectou", linhas.Single(l => l.Algoritmo == "Floyd-Warshall").Resposta, StringComparison.Ordinal);
    }
}
